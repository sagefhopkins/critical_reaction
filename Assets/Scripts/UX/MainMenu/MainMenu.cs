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

        [Header("Camera Zoom")]
        [SerializeField] private Camera menuCamera;
        [SerializeField] private float defaultZoom = 5f;
        [SerializeField] private float coopZoom = 3f;
        [SerializeField] private Vector3 defaultCameraPosition = new Vector3(0f, 0f, -10f);
        [SerializeField] private Vector3 coopCameraPosition = new Vector3(0f, 0f, -10f);
        [SerializeField] private float zoomSpeed = 5f;

        private float targetZoom;
        private Vector3 targetCameraPosition;

        private enum MenuState
        {
            Main,
            CoopSubMenu,
            EnteringCode
        }

        private MenuState state = MenuState.Main;
        private int coopSubMenuIndex;

        private void OnEnable()
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, menuOptions.Length - 1));

            targetZoom = defaultZoom;
            targetCameraPosition = defaultCameraPosition;
            ApplyCameraImmediate();

            ShowMainMenu();
            UpdateSelection();
        }

        private void Update()
        {
            UpdateCameraZoom();

            if (!mainMenuPanel || !mainMenuPanel.activeSelf) return;

            switch (state)
            {
                case MenuState.Main:
                    UpdateMainMenu();
                    break;
                case MenuState.CoopSubMenu:
                    UpdateCoopSubMenu();
                    break;
                case MenuState.EnteringCode:
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
                CloseCoopSubMenu();
                return;
            }

            int prev = coopSubMenuIndex;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                coopSubMenuIndex = (coopSubMenuIndex - 1 + coopSubMenuOptions.Length) % coopSubMenuOptions.Length;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
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
                HideJoinCodeInput();
                state = MenuState.CoopSubMenu;
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
            SetCameraTarget(defaultZoom, defaultCameraPosition);
            if (Audio.MusicManager.Instance != null)
                Audio.MusicManager.Instance.PlayMainMenuMusic();
        }

        private void ShowCoop()
        {
            state = MenuState.CoopSubMenu;
            coopSubMenuIndex = 0;
            if (coopSubMenuRoot != null) coopSubMenuRoot.SetActive(true);
            if (coopStatusText != null) coopStatusText.text = string.Empty;
            HideJoinCodeInput();
            UpdateCoopSubMenuSelection();
            SetCameraTarget(coopZoom, coopCameraPosition);
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
        }

        private void ShowCredits()
        {
            SetActivePanel(creditsPanel);
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
                    HostLobby();
                    break;
                case 1:
                    ShowJoinCodeInput();
                    break;
                case 2:
                    CloseCoopSubMenu();
                    state = MenuState.Main;
                    break;
            }
        }

        private async void HostLobby()
        {
            if (relay == null)
                relay = FindFirstObjectByType<UX.Net.RelayConnector>();

            if (relay == null) return;

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

            if (coopConnectMenu != null)
                coopConnectMenu.ShowLobbyPanel();

            if (Audio.MusicManager.Instance != null)
                Audio.MusicManager.Instance.PlayLobbyMusic();
        }

        private void ShowJoinCodeInput()
        {
            state = MenuState.EnteringCode;
            if (joinCodeRoot != null) joinCodeRoot.SetActive(true);
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

        private void UpdateCameraZoom()
        {
            if (menuCamera == null) return;

            float dt = Time.unscaledDeltaTime * zoomSpeed;
            menuCamera.orthographicSize = Mathf.Lerp(menuCamera.orthographicSize, targetZoom, dt);
            menuCamera.transform.position = Vector3.Lerp(menuCamera.transform.position, targetCameraPosition, dt);
        }

        private void ApplyCameraImmediate()
        {
            if (menuCamera == null) return;

            menuCamera.orthographicSize = targetZoom;
            menuCamera.transform.position = targetCameraPosition;
        }

        private void SetCameraTarget(float zoom, Vector3 position)
        {
            targetZoom = zoom;
            targetCameraPosition = position;
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
