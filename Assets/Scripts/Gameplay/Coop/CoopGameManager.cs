using System;
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

        [Header("Level Settings")]
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private float levelTimeLimit = 300f;
        [SerializeField] private bool autoStartLevel = true;
        [SerializeField] private int autoStartLevelId = 0;

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

        private LevelConfig currentLevelConfig;
        private GameObject currentLayoutInstance;

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

            if (IsTimeUp)
            {
                levelActive.Value = false;
                if (IsLevelComplete)
                    BroadcastLevelWonClientRpc();
                else
                    BroadcastLevelFailedClientRpc();
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
            Debug.Log($"Level {levelId} starting");

            if (IsServer)
            {
                LoadLevelConfig(levelId);
                elapsedTime.Value = 0f;
                deliveredCount.Value = 0;
                levelActive.Value = true;
            }
        }

        private void LoadLevelConfig(int levelId)
        {
            if (levelDatabase == null)
            {
                Debug.LogWarning("No LevelDatabase assigned to CoopGameManager");
                return;
            }

            if (!levelDatabase.TryGetLevel(levelId, out LevelConfig config))
            {
                Debug.LogWarning($"Level {levelId} not found in database");
                return;
            }

            currentLevelConfig = config;
            levelTimeLimit = config.TimeLimit;
            targetCount.Value = config.TotalTargetCount;
            targetCompoundName.Value = config.PrimaryTargetName;

            SpawnLayout(config.LayoutPrefab);

            Debug.Log($"Loaded level config: {config.LevelName}, Time: {config.TimeLimit}s, Target: {config.PrimaryTargetName} x{config.TotalTargetCount}");
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
            {
                Debug.LogWarning("No layout prefab assigned for this level");
                return;
            }

            currentLayoutInstance = Instantiate(layoutPrefab);
            SpawnAllNetworkObjects(currentLayoutInstance);
        }

        private void SpawnAllNetworkObjects(GameObject root)
        {
            var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
            foreach (var netObj in networkObjects)
            {
                if (!netObj.IsSpawned)
                    netObj.Spawn();
            }
        }

        private void DespawnAllNetworkObjects(GameObject root)
        {
            var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
            foreach (var netObj in networkObjects)
            {
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn();
            }
        }

        public void RegisterDelivery(int amount = 1)
        {
            if (!NetworkManager.IsListening) return;
            RegisterDeliveryServerRpc(amount);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RegisterDeliveryServerRpc(int amount, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (!levelActive.Value) return;

            bool wasComplete = IsLevelComplete;
            deliveredCount.Value += amount;

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
    }
}
