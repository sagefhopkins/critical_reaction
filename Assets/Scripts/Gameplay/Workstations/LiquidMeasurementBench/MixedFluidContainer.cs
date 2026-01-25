using System;
using System.Collections.Generic;
using Gameplay.Items;
using Gameplay.Workstations.Scale;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Workstations.LiquidMeasurementBench
{
    public class MixedFluidContainer : MonoBehaviour
    {
        [Header("Container Settings")]
        [SerializeField] private float maxVolume = 500f;

        [Header("Visual References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image liquidColorImage;

        [Header("Animation")]
        [SerializeField] private float fillAnimationSpeed = 5f;

        private Dictionary<ushort, float> chemicalVolumes = new();
        private float displayedFillAmount;
        private Color currentColor = Color.cyan;

        public event Action OnContentsChanged;

        public float TotalVolume
        {
            get
            {
                float total = 0f;
                foreach (var volume in chemicalVolumes.Values)
                {
                    total += volume;
                }
                return total;
            }
        }

        public float MaxVolume => maxVolume;
        public float RemainingCapacity => Mathf.Max(0f, maxVolume - TotalVolume);
        public IReadOnlyDictionary<ushort, float> Contents => chemicalVolumes;

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
                liquidColorImage.color = currentColor;
                liquidColorImage.fillAmount = displayedFillAmount;
            }
        }

        public float GetFillPercent()
        {
            if (maxVolume <= 0f) return 0f;
            return Mathf.Clamp01(TotalVolume / maxVolume);
        }

        public void AddChemical(ushort chemicalId, float volume)
        {
            if (volume <= 0f) return;

            float actualVolume = Mathf.Min(volume, RemainingCapacity);
            if (actualVolume <= 0f) return;

            if (chemicalVolumes.ContainsKey(chemicalId))
            {
                chemicalVolumes[chemicalId] += actualVolume;
            }
            else
            {
                chemicalVolumes[chemicalId] = actualVolume;
            }

            OnContentsChanged?.Invoke();
        }

        public float RemoveChemical(ushort chemicalId, float requestedAmount)
        {
            if (!chemicalVolumes.ContainsKey(chemicalId)) return 0f;

            float available = chemicalVolumes[chemicalId];
            float removed = Mathf.Min(available, requestedAmount);

            chemicalVolumes[chemicalId] -= removed;

            if (chemicalVolumes[chemicalId] <= 0.001f)
            {
                chemicalVolumes.Remove(chemicalId);
            }

            OnContentsChanged?.Invoke();
            return removed;
        }

        public float GetVolume(ushort chemicalId)
        {
            return chemicalVolumes.TryGetValue(chemicalId, out float volume) ? volume : 0f;
        }

        public float GetConcentration(ushort chemicalId)
        {
            float total = TotalVolume;
            if (total <= 0f) return 0f;

            return GetVolume(chemicalId) / total;
        }

        public float CalculatePH(Func<ushort, ChemicalParticleData> dataLookup)
        {
            float totalWeightedPH = 0f;
            float totalVolume = 0f;

            foreach (var kvp in chemicalVolumes)
            {
                var data = dataLookup(kvp.Key);
                if (data != null)
                {
                    totalWeightedPH += data.PHValue * kvp.Value;
                    totalVolume += kvp.Value;
                }
            }

            return totalVolume > 0f ? totalWeightedPH / totalVolume : 7f;
        }

        public Color GetBlendedColor(Func<ushort, Color> colorLookup)
        {
            float total = TotalVolume;
            if (total <= 0f) return Color.cyan;

            float r = 0f, g = 0f, b = 0f, a = 0f;

            foreach (var kvp in chemicalVolumes)
            {
                Color color = colorLookup(kvp.Key);
                float weight = kvp.Value / total;

                r += color.r * weight;
                g += color.g * weight;
                b += color.b * weight;
                a += color.a * weight;
            }

            return new Color(r, g, b, a);
        }

        public void SetDisplayColor(Color color)
        {
            currentColor = color;
            UpdateFillVisual();
        }

        public void Clear()
        {
            chemicalVolumes.Clear();
            displayedFillAmount = 0f;
            currentColor = Color.cyan;
            UpdateFillVisual();
            OnContentsChanged?.Invoke();
        }

        public void SetMaxVolume(float volume)
        {
            maxVolume = Mathf.Max(0f, volume);
        }

        public bool HasChemical(ushort chemicalId)
        {
            return chemicalVolumes.ContainsKey(chemicalId) && chemicalVolumes[chemicalId] > 0f;
        }

        public int ChemicalCount => chemicalVolumes.Count;
    }
}
