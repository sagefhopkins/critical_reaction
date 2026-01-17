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

        [Header("Level Settings")]
        [SerializeField] private float levelTimeLimit = 300f;

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

        public float ElapsedTime => elapsedTime.Value;
        public float RemainingTime => Mathf.Max(0f, levelTimeLimit - elapsedTime.Value);
        public float TimeLimit => levelTimeLimit;
        public int DeliveredCount => deliveredCount.Value;
        public int TargetCount => targetCount.Value;
        public bool IsLevelActive => levelActive.Value;
        public bool IsLevelComplete => deliveredCount.Value >= targetCount.Value;
        public bool IsTimeUp => elapsedTime.Value >= levelTimeLimit;

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

            if (LevelId.Value >= 0)
            {
                Begin(LevelId.Value);
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

            if (IsTimeUp && !IsLevelComplete)
            {
                levelActive.Value = false;
                BroadcastAlertClientRpc("Time's up!");
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
                elapsedTime.Value = 0f;
                deliveredCount.Value = 0;
                levelActive.Value = true;
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

            deliveredCount.Value += amount;

            if (IsLevelComplete)
            {
                levelActive.Value = false;
                BroadcastAlertClientRpc("Level Complete!");
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
    }
}
