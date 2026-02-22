using UnityEngine;
using Gameplay.Coop;
using TMPro;

namespace UX.HUD
{
    public class GlobalDeliveryTimerHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private CanvasGroup root;

        private void OnEnable()
        {
            TrySubscribe();
            HandleTimerUpdated();
        }
        private void OnDisable()
        {
            if (CoopGameManager.Instance != null)
                CoopGameManager.Instance.OnTimerUpdated -= HandleTimerUpdated;
        }
        private void TrySubscribe()
        {
            if (CoopGameManager.Instance == null)
                return;

            CoopGameManager.Instance.OnTimerUpdated -= HandleTimerUpdated;

            CoopGameManager.Instance.OnTimerUpdated += HandleTimerUpdated;
        }
        private void HandleTimerUpdated()
        {
            if (CoopGameManager.Instance == null)
                return;

            Refresh(CoopGameManager.Instance.RemainingTime);
        }
        private void Refresh(float seconds)
        {
            if (timerText == null)
                return;

            if (CoopGameManager.Instance == null || !CoopGameManager.Instance.IsLevelActive)
            {
                if (root != null)
                    root.alpha = 0f;
                timerText.text = "00:00";
                return;
            }
            if (root != null)
                root.alpha = 1f;

            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);

            timerText.text = $"{minutes:00}:{secs:00}";
        }
    }
}

