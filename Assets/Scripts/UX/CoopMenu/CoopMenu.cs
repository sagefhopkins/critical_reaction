using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UX.CoopMenu
{
    public class CoopMenu : MonoBehaviour
    {
        [Header("Rows")]
        [SerializeField] private PlayerMenuOption[] rowOne;
        [SerializeField] private Button[] rowTwo;

        [Header("Selection")]
        [SerializeField] private Vector2Int selectedIndex;
        [SerializeField] private bool isSelectedMenu;

        [Header("Dependencies")]
        [SerializeField] private UX.MainMenu.MainMenu menuController;
        [SerializeField] private LobbyManager lobby;

        [Header("Flow")]
        [SerializeField] private CoopFlow flow;

        [Header("Controls")]
        [SerializeField] private KeyCode deselectKey = KeyCode.Escape;

        private static readonly Color32 RowTwoIdleColor = new Color32(120, 64, 24, 255);
        private static readonly Color RowTwoSelectedColor = Color.yellow;

        private const int RowOne = 0;
        private const int RowTwo = 1;

        private bool HasJoinedSlot => lobby != null && lobby.GetLocalClientSlotIndex() >= 0;

        private void OnEnable()
        {
            isSelectedMenu = true;

            if (lobby == null)
                lobby = FindFirstObjectByType<LobbyManager>();

            if (flow == null)
                flow = FindFirstObjectByType<CoopFlow>();

            SubscribeLobby();
            ClampIndexToCurrentRow();
            ApplySelectionVisuals();
            ApplyLobbyStateToUI();
        }

        private void OnDisable()
        {
            isSelectedMenu = false;
            UnsubscribeLobby();
        }

        private void SubscribeLobby()
        {
            if (lobby == null) return;
            lobby.Slots.OnListChanged += OnSlotsChanged;
        }

        private void UnsubscribeLobby()
        {
            if (lobby == null) return;
            lobby.Slots.OnListChanged -= OnSlotsChanged;
        }

        private void OnSlotsChanged(NetworkListEvent<LobbySlot> _)
        {
            ApplyLobbyStateToUI();
            ClampIndexToCurrentRow();
            ApplySelectionVisuals();
        }

        private void Update()
        {
            if (!isSelectedMenu) return;
            HandleInput();
        }

        private void HandleInput()
        {
            if (HasJoinedSlot && Input.GetKeyDown(deselectKey))
            {
                RequestLeaveSlot();
                return;
            }

            if (HasJoinedSlot)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveColumn(-1);
                if (Input.GetKeyDown(KeyCode.RightArrow)) MoveColumn(+1);

                if (Input.GetKeyDown(KeyCode.Return))
                    ActivateSelected();

                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
                ActivateSelected();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                MoveRow(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                MoveRow(+1);

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                MoveColumn(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                MoveColumn(+1);
        }

        private void ActivateSelected()
        {
            ClampIndexToCurrentRow();

            if (selectedIndex.x == RowOne)
            {
                RequestClaimSlot(selectedIndex.y);
                return;
            }

            switch (selectedIndex.y)
            {
                case 0:
                    ResetMenuState();
                    ShutdownSession();
                    if (menuController != null)
                        menuController.ShowMainMenu();
                    gameObject.SetActive(false);
                    break;

                case 1:
                    if (NetworkManager.Singleton == null) return;
                    if (!NetworkManager.Singleton.IsListening) return;

                    if (flow == null)
                        flow = FindFirstObjectByType<CoopFlow>();

                    if (flow == null) return;

                    if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                        flow.SetScreenServerRpc((byte)CoopFlow.Screen.Campaign);

                    break;
            }
        }

        private void RequestClaimSlot(int slotIndex)
        {
            if (lobby == null) return;
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

            lobby.RequestClaimSlotServerRpc(slotIndex);

            selectedIndex = new Vector2Int(RowTwo, 0);
            ClampIndexToCurrentRow();
            ApplySelectionVisuals();
        }

        private void RequestLeaveSlot()
        {
            if (lobby == null) return;
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

            lobby.RequestLeaveSlotServerRpc();

            selectedIndex = new Vector2Int(RowOne, Mathf.Clamp(selectedIndex.y, 0, Mathf.Max(0, rowOne.Length - 1)));
            ClampIndexToCurrentRow();
            ApplySelectionVisuals();
        }

        private void ShutdownSession()
        {
            if (NetworkManager.Singleton == null) return;
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
                NetworkManager.Singleton.Shutdown();
        }

        private void ResetMenuState()
        {
            selectedIndex = new Vector2Int(RowOne, 0);

            for (int i = 0; i < rowOne.Length; i++)
                rowOne[i].ResetState();

            for (int i = 0; i < rowTwo.Length; i++)
                rowTwo[i].image.color = RowTwoIdleColor;

            ClampIndexToCurrentRow();
            ApplySelectionVisuals();
        }

        private void MoveRow(int delta)
        {
            selectedIndex.x += delta;
            ClampIndexToCurrentRow();
            ApplySelectionVisuals();
        }

        private void MoveColumn(int delta)
        {
            selectedIndex.y += delta;
            ClampIndexToCurrentRow();
            ApplySelectionVisuals();
        }

        private void ClampIndexToCurrentRow()
        {
            if (HasJoinedSlot)
                selectedIndex.x = RowTwo;

            selectedIndex.x = Mathf.Clamp(selectedIndex.x, RowOne, RowTwo);

            int maxY = (selectedIndex.x == RowOne ? rowOne.Length : rowTwo.Length) - 1;
            if (maxY < 0)
            {
                selectedIndex.y = 0;
                return;
            }

            selectedIndex.y = Mathf.Clamp(selectedIndex.y, 0, maxY);
        }

        private void ApplyLobbyStateToUI()
        {
            if (lobby == null) return;
            if (lobby.Slots.Count == 0) return;

            int count = Mathf.Min(rowOne.Length, lobby.Slots.Count);

            for (int i = 0; i < count; i++)
                rowOne[i].SetSlotJoined(lobby.Slots[i].IsOccupied);
        }

        private void ApplySelectionVisuals()
        {
            ClampIndexToCurrentRow();

            for (int i = 0; i < rowOne.Length; i++)
            {
                bool selected = !HasJoinedSlot && selectedIndex.x == RowOne && i == selectedIndex.y;
                rowOne[i].SetSelected(selected);
            }

            for (int i = 0; i < rowTwo.Length; i++)
            {
                bool selected = selectedIndex.x == RowTwo && i == selectedIndex.y;
                rowTwo[i].image.color = selected ? RowTwoSelectedColor : RowTwoIdleColor;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampIndexToCurrentRow();

            if (!Application.isPlaying)
                ApplySelectionVisuals();
        }
#endif
    }
}
