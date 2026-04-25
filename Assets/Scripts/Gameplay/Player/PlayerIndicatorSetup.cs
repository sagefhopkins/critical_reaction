using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerIndicatorSetup : NetworkBehaviour
    {
        private static readonly Color[] PlayerColors =
        {
            Color.cyan,
            Color.magenta,
            Color.yellow,
            Color.green
        };

        private static byte nextColorIndex;

        [SerializeField] private PlayerNameTag nameTag;
        [SerializeField] private PlayerOutline outline;

        private NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private NetworkVariable<byte> colorIndex = new NetworkVariable<byte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public string PlayerName => playerName.Value.ToString();

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                string name = PlayerPrefs.GetString("PlayerName", $"Player {OwnerClientId + 1}");
                playerName.Value = new FixedString32Bytes(name);
            }

            if (IsServer)
            {
                colorIndex.Value = nextColorIndex;
                nextColorIndex = (byte)((nextColorIndex + 1) % PlayerColors.Length);
            }

            playerName.OnValueChanged += OnNameChanged;
            colorIndex.OnValueChanged += OnColorChanged;

            if (nameTag != null)
                nameTag.SetTarget(transform);

            ApplyName();
            ApplyColor();
        }

        public override void OnNetworkDespawn()
        {
            playerName.OnValueChanged -= OnNameChanged;
            colorIndex.OnValueChanged -= OnColorChanged;
        }

        private void OnNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
        {
            ApplyName();
        }

        private void OnColorChanged(byte previous, byte current)
        {
            ApplyColor();
        }

        private void ApplyName()
        {
            if (nameTag != null)
                nameTag.SetName(playerName.Value.ToString());
        }

        private void ApplyColor()
        {
            Color color = PlayerColors[colorIndex.Value % PlayerColors.Length];

            if (nameTag != null)
                nameTag.SetColor(color);

            if (outline != null)
                outline.SetColor(color);
        }
    }
}
