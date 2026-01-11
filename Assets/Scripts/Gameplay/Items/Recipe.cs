using UnityEngine;

namespace Gameplay.Items
{
    public enum RecipeType
    {
        Mixing,
        Heating,
        Cooling,
        Distillation,
        Synthesis,
        Extraction
    }

    [CreateAssetMenu(menuName = "Items/Recipe", fileName = "Recipe")]
    public class Recipe : ScriptableObject
    {
        [Header("Recipe Info")]
        [SerializeField] private string recipeName;
        [SerializeField] private RecipeType recipeType;

        [Header("Ingredients (up to 5)")]
        [SerializeField] private LabItem[] ingredients;

        [Header("Output")]
        [SerializeField] private LabItem outputItem;
        [SerializeField] private int outputQuantity = 1;

        [Header("Temperature Requirements")]
        [SerializeField] private float minTemperature = 0f;
        [SerializeField] private float maxTemperature = 100f;

        [Header("Work Requirements")]
        [SerializeField] private float workDuration = 5f;

        public string RecipeName => recipeName;
        public RecipeType Type => recipeType;
        public LabItem[] Ingredients => ingredients;
        public LabItem OutputItem => outputItem;
        public int OutputQuantity => outputQuantity;
        public float MinTemperature => minTemperature;
        public float MaxTemperature => maxTemperature;
        public float WorkDuration => workDuration;

        public bool IsTemperatureValid(float temperature)
        {
            return temperature >= minTemperature && temperature <= maxTemperature;
        }

        public bool CanCraftWith(ushort[] slotItemIds, int slotCount)
        {
            if (ingredients == null || ingredients.Length == 0)
                return false;

            int[] requiredCounts = new int[ingredients.Length];
            for (int i = 0; i < ingredients.Length; i++)
            {
                if (ingredients[i] != null)
                    requiredCounts[i] = 1;
            }

            for (int s = 0; s < slotCount; s++)
            {
                ushort slotId = slotItemIds[s];
                if (slotId == 0) continue;

                for (int i = 0; i < ingredients.Length; i++)
                {
                    if (ingredients[i] != null && ingredients[i].Id == slotId && requiredCounts[i] > 0)
                    {
                        requiredCounts[i]--;
                        break;
                    }
                }
            }

            for (int i = 0; i < requiredCounts.Length; i++)
            {
                if (requiredCounts[i] > 0)
                    return false;
            }

            return true;
        }
    }
}
