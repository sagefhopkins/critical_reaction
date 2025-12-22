using System;
using TMPro;
using UnityEngine;

namespace UX.Options
{
    public class AppearanceEditor : MonoBehaviour
    {
        private enum Field
        {
            HeadOption,
            BodyOption,
            LegsOption,
            HeadColor,
            BodyColor,
            LegsColor,
            Done
        }

        private enum Channel
        {
            R,
            G,
            B,
            A
        }

        public Options optionMenu;

        [Header("UI")]
        [SerializeField] private TMP_Text[] lines;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float blinkSpeed = 6f;

        [Header("Options")]
        [SerializeField] private string[] headOptions;
        [SerializeField] private string[] bodyOptions;
        [SerializeField] private string[] legsOptions;

        [Header("Color Step")]
        [SerializeField] private int channelStep = 5;

        private PlayerAppearance working;
        private int selectedLine;
        private bool editing;
        private Channel selectedChannel = Channel.R;

        private Action<PlayerAppearance> onApply;
        private Action onCancel;

        private int LineCount => lines != null ? lines.Length : 0;

        private float BlinkT
        {
            get
            {
                float s = Mathf.Max(0.01f, blinkSpeed);
                return 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * s);
            }
        }

        public void Open(PlayerAppearance current, Action<PlayerAppearance> apply, Action cancel)
        {
            working = current;
            onApply = apply;
            onCancel = cancel;

            editing = false;
            selectedLine = 0;
            selectedChannel = Channel.R;

            gameObject.SetActive(true);
            RefreshAll();
            UpdateHighlight();
        }

        private void OnEnable()
        {
            if (LineCount > 0)
            {
                RefreshAll();
                UpdateHighlight();
            }
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            if (LineCount == 0) return;

            if (!editing)
                UpdateHighlight();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (!editing)
                {
                    editing = true;
                    UpdateHighlight();
                }
                else
                {
                    if ((Field)selectedLine == Field.Done)
                    {
                        Apply();
                        return;
                    }

                    editing = false;
                    UpdateHighlight();
                }

                return;
            }

            if (!editing)
            {
                HandleNavigate();
                return;
            }

            HandleEdit();
        }

        private void HandleNavigate()
        {
            int prev = selectedLine;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                selectedLine = (selectedLine - 1 + LineCount) % LineCount;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                selectedLine = (selectedLine + 1) % LineCount;

            if (prev != selectedLine)
                UpdateHighlight();
        }

        private void HandleEdit()
        {
            int delta = 0;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) delta = -1;
            else if (Input.GetKeyDown(KeyCode.RightArrow)) delta = 1;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (IsColorField((Field)selectedLine))
                    selectedChannel = PrevChannel(selectedChannel);
                RefreshAll();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (IsColorField((Field)selectedLine))
                    selectedChannel = NextChannel(selectedChannel);
                RefreshAll();
                return;
            }

            if (delta == 0) return;

            switch ((Field)selectedLine)
            {
                case Field.HeadOption:
                    working.HeadOption = CycleIndex(working.HeadOption, headOptions, delta);
                    break;

                case Field.BodyOption:
                    working.BodyOption = CycleIndex(working.BodyOption, bodyOptions, delta);
                    break;

                case Field.LegsOption:
                    working.LegsOption = CycleIndex(working.LegsOption, legsOptions, delta);
                    break;

                case Field.HeadColor:
                    working.HeadColor = AdjustChannel(working.HeadColor, selectedChannel, delta * channelStep);
                    break;

                case Field.BodyColor:
                    working.BodyColor = AdjustChannel(working.BodyColor, selectedChannel, delta * channelStep);
                    break;

                case Field.LegsColor:
                    working.LegsColor = AdjustChannel(working.LegsColor, selectedChannel, delta * channelStep);
                    break;
            }

            RefreshAll();
        }

        private void Apply()
        {
            onApply?.Invoke(working);
            Close();
        }

        private void Cancel()
        {
            onCancel?.Invoke();
            Close();
        }

        private void Close()
        {
            editing = false;
            onApply = null;
            onCancel = null;
            gameObject.SetActive(false);
            optionMenu.ReturnFocus();
        }

        private void RefreshAll()
        {
            RefreshLine(Field.HeadOption);
            RefreshLine(Field.BodyOption);
            RefreshLine(Field.LegsOption);
            RefreshLine(Field.HeadColor);
            RefreshLine(Field.BodyColor);
            RefreshLine(Field.LegsColor);
            RefreshLine(Field.Done);
        }

        private void RefreshLine(Field field)
        {
            int i = (int)field;
            if (i < 0 || i >= LineCount) return;

            TMP_Text t = lines[i];
            if (t == null) return;

            switch (field)
            {
                case Field.HeadOption:
                    t.text = $"Head: {GetOptionLabel(headOptions, working.HeadOption)}";
                    break;

                case Field.BodyOption:
                    t.text = $"Body: {GetOptionLabel(bodyOptions, working.BodyOption)}";
                    break;

                case Field.LegsOption:
                    t.text = $"Legs: {GetOptionLabel(legsOptions, working.LegsOption)}";
                    break;

                case Field.HeadColor:
                    t.text = $"Head Color [{selectedChannel}]: {ToRgbaString(working.HeadColor)}";
                    break;

                case Field.BodyColor:
                    t.text = $"Body Color [{selectedChannel}]: {ToRgbaString(working.BodyColor)}";
                    break;

                case Field.LegsColor:
                    t.text = $"Legs Color [{selectedChannel}]: {ToRgbaString(working.LegsColor)}";
                    break;

                case Field.Done:
                    t.text = "Done";
                    break;
            }
        }

        private void UpdateHighlight()
        {
            bool solid = editing;
            for (int i = 0; i < LineCount; i++)
            {
                TMP_Text t = lines[i];
                if (t == null) continue;

                if (i != selectedLine)
                {
                    t.color = defaultColor;
                    continue;
                }

                t.color = solid ? selectedColor : Color.Lerp(defaultColor, selectedColor, BlinkT);
            }
        }

        private static bool IsColorField(Field f)
        {
            return f == Field.HeadColor || f == Field.BodyColor || f == Field.LegsColor;
        }

        private static Channel NextChannel(Channel c)
        {
            return (Channel)(((int)c + 1) % 4);
        }

        private static Channel PrevChannel(Channel c)
        {
            return (Channel)(((int)c + 3) % 4);
        }

        private static int CycleIndex(int current, string[] list, int delta)
        {
            if (list == null || list.Length == 0) return 0;
            int n = list.Length;
            int next = (current + delta) % n;
            if (next < 0) next += n;
            return next;
        }

        private static string GetOptionLabel(string[] list, int index)
        {
            if (list == null || list.Length == 0) return "N/A";
            index = Mathf.Clamp(index, 0, list.Length - 1);
            string label = list[index];
            return string.IsNullOrWhiteSpace(label) ? $"#{index}" : label;
        }

        private static Color AdjustChannel(Color c, Channel channel, int delta255)
        {
            Color32 c32 = (Color32)c;

            byte Next(byte v)
            {
                int next = Mathf.Clamp(v + delta255, 0, 255);
                return (byte)next;
            }

            switch (channel)
            {
                case Channel.R: c32.r = Next(c32.r); break;
                case Channel.G: c32.g = Next(c32.g); break;
                case Channel.B: c32.b = Next(c32.b); break;
                case Channel.A: c32.a = Next(c32.a); break;
            }

            return c32;
        }

        private static string ToRgbaString(Color c)
        {
            Color32 c32 = (Color32)c;
            return $"({c32.r},{c32.g},{c32.b},{c32.a})";
        }
    }
}
