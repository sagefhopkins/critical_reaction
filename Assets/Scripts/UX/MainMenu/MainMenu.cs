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
        [SerializeField] private GameObject campaignPanel;
        [SerializeField] private GameObject coopPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;

        private void OnEnable()
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, menuOptions.Length - 1));
            ShowMainMenu();
            UpdateSelection();
        }

        private void Update()
        {
            if (!mainMenuPanel || !mainMenuPanel.activeSelf) return;

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

        public void ShowMainMenu()
        {
            SetActivePanel(mainMenuPanel);
            UpdateSelection();
        }

        private void ShowCampaign()
        {
            SetActivePanel(campaignPanel);
        }

        private void ShowCoop()
        {
            SetActivePanel(coopPanel);
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
                    ShowCampaign();
                    break;
                case 1:
                    ShowCoop();
                    break;
                case 2:
                    ShowOptions();
                    break;
                case 3:
                    ShowCredits();
                    break;
                case 4:
                    Quit();
                    break;
            }
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

        private void SetActivePanel(GameObject panelToEnable)
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(panelToEnable == mainMenuPanel);
            if (campaignPanel) campaignPanel.SetActive(panelToEnable == campaignPanel);
            if (coopPanel) coopPanel.SetActive(panelToEnable == coopPanel);
            if (optionsPanel) optionsPanel.SetActive(panelToEnable == optionsPanel);
            if (creditsPanel) creditsPanel.SetActive(panelToEnable == creditsPanel);
        }
    }
}
