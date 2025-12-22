using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UX.Campaign
{
    public class Campaign : MonoBehaviour
    {
        [Serializable]
        public struct Level
        {
            public int levelID;
            public string name;
            public string bestTime;
            [Range(0, 5)] public int bestScore;
            [TextArea] public string description;
            public bool unlocked;
        }

        [Serializable]
        public struct CampaignData
        {
            public int campaignID;
            public string name;
            public Level[] levels;
        }

        [Serializable]
        public class LevelSelectedEvent : UnityEvent<int> { }

        private enum FooterId
        {
            Play,
            Back
        }

        [Header("Mode")]
        [SerializeField] private bool isCoop;

        [Header("Dependencies")]
        [SerializeField] private UX.MainMenu.MainMenu mainMenu;

        [Header("Data")]
        [SerializeField] private CampaignData campaign;

        [Header("UI: Level Strip")]
        [SerializeField] private TMP_Text levelNumbersText;
        [SerializeField] private TMP_Text levelStatusText;

        [Header("UI: Details")]
        [SerializeField] private TMP_Text campaignTitleText;
        [SerializeField] private TMP_Text selectedLevelText;
        [SerializeField] private TMP_Text bestTimeText;
        [SerializeField] private TMP_Text bestRankText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("UI: Footer")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button backButton;

        [Header("Selection")]
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private int selectedLevelIndex;
        [SerializeField] private FooterId selectedFooter = FooterId.Play;

        [Header("Events")]
        [SerializeField] private LevelSelectedEvent onPlayLevel;
        [SerializeField] private LevelSelectedEvent onPlayCoopLevel;

        private bool active;

        private void OnEnable()
        {
            active = true;
            selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, Mathf.Max(0, LevelCount - 1));
            if (!IsSelectableLevel(selectedLevelIndex))
                selectedLevelIndex = FindFirstSelectableLevelIndex();

            RefreshAll();
        }

        private void OnDisable()
        {
            active = false;
        }

        private int LevelCount => campaign.levels != null ? campaign.levels.Length : 0;

        private void Update()
        {
            if (!active) return;
            if (LevelCount == 0) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                MoveLevel(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                MoveLevel(+1);

            if (Input.GetKeyDown(KeyCode.UpArrow))
                selectedFooter = FooterId.Play;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                selectedFooter = FooterId.Back;

            if (Input.GetKeyDown(KeyCode.Return))
                ActivateFooter();

            RefreshFooterHighlight();
        }

        private void MoveLevel(int delta)
        {
            int next = FindNextSelectableLevelIndex(selectedLevelIndex, delta);
            if (next == selectedLevelIndex) return;

            selectedLevelIndex = next;
            RefreshAll();
        }

        private int FindFirstSelectableLevelIndex()
        {
            for (int i = 0; i < LevelCount; i++)
                if (IsSelectableLevel(i))
                    return i;

            return 0;
        }

        private int FindNextSelectableLevelIndex(int fromIndex, int delta)
        {
            if (LevelCount == 0) return 0;

            int dir = delta < 0 ? -1 : 1;

            for (int step = 1; step <= LevelCount; step++)
            {
                int idx = (fromIndex + (step * dir)) % LevelCount;
                if (idx < 0) idx += LevelCount;

                if (IsSelectableLevel(idx))
                    return idx;
            }

            return fromIndex;
        }

        private bool IsSelectableLevel(int index)
        {
            if (index < 0 || index >= LevelCount) return false;
            Level l = campaign.levels[index];
            return l.unlocked || l.bestScore > 0;
        }

        private void ActivateFooter()
        {
            if (selectedFooter == FooterId.Play)
            {
                if (!IsSelectableLevel(selectedLevelIndex)) return;

                int levelId = campaign.levels[selectedLevelIndex].levelID;

                if (playButton != null)
                {
                    playButton.onClick.Invoke();
                }
                else
                {
                    if (isCoop) onPlayCoopLevel?.Invoke(levelId);
                    else onPlayLevel?.Invoke(levelId);
                }

                return;
            }

            if (backButton != null)
                backButton.onClick.Invoke();
            else
                BackToMainMenu();
        }

        public void BackToMainMenu()
        {
            if (mainMenu != null)
                mainMenu.ShowMainMenu();

            gameObject.SetActive(false);
        }

        public int GetSelectedLevelId()
        {
            if (LevelCount == 0) return -1;
            return campaign.levels[Mathf.Clamp(selectedLevelIndex, 0, LevelCount - 1)].levelID;
        }

        public void PlaySelectedLevel()
        {
            if (!IsSelectableLevel(selectedLevelIndex)) return;

            int id = GetSelectedLevelId();

            if (isCoop) onPlayCoopLevel?.Invoke(id);
            else onPlayLevel?.Invoke(id);
        }

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshLevelStrip();
            RefreshDetails();
            RefreshFooterHighlight();
        }

        private void RefreshHeader()
        {
            if (campaignTitleText != null)
                campaignTitleText.text = $"{(isCoop ? "Co-op" : "Campaign")}: {campaign.name}";
        }

        private void RefreshLevelStrip()
        {
            if (levelNumbersText != null)
                levelNumbersText.text = BuildLevelsLine();

            if (levelStatusText != null)
                levelStatusText.text = BuildStatusLine();
        }

        private string BuildLevelsLine()
        {
            if (LevelCount == 0) return string.Empty;

            string selectedHex = ColorUtility.ToHtmlStringRGB(selectedColor);

            string s = string.Empty;
            for (int i = 0; i < LevelCount; i++)
            {
                string token = (i + 1).ToString();
                if (i == selectedLevelIndex)
                    token = $"<color=#{selectedHex}>{token}</color>";

                s += token;
                if (i < LevelCount - 1) s += " - ";
            }

            return s;
        }

        private string BuildStatusLine()
        {
            if (LevelCount == 0) return string.Empty;

            string selectedHex = ColorUtility.ToHtmlStringRGB(selectedColor);

            string s = string.Empty;
            for (int i = 0; i < LevelCount; i++)
            {
                Level l = campaign.levels[i];

                string core;
                if (l.bestScore > 0) core = "x";
                else if (l.unlocked) core = "u";
                else core = " ";

                string token = $"[{core}]";
                if (i == selectedLevelIndex)
                    token = $"<color=#{selectedHex}>{token}</color>";

                s += token;
                if (i < LevelCount - 1) s += "-";
            }

            return s;
        }

        private void RefreshDetails()
        {
            if (LevelCount == 0) return;

            Level l = campaign.levels[Mathf.Clamp(selectedLevelIndex, 0, LevelCount - 1)];

            if (selectedLevelText != null)
                selectedLevelText.text = $"Selected Level: Level {selectedLevelIndex + 1} - {l.name}";

            if (bestTimeText != null)
                bestTimeText.text = $"Best Time: {FormatBestTime(l.bestTime)}";

            if (bestRankText != null)
                bestRankText.text = $"Best Rank: {FormatStars(l.bestScore)}";

            if (descriptionText != null)
                descriptionText.text = $"Description: {l.description}";
        }

        private void RefreshFooterHighlight()
        {
            ApplyFooterHighlight(playButton, selectedFooter == FooterId.Play);
            ApplyFooterHighlight(backButton, selectedFooter == FooterId.Back);
        }

        private void ApplyFooterHighlight(Button button, bool selected)
        {
            if (button == null) return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;

            label.color = selected ? selectedColor : defaultColor;
        }

        private static string FormatStars(int filled)
        {
            int f = Mathf.Clamp(filled, 0, 5);
            int e = 5 - f;

            string s = string.Empty;
            for (int i = 0; i < f; i++) s += "★";
            for (int i = 0; i < e; i++) s += "☆";
            return s;
        }

        private static string FormatBestTime(string bestTime)
        {
            if (string.IsNullOrWhiteSpace(bestTime)) return "--:--";
            return bestTime;
        }
    }
}
