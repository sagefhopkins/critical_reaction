using System.Collections.Generic;
using Gameplay.Items;
using Gameplay.Workstations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UX
{
    public class WorkstationHoverInfo : MonoBehaviour
    {
        public static WorkstationHoverInfo Instance { get; private set; }

        private const float PanelWidth = 340f;

        private Canvas canvas;
        private GameObject panel;
        private RectTransform panelRect;
        private TextMeshProUGUI titleText;
        private RectTransform recipesParent;
        private readonly List<GameObject> recipeEntries = new List<GameObject>();

        private Workstation currentWorkstation;
        private Camera mainCam;

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
            panel.SetActive(false);
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
            var go = new GameObject("WorkstationHoverInfo");
            go.AddComponent<WorkstationHoverInfo>();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.Tab))
            {
                HidePanel();
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                HidePanel();
                return;
            }

            if (mainCam == null)
                mainCam = Camera.main;
            if (mainCam == null)
            {
                HidePanel();
                return;
            }

            Vector2 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            Workstation ws = null;
            if (hit.collider != null)
                ws = hit.collider.GetComponentInParent<Workstation>();

            if (ws != null)
            {
                if (ws != currentWorkstation)
                {
                    currentWorkstation = ws;
                    Refresh(ws);
                }
                FollowMouse();
                panel.SetActive(true);
            }
            else
            {
                HidePanel();
            }
        }

        private void HidePanel()
        {
            if (currentWorkstation == null) return;
            currentWorkstation = null;
            panel.SetActive(false);
        }

        private void Refresh(Workstation ws)
        {
            ClearRecipeEntries();

            string typeName = FormatTypeName(ws.Type);
            titleText.text = typeName;

            if (ws.Recipes == null || ws.Recipes.Length == 0)
            {
                var noRecipe = CreateText(recipesParent, "NoRecipe", "No recipes",
                    14, FontStyles.Italic, new Color(0.5f, 0.5f, 0.55f));
                recipeEntries.Add(noRecipe);
                return;
            }

            for (int r = 0; r < ws.Recipes.Length; r++)
            {
                Recipe recipe = ws.Recipes[r];
                if (recipe == null) continue;
                CreateRecipeRow(recipe);
            }
        }

        private void CreateRecipeRow(Recipe recipe)
        {
            var row = CreateChild(recipesParent, "Recipe");
            var vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(0, 0, 4, 4);
            recipeEntries.Add(row);

            var outputRow = CreateChild(row.transform, "Output");
            var outHlg = outputRow.AddComponent<HorizontalLayoutGroup>();
            outHlg.spacing = 6f;
            outHlg.childForceExpandWidth = false;
            outHlg.childForceExpandHeight = false;
            outHlg.childControlWidth = true;
            outHlg.childControlHeight = true;
            outHlg.childAlignment = TextAnchor.MiddleLeft;

            if (recipe.OutputItem != null && recipe.OutputItem.Sprite != null)
                CreateHoverableIcon(outputRow.transform, recipe.OutputItem, 26);

            string outputLabel = recipe.OutputItem != null ? recipe.OutputItem.DisplayName : "???";
            if (recipe.OutputQuantity > 1)
                outputLabel += $" x{recipe.OutputQuantity}";
            CreateText(outputRow.transform, "OutputName", outputLabel, 15, FontStyles.Bold,
                new Color(0.95f, 0.85f, 0.4f));

            if (recipe.Ingredients != null && recipe.Ingredients.Length > 0)
            {
                var ingRow = CreateChild(row.transform, "Ingredients");
                var ingHlg = ingRow.AddComponent<HorizontalLayoutGroup>();
                ingHlg.spacing = 10f;
                ingHlg.childForceExpandWidth = false;
                ingHlg.childForceExpandHeight = false;
                ingHlg.childControlWidth = true;
                ingHlg.childControlHeight = true;
                ingHlg.childAlignment = TextAnchor.MiddleLeft;

                var grouped = GroupIngredients(recipe.Ingredients);
                foreach (var kvp in grouped)
                {
                    var grp = CreateChild(ingRow.transform, "Ing");
                    var grpHlg = grp.AddComponent<HorizontalLayoutGroup>();
                    grpHlg.spacing = 3f;
                    grpHlg.childForceExpandWidth = false;
                    grpHlg.childForceExpandHeight = false;
                    grpHlg.childControlWidth = true;
                    grpHlg.childControlHeight = true;
                    grpHlg.childAlignment = TextAnchor.MiddleLeft;

                    CreateHoverableIcon(grp.transform, kvp.Value.item, 22);

                    string countLabel = kvp.Value.count > 1 ? $"x{kvp.Value.count}" : "x1";
                    CreateText(grp.transform, "Ct", countLabel, 13, FontStyles.Normal,
                        new Color(0.8f, 0.8f, 0.8f));
                }
            }
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

        private void FollowMouse()
        {
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out Vector2 localPoint
            );

            localPoint += new Vector2(20f, -20f);

            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector2 canvasSize = canvasRect.sizeDelta;
            float halfW = canvasSize.x * 0.5f;
            float halfH = canvasSize.y * 0.5f;
            Vector2 panelSize = panelRect.sizeDelta;

            if (localPoint.x + panelSize.x > halfW)
                localPoint.x -= panelSize.x + 40f;
            if (localPoint.y - panelSize.y < -halfH)
                localPoint.y += panelSize.y + 40f;

            panelRect.localPosition = localPoint;
        }

        private string FormatTypeName(WorkstationType type)
        {
            return type switch
            {
                WorkstationType.GraduatedCylinder => "Graduated Cylinder",
                WorkstationType.WashSink => "Wash Sink",
                WorkstationType.CoolingBath => "Cooling Bath",
                WorkstationType.VacuumFiltration => "Vacuum Filtration",
                WorkstationType.DryingOven => "Drying Oven",
                _ => type.ToString()
            };
        }


        private void BuildUI()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            panel = CreateChild(transform, "Panel");
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(PanelWidth, 0f);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.12f, 0.94f);
            panelImg.raycastTarget = false;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 12, 12);
            vlg.spacing = 6f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            var titleGo = CreateChild(panel.transform, "Title");
            titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 17;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            var sep = CreateChild(panel.transform, "Sep");
            var sepImg = sep.AddComponent<Image>();
            sepImg.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            sepImg.raycastTarget = false;
            var sepLe = sep.AddComponent<LayoutElement>();
            sepLe.preferredHeight = 1;

            var recipes = CreateChild(panel.transform, "Recipes");
            recipesParent = recipes.GetComponent<RectTransform>();
            var recipesVlg = recipes.AddComponent<VerticalLayoutGroup>();
            recipesVlg.spacing = 6f;
            recipesVlg.childForceExpandWidth = true;
            recipesVlg.childForceExpandHeight = false;
            recipesVlg.childControlWidth = true;
            recipesVlg.childControlHeight = true;
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

        private void ClearRecipeEntries()
        {
            for (int i = recipeEntries.Count - 1; i >= 0; i--)
            {
                if (recipeEntries[i] != null)
                    Destroy(recipeEntries[i]);
            }
            recipeEntries.Clear();

            if (recipesParent != null)
            {
                for (int i = recipesParent.childCount - 1; i >= 0; i--)
                    Destroy(recipesParent.GetChild(i).gameObject);
            }
        }

    }
}
