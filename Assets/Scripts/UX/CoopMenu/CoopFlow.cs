using Unity.Netcode;
using UnityEngine;

public class CoopFlow : NetworkBehaviour
{
    public enum Screen : byte
    {
        Lobby = 0,
        Campaign = 1
    }

    [Header("UI Roots")]
    [SerializeField] private GameObject lobbyRoot;
    [SerializeField] private GameObject campaignRoot;

    private NetworkVariable<byte> screen = new NetworkVariable<byte>(
        (byte)Screen.Lobby,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        Apply((Screen)screen.Value);
        screen.OnValueChanged += OnScreenChanged;
    }

    public override void OnNetworkDespawn()
    {
        screen.OnValueChanged -= OnScreenChanged;
    }

    private void OnScreenChanged(byte previous, byte next)
    {
        Apply((Screen)next);
    }

    private void Apply(Screen s)
    {
        if (lobbyRoot != null) lobbyRoot.SetActive(s == Screen.Lobby);
        if (campaignRoot != null) campaignRoot.SetActive(s == Screen.Campaign);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetScreenServerRpc(byte nextScreen)
    {
        if (!IsServer) return;

        Screen s = (Screen)nextScreen;
        if (s != Screen.Lobby && s != Screen.Campaign)
            s = Screen.Lobby;

        screen.Value = (byte)s;
    }

    public void RequestShowLobby()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsListening) return;

        SetScreenServerRpc((byte)Screen.Lobby);
    }

    public void RequestShowCampaign()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsListening) return;

        SetScreenServerRpc((byte)Screen.Campaign);
    }
}
