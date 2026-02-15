using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class WorkstationWorldVisual : NetworkBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Workstation workstation;

        [Header("Slot Renderers (size 5)")]
        [SerializeField] private SpriteRenderer[] slotRenderers;

        [Header("Status Icon")]
        [SerializeField] private SpriteRenderer statusIcon;
        [SerializeField] private Sprite workingSprite;
        [SerializeField] private Sprite completedSprite;
        [SerializeField] private Sprite failedSprite;

        [Header("Recipe Ghost")]
        [SerializeField] private float ghostAlpha = 0.3f;
        [SerializeField] private float ghostCycleDuration = 1f;

        [Header("Output Renderers (size 5)")]
        [SerializeField] private SpriteRenderer[] outputRenderers;

        private int ghostRecipeIndex;
        private float ghostCycleTimer;

        private void Awake()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        public override void OnNetworkSpawn()
        {
            if (workstation != null)
            {
                if (workstation.SlotItemIds != null)
                    workstation.SlotItemIds.OnListChanged += OnSlotsChanged;

                if (workstation.OutputSlotIds != null)
                    workstation.OutputSlotIds.OnListChanged += OnOutputSlotsChanged;

                workstation.OnWorkStateChanged += OnWorkStateChanged;
            }

            RefreshAll();
        }

        public override void OnNetworkDespawn()
        {
            if (workstation != null)
            {
                if (workstation.SlotItemIds != null)
                    workstation.SlotItemIds.OnListChanged -= OnSlotsChanged;

                if (workstation.OutputSlotIds != null)
                    workstation.OutputSlotIds.OnListChanged -= OnOutputSlotsChanged;

                workstation.OnWorkStateChanged -= OnWorkStateChanged;
            }
        }

        private void Update()
        {
            if (workstation == null) return;
            if (workstation.CurrentWorkState != WorkState.Idle) return;

            var recipes = workstation.Recipes;
            if (recipes == null || recipes.Length <= 1) return;

            if (HasAnyDepositedItem() && CountTiedBestRecipes() <= 1) return;

            ghostCycleTimer += Time.deltaTime;
            if (ghostCycleTimer >= ghostCycleDuration)
            {
                ghostCycleTimer = 0f;
                ghostRecipeIndex++;
                RefreshSlots();
            }
        }

        private void OnSlotsChanged(NetworkListEvent<ushort> _)
        {
            RefreshSlots();
        }

        private void OnOutputSlotsChanged(NetworkListEvent<ushort> _)
        {
            RefreshOutput();
        }

        private void OnWorkStateChanged()
        {
            ghostRecipeIndex = 0;
            ghostCycleTimer = 0f;
            RefreshSlots();
            RefreshStatusIcon();
            RefreshOutput();
        }

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshStatusIcon();
            RefreshOutput();
        }

        private void RefreshSlots()
        {
            if (workstation == null) return;

            bool showGhosts = workstation.CurrentWorkState == WorkState.Idle;
            Recipe recipe = GetGhostRecipe();
            LabItem[] ingredients = recipe != null ? recipe.Ingredients : null;

            bool[] satisfied = null;
            if (showGhosts && ingredients != null)
            {
                satisfied = new bool[ingredients.Length];
                for (int s = 0; s < Workstation.SlotCount; s++)
                {
                    ushort slotId = workstation.GetSlotId(s);
                    if (slotId == 0) continue;

                    for (int ing = 0; ing < ingredients.Length; ing++)
                    {
                        if (!satisfied[ing] && ingredients[ing] != null && ingredients[ing].Id == slotId)
                        {
                            satisfied[ing] = true;
                            break;
                        }
                    }
                }
            }

            int nextGhost = 0;

            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                SpriteRenderer sr = (slotRenderers != null && i < slotRenderers.Length) ? slotRenderers[i] : null;
                if (sr == null) continue;

                Sprite spr = workstation.GetSpriteForSlot(i);

                if (spr != null)
                {
                    sr.sprite = spr;
                    sr.color = Color.white;
                    sr.enabled = true;
                }
                else if (showGhosts && ingredients != null)
                {
                    Sprite ghostSprite = null;
                    while (nextGhost < ingredients.Length)
                    {
                        if (!satisfied[nextGhost] && ingredients[nextGhost] != null
                            && ingredients[nextGhost].Sprite != null)
                        {
                            ghostSprite = ingredients[nextGhost].Sprite;
                            nextGhost++;
                            break;
                        }
                        nextGhost++;
                    }

                    if (ghostSprite != null)
                    {
                        sr.sprite = ghostSprite;
                        sr.color = new Color(1f, 1f, 1f, ghostAlpha);
                        sr.enabled = true;
                    }
                    else
                    {
                        sr.sprite = null;
                        sr.enabled = false;
                    }
                }
                else
                {
                    sr.sprite = null;
                    sr.enabled = false;
                }
            }
        }

        private Recipe GetGhostRecipe()
        {
            var state = workstation.CurrentWorkState;

            if ((state == WorkState.Working || state == WorkState.Completed)
                && workstation.AssignedRecipe != null)
                return workstation.AssignedRecipe;

            var recipes = workstation.Recipes;
            if (recipes == null || recipes.Length == 0) return null;
            if (recipes.Length == 1) return recipes[0];

            if (!HasAnyDepositedItem())
            {
                int idx = ghostRecipeIndex % recipes.Length;
                return recipes[idx];
            }

            int bestScore = -1;
            int tiedCount = 0;

            for (int r = 0; r < recipes.Length; r++)
            {
                int score = ScoreRecipe(recipes[r]);
                if (score > bestScore)
                {
                    bestScore = score;
                    tiedCount = 1;
                }
                else if (score == bestScore)
                {
                    tiedCount++;
                }
            }

            if (tiedCount <= 1)
            {
                for (int r = 0; r < recipes.Length; r++)
                {
                    if (ScoreRecipe(recipes[r]) == bestScore)
                        return recipes[r];
                }
            }

            int pick = ghostRecipeIndex % tiedCount;
            int seen = 0;
            for (int r = 0; r < recipes.Length; r++)
            {
                if (ScoreRecipe(recipes[r]) == bestScore)
                {
                    if (seen == pick) return recipes[r];
                    seen++;
                }
            }

            return recipes[0];
        }

        private int ScoreRecipe(Recipe recipe)
        {
            if (recipe == null || recipe.Ingredients == null) return 0;

            int score = 0;
            bool[] matched = new bool[recipe.Ingredients.Length];

            for (int s = 0; s < Workstation.SlotCount; s++)
            {
                ushort slotId = workstation.GetSlotId(s);
                if (slotId == 0) continue;

                for (int ing = 0; ing < recipe.Ingredients.Length; ing++)
                {
                    if (!matched[ing] && recipe.Ingredients[ing] != null
                        && recipe.Ingredients[ing].Id == slotId)
                    {
                        matched[ing] = true;
                        score++;
                        break;
                    }
                }
            }

            return score;
        }

        private int CountTiedBestRecipes()
        {
            var recipes = workstation.Recipes;
            if (recipes == null || recipes.Length == 0) return 0;

            int bestScore = -1;
            int tiedCount = 0;

            for (int r = 0; r < recipes.Length; r++)
            {
                int score = ScoreRecipe(recipes[r]);
                if (score > bestScore)
                {
                    bestScore = score;
                    tiedCount = 1;
                }
                else if (score == bestScore)
                {
                    tiedCount++;
                }
            }

            return tiedCount;
        }

        private bool HasAnyDepositedItem()
        {
            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                if (workstation.GetSlotId(i) != 0)
                    return true;
            }
            return false;
        }

        private void RefreshStatusIcon()
        {
            if (statusIcon == null || workstation == null) return;

            WorkState state = workstation.CurrentWorkState;

            switch (state)
            {
                case WorkState.Working:
                    statusIcon.sprite = workingSprite;
                    statusIcon.enabled = workingSprite != null;
                    break;
                case WorkState.Completed:
                    statusIcon.sprite = completedSprite;
                    statusIcon.enabled = completedSprite != null;
                    break;
                case WorkState.Failed:
                    statusIcon.sprite = failedSprite;
                    statusIcon.enabled = failedSprite != null;
                    break;
                default:
                    statusIcon.sprite = null;
                    statusIcon.enabled = false;
                    break;
            }
        }

        private void RefreshOutput()
        {
            if (workstation == null) return;

            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                SpriteRenderer sr = (outputRenderers != null && i < outputRenderers.Length) ? outputRenderers[i] : null;
                if (sr == null) continue;

                Sprite spr = workstation.GetOutputSpriteForSlot(i);
                if (spr != null)
                {
                    sr.sprite = spr;
                    sr.enabled = true;
                }
                else
                {
                    sr.sprite = null;
                    sr.enabled = false;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();

            if (Application.isPlaying) return;
            if (workstation == null) return;
            if (workstation.Recipes == null || workstation.Recipes.Length == 0) return;

            RefreshSlots();
        }
#endif
    }
}
