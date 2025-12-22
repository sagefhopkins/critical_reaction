using TMPro;
using UnityEngine;

namespace UX.Credits
{
    public class Credits : MonoBehaviour
    {
        [SerializeField] private UX.MainMenu.MainMenu mainMenu;

        [Header("UI")]
        [SerializeField] private TMP_Text backText;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float blinkSpeed = 6f;

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
            UpdateSelectionVisual();
        }

        private void Update()
        {
            UpdateSelectionVisual();

            if (Input.GetKeyDown(KeyCode.Return))
                mainMenu.ShowMainMenu();
        }

        private void UpdateSelectionVisual()
        {
            if (backText == null) return;
            backText.color = Color.Lerp(defaultColor, selectedColor, BlinkT);
        }
    }
}