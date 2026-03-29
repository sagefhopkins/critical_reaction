using Gameplay.Coop;
using Gameplay.Items;
using Gameplay.Workstations.Conveyor;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.RecipeTray
{
    public class TraySpawner : MonoBehaviour
    {
        [Header("Tray Prefab")]
        [SerializeField] private GameObject trayPrefab;

        [Header("Spawning")]
        [SerializeField] private float spawnInterval = 8f;
        [SerializeField] private int maxActiveTrays = 5;
        [SerializeField] private float initialDelay = 2f;
        [SerializeField] private float spawnZ = -1f;

        [Header("Conveyor")]
        [SerializeField] private ConveyorBelt targetConveyor;

        [Header("Level Config")]
        [SerializeField] private LevelDatabase levelDatabase;

        public void InitializeFrom(TraySpawner other)
        {
            trayPrefab = other.trayPrefab;
            spawnInterval = other.spawnInterval;
            maxActiveTrays = other.maxActiveTrays;
            initialDelay = other.initialDelay;
            targetConveyor = other.targetConveyor;
            levelDatabase = other.levelDatabase;
        }

        private float spawnTimer;
        private int activeCount;
        private LevelConfig currentConfig;
        private bool initialized;

        private void Start()
        {
            spawnTimer = initialDelay;
        }

        private bool IsServer
        {
            get
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                return nm != null && nm.IsServer;
            }
        }

        private bool TryInitialize()
        {
            if (initialized) return true;

            if (CoopGameManager.Instance == null) return false;
            if (levelDatabase == null) return false;

            int levelId = CoopGameManager.Instance.LevelId.Value;
            if (levelId < 0) return false;

            if (!levelDatabase.TryGetLevel(levelId, out currentConfig)) return false;
            if (currentConfig.AvailableProducts == null || currentConfig.AvailableProducts.Length == 0) return false;

            if (targetConveyor == null)
                targetConveyor = FindFirstObjectByType<ConveyorBelt>();

            initialized = true;
            return true;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (!TryInitialize()) return;
            if (targetConveyor == null) return;
            if (trayPrefab == null) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;

                if (activeCount < maxActiveTrays)
                    SpawnTray();
            }
        }

        private void SpawnTray()
        {
            LabItem[] pool = currentConfig.AvailableProducts;

            int count = Random.Range(
                currentConfig.MinItemsPerOrder,
                currentConfig.MaxItemsPerOrder + 1);

            ushort item1 = PickRandom(pool);
            ushort item2 = 0;

            if (count >= 2)
                item2 = PickRandom(pool);

            if (item1 == 0) return;

            Vector3 startPos = targetConveyor.GetWorldPosition(0f);
            startPos.z = spawnZ;
            GameObject go = Instantiate(trayPrefab, startPos, Quaternion.identity);

            NetworkObject netObj = go.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Destroy(go);
                return;
            }

            netObj.Spawn(true);

            RecipeTray tray = go.GetComponent<RecipeTray>();
            if (tray != null)
                tray.SetOrderServer(item1, item2, targetConveyor, pool);

            activeCount++;
        }

        private ushort PickRandom(LabItem[] pool)
        {
            if (pool == null || pool.Length == 0) return 0;

            int idx = Random.Range(0, pool.Length);
            if (pool[idx] != null)
                return pool[idx].Id;

            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null)
                    return pool[i].Id;
            }

            return 0;
        }

        public void NotifyTrayRemoved()
        {
            activeCount = Mathf.Max(0, activeCount - 1);
        }
    }
}
