using Gameplay.Coop;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CoopGameStarter : NetworkBehaviour
    {
        [SerializeField] private string gameplaySceneName = "CoopGame";
        [SerializeField] private LevelDatabase levelDatabase;
        private int pendingLevelId = -1;

        public override void OnNetworkSpawn()
        {
            if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }
        }

        public void RequestStart(int levelId)
        {
            if (NetworkManager == null || !NetworkManager.IsListening) return;

            if (Audio.MusicManager.Instance != null)
            {
                AudioClip levelMusicClip = null;
                if (levelDatabase != null && levelDatabase.TryGetLevel(levelId, out LevelConfig config))
                    levelMusicClip = config.Music;
                Audio.MusicManager.Instance.PlayGameStartTransition(levelMusicClip);
            }

            RequestStartServerRpc(levelId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestStartServerRpc(int levelId, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            pendingLevelId = levelId;

            if (NetworkManager.SceneManager == null) return;
            NetworkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
            System.Collections.Generic.List<ulong> clientsCompleted,
            System.Collections.Generic.List<ulong> clientsTimedOut)
        {
            if (!IsServer) return;
            if (!string.Equals(sceneName, gameplaySceneName)) return;

            CoopGameManager gm = FindFirstObjectByType<CoopGameManager>();
            if (gm != null)
            {
                gm.SetLevelServer(pendingLevelId);
            }
        }
    }
