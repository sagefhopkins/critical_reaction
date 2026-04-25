using UnityEngine;
using UnityEngine.UI;

namespace UX.CoopMenu
{
    public class CoopLobbyTypeSelector : MonoBehaviour
    {
        public CoopConnectMenu connectMenu;
        public Button[] options;
        public int selectedIndex;
        private void Update()
        {
            if (connectMenu != null && connectMenu.gameObject.activeSelf)
                ApplyVisual();
        }

        private void ApplyVisual()
        {
            Color selectedColor = connectMenu != null && connectMenu.MainMenu != null
                ? connectMenu.MainMenu.SelectedColor
                : Color.yellow;

            foreach (var option in options)
            {
                bool selected = options[selectedIndex] == option;
                option.image.color = selected ? selectedColor : new Color32(120, 64, 24, 255);
            }
        }
    }
}
