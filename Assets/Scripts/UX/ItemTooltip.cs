using Gameplay.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UX
{
    public class ItemTooltip : MonoBehaviour
    {
        public static ItemTooltip Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;

        private RectTransform panelRect;
        private Canvas canvas;
        private bool built;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;

            var go = new GameObject("ItemTooltip");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ItemTooltip>();
            Instance.BuildUI();
        }

        private void BuildUI()
        {
            if (built) return;
            built = true;

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            panelRect = panel.AddComponent<RectTransform>();
            panelRect.pivot = new Vector2(0f, 1f);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.12f, 0.92f);
            panelImg.raycastTarget = false;

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            var headerRect = header.AddComponent<RectTransform>();

            var hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(header.transform, false);
            iconImage = iconGo.AddComponent<Image>();
            iconImage.raycastTarget = false;
            var iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 32;
            iconLayout.preferredHeight = 32;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(header.transform, false);
            nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            var nameLayout = nameGo.AddComponent<LayoutElement>();
            nameLayout.preferredWidth = 200;

            var descGo = new GameObject("Description");
            descGo.transform.SetParent(panel.transform, false);
            descriptionText = descGo.AddComponent<TextMeshProUGUI>();
            descriptionText.fontSize = 14;
            descriptionText.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            descriptionText.raycastTarget = false;
            descriptionText.enableWordWrapping = true;
            var descLayout = descGo.AddComponent<LayoutElement>();
            descLayout.preferredWidth = 240;

            panel.SetActive(false);
        }

        public void Show(LabItem item)
        {
            if (item == null) { Hide(); return; }

            EnsurePanelRef();

            if (nameText != null)
                nameText.text = item.DisplayName;

            bool hasDesc = !string.IsNullOrEmpty(item.Description);
            if (descriptionText != null)
            {
                descriptionText.text = hasDesc ? item.Description : "";
                descriptionText.gameObject.SetActive(hasDesc);
            }

            if (iconImage != null)
            {
                iconImage.sprite = item.Sprite;
                iconImage.gameObject.SetActive(item.Sprite != null);
            }

            if (panelRect != null)
                panelRect.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panelRect != null)
                panelRect.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (panelRect == null || !panelRect.gameObject.activeSelf) return;
            FollowMouse();
        }

        private void FollowMouse()
        {
            Vector2 mousePos = Input.mousePosition;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                mousePos,
                canvas.worldCamera,
                out Vector2 localPoint
            );

            localPoint += new Vector2(16f, -16f);

            Vector2 panelSize = panelRect.sizeDelta;
            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector2 canvasSize = canvasRect.sizeDelta;
            float halfW = canvasSize.x * 0.5f;
            float halfH = canvasSize.y * 0.5f;

            if (localPoint.x + panelSize.x > halfW)
                localPoint.x = localPoint.x - panelSize.x - 32f;
            if (localPoint.y - panelSize.y < -halfH)
                localPoint.y = localPoint.y + panelSize.y + 32f;

            panelRect.localPosition = localPoint;
        }

        private void EnsurePanelRef()
        {
            if (panelRect == null && built)
                panelRect = transform.Find("Panel")?.GetComponent<RectTransform>();

            if (!built)
                BuildUI();
        }
    }
}
