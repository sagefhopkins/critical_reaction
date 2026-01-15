using System;
using Gameplay.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Workstations.GraduatedCylinder
{
    public class FluidContainer : MonoBehaviour
    {
        [Header("Container Settings")]
        [SerializeField] private float maxVolume = 100f;

        [Header("Visual References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image liquidColorImage;

        [Header("Animation")]
        [SerializeField] private float fillAnimationSpeed = 5f;

        private float currentVolume;
        private float displayedFillAmount;
        private bool isConfigured;
        private LabItem configuredItem;
        private Color liquidColor = Color.cyan;

        public event Action OnVolumeChanged;

        public float CurrentVolume => currentVolume;
        public float MaxVolume => maxVolume;
        public bool IsConfigured => isConfigured;
        public LabItem ConfiguredItem => configuredItem;

        private void Update()
        {
            AnimateFillLevel();
        }

        private void AnimateFillLevel()
        {
            float targetFill = GetFillPercent();

            if (Mathf.Abs(displayedFillAmount - targetFill) > 0.001f)
            {
                displayedFillAmount = Mathf.Lerp(displayedFillAmount, targetFill, Time.deltaTime * fillAnimationSpeed);
                UpdateFillVisual();
            }
        }

        private void UpdateFillVisual()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = displayedFillAmount;
            }

            if (liquidColorImage != null)
            {
                liquidColorImage.color = liquidColor;
                liquidColorImage.fillAmount = displayedFillAmount;
            }
        }

        public float GetFillPercent()
        {
            if (maxVolume <= 0f) return 0f;
            return Mathf.Clamp01(currentVolume / maxVolume);
        }

        public void SetVolume(float volume)
        {
            float oldVolume = currentVolume;
            currentVolume = Mathf.Clamp(volume, 0f, maxVolume);

            if (Mathf.Abs(oldVolume - currentVolume) > 0.001f)
            {
                OnVolumeChanged?.Invoke();
            }
        }

        public void AddVolume(float amount)
        {
            SetVolume(currentVolume + amount);
        }

        public float RemoveVolume(float requestedAmount)
        {
            float available = currentVolume;
            float removed = Mathf.Min(available, requestedAmount);
            SetVolume(currentVolume - removed);
            return removed;
        }

        public void ConfigureFromLabItem(LabItem labItem, float initialVolume)
        {
            if (labItem == null)
            {
                ClearConfiguration();
                return;
            }

            configuredItem = labItem;
            isConfigured = true;

            if (labItem.ChemicalParticleData != null)
            {
                liquidColor = labItem.ChemicalParticleData.ParticleColor;
            }
            else
            {
                liquidColor = Color.cyan;
            }

            currentVolume = Mathf.Clamp(initialVolume, 0f, maxVolume);
            displayedFillAmount = GetFillPercent();
            UpdateFillVisual();

            OnVolumeChanged?.Invoke();
        }

        public void ClearConfiguration()
        {
            configuredItem = null;
            isConfigured = false;
            currentVolume = 0f;
            displayedFillAmount = 0f;
            liquidColor = Color.cyan;
            UpdateFillVisual();

            OnVolumeChanged?.Invoke();
        }

        public void SetLiquidColor(Color color)
        {
            liquidColor = color;
            UpdateFillVisual();
        }

        public void SetMaxVolume(float volume)
        {
            maxVolume = Mathf.Max(0f, volume);
            if (currentVolume > maxVolume)
            {
                SetVolume(maxVolume);
            }
        }
    }
}
