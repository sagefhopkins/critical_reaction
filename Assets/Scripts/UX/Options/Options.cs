using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UX.Options
{
    public class Options : MonoBehaviour
    {
        public enum OptionId
        {
            MusicVolume,
            SfxVolume,
            Resolution,
            Fullscreen,
            Controls,
            Rebind,
            Name,
            Appearance
        }

        private enum Mode
        {
            Navigate,
            Adjust,
            EditName
        }

        [Header("UI")]
        [SerializeField] private TMP_Text[] textOptions;
        [SerializeField] private Button backButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float blinkSpeed = 6f;

        [Header("Name Editing")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private GameObject nameInputRoot;

        [Header("Submenus")]
        [SerializeField] private GameObject controlsMenu;
        [SerializeField] private GameObject rebindMenu;
        [SerializeField] private GameObject appearanceMenu;

        [Header("Settings")]
        [Range(0, 100)] [SerializeField] private int musicVolume = 100;
        [Range(0, 100)] [SerializeField] private int sfxVolume = 100;

        [SerializeField] private ResolutionOption[] resolutions;
        [SerializeField] private int resolutionOption;
        [SerializeField] private bool fullScreen = true;

        [SerializeField] private Controls controls;
        [SerializeField] private string playerName = "Player";
        [SerializeField] private PlayerAppearance playerAppearance;

        [Header("Selection")]
        [SerializeField] private Vector2Int selectedIndex;

        [Header("Dependencies")]
        [SerializeField] private UX.MainMenu.MainMenu mainMenu;

        private Mode mode = Mode.Navigate;
        private bool inputLocked;

        private int TextOptionCount => textOptions != null ? textOptions.Length : 0;
        private bool HasBack => backButton != null;
        private bool HasSave => saveButton != null;

        private int BackIndex => TextOptionCount;
        private int SaveIndex => TextOptionCount + (HasBack ? 1 : 0);

        private int FooterCount => (HasBack ? 1 : 0) + (HasSave ? 1 : 0);
        private int TotalSelectableCount => TextOptionCount + FooterCount;
        
        public Controls GetControls() => controls;

        


        private bool IsFooterSelection
        {
            get
            {
                int idx = Mathf.Clamp(selectedIndex.y, 0, Mathf.Max(0, TotalSelectableCount - 1));
                return idx >= TextOptionCount;
            }
        }

        private bool AnySubmenuActive
        {
            get
            {
                if (controlsMenu != null && controlsMenu.activeSelf) return true;
                if (rebindMenu != null && rebindMenu.activeSelf) return true;
                if (appearanceMenu != null && appearanceMenu.activeSelf) return true;
                return false;
            }
        }

        private bool CanProcessInput => !inputLocked && !AnySubmenuActive;

        private float BlinkT
        {
            get
            {
                float s = Mathf.Max(0.01f, blinkSpeed);
                return 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * s);
            }
        }

        private void OnEnable()
        {
            mode = Mode.Navigate;
            inputLocked = false;
            selectedIndex.x = 0;

            SetNameInputActive(false);

            if (TotalSelectableCount == 0) return;

            selectedIndex.y = Mathf.Clamp(selectedIndex.y, 0, TotalSelectableCount - 1);

            RefreshAllLines();
            UpdateHighlight();
        }

        private void Update()
        {
            if (TotalSelectableCount == 0) return;

            if (!CanProcessInput)
                return;

            if (mode == Mode.Navigate)
                UpdateHighlight();

            if (mode == Mode.EditName)
            {
                HandleNameEditInput();
                return;
            }

            HandleNavigationInput();

            if (mode == Mode.Adjust)
            {
                HandleAdjustInput();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
                ActivateSelected();
        }

        private void HandleNavigationInput()
        {
            if (mode != Mode.Navigate) return;

            int prev = selectedIndex.y;

            if (IsFooterSelection)
            {
                if (FooterCount <= 1) return;

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                    selectedIndex.y = HasBack ? BackIndex : SaveIndex;
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                    selectedIndex.y = HasSave ? SaveIndex : BackIndex;

                if (prev != selectedIndex.y)
                    UpdateHighlight();

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    selectedIndex.y = Mathf.Clamp(TextOptionCount - 1, 0, Mathf.Max(0, TotalSelectableCount - 1));
                    UpdateHighlight();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
                selectedIndex.y = (selectedIndex.y - 1 + TotalSelectableCount) % TotalSelectableCount;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                selectedIndex.y = (selectedIndex.y + 1) % TotalSelectableCount;

            if (prev != selectedIndex.y)
                UpdateHighlight();
        }

        private void ActivateSelected()
        {
            int idx = Mathf.Clamp(selectedIndex.y, 0, TotalSelectableCount - 1);

            if (idx < TextOptionCount)
            {
                OptionId id = (OptionId)idx;

                switch (id)
                {
                    case OptionId.MusicVolume:
                    case OptionId.SfxVolume:
                    case OptionId.Resolution:
                    case OptionId.Fullscreen:
                        mode = Mode.Adjust;
                        UpdateHighlight();
                        break;

                    case OptionId.Controls:
                        OpenMenu(controlsMenu);
                        break;

                    case OptionId.Rebind:
                        if (rebindMenu != null)
                        {
                            RebindControlsEditor editor = rebindMenu.GetComponent<RebindControlsEditor>();
                            if (editor != null)
                            {
                                editor.SetOptions(this);
                                Controls current = InputSettings.Instance != null ? InputSettings.Instance.Controls : controls;
                                editor.Open(current);
                            }

                            OpenMenu(rebindMenu);
                        }
                        break;


                    case OptionId.Name:
                        BeginNameEdit();
                        break;

                    case OptionId.Appearance:
                        OpenMenu(appearanceMenu);
                        break;
                }

                return;
            }

            if (HasBack && idx == BackIndex)
            {
                backButton.onClick.Invoke();
                return;
            }

            if (HasSave && idx == SaveIndex)
            {
                saveButton.onClick.Invoke();
                return;
            }
        }

        private void HandleAdjustInput()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                mode = Mode.Navigate;
                UpdateHighlight();
                return;
            }

            int idx = Mathf.Clamp(selectedIndex.y, 0, TotalSelectableCount - 1);
            if (idx >= TextOptionCount) return;

            int delta = 0;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) delta = -1;
            else if (Input.GetKeyDown(KeyCode.RightArrow)) delta = 1;

            if (delta == 0) return;

            OptionId id = (OptionId)idx;

            switch (id)
            {
                case OptionId.MusicVolume:
                    musicVolume = Mathf.Clamp(musicVolume + delta, 0, 100);
                    RefreshLine(OptionId.MusicVolume);
                    break;

                case OptionId.SfxVolume:
                    sfxVolume = Mathf.Clamp(sfxVolume + delta, 0, 100);
                    RefreshLine(OptionId.SfxVolume);
                    break;

                case OptionId.Resolution:
                    if (resolutions == null || resolutions.Length == 0) return;
                    resolutionOption = (resolutionOption + delta + resolutions.Length) % resolutions.Length;
                    ApplyResolution();
                    RefreshLine(OptionId.Resolution);
                    break;

                case OptionId.Fullscreen:
                    fullScreen = !fullScreen;
                    ApplyFullscreen();
                    RefreshLine(OptionId.Fullscreen);
                    break;
            }
        }

        private void BeginNameEdit()
        {
            mode = Mode.EditName;

            if (nameInputField != null)
                nameInputField.text = playerName ?? string.Empty;

            SetNameInputActive(true);

            if (nameInputField != null)
            {
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }

            UpdateHighlight();
        }

        private void HandleNameEditInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelNameEdit();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                CommitNameEdit();
                return;
            }
        }

        public void CommitNameEdit()
        {
            if (nameInputField != null)
                playerName = nameInputField.text;

            mode = Mode.Navigate;
            SetNameInputActive(false);

            RefreshLine(OptionId.Name);
            UpdateHighlight();
        }

        public void CancelNameEdit()
        {
            mode = Mode.Navigate;
            SetNameInputActive(false);
            UpdateHighlight();
        }

        private void SetNameInputActive(bool active)
        {
            if (nameInputRoot != null)
                nameInputRoot.SetActive(active);
            else if (nameInputField != null)
                nameInputField.gameObject.SetActive(active);
        }

        private void OpenMenu(GameObject menu)
        {
            if (menu == null) return;
            inputLocked = true;
            menu.SetActive(true);
        }

        public void ReturnFocus()
        {
            inputLocked = false;
            mode = Mode.Navigate;
            UpdateHighlight();
        }

        public void BackToMainMenu()
        {
            if (mainMenu != null)
                mainMenu.ShowMainMenu();

            gameObject.SetActive(false);
        }

        public void SaveToPrefs()
        {
            PlayerPrefs.SetInt("MusicVolume", musicVolume);
            PlayerPrefs.SetInt("SfxVolume", sfxVolume);
            PlayerPrefs.SetInt("ResolutionOption", resolutionOption);
            PlayerPrefs.SetInt("Fullscreen", fullScreen ? 1 : 0);
            PlayerPrefs.SetString("PlayerName", playerName ?? string.Empty);
            PlayerPrefs.Save();

            BackToMainMenu();
        }

        public void Load()
        {
            musicVolume = PlayerPrefs.GetInt("MusicVolume", musicVolume);
            sfxVolume = PlayerPrefs.GetInt("SfxVolume", sfxVolume);
            resolutionOption = PlayerPrefs.GetInt("ResolutionOption", resolutionOption);
            fullScreen = PlayerPrefs.GetInt("Fullscreen", fullScreen ? 1 : 0) == 1;
            playerName = PlayerPrefs.GetString("PlayerName", playerName);

            ApplyFullscreen();
            ApplyResolution();
            RefreshAllLines();
            UpdateHighlight();
        }

        private void ApplyResolution()
        {
            if (resolutions == null || resolutions.Length == 0) return;
            resolutionOption = Mathf.Clamp(resolutionOption, 0, resolutions.Length - 1);

            ResolutionOption opt = resolutions[resolutionOption];
            if (opt.width <= 0 || opt.height <= 0) return;

            Screen.SetResolution(opt.width, opt.height, fullScreen);
        }

        private void ApplyFullscreen()
        {
            Screen.fullScreen = fullScreen;
        }

        private void UpdateHighlight()
        {
            if (TotalSelectableCount == 0) return;

            int clamped = Mathf.Clamp(selectedIndex.y, 0, TotalSelectableCount - 1);
            bool solid = mode != Mode.Navigate;

            for (int i = 0; i < TextOptionCount; i++)
            {
                TMP_Text t = textOptions[i];
                if (t == null) continue;

                if (i != clamped)
                {
                    t.color = defaultColor;
                    continue;
                }

                t.color = solid ? selectedColor : Color.Lerp(defaultColor, selectedColor, BlinkT);
            }

            ApplyButtonHighlight(backButton, HasBack && clamped == BackIndex, solid);
            ApplyButtonHighlight(saveButton, HasSave && clamped == SaveIndex, solid);
        }

        private void ApplyButtonHighlight(Button button, bool selected, bool solid)
        {
            if (button == null) return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;

            if (!selected)
            {
                label.color = defaultColor;
                return;
            }

            label.color = solid ? selectedColor : Color.Lerp(defaultColor, selectedColor, BlinkT);
        }

        private void RefreshAllLines()
        {
            if (TextOptionCount == 0) return;

            for (int i = 0; i < TextOptionCount; i++)
                RefreshLine((OptionId)i);
        }

        private void RefreshLine(OptionId id)
        {
            int i = (int)id;
            if (i < 0 || i >= TextOptionCount) return;

            TMP_Text t = textOptions[i];
            if (t == null) return;

            switch (id)
            {
                case OptionId.MusicVolume:
                    t.text = $" - Music Volume\t\t[{musicVolume}%]";
                    break;

                case OptionId.SfxVolume:
                    t.text = $" - SFX Volume\t\t[{sfxVolume}%]";
                    break;

                case OptionId.Resolution:
                    t.text = $" - Resolution\t\t[{GetResolutionLabel()}]";
                    break;

                case OptionId.Fullscreen:
                    t.text = $" - Fullscreen\t\t[{(fullScreen ? "On" : "Off")}]";
                    break;

                case OptionId.Controls:
                    t.text = " - Controls\t\t[View]";
                    break;

                case OptionId.Rebind:
                    t.text = " - Rebind\t\t\t[Edit]";
                    break;

                case OptionId.Name:
                    t.text = $" - Name\t\t\t[{playerName}]";
                    break;

                case OptionId.Appearance:
                    t.text = " - Appearance\t\t[Edit]";
                    break;
            }
        }

        private string GetResolutionLabel()
        {
            if (resolutions == null || resolutions.Length == 0) return "N/A";
            resolutionOption = Mathf.Clamp(resolutionOption, 0, resolutions.Length - 1);
            ResolutionOption opt = resolutions[resolutionOption];
            return $"{opt.width}x{opt.height}";
        }
        
        public void SetControls(Controls updated)
        {
            controls = updated;
            if (InputSettings.Instance != null)
                InputSettings.Instance.SetControls(updated);
        }
    }

    [Serializable]
    public struct ResolutionOption
    {
        public int width;
        public int height;
    }

    [Serializable]
    public struct PlayerAppearance
    {
        public int HeadOption;
        public int BodyOption;
        public int LegsOption;

        public Color HeadColor;
        public Color BodyColor;
        public Color LegsColor;
    }

    [Serializable]
    public struct Controls
    {
        public KeyCode Forward;
        public KeyCode Back;
        public KeyCode Left;
        public KeyCode Right;
        public KeyCode Apply;
        public KeyCode Cancel;
        public KeyCode Interact;
    }
}
