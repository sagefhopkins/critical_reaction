using Gameplay.Coop;
using TMPro;
using UnityEngine;
using UX.Options;

namespace UX.LevelWon
{
    public class LevelWonScreen : MonoBehaviour
    {
        private enum MenuOption
        {
            NextLevel,
            Quit
        }

        [Header("UI")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text[] optionTexts;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float blinkSpeed = 6f;

        private int selectedIndex;
        private bool isActive;

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
            if (CoopGameManager.Instance != null)
                CoopGameManager.Instance.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            if (CoopGameManager.Instance != null)
                CoopGameManager.Instance.OnLevelWon -= HandleLevelWon;
        }

        private void Start()
        {
            StartCoroutine(LateSubscribe());
        }

        private System.Collections.IEnumerator LateSubscribe()
        {
            yield return null;

            if (CoopGameManager.Instance != null)
            {
                CoopGameManager.Instance.OnLevelWon -= HandleLevelWon;
                CoopGameManager.Instance.OnLevelWon += HandleLevelWon;
            }
        }

        private void HandleLevelWon()
        {
            ShowScreen();
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateHighlight();
            HandleInput();
        }

        private void HandleInput()
        {
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
                case MenuOption.NextLevel:
                    SelectNextLevel();
                    break;

                case MenuOption.Quit:
                    SelectQuit();
                    break;
            }
        }

        private void SelectNextLevel()
        {
            HideScreen();
            if (PauseManager.Instance != null)
                PauseManager.Instance.RequestNextLevel();
        }

        private void SelectQuit()
        {
            HideScreen();
            if (PauseManager.Instance != null)
                PauseManager.Instance.RequestQuit();
        }

        private void ShowScreen()
        {
            if (menuRoot != null)
                menuRoot.SetActive(true);

            if (titleText != null)
                titleText.text = "Level Complete!";

            selectedIndex = 0;
            isActive = true;
            RefreshAllLines();
            UpdateHighlight();
        }

        private void HideScreen()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false);

            isActive = false;
        }

        private void RefreshAllLines()
        {
            SetLineText(MenuOption.NextLevel, "Next Level");
            SetLineText(MenuOption.Quit, "Exit to Menu");
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
