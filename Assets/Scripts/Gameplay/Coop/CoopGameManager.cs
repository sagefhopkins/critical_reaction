using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Player;
using Gameplay.Save;
using Gameplay.Workstations;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Coop
{
    public class CoopGameManager : NetworkBehaviour
    {
        public static CoopGameManager Instance { get; private set; }

        public event Action OnTimerUpdated;
        public event Action OnDeliveryUpdated;
        public event Action<string> OnAlert;
        public event Action OnLevelFailed;
        public event Action OnLevelWon;
        public event Action<int, float> OnLevelResults;

        [Header("Level Settings")]
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private float levelTimeLimit = 300f;
        [SerializeField] private bool autoStartLevel = true;
        [SerializeField] private int autoStartLevelId = 0;

        [Header("Dropped Items")]
        [SerializeField] private GameObject droppedItemPrefab;

        public NetworkVariable<int> LevelId = new NetworkVariable<int>(-1);

        private NetworkVariable<float> elapsedTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> deliveredCount = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> targetCount = new NetworkVariable<int>(
            5,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<bool> levelActive = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<Unity.Collections.FixedString64Bytes> targetCompoundName = new NetworkVariable<Unity.Collections.FixedString64Bytes>(
            "Benzoic Acid",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkList<OrderData> Orders { get; private set; }

        private LevelConfig currentLevelConfig;
        private GameObject currentLayoutInstance;
        private List<NetworkObject> spawnedNetworkObjects = new List<NetworkObject>();
        private Dictionary<ulong, PlayerLevelStats> playerStats = new Dictionary<ulong, PlayerLevelStats>();
        private ulong[] syncedObjectIds;
        private int[] syncedLayoutIndices;

        public float ElapsedTime => elapsedTime.Value;
        public float RemainingTime => Mathf.Max(0f, levelTimeLimit - elapsedTime.Value);
        public float TimeLimit => levelTimeLimit;
        public int DeliveredCount => deliveredCount.Value;
        public int TargetCount => targetCount.Value;
        public string TargetCompoundName => targetCompoundName.Value.ToString();
        public bool IsLevelActive => levelActive.Value;
        public bool IsLevelComplete => deliveredCount.Value >= targetCount.Value;
        public bool IsTimeUp => elapsedTime.Value >= levelTimeLimit;
        public LevelConfig CurrentLevelConfig => currentLevelConfig;

        public void SpawnDroppedItemServer(ushort itemId, Vector3 worldPosition)
        {
            if (!IsServer) return;
            if (itemId == 0) return;
            if (droppedItemPrefab == null) return;

            GameObject go = Instantiate(droppedItemPrefab, worldPosition, Quaternion.identity);

            NetworkObject netObj = go.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Destroy(go);
                return;
            }

            netObj.Spawn(true);

            var dropped = go.GetComponent<Gameplay.Items.DroppedItem>();
            if (dropped != null)
                dropped.SetItemServer(itemId);
        }

        public int CalculateStars()
        {
            if (currentLevelConfig == null) return 0;
            if (!IsLevelComplete) return 0;

            LevelConfig.StarThreshold t = currentLevelConfig.StarThresholds;
            float remaining = RemainingTime;

            if (remaining >= t.threeStarTimeRemaining) return 3;
            if (remaining >= t.twoStarTimeRemaining) return 2;
            if (deliveredCount.Value >= t.oneStar) return 1;
            return 0;
        }

        public void RecordDeposit(ulong clientId)
        {
            EnsurePlayerStats(clientId).deposits++;
        }

        public void RecordCollection(ulong clientId)
        {
            EnsurePlayerStats(clientId).collections++;
        }

        public void RecordDelivery(ulong clientId)
        {
            EnsurePlayerStats(clientId).deliveries++;
        }

        public void RecordFailure(ulong clientId)
        {
            EnsurePlayerStats(clientId).failures++;
        }

        public PlayerLevelStats[] GetAllPlayerStats()
        {
            PlayerLevelStats[] result = new PlayerLevelStats[playerStats.Count];
            int i = 0;
            foreach (var kvp in playerStats)
            {
                result[i] = kvp.Value;
                i++;
            }
            return result;
        }

        private PlayerLevelStats EnsurePlayerStats(ulong clientId)
        {
            if (!playerStats.TryGetValue(clientId, out PlayerLevelStats stats))
            {
                stats = new PlayerLevelStats();
                playerStats[clientId] = stats;
            }
            return stats;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            Orders = new NetworkList<OrderData>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public override void OnNetworkSpawn()
        {
            elapsedTime.OnValueChanged += HandleTimerChanged;
            deliveredCount.OnValueChanged += HandleDeliveryChanged;

            if (IsServer && PauseManager.PendingNextLevelId >= 0)
            {
                int nextLevel = PauseManager.PendingNextLevelId;
                PauseManager.PendingNextLevelId = -1;
                LevelId.Value = nextLevel;
                Begin(nextLevel);
            }
            else if (LevelId.Value >= 0)
            {
                Begin(LevelId.Value);
            }
            else if (IsServer && autoStartLevel)
            {
                LevelId.Value = autoStartLevelId;
                Begin(autoStartLevelId);
            }
            else
            {
                LevelId.OnValueChanged += OnLevelChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            elapsedTime.OnValueChanged -= HandleTimerChanged;
            deliveredCount.OnValueChanged -= HandleDeliveryChanged;
            LevelId.OnValueChanged -= OnLevelChanged;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (!levelActive.Value) return;

            elapsedTime.Value += Time.deltaTime;

            if (IsTimeUp && levelActive.Value)
            {
                levelActive.Value = false;
                if (IsLevelComplete)
                {
                    int stars = CalculateStars();
                    float remaining = RemainingTime;

                    SaveManager.Instance.RecordLevelResult(
                        LevelId.Value,
                        stars,
                        remaining,
                        GetAllPlayerStats(),
                        true
                    );

                    BroadcastLevelWonClientRpc();
                    BroadcastLevelResultsClientRpc(stars, remaining);
                }
                else
                {
                    SaveManager.Instance.RecordLevelFailure(
                        LevelId.Value,
                        GetAllPlayerStats(),
                        true
                    );

                    BroadcastLevelFailedClientRpc();
                }
            }
        }

        private void HandleTimerChanged(float previous, float next)
        {
            OnTimerUpdated?.Invoke();
        }

        private void HandleDeliveryChanged(int previous, int next)
        {
            OnDeliveryUpdated?.Invoke();
        }

        public void SetLevelServer(int levelId)
        {
            if (!IsServer) return;
            LevelId.Value = levelId;
        }

        public void SetTargetServer(int target)
        {
            if (!IsServer) return;
            targetCount.Value = target;
        }

        public void SetTimeLimitServer(float timeLimit)
        {
            if (!IsServer) return;
            levelTimeLimit = timeLimit;
        }

        public void SetTargetCompoundServer(string compoundName)
        {
            if (!IsServer) return;
            targetCompoundName.Value = compoundName;
        }

        private void OnLevelChanged(int previous, int next)
        {
            if (next < 0) return;
            LevelId.OnValueChanged -= OnLevelChanged;
            Begin(next);
        }

        private void Begin(int levelId)
        {
            levelActive.Value = false;

            CacheLevelConfig(levelId);

            if (IsServer)
            {
                playerStats.Clear();
                ApplyLevelConfigServer();
                StartCoroutine(SpawnLayoutDeferred(
                    currentLevelConfig != null ? currentLevelConfig.LayoutPrefab : null));
                ResetAllPlayers();
                elapsedTime.Value = 0f;
                deliveredCount.Value = 0;
                PrepareForSceneReloadServer();
            }
            else
            {
                SpawnLayoutClient(currentLevelConfig != null ? currentLevelConfig.LayoutPrefab : null);
                StartCoroutine(WaitForServerObjectsThenApplySettings());
            }
        }

        private IEnumerator SpawnLayoutDeferred(GameObject layoutPrefab)
        {
            yield return null;
            SpawnLayout(layoutPrefab);
            ResetAllWorkstations();
            RefreshAllStorageRackVisuals();
            levelActive.Value = true;

            if (syncedObjectIds != null && syncedObjectIds.Length > 0)
                SyncLayoutSettingsClientRpc(syncedObjectIds, syncedLayoutIndices);
        }

        private void ResetAllWorkstations()
        {
            Workstation[] all = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].FullResetServer();
            }
        }

        private void RefreshAllStorageRackVisuals()
        {
            StorageRack[] racks = FindObjectsByType<StorageRack>(FindObjectsSortMode.None);
            for (int i = 0; i < racks.Length; i++)
            {
                if (racks[i] != null)
                    racks[i].RefreshVisuals();
            }
        }

        private void ResetAllPlayers()
        {
            PlayerCarry[] carries = FindObjectsByType<PlayerCarry>(FindObjectsSortMode.None);
            for (int i = 0; i < carries.Length; i++)
            {
                if (carries[i] != null)
                    carries[i].ClearHeldItemServer();
            }
        }

        public void PrepareForSceneReloadServer()
        {
            if (!IsServer) return;

            levelActive.Value = false;

            if (currentLayoutInstance != null)
            {
                DespawnAllNetworkObjects(currentLayoutInstance);
                Destroy(currentLayoutInstance);
                currentLayoutInstance = null;
            }

            ResetAllPlayers();
        }

        public void CleanupLayoutClient()
        {
            if (currentLayoutInstance != null)
            {
                Destroy(currentLayoutInstance);
                currentLayoutInstance = null;
            }
        }

        private void CacheLevelConfig(int levelId)
        {
            if (levelDatabase == null)
                return;

            if (!levelDatabase.TryGetLevel(levelId, out LevelConfig config))
                return;

            currentLevelConfig = config;
            levelTimeLimit = config.TimeLimit;
        }

        private void ApplyLevelConfigServer()
        {
            if (!IsServer || currentLevelConfig == null) return;

            Orders.Clear();
        }

        private void SpawnLayout(GameObject layoutPrefab)
        {
            if (currentLayoutInstance != null)
            {
                DespawnAllNetworkObjects(currentLayoutInstance);
                Destroy(currentLayoutInstance);
                currentLayoutInstance = null;
            }

            if (layoutPrefab == null)
                return;

            currentLayoutInstance = Instantiate(layoutPrefab);
            SpawnAllNetworkObjects(currentLayoutInstance);
        }

        private void SpawnLayoutClient(GameObject layoutPrefab)
        {
            if (currentLayoutInstance != null)
            {
                Destroy(currentLayoutInstance);
                currentLayoutInstance = null;
            }

            if (layoutPrefab == null) return;

            currentLayoutInstance = Instantiate(layoutPrefab);

            var networkObjects = currentLayoutInstance.GetComponentsInChildren<NetworkObject>(true);
            for (int i = networkObjects.Length - 1; i >= 0; i--)
            {
                if (networkObjects[i] != null)
                    Destroy(networkObjects[i].gameObject);
            }
        }

        private void SpawnAllNetworkObjects(GameObject root)
        {
            spawnedNetworkObjects.Clear();
            var mappings = new List<(ulong id, int index)>();

            var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
            var replacements = new List<(NetworkObject prefab, GameObject source, Vector3 pos, Quaternion rot, Vector3 scale, int layoutIndex)>();

            for (int i = 0; i < networkObjects.Length; i++)
            {
                var netObj = networkObjects[i];
                if (NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks
                        .ContainsKey(netObj.PrefabIdHash))
                {
                    netObj.Spawn(true);
                    spawnedNetworkObjects.Add(netObj);
                    mappings.Add((netObj.NetworkObjectId, i));
                    continue;
                }

                NetworkObject registeredPrefab = FindRegisteredPrefab(netObj);
                if (registeredPrefab == null)
                    continue;

                replacements.Add((
                    registeredPrefab,
                    netObj.gameObject,
                    netObj.transform.position,
                    netObj.transform.rotation,
                    netObj.transform.localScale,
                    i
                ));
            }

            foreach (var (prefab, source, pos, rot, scale, layoutIndex) in replacements)
            {
                GameObject instance = Instantiate(prefab.gameObject, pos, rot);
                instance.transform.localScale = scale;
                CopyComponentSettings(source, instance);
                Destroy(source);

                NetworkObject no = instance.GetComponent<NetworkObject>();
                if (no != null)
                {
                    no.Spawn(true);
                    spawnedNetworkObjects.Add(no);
                    mappings.Add((no.NetworkObjectId, layoutIndex));
                }
            }

            syncedObjectIds = new ulong[mappings.Count];
            syncedLayoutIndices = new int[mappings.Count];
            for (int i = 0; i < mappings.Count; i++)
            {
                syncedObjectIds[i] = mappings[i].id;
                syncedLayoutIndices[i] = mappings[i].index;
            }
        }

        private void CopyComponentSettings(GameObject source, GameObject destination)
        {
            var srcWs = source.GetComponent<Workstation>();
            var dstWs = destination.GetComponent<Workstation>();
            if (srcWs != null && dstWs != null)
                dstWs.InitializeFrom(srcWs);

            var srcRack = source.GetComponent<StorageRack>();
            var dstRack = destination.GetComponent<StorageRack>();
            if (srcRack != null && dstRack != null)
                dstRack.InitializeFrom(srcRack);

            var srcDp = source.GetComponent<DeliveryPoint>();
            var dstDp = destination.GetComponent<DeliveryPoint>();
            if (srcDp != null && dstDp != null)
                dstDp.InitializeFrom(srcDp);

            var srcHatch = source.GetComponent<Workstations.SupplyHatch.SupplyHatch>();
            var dstHatch = destination.GetComponent<Workstations.SupplyHatch.SupplyHatch>();
            if (srcHatch != null && dstHatch != null)
                dstHatch.InitializeFrom(srcHatch);

            var srcBelt = source.GetComponent<Workstations.Conveyor.ConveyorBelt>();
            var dstBelt = destination.GetComponent<Workstations.Conveyor.ConveyorBelt>();
            if (srcBelt != null && dstBelt != null)
                dstBelt.InitializeFrom(srcBelt);

            var srcEnd = source.GetComponent<Workstations.Conveyor.ConveyorEnd>();
            var dstEnd = destination.GetComponent<Workstations.Conveyor.ConveyorEnd>();
            if (srcEnd != null && dstEnd != null)
                dstEnd.InitializeFrom(srcEnd);

            var srcSpawner = source.GetComponent<Workstations.RecipeTray.TraySpawner>();
            var dstSpawner = destination.GetComponent<Workstations.RecipeTray.TraySpawner>();
            if (srcSpawner != null && dstSpawner != null)
                dstSpawner.InitializeFrom(srcSpawner);
        }

        private NetworkObject FindRegisteredPrefab(NetworkObject netObj)
        {
            string name = netObj.gameObject.name;
            NetworkObject bestMatch = null;
            int bestScore = 0;

            foreach (var entry in NetworkManager.NetworkConfig.Prefabs.Prefabs)
            {
                if (entry.Prefab == null) continue;
                var prefabNetObj = entry.Prefab.GetComponent<NetworkObject>();
                if (prefabNetObj == null) continue;

                string prefabName = entry.Prefab.name;
                int score = 0;

                if (prefabName == name)
                    score = 3;
                else if (prefabName.StartsWith(name))
                    score = 2;
                else if (name.StartsWith(prefabName))
                    score = 2;
                else if (prefabName.Contains(name) || name.Contains(prefabName))
                    score = 1;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = prefabNetObj;
                }
            }

            return bestMatch;
        }

        private IEnumerator WaitForServerObjectsThenApplySettings()
        {
            if (currentLevelConfig == null || currentLevelConfig.LayoutPrefab == null)
                yield break;

            yield return null;

            GameObject layoutPrefab = currentLevelConfig.LayoutPrefab;
            int expectedWorkstations = layoutPrefab.GetComponentsInChildren<Workstation>(true).Length;
            int expectedRacks = layoutPrefab.GetComponentsInChildren<StorageRack>(true).Length;
            int expectedDeliveryPoints = layoutPrefab.GetComponentsInChildren<DeliveryPoint>(true).Length;

            float timeout = 15f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                int foundWorkstations = FindObjectsByType<Workstation>(FindObjectsSortMode.None).Length;
                int foundRacks = FindObjectsByType<StorageRack>(FindObjectsSortMode.None).Length;
                int foundDeliveryPoints = FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None).Length;

                if (foundWorkstations >= expectedWorkstations &&
                    foundRacks >= expectedRacks &&
                    foundDeliveryPoints >= expectedDeliveryPoints)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            RequestLayoutSettingsServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestLayoutSettingsServerRpc(ServerRpcParams rpcParams = default)
        {
            if (syncedObjectIds == null || syncedObjectIds.Length == 0) return;
            SyncLayoutSettingsClientRpc(syncedObjectIds, syncedLayoutIndices);
        }

        [ClientRpc]
        private void SyncLayoutSettingsClientRpc(ulong[] networkObjectIds, int[] layoutChildIndices)
        {
            if (IsServer) return;
            ApplySettingsFromMapping(networkObjectIds, layoutChildIndices);
        }

        private void ApplySettingsFromMapping(ulong[] networkObjectIds, int[] layoutChildIndices)
        {
            if (currentLevelConfig == null || currentLevelConfig.LayoutPrefab == null) return;

            GameObject tempLayout = Instantiate(currentLevelConfig.LayoutPrefab);
            var layoutNetObjs = tempLayout.GetComponentsInChildren<NetworkObject>(true);

            for (int i = 0; i < networkObjectIds.Length; i++)
            {
                if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectIds[i], out NetworkObject netObj))
                    continue;

                int layoutIdx = layoutChildIndices[i];
                if (layoutIdx < 0 || layoutIdx >= layoutNetObjs.Length)
                    continue;

                CopyComponentSettings(layoutNetObjs[layoutIdx].gameObject, netObj.gameObject);

                var ws = netObj.GetComponent<Workstation>();
                if (ws != null) ws.RefreshVisuals();

                var rack = netObj.GetComponent<StorageRack>();
                if (rack != null) rack.RefreshVisuals();
            }

            Destroy(tempLayout);
        }

        private void DespawnAllNetworkObjects(GameObject root)
        {
            for (int i = spawnedNetworkObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedNetworkObjects[i] != null && spawnedNetworkObjects[i].IsSpawned)
                    spawnedNetworkObjects[i].Despawn(true);
            }
            spawnedNetworkObjects.Clear();
            var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
            foreach (var netObj in networkObjects)
            {
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn();
            }
        }

        public void RegisterDelivery(ushort productId, int amount = 1)
        {
            if (!NetworkManager.IsListening) return;
            RegisterDeliveryServerRpc(productId, amount);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RegisterDeliveryServerRpc(ushort productId, int amount, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (!levelActive.Value) return;

            RecordDelivery(rpcParams.Receive.SenderClientId);

            bool wasComplete = IsLevelComplete;
            deliveredCount.Value += amount;

            for (int i = 0; i < Orders.Count; i++)
            {
                OrderData order = Orders[i];
                if (order.RequiredProductId == productId && !order.IsComplete)
                {
                    order.DeliveredCount += amount;
                    Orders[i] = order;
                    break;
                }
            }

            if (!wasComplete && IsLevelComplete)
            {
                BroadcastAlertClientRpc("Target reached! Keep going!");
            }
        }

        public void ShowAlert(string message)
        {
            if (!NetworkManager.IsListening) return;
            ShowAlertServerRpc(message);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ShowAlertServerRpc(string message, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            BroadcastAlertClientRpc(message);
        }

        [ClientRpc]
        private void BroadcastAlertClientRpc(string message)
        {
            OnAlert?.Invoke(message);
        }

        public void FailLevelServer()
        {
            if (!IsServer) return;
            if (!levelActive.Value) return;

            levelActive.Value = false;
            BroadcastLevelFailedClientRpc();
        }

        [ClientRpc]
        private void BroadcastLevelFailedClientRpc()
        {
            OnLevelFailed?.Invoke();
        }

        [ClientRpc]
        private void BroadcastLevelWonClientRpc()
        {
            OnLevelWon?.Invoke();
        }

        [ClientRpc]
        private void BroadcastLevelResultsClientRpc(int stars, float timeRemaining)
        {
            OnLevelResults?.Invoke(stars, timeRemaining);
        }

        public void QuitLevelServer()
        {
            if (!IsServer) return;

            Debug.Log("Quitting mid-level → NO SAVE, resetting state");
            levelActive.Value = false;

            PrepareForSceneReloadServer();

            ForceReturnToMenuClientRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestQuitServerRpc()
        {
            QuitLevelServer();
        }

        [ClientRpc]
        private void ForceReturnToMenuClientRpc()
        {
            CleanupLayoutClient();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
