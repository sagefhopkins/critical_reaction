using System;
using TMPro;
using UnityEngine;

namespace UX.Options
{
    public class RebindControlsEditor : MonoBehaviour
    {
        private enum LineId
        {
            Forward,
            Back,
            Left,
            Right,
            Apply,
            Cancel,
            Interact,
            Save
        }

        [Header("UI")]
        [SerializeField] private TMP_Text[] lines;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float blinkSpeed = 6f;

        private Options options;
        private MonoBehaviour pauseMenuOwner;
        private Controls working;

        private int selectedIndex;
        private bool capturing;
        private LineId capturingLine;

        private int LineCount => lines != null ? lines.Length : 0;

        private float BlinkT
        {
            get
            {
                float s = Mathf.Max(0.01f, blinkSpeed);
                return 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * s);
            }
        }

        public void SetOptions(Options owner)
        {
            options = owner;
            pauseMenuOwner = null;
        }

        public void SetPauseMenu(MonoBehaviour owner)
        {
            pauseMenuOwner = owner;
            options = null;
        }

        public void Open(Controls current)
        {
            working = current;

            selectedIndex = 0;
            capturing = false;

            gameObject.SetActive(true);
            RefreshAll();
            UpdateHighlight();
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            if (LineCount == 0) return;

            if (!capturing)
                UpdateHighlight();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (capturing)
                {
                    capturing = false;
                    RefreshAll();
                    UpdateHighlight();
                    return;
                }

                Close();
                return;
            }

            if (capturing)
            {
                if (TryGetPressedKey(out KeyCode key))
                {
                    SetBinding(capturingLine, key);
                    capturing = false;
                    RefreshAll();
                    UpdateHighlight();
                }

                return;
            }

            int prev = selectedIndex;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                selectedIndex = (selectedIndex - 1 + LineCount) % LineCount;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                selectedIndex = (selectedIndex + 1) % LineCount;

            if (prev != selectedIndex)
                UpdateHighlight();

            if (Input.GetKeyDown(KeyCode.Return))
                ActivateSelected();
        }

        private void ActivateSelected()
        {
            LineId id = (LineId)Mathf.Clamp(selectedIndex, 0, LineCount - 1);

            if (id == LineId.Save)
            {
                SaveToOptions();
                Close();
                return;
            }

            capturing = true;
            capturingLine = id;
            RefreshAll();
            UpdateHighlight();
        }

        private void SaveToOptions()
        {
            if (options != null)
            {
                options.SetControls(working);
                return;
            }

            if (InputSettings.Instance != null)
            {
                InputSettings.Instance.SetControls(working);
            }
        }

        private void Close()
        {
            capturing = false;
            gameObject.SetActive(false);

            if (options != null)
            {
                options.ReturnFocus();
                return;
            }

            if (pauseMenuOwner != null)
            {
                pauseMenuOwner.SendMessage("ReturnFocus", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void RefreshAll()
        {
            RefreshLine(LineId.Forward);
            RefreshLine(LineId.Back);
            RefreshLine(LineId.Left);
            RefreshLine(LineId.Right);
            RefreshLine(LineId.Apply);
            RefreshLine(LineId.Cancel);
            RefreshLine(LineId.Interact);
            RefreshLine(LineId.Save);
        }

        private void RefreshLine(LineId id)
        {
            int i = (int)id;
            if (i < 0 || i >= LineCount) return;

            TMP_Text t = lines[i];
            if (t == null) return;

            if (id == LineId.Save)
            {
                t.text = "Save";
                return;
            }

            string label = GetLabel(id);

            if (capturing && capturingLine == id)
            {
                t.text = $"{label}\t\t[Press a key]";
                return;
            }

            t.text = $"{label}\t\t[{GetBinding(id)}]";
        }

        private void UpdateHighlight()
        {
            bool solid = capturing;

            for (int i = 0; i < LineCount; i++)
            {
                TMP_Text t = lines[i];
                if (t == null) continue;

                if (i != selectedIndex)
                {
                    t.color = defaultColor;
                    continue;
                }

                t.color = solid ? selectedColor : Color.Lerp(defaultColor, selectedColor, BlinkT);
            }
        }

        private static string GetLabel(LineId id)
        {
            switch (id)
            {
                case LineId.Forward: return "Forward";
                case LineId.Back: return "Back";
                case LineId.Left: return "Left";
                case LineId.Right: return "Right";
                case LineId.Apply: return "Apply";
                case LineId.Cancel: return "Cancel";
                case LineId.Interact: return "Interact";
                default: return id.ToString();
            }
        }

        private KeyCode GetBinding(LineId id)
        {
            switch (id)
            {
                case LineId.Forward: return working.Forward;
                case LineId.Back: return working.Back;
                case LineId.Left: return working.Left;
                case LineId.Right: return working.Right;
                case LineId.Apply: return working.Apply;
                case LineId.Cancel: return working.Cancel;
                case LineId.Interact: return working.Interact;
                default: return KeyCode.None;
            }
        }

        private void SetBinding(LineId id, KeyCode key)
        {
            switch (id)
            {
                case LineId.Forward: working.Forward = key; break;
                case LineId.Back: working.Back = key; break;
                case LineId.Left: working.Left = key; break;
                case LineId.Right: working.Right = key; break;
                case LineId.Apply: working.Apply = key; break;
                case LineId.Cancel: working.Cancel = key; break;
                case LineId.Interact: working.Interact = key; break;
            }
        }

        private static bool TryGetPressedKey(out KeyCode key)
        {
            if (!Input.anyKeyDown)
            {
                key = KeyCode.None;
                return false;
            }

            Array values = Enum.GetValues(typeof(KeyCode));
            for (int i = 0; i < values.Length; i++)
            {
                KeyCode k = (KeyCode)values.GetValue(i);
                if (Input.GetKeyDown(k))
                {
                    key = k;
                    return true;
                }
            }

            key = KeyCode.None;
            return false;
        }
    }
}
