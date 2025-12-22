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
            if (connectMenu.gameObject.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    options[selectedIndex].onClick.Invoke();
                }

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    selectedIndex = Mathf.Clamp(selectedIndex - 1, 0, options.Length);
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    selectedIndex = Mathf.Clamp(selectedIndex + 1, 0, options.Length);
                }
                ApplyVisual();
            }
        }

        private void ApplyVisual()
        {
            
            foreach (var option in options)
            {
                bool selected = options[selectedIndex] == option;
                option.image.color = selected ? Color.yellow : new Color32(120, 64, 24, 255);
            }
        }
    }
}
