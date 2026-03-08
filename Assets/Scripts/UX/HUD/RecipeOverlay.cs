using System.Collections;
using System.Collections.Generic;
using Gameplay.Coop;
using Gameplay.Items;
using Gameplay.Workstations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UX.HUD
{
    public class RecipeOverlay : MonoBehaviour
    {
        public static RecipeOverlay Instance { get; private set; }

        private const float PanelWidth = 520f;
        private const float SlideMargin = 24f;
        private const float SlideDuration = 0.2f;

        private Canvas canvas;
        private GameObject dimBg;
        private GameObject panel;
        private RectTransform panelRect;
        private RectTransform contentParent;
        private readonly List<GameObject> entries = new List<GameObject>();
        private Coroutine slideCoroutine;
        private bool isVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
            SetPanelHidden();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("RecipeOverlay");
            go.AddComponent<RecipeOverlay>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                Show();
            if (Input.GetKeyUp(KeyCode.Tab))
                Hide();
        }

        private void Show()
        {
            if (isVisible) return;
            isVisible = true;
            Refresh();
            dimBg.SetActive(true);
            panel.SetActive(true);
            Slide(-PanelWidth, SlideMargin, 0f, 0.4f);
        }

        private void Hide()
        {
            if (!isVisible) return;
            isVisible = false;
            ItemTooltip.Instance?.Hide();
            Slide(panelRect.anchoredPosition.x, -PanelWidth, 0.4f, 0f);
        }

        private void Slide(float fromX, float toX, float fromBgAlpha, float toBgAlpha)
        {
            if (slideCoroutine != null)
                StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideCoroutine(fromX, toX, fromBgAlpha, toBgAlpha));
        }

        private IEnumerator SlideCoroutine(float fromX, float toX, float fromBgAlpha, float toBgAlpha)
        {
            var bgImg = dimBg.GetComponent<Image>();
            Color bgColor = bgImg.color;
            float elapsed = 0f;

            while (elapsed < SlideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SlideDuration));

                var pos = panelRect.anchoredPosition;
                pos.x = Mathf.Lerp(fromX, toX, t);
                panelRect.anchoredPosition = pos;

                bgColor.a = Mathf.Lerp(fromBgAlpha, toBgAlpha, t);
                bgImg.color = bgColor;

                yield return null;
            }

            var finalPos = panelRect.anchoredPosition;
            finalPos.x = toX;
            panelRect.anchoredPosition = finalPos;

            bgColor.a = toBgAlpha;
            bgImg.color = bgColor;

            if (!isVisible)
            {
                panel.SetActive(false);
                dimBg.SetActive(false);
            }

            slideCoroutine = null;
        }

        private void SetPanelHidden()
        {
            var pos = panelRect.anchoredPosition;
            pos.x = -PanelWidth;
            panelRect.anchoredPosition = pos;
            panel.SetActive(false);
            dimBg.SetActive(false);
        }

        private void Refresh()
        {
            ClearEntries();

            var gm = CoopGameManager.Instance;
            if (gm == null || gm.Orders == null || gm.Orders.Count == 0)
            {
                AddNoOrdersEntry();
                return;
            }

            for (int i = 0; i < gm.Orders.Count; i++)
            {
                OrderData order = gm.Orders[i];
                Recipe recipe = FindRecipeForProduct(order.RequiredProductId);
                CreateOrderEntry(order, recipe);
            }
        }

        private void CreateOrderEntry(OrderData order, Recipe recipe)
        {
            var entry = CreateChild(contentParent, "Entry");
            var entryVlg = entry.AddComponent<VerticalLayoutGroup>();
            entryVlg.padding = new RectOffset(20, 20, 16, 16);
            entryVlg.spacing = 10f;
            entryVlg.childForceExpandWidth = true;
            entryVlg.childForceExpandHeight = false;
            entryVlg.childControlWidth = true;
            entryVlg.childControlHeight = true;
            entries.Add(entry);

            var entryImg = entry.AddComponent<Image>();
            entryImg.color = new Color(0.15f, 0.15f, 0.18f, 0.6f);
            entryImg.raycastTarget = false;

            var header = CreateChild(entry.transform, "Header");
            var headerHlg = header.AddComponent<HorizontalLayoutGroup>();
            headerHlg.spacing = 12f;
            headerHlg.childForceExpandWidth = false;
            headerHlg.childForceExpandHeight = false;
            headerHlg.childControlWidth = true;
            headerHlg.childControlHeight = true;
            headerHlg.childAlignment = TextAnchor.MiddleLeft;

            LabItem outputItem = recipe != null ? recipe.OutputItem : null;
            if (outputItem != null && outputItem.Sprite != null)
                CreateHoverableIcon(header.transform, outputItem, 44);

            var nameText = CreateText(header.transform, "Name",
                order.ProductName.ToString(), 22, FontStyles.Bold, Color.white);
            var nameLe = nameText.AddComponent<LayoutElement>();
            nameLe.flexibleWidth = 1f;

            string progress = $"{order.DeliveredCount} / {order.RequiredQuantity}";
            Color progressColor = order.IsComplete
                ? new Color(0.4f, 1f, 0.4f)
                : new Color(0.9f, 0.9f, 0.9f);
            CreateText(header.transform, "Progress", progress, 22, FontStyles.Bold, progressColor);

            if (recipe != null && recipe.Ingredients != null && recipe.Ingredients.Length > 0)
            {
                var ingRow = CreateChild(entry.transform, "Ingredients");
                var ingHlg = ingRow.AddComponent<HorizontalLayoutGroup>();
                ingHlg.spacing = 14f;
                ingHlg.childForceExpandWidth = false;
                ingHlg.childForceExpandHeight = false;
                ingHlg.childControlWidth = true;
                ingHlg.childControlHeight = true;
                ingHlg.childAlignment = TextAnchor.MiddleLeft;

                CreateText(ingRow.transform, "Label", "Needs:", 16, FontStyles.Italic,
                    new Color(0.6f, 0.6f, 0.65f));

                var grouped = GroupIngredients(recipe.Ingredients);
                foreach (var kvp in grouped)
                    CreateIngredientGroup(ingRow.transform, kvp.Value.item, kvp.Value.count);
            }
        }

        private void CreateIngredientGroup(Transform parent, LabItem item, int count)
        {
            var group = CreateChild(parent, "Ingredient");
            var hlg = group.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            if (item.Sprite != null)
                CreateHoverableIcon(group.transform, item, 34);

            string label = count > 1 ? $"x{count}" : "x1";
            CreateText(group.transform, "Count", label, 16, FontStyles.Normal,
                new Color(0.85f, 0.85f, 0.85f));
        }

        private GameObject CreateHoverableIcon(Transform parent, LabItem item, float size)
        {
            var go = CreateChild(parent, "Icon");
            var img = go.AddComponent<Image>();
            img.sprite = item.Sprite;
            img.preserveAspect = true;
            img.raycastTarget = true;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;

            var trigger = go.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ =>
            {
                ItemTooltip.EnsureInstance();
                ItemTooltip.Instance?.Show(item);
            });
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => ItemTooltip.Instance?.Hide());
            trigger.triggers.Add(exitEntry);

            return go;
        }

        private void AddNoOrdersEntry()
        {
            var entry = CreateChild(contentParent, "NoOrders");
            entries.Add(entry);
            var le = entry.AddComponent<LayoutElement>();
            le.preferredHeight = 50;

            var text = CreateText(entry.transform, "Text", "No active orders", 18,
                FontStyles.Italic, new Color(0.5f, 0.5f, 0.55f));
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        private Dictionary<ushort, (LabItem item, int count)> GroupIngredients(LabItem[] ingredients)
        {
            var grouped = new Dictionary<ushort, (LabItem item, int count)>();
            for (int i = 0; i < ingredients.Length; i++)
            {
                if (ingredients[i] == null) continue;
                ushort id = ingredients[i].Id;
                if (grouped.ContainsKey(id))
                    grouped[id] = (ingredients[i], grouped[id].count + 1);
                else
                    grouped[id] = (ingredients[i], 1);
            }
            return grouped;
        }

        private Recipe FindRecipeForProduct(ushort productId)
        {
            var workstations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
            for (int w = 0; w < workstations.Length; w++)
            {
                if (workstations[w].Recipes == null) continue;
                var recipes = workstations[w].Recipes;
                for (int r = 0; r < recipes.Length; r++)
                {
                    if (recipes[r] != null && recipes[r].OutputItem != null
                        && recipes[r].OutputItem.Id == productId)
                        return recipes[r];
                }
            }
            return null;
        }


        private void BuildUI()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            dimBg = CreateChild(transform, "DimBackground");
            var dimRect = dimBg.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImg = dimBg.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0f);
            dimImg.raycastTarget = false;

            panel = CreateChild(dimBg.transform, "Panel");
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, 0f);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            panelImg.raycastTarget = false;

            var panelFitter = panel.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var panelVlg = panel.AddComponent<VerticalLayoutGroup>();
            panelVlg.padding = new RectOffset(24, 24, 20, 20);
            panelVlg.spacing = 14f;
            panelVlg.childForceExpandWidth = true;
            panelVlg.childForceExpandHeight = false;
            panelVlg.childControlWidth = true;
            panelVlg.childControlHeight = true;

            var titleGo = CreateText(panel.transform, "Title", "Recipes", 28, FontStyles.Bold, Color.white);
            titleGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            CreateText(panel.transform, "Hint", "Hold TAB to view", 13, FontStyles.Italic,
                new Color(0.45f, 0.45f, 0.5f));

            var sep = CreateChild(panel.transform, "Separator");
            var sepImg = sep.AddComponent<Image>();
            sepImg.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            sepImg.raycastTarget = false;
            var sepLe = sep.AddComponent<LayoutElement>();
            sepLe.preferredHeight = 1;

            var content = CreateChild(panel.transform, "Content");
            contentParent = content.GetComponent<RectTransform>();
            var contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 10f;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
        }

        private GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private GameObject CreateText(Transform parent, string name, string content,
            float fontSize, FontStyles style, Color color)
        {
            var go = CreateChild(parent, name);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return go;
        }

        private void ClearEntries()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i] != null)
                    Destroy(entries[i]);
            }
            entries.Clear();

            if (contentParent != null)
            {
                for (int i = contentParent.childCount - 1; i >= 0; i--)
                    Destroy(contentParent.GetChild(i).gameObject);
            }
        }

    }
}
