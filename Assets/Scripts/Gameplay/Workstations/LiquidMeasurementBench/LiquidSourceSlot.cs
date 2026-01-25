using System;
using Gameplay.Items;
using Gameplay.Workstations.Scale;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Workstations.LiquidMeasurementBench
{
    public class LiquidSourceSlot : MonoBehaviour, IPointerClickHandler
    {
        [Header("Visual References")]
        [SerializeField] private Image containerImage;
        [SerializeField] private Image liquidFillImage;
        [SerializeField] private Image emptyOverlay;

        [Header("Container Settings")]
        [SerializeField] private float maxVolume = 100f;

        // Set at runtime by the menu
        private ClickPourController pourController;

        private LabItem configuredItem;
        private ushort chemicalId;
        private float currentVolume;
        private Color liquidColor = Color.cyan;
        private bool isConfigured;

        public event Action OnVolumeChanged;

        public float CurrentVolume => currentVolume;
        public float MaxVolume => maxVolume;
        public bool IsConfigured => isConfigured;
        public LabItem ConfiguredItem => configuredItem;
        public ushort ChemicalId => chemicalId;

        public void Initialize(ClickPourController controller)
        {
            pourController = controller;
        }

        public void Configure(LabItem labItem, float volume, float sourceMaxVolume = 0f)
        {
            if (labItem == null)
            {
                ClearConfiguration();
                return;
            }

            configuredItem = labItem;
            chemicalId = labItem.Id;
            isConfigured = true;

            if (sourceMaxVolume > 0f)
            {
                maxVolume = sourceMaxVolume;
            }

            if (labItem.ChemicalParticleData != null)
            {
                liquidColor = labItem.ChemicalParticleData.ParticleColor;
            }
            else
            {
                liquidColor = Color.cyan;
            }

            currentVolume = Mathf.Clamp(volume, 0f, maxVolume);

            UpdateVisual();
            OnVolumeChanged?.Invoke();
        }

        public void ClearConfiguration()
        {
            configuredItem = null;
            chemicalId = 0;
            isConfigured = false;
            currentVolume = 0f;
            liquidColor = Color.cyan;

            UpdateVisual();
            OnVolumeChanged?.Invoke();
        }

        public void SetVolume(float volume)
        {
            float oldVolume = currentVolume;
            currentVolume = Mathf.Clamp(volume, 0f, maxVolume);

            if (Mathf.Abs(oldVolume - currentVolume) > 0.001f)
            {
                UpdateVisual();
                OnVolumeChanged?.Invoke();
            }
        }

        public float RemoveVolume(float amount)
        {
            float available = currentVolume;
            float removed = Mathf.Min(available, amount);
            SetVolume(currentVolume - removed);
            return removed;
        }

        public void ReturnVolume(float amount)
        {
            SetVolume(currentVolume + amount);
        }

        private void UpdateVisual()
        {
            if (containerImage != null)
            {
                containerImage.gameObject.SetActive(isConfigured);
            }

            if (liquidFillImage != null)
            {
                float fillPercent = maxVolume > 0f ? currentVolume / maxVolume : 0f;
                liquidFillImage.fillAmount = fillPercent;
                liquidFillImage.color = liquidColor;
                liquidFillImage.gameObject.SetActive(isConfigured && currentVolume > 0f);
            }

            if (emptyOverlay != null)
            {
                emptyOverlay.gameObject.SetActive(!isConfigured || currentVolume <= 0f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!isConfigured || currentVolume <= 0f) return;
            if (pourController == null) return;
            if (pourController.CurrentState != ClickPourController.PourState.Idle) return;

            float volumeToPickUp = currentVolume;
            currentVolume = 0f;
            UpdateVisual();

            pourController.PickUpSource(this, chemicalId, volumeToPickUp, maxVolume, liquidColor);
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
