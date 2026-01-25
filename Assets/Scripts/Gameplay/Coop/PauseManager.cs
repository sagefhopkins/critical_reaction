using System;
using Unity.Netcode;
using UnityEngine;
using UX;

namespace Gameplay.Coop
{
    public class PauseManager : NetworkBehaviour
    {
        public static PauseManager Instance { get; private set; }
        public static int PendingNextLevelId { get; set; } = -1;

        public event Action<bool> OnPauseStateChanged;

        private NetworkVariable<bool> isPaused = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool IsPaused => isPaused.Value;

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
            isPaused.OnValueChanged += HandlePauseStateChanged;

            if (isPaused.Value)
                OnPauseStateChanged?.Invoke(true);
        }

        public override void OnNetworkDespawn()
        {
            isPaused.OnValueChanged -= HandlePauseStateChanged;
        }

        private void HandlePauseStateChanged(bool previous, bool next)
        {
            if (next && InteractionMenus.Instance != null)
            {
                InteractionMenus.Instance.CloseAll();
            }

            OnPauseStateChanged?.Invoke(next);
        }

        public void RequestPause()
        {
            if (!NetworkManager.IsListening) return;
            RequestPauseServerRpc();
        }

        public void RequestResume()
        {
            if (!NetworkManager.IsListening) return;
            RequestResumeServerRpc();
        }

        public void RequestRestartLevel()
        {
            if (!NetworkManager.IsListening) return;
            RequestRestartLevelServerRpc();
        }

        public void RequestQuit()
        {
            if (!NetworkManager.IsListening) return;
            RequestQuitServerRpc();
        }

        public void RequestNextLevel()
        {
            if (!NetworkManager.IsListening) return;
            RequestNextLevelServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestPauseServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            isPaused.Value = true;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestResumeServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            isPaused.Value = false;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestRestartLevelServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            isPaused.Value = false;

            if (NetworkManager.SceneManager != null)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                NetworkManager.SceneManager.LoadScene(currentScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestQuitServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            isPaused.Value = false;
            QuitToMenuClientRpc();
        }

        [ClientRpc]
        private void QuitToMenuClientRpc()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestNextLevelServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            isPaused.Value = false;

            int nextLevelId = 0;
            if (CoopGameManager.Instance != null)
            {
                nextLevelId = CoopGameManager.Instance.LevelId.Value + 1;
            }

            PendingNextLevelId = nextLevelId;
            SetPendingLevelClientRpc(nextLevelId);

            if (NetworkManager.SceneManager != null)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                NetworkManager.SceneManager.LoadScene(currentScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        [ClientRpc]
        private void SetPendingLevelClientRpc(int levelId)
        {
            PendingNextLevelId = levelId;
        }
    }
}
