using Unity.Netcode;
using UnityEngine;

public class CoopGameManager : NetworkBehaviour
{
    public NetworkVariable<int> LevelId = new NetworkVariable<int>(-1);

    public void SetLevelServer(int levelId)
    {
        if (!IsServer) return;
        LevelId.Value = levelId;
    }

    public override void OnNetworkSpawn()
    {
        if (LevelId.Value >= 0)
        {
            Begin(LevelId.Value);
        }
        else
        {
            LevelId.OnValueChanged += OnLevelChanged;
        }
    }

    private void OnLevelChanged(int previous, int next)
    {
        if (next < 0) return;
        LevelId.OnValueChanged -= OnLevelChanged;
        Begin(next);
    }

    private void Begin(int levelId)
    {
        Debug.Log("Begin");
    }
}
