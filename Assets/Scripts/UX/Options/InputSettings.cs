using UnityEngine;

namespace UX.Options
{
    public class InputSettings : MonoBehaviour
    {
        public static InputSettings Instance { get; private set; }

        [SerializeField] private Controls controls = new Controls
        {
            Forward = KeyCode.W,
            Back = KeyCode.S,
            Left = KeyCode.A,
            Right = KeyCode.D,
            Apply = KeyCode.Return,
            Cancel = KeyCode.Escape,
            Interact = KeyCode.E
        };

        public Controls Controls => controls;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadControls();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetControls(Controls updated)
        {
            controls = updated;
            SaveControls();
        }

        public void SaveControls()
        {
            PlayerPrefs.SetInt("Control_Forward", (int)controls.Forward);
            PlayerPrefs.SetInt("Control_Back", (int)controls.Back);
            PlayerPrefs.SetInt("Control_Left", (int)controls.Left);
            PlayerPrefs.SetInt("Control_Right", (int)controls.Right);
            PlayerPrefs.SetInt("Control_Apply", (int)controls.Apply);
            PlayerPrefs.SetInt("Control_Cancel", (int)controls.Cancel);
            PlayerPrefs.SetInt("Control_Interact", (int)controls.Interact);
            PlayerPrefs.Save();
        }

        public void LoadControls()
        {
            controls.Forward = (KeyCode)PlayerPrefs.GetInt("Control_Forward", (int)KeyCode.W);
            controls.Back = (KeyCode)PlayerPrefs.GetInt("Control_Back", (int)KeyCode.S);
            controls.Left = (KeyCode)PlayerPrefs.GetInt("Control_Left", (int)KeyCode.A);
            controls.Right = (KeyCode)PlayerPrefs.GetInt("Control_Right", (int)KeyCode.D);
            controls.Apply = (KeyCode)PlayerPrefs.GetInt("Control_Apply", (int)KeyCode.Return);
            controls.Cancel = (KeyCode)PlayerPrefs.GetInt("Control_Cancel", (int)KeyCode.Escape);
            controls.Interact = (KeyCode)PlayerPrefs.GetInt("Control_Interact", (int)KeyCode.E);
        }

        public bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public bool IsForwardPressed() => Input.GetKey(controls.Forward);
        public bool IsBackPressed() => Input.GetKey(controls.Back);
        public bool IsLeftPressed() => Input.GetKey(controls.Left);
        public bool IsRightPressed() => Input.GetKey(controls.Right);
        public bool IsApplyPressed() => Input.GetKeyDown(controls.Apply);
        public bool IsCancelPressed() => Input.GetKeyDown(controls.Cancel);
        public bool IsInteractPressed() => Input.GetKeyDown(controls.Interact);
    }
}
