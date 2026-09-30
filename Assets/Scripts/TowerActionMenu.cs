using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed class TowerActionMenu : MonoBehaviour
{
    private Level01TowerBuildController controller;
    private BasicDefenseTurret selected;
    private Canvas canvas;
    private RectTransform root;
    private Image ring;
    private Button upgradeButton;
    private Image upgradeImage;
    private Sprite upgradeSprite;
    private Sprite lockedSprite;
    private Camera mainCamera;

    public void Initialize(Level01TowerBuildController source)
    {
        controller = source;
        mainCamera = Camera.main;
        Build();
        Hide();
    }

    private void Build()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystem = new("TowerUIEventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystem);
        }

        GameObject canvasObject = new("TowerActionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("TowerActionMenu", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvasObject.transform, false);
        root.sizeDelta = new Vector2(340f, 250f);

        ring = CreateImage("SelectionRing", "UI/TowerActions/tower_selection_ring", new Vector2(0f, 30f), new Vector2(190f, 190f));
        ring.raycastTarget = false;
        upgradeSprite = LoadSprite("UI/TowerActions/tower_upgrade");
        lockedSprite = LoadSprite("UI/TowerActions/tower_locked");
        upgradeButton = CreateButton(
            "Upgrade", upgradeSprite, new Vector2(-82f, -88f),
            new Color(0.18f, 0.48f, 0.70f, 0.94f), Upgrade, out upgradeImage);
        CreateButton(
            "Sell", LoadSprite("UI/TowerActions/tower_close"), new Vector2(82f, -88f),
            new Color(0.68f, 0.24f, 0.16f, 0.94f), Sell, out _);
    }

    private void LateUpdate()
    {
        if (selected == null || root == null || !root.gameObject.activeSelf || mainCamera == null) return;
        Vector3 position = mainCamera.WorldToScreenPoint(selected.transform.position + Vector3.up * 0.55f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, position, null, out Vector2 local);
        root.anchoredPosition = local;
    }

    public void Show(BasicDefenseTurret turret)
    {
        selected = turret;
        root.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        selected = null;
        if (root != null) root.gameObject.SetActive(false);
    }

    private void Upgrade()
    {
        if (selected == null) return;
        controller.TryUpgrade(selected);
        Refresh();
    }

    private void Sell()
    {
        if (selected != null) controller.Sell(selected);
    }

    private void Refresh()
    {
        if (selected == null) return;
        bool canUpgrade = selected.CanUpgrade && controller.Coins >= controller.GetUpgradeCost(selected);
        upgradeButton.interactable = canUpgrade;
        upgradeImage.sprite = selected.CanUpgrade ? upgradeSprite : lockedSprite;
        upgradeImage.color = canUpgrade || !selected.CanUpgrade ? Color.white : new Color(1f, 1f, 1f, 0.48f);
    }

    private Image CreateImage(string name, string resource, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = rect.GetComponent<Image>();
        image.sprite = LoadSprite(resource);
        image.preserveAspect = true;
        return image;
    }

    private Button CreateButton(
        string name,
        Sprite sprite,
        Vector2 position,
        Color plateColor,
        UnityEngine.Events.UnityAction action,
        out Image icon)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button))
            .GetComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(118f, 118f);

        Image plate = rect.GetComponent<Image>();
        plate.sprite = CreateCircularPlateSprite();
        plate.color = plateColor;
        plate.preserveAspect = true;

        Outline outline = rect.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.78f, 0.30f, 0.98f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = false;

        RectTransform iconRect = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);
        icon = iconRect.GetComponent<Image>();
        icon.sprite = sprite;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = plate;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.disabledColor = new Color(0.52f, 0.52f, 0.52f, 0.72f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(action);
        return button;
    }

    private static Sprite CreateCircularPlateSprite()
    {
        const int size = 128;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "TowerActionCircularPlate";
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.47f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center);
            float alpha = Mathf.Clamp01(radius - distance + 1f);
            float highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), center + new Vector2(-18f, 20f)) / 75f);
            texture.SetPixel(x, y, new Color(0.82f + highlight * 0.18f, 0.70f + highlight * 0.16f, 0.48f + highlight * 0.12f, alpha));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite LoadSprite(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
