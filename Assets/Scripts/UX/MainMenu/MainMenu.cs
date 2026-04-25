using System;
using TMPro;
using UnityEngine;

namespace UX.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Selection")]
        [SerializeField] private GameObject[] menuOptions;
        [SerializeField] private int selectedIndex;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;

        public Color SelectedColor => selectedColor;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject coopPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Coop Sub Menu")]
        [SerializeField] private GameObject coopSubMenuRoot;
        [SerializeField] private GameObject[] coopSubMenuOptions;
        [SerializeField] private GameObject joinCodeRoot;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TMP_Text coopStatusText;
        [SerializeField] private UX.Net.RelayConnector relay;
        [SerializeField] private UX.CoopMenu.CoopConnectMenu coopConnectMenu;
        [SerializeField] private int maxClients = 3;

        [Header("UI Zoom")]
        [SerializeField] private RectTransform uiRoot;
        [SerializeField] private float defaultScale = 1f;
        [SerializeField] private float menuScale = 1.6667f;
        [SerializeField] private Vector2 defaultUiOffset = Vector2.zero;
        [SerializeField] private Vector2 menuUiOffset = Vector2.zero;
        [SerializeField] private float zoomSpeed = 5f;

        private float targetScale;
        private Vector2 targetUiOffset;

        private enum MenuState
        {
            Main,
            CoopSubMenu,
            EnteringCode,
            InLobby
        }

        private MenuState state = MenuState.Main;
        private int coopSubMenuIndex;
        private UX.CoopMenu.CoopLobbyTypeSelector lobbyTypeSelector;

        private void OnEnable()
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, menuOptions.Length - 1));

            targetScale = defaultScale;
            targetUiOffset = defaultUiOffset;
            ApplyUiZoomImmediate();

            ShowMainMenu();
            UpdateSelection();
        }

        private void Update()
        {
            UpdateUiZoom();

            switch (state)
            {
                case MenuState.Main:
                    if (mainMenuPanel != null && mainMenuPanel.activeSelf)
                        UpdateMainMenu();
                    break;
                case MenuState.CoopSubMenu:
                    if (coopPanel != null && coopPanel.activeSelf)
                        UpdateCoopSubMenu();
                    break;
                case MenuState.EnteringCode:
                    if (coopPanel != null && coopPanel.activeSelf)
                        UpdateEnteringCode();
                    break;
            }
        }

        private void UpdateMainMenu()
        {
            int prev = selectedIndex;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                selectedIndex = (selectedIndex - 1 + menuOptions.Length) % menuOptions.Length;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                selectedIndex = (selectedIndex + 1) % menuOptions.Length;

            if (prev != selectedIndex)
                UpdateSelection();

            if (Input.GetKeyDown(KeyCode.Return))
                ActivateSelected();
        }

        private void UpdateCoopSubMenu()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowMainMenu();
                return;
            }

            if (coopSubMenuOptions == null || coopSubMenuOptions.Length == 0) return;

            int prev = coopSubMenuIndex;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
                coopSubMenuIndex = (coopSubMenuIndex - 1 + coopSubMenuOptions.Length) % coopSubMenuOptions.Length;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                coopSubMenuIndex = (coopSubMenuIndex + 1) % coopSubMenuOptions.Length;

            if (prev != coopSubMenuIndex)
                UpdateCoopSubMenuSelection();

            if (Input.GetKeyDown(KeyCode.Return))
                ActivateCoopSubMenuSelected();
        }

        private void UpdateEnteringCode()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                state = MenuState.CoopSubMenu;
                if (coopConnectMenu != null) coopConnectMenu.ShowSelector();
                else HideJoinCodeInput();
                UpdateCoopSubMenuSelection();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
                JoinWithCode();
        }

        public void ShowMainMenu()
        {
            state = MenuState.Main;
            CloseCoopSubMenu();
            SetActivePanel(mainMenuPanel);
            UpdateSelection();
            SetUiZoomTarget(defaultScale, defaultUiOffset);
            if (Audio.MusicManager.Instance != null)
                Audio.MusicManager.Instance.PlayMainMenuMusic();
        }

        private void ShowCoop()
        {
            state = MenuState.CoopSubMenu;
            coopSubMenuIndex = 0;
            SetActivePanel(coopPanel);
            if (coopSubMenuRoot != null) coopSubMenuRoot.SetActive(true);
            if (coopConnectMenu != null) coopConnectMenu.ShowSelector();
            if (coopStatusText != null) coopStatusText.text = string.Empty;
            UpdateCoopSubMenuSelection();
            SetUiZoomTarget(menuScale, menuUiOffset);
        }

        private void CloseCoopSubMenu()
        {
            if (coopSubMenuRoot != null) coopSubMenuRoot.SetActive(false);
            HideJoinCodeInput();
            if (coopStatusText != null) coopStatusText.text = string.Empty;
        }

        private void ShowOptions()
        {
            SetActivePanel(optionsPanel);
            SetUiZoomTarget(menuScale, menuUiOffset);
        }

        private void ShowCredits()
        {
            SetActivePanel(creditsPanel);
            SetUiZoomTarget(menuScale, menuUiOffset);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ActivateSelected()
        {
            switch (selectedIndex)
            {
                case 0:
                    ShowCoop();
                    break;
                case 1:
                    ShowOptions();
                    break;
                case 2:
                    ShowCredits();
                    break;
                case 3:
                    Quit();
                    break;
            }
        }

        private void ActivateCoopSubMenuSelected()
        {
            switch (coopSubMenuIndex)
            {
                case 0:
                    ShowJoinCodeInput();
                    break;
                case 1:
                    HostLobby();
                    break;
            }
        }

        private async void HostLobby()
        {
            if (relay == null)
                relay = FindFirstObjectByType<UX.Net.RelayConnector>();

            if (relay == null) return;

            if (coopConnectMenu != null) coopConnectMenu.HideSelector();

            try
            {
                SetCoopStatus("Creating lobby...");
                await relay.HostAsync(maxClients);
                SetCoopStatus("Lobby created.");
                OpenLobbyPanel();
            }
            catch (Exception e)
            {
                SetCoopStatus(e.Message);
                if (coopConnectMenu != null) coopConnectMenu.ShowSelector();
            }
        }

        private async void JoinWithCode()
        {
            if (relay == null)
                relay = FindFirstObjectByType<UX.Net.RelayConnector>();

            if (relay == null) return;

            string code = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                SetCoopStatus("Enter a code.");
                return;
            }

            try
            {
                SetCoopStatus("Joining lobby...");
                await relay.JoinAsync(code);
                SetCoopStatus("Connected.");
                OpenLobbyPanel();
            }
            catch (Exception e)
            {
                SetCoopStatus(e.Message);
            }
        }

        private void OpenLobbyPanel()
        {
            CloseCoopSubMenu();
            SetActivePanel(coopPanel);
            state = MenuState.InLobby;

            if (coopConnectMenu != null)
                coopConnectMenu.ShowLobbyPanel();

            if (Audio.MusicManager.Instance != null)
                Audio.MusicManager.Instance.PlayLobbyMusic();
        }

        private void ShowJoinCodeInput()
        {
            state = MenuState.EnteringCode;
            if (coopConnectMenu != null)
                coopConnectMenu.ShowConnectPanel();
            else if (joinCodeRoot != null)
                joinCodeRoot.SetActive(true);

            if (joinCodeInput != null)
            {
                joinCodeInput.text = string.Empty;
                joinCodeInput.Select();
                joinCodeInput.ActivateInputField();
            }
        }

        private void HideJoinCodeInput()
        {
            if (joinCodeRoot != null) joinCodeRoot.SetActive(false);
        }

        private void SetCoopStatus(string message)
        {
            if (coopStatusText != null)
                coopStatusText.text = message;
        }

        private void UpdateUiZoom()
        {
            if (uiRoot == null) return;

            float dt = Time.unscaledDeltaTime * zoomSpeed;
            float scale = Mathf.Lerp(uiRoot.localScale.x, targetScale, dt);
            uiRoot.localScale = new Vector3(scale, scale, 1f);
            uiRoot.anchoredPosition = Vector2.Lerp(uiRoot.anchoredPosition, targetUiOffset, dt);
        }

        private void ApplyUiZoomImmediate()
        {
            if (uiRoot == null) return;

            uiRoot.localScale = new Vector3(targetScale, targetScale, 1f);
            uiRoot.anchoredPosition = targetUiOffset;
        }

        private void SetUiZoomTarget(float scale, Vector2 offset)
        {
            targetScale = scale;
            targetUiOffset = offset;
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < menuOptions.Length; i++)
            {
                TMP_Text text = menuOptions[i] ? menuOptions[i].GetComponent<TMP_Text>() : null;
                if (!text) continue;

                text.color = (i == selectedIndex) ? selectedColor : defaultColor;
            }
        }

        private void UpdateCoopSubMenuSelection()
        {
            for (int i = 0; i < coopSubMenuOptions.Length; i++)
            {
                TMP_Text text = coopSubMenuOptions[i] ? coopSubMenuOptions[i].GetComponent<TMP_Text>() : null;
                if (!text) continue;

                text.color = (i == coopSubMenuIndex) ? selectedColor : defaultColor;
            }

            if (lobbyTypeSelector == null && coopConnectMenu != null)
                lobbyTypeSelector = coopConnectMenu.GetComponentInChildren<UX.CoopMenu.CoopLobbyTypeSelector>(true);

            if (lobbyTypeSelector != null)
                lobbyTypeSelector.selectedIndex = coopSubMenuIndex;
        }

        private void SetActivePanel(GameObject panelToEnable)
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(panelToEnable == mainMenuPanel);
            if (coopPanel) coopPanel.SetActive(panelToEnable == coopPanel);
            if (optionsPanel) optionsPanel.SetActive(panelToEnable == optionsPanel);
            if (creditsPanel) creditsPanel.SetActive(panelToEnable == creditsPanel);
        }
    }
}
