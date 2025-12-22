using System;
using TMPro;
using UnityEngine;

namespace UX.CoopMenu
{
    public class CoopConnectMenu : MonoBehaviour
    {
        [SerializeField] private UX.MainMenu.MainMenu mainMenu;
        [SerializeField] private UX.Net.RelayConnector relay;
        [SerializeField] private GameObject connectPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject controlPanel;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TMP_Text hostCodeText;
        [SerializeField] private TMP_Text statusText;

        [SerializeField] private int maxClients = 3;

        private void OnEnable()
        {
            if (relay == null)
                relay = FindFirstObjectByType<UX.Net.RelayConnector>();

            if (connectPanel != null) connectPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);

            if (hostCodeText != null) hostCodeText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;
            if (joinCodeInput != null) joinCodeInput.text = string.Empty;
        }

        public async void HostLobby()
        {
            try
            {
                SetStatus("Creating lobby...");
                string code = await relay.HostAsync(maxClients);
                if (hostCodeText != null) hostCodeText.text = code;
                SetStatus("Lobby created.");
                ShowLobbyPanel();
            }
            catch (Exception e)
            {
                SetStatus(e.Message);
            }
        }

        public async void JoinLobby()
        {
            try
            {
                string code = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(code))
                {
                    SetStatus("Enter a code.");
                    return;
                }

                SetStatus("Joining lobby...");
                await relay.JoinAsync(code);
                SetStatus("Connected.");
                ShowLobbyPanel();
            }
            catch (Exception e)
            {
                SetStatus(e.Message);
            }
        }

        public void BackToMainMenu()
        {
            if (relay != null)
                relay.Shutdown();

            if (mainMenu != null)
                mainMenu.ShowMainMenu();

            gameObject.SetActive(false);
        }

        public void ShowLobbyPanel()
        {
            if (connectPanel != null) connectPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (controlPanel != null) controlPanel.SetActive(false);
        }

        public void ShowConnectPanel()
        {
            if (connectPanel != null) connectPanel.SetActive(true);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if(controlPanel != null) controlPanel.SetActive(false);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
