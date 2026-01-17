using System.Collections;
using Gameplay.Coop;
using TMPro;
using UnityEngine;

namespace UX.HUD
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Timer")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private bool showRemainingTime = true;
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color warningTimerColor = Color.yellow;
        [SerializeField] private Color criticalTimerColor = Color.red;
        [SerializeField] private float warningTimeThreshold = 60f;
        [SerializeField] private float criticalTimeThreshold = 30f;

        [Header("Delivery Progress")]
        [SerializeField] private TMP_Text deliveryText;
        [SerializeField] private string deliveryFormat = "{0} / {1}";

        [Header("Alerts")]
        [SerializeField] private TMP_Text alertText;
        [SerializeField] private float alertDisplayDuration = 3f;
        [SerializeField] private float alertFadeDuration = 0.5f;
        [SerializeField] private CanvasGroup alertCanvasGroup;

        private Coroutine alertCoroutine;

        private void OnEnable()
        {
            if (CoopGameManager.Instance != null)
            {
                CoopGameManager.Instance.OnTimerUpdated += RefreshTimer;
                CoopGameManager.Instance.OnDeliveryUpdated += RefreshDelivery;
                CoopGameManager.Instance.OnAlert += ShowAlert;
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            if (CoopGameManager.Instance != null)
            {
                CoopGameManager.Instance.OnTimerUpdated -= RefreshTimer;
                CoopGameManager.Instance.OnDeliveryUpdated -= RefreshDelivery;
                CoopGameManager.Instance.OnAlert -= ShowAlert;
            }
        }

        private void Start()
        {
            if (alertCanvasGroup != null)
                alertCanvasGroup.alpha = 0f;

            StartCoroutine(LateSubscribe());
        }

        private IEnumerator LateSubscribe()
        {
            yield return null;

            if (CoopGameManager.Instance != null)
            {
                CoopGameManager.Instance.OnTimerUpdated -= RefreshTimer;
                CoopGameManager.Instance.OnDeliveryUpdated -= RefreshDelivery;
                CoopGameManager.Instance.OnAlert -= ShowAlert;

                CoopGameManager.Instance.OnTimerUpdated += RefreshTimer;
                CoopGameManager.Instance.OnDeliveryUpdated += RefreshDelivery;
                CoopGameManager.Instance.OnAlert += ShowAlert;
            }

            RefreshAll();
        }

        private void Update()
        {
            RefreshTimer();
        }

        private void RefreshAll()
        {
            RefreshTimer();
            RefreshDelivery();
        }

        private void RefreshTimer()
        {
            if (timerText == null) return;
            if (CoopGameManager.Instance == null)
            {
                timerText.text = FormatTime(0f);
                return;
            }

            float displayTime = showRemainingTime
                ? CoopGameManager.Instance.RemainingTime
                : CoopGameManager.Instance.ElapsedTime;

            timerText.text = FormatTime(displayTime);

            if (showRemainingTime)
            {
                float remaining = CoopGameManager.Instance.RemainingTime;

                if (remaining <= criticalTimeThreshold)
                    timerText.color = criticalTimerColor;
                else if (remaining <= warningTimeThreshold)
                    timerText.color = warningTimerColor;
                else
                    timerText.color = normalTimerColor;
            }
            else
            {
                timerText.color = normalTimerColor;
            }
        }

        private void RefreshDelivery()
        {
            if (deliveryText == null) return;
            if (CoopGameManager.Instance == null)
            {
                deliveryText.text = string.Format(deliveryFormat, 0, 0);
                return;
            }

            int delivered = CoopGameManager.Instance.DeliveredCount;
            int target = CoopGameManager.Instance.TargetCount;
            deliveryText.text = string.Format(deliveryFormat, delivered, target);
        }

        public void ShowAlert(string message)
        {
            if (alertText == null) return;

            if (alertCoroutine != null)
                StopCoroutine(alertCoroutine);

            alertCoroutine = StartCoroutine(DisplayAlertCoroutine(message));
        }

        private IEnumerator DisplayAlertCoroutine(string message)
        {
            alertText.text = message;

            if (alertCanvasGroup != null)
            {
                alertCanvasGroup.alpha = 0f;

                float elapsed = 0f;
                while (elapsed < alertFadeDuration)
                {
                    elapsed += Time.deltaTime;
                    alertCanvasGroup.alpha = Mathf.Clamp01(elapsed / alertFadeDuration);
                    yield return null;
                }

                alertCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(alertDisplayDuration);

            if (alertCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < alertFadeDuration)
                {
                    elapsed += Time.deltaTime;
                    alertCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / alertFadeDuration);
                    yield return null;
                }

                alertCanvasGroup.alpha = 0f;
            }

            alertCoroutine = null;
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int minutes = totalSeconds / 60;
            int secs = totalSeconds % 60;
            return $"{minutes:D2}:{secs:D2}";
        }
    }
}
