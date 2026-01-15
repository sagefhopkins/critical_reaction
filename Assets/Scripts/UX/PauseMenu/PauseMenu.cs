using Gameplay.Coop;
using TMPro;
using UnityEngine;
using UX.Options;

namespace UX.PauseMenu
{
    public class PauseMenu : MonoBehaviour
    {
        private enum MenuOption
        {
            Resume,
            Restart,
            EditControls,
            Quit
        }

        [Header("UI")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private TMP_Text[] optionTexts;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float blinkSpeed = 6f;

        [Header("Submenus")]
        [SerializeField] private GameObject controlsEditorRoot;
        [SerializeField] private RebindControlsEditor controlsEditor;

        private int selectedIndex;
        private bool isSubmenuOpen;

        private int OptionCount => optionTexts != null ? optionTexts.Length : 0;

        private float BlinkT
        {
            get
            {
                float s = Mathf.Max(0.01f, blinkSpeed);
                return 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * s);
            }
        }

        private void Awake()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.OnPauseStateChanged += HandlePauseStateChanged;
        }

        private void OnDisable()
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.OnPauseStateChanged -= HandlePauseStateChanged;
        }

        private void HandlePauseStateChanged(bool paused)
        {
            if (paused)
                OpenMenu();
            else
                CloseMenu();
        }

        private void Update()
        {
            if (menuRoot == null || !menuRoot.activeSelf)
            {
                CheckForPauseInput();
                return;
            }

            if (isSubmenuOpen)
                return;

            UpdateHighlight();
            HandleInput();
        }

        private void CheckForPauseInput()
        {
            bool pausePressed = InputSettings.Instance != null
                ? InputSettings.Instance.IsCancelPressed()
                : Input.GetKeyDown(KeyCode.Escape);

            if (pausePressed && PauseManager.Instance != null)
            {
                PauseManager.Instance.RequestPause();
            }
        }

        private void HandleInput()
        {
            bool cancel = InputSettings.Instance != null
                ? InputSettings.Instance.IsCancelPressed()
                : Input.GetKeyDown(KeyCode.Escape);

            if (cancel)
            {
                SelectResume();
                return;
            }

            int prev = selectedIndex;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                selectedIndex = (selectedIndex - 1 + OptionCount) % OptionCount;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                selectedIndex = (selectedIndex + 1) % OptionCount;

            if (prev != selectedIndex)
                UpdateHighlight();

            bool apply = InputSettings.Instance != null
                ? InputSettings.Instance.IsApplyPressed()
                : Input.GetKeyDown(KeyCode.Return);

            if (apply)
                ActivateSelected();
        }

        private void ActivateSelected()
        {
            MenuOption option = (MenuOption)Mathf.Clamp(selectedIndex, 0, OptionCount - 1);

            switch (option)
            {
                case MenuOption.Resume:
                    SelectResume();
                    break;

                case MenuOption.Restart:
                    SelectRestart();
                    break;

                case MenuOption.EditControls:
                    SelectEditControls();
                    break;

                case MenuOption.Quit:
                    SelectQuit();
                    break;
            }
        }

        private void SelectResume()
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.RequestResume();
        }

        private void SelectRestart()
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.RequestRestartLevel();
        }

        private void SelectEditControls()
        {
            if (controlsEditor == null || controlsEditorRoot == null) return;

            isSubmenuOpen = true;
            Controls current = InputSettings.Instance != null
                ? InputSettings.Instance.Controls
                : LoadControlsFromPrefs();

            controlsEditor.SetPauseMenu(this);
            controlsEditor.Open(current);
        }

        private static Controls LoadControlsFromPrefs()
        {
            return new Controls
            {
                Forward = (KeyCode)PlayerPrefs.GetInt("Control_Forward", (int)KeyCode.W),
                Back = (KeyCode)PlayerPrefs.GetInt("Control_Back", (int)KeyCode.S),
                Left = (KeyCode)PlayerPrefs.GetInt("Control_Left", (int)KeyCode.A),
                Right = (KeyCode)PlayerPrefs.GetInt("Control_Right", (int)KeyCode.D),
                Apply = (KeyCode)PlayerPrefs.GetInt("Control_Apply", (int)KeyCode.Return),
                Cancel = (KeyCode)PlayerPrefs.GetInt("Control_Cancel", (int)KeyCode.Escape),
                Interact = (KeyCode)PlayerPrefs.GetInt("Control_Interact", (int)KeyCode.E)
            };
        }

        private void SelectQuit()
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.RequestQuit();
        }

        private void OpenMenu()
        {
            if (menuRoot != null)
                menuRoot.SetActive(true);

            selectedIndex = 0;
            isSubmenuOpen = false;
            RefreshAllLines();
            UpdateHighlight();
        }

        private void CloseMenu()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false);

            if (controlsEditorRoot != null)
                controlsEditorRoot.SetActive(false);

            isSubmenuOpen = false;
        }

        public void ReturnFocus()
        {
            isSubmenuOpen = false;
            UpdateHighlight();
        }

        private void RefreshAllLines()
        {
            SetLineText(MenuOption.Resume, "Resume");
            SetLineText(MenuOption.Restart, "Restart Level");
            SetLineText(MenuOption.EditControls, "Edit Controls");
            SetLineText(MenuOption.Quit, "Quit");
        }

        private void SetLineText(MenuOption option, string text)
        {
            int i = (int)option;
            if (i < 0 || i >= OptionCount) return;

            TMP_Text t = optionTexts[i];
            if (t != null)
                t.text = text;
        }

        private void UpdateHighlight()
        {
            if (OptionCount == 0) return;

            for (int i = 0; i < OptionCount; i++)
            {
                TMP_Text t = optionTexts[i];
                if (t == null) continue;

                if (i != selectedIndex)
                {
                    t.color = defaultColor;
                    continue;
                }

                t.color = Color.Lerp(defaultColor, selectedColor, BlinkT);
            }
        }
    }
}
