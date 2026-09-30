using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class Level01Hud : MonoBehaviour
{
    private Level01WaveController controller;
    private Level01TowerBuildController buildController;
    private RectTransform safeRoot;
    private DigitStrip waveValue;
    private DigitStrip enemyValue;
    private DigitStrip lifeValue;
    private DigitStrip defeatedValue;
    private DigitStrip coinValue;
    private Rect lastSafeArea;
    private Sprite[] digitSprites;
    private Sprite slashSprite;

    public void Initialize(Level01WaveController source)
    {
        controller = source;
        buildController = source.GetComponent<Level01TowerBuildController>();
        LoadDigitSprites();
        Build();
        RefreshSafeArea();
        RefreshValues();
    }

    private void Update()
    {
        if (controller == null) return;
        if (Screen.safeArea != lastSafeArea) RefreshSafeArea();
        RefreshValues();
    }

    private void LoadDigitSprites()
    {
        digitSprites = new Sprite[10];
        for (int i = 0; i < digitSprites.Length; i++)
        {
            Texture2D texture = Resources.Load<Texture2D>($"UI/HUD/Digits/hud_digit_{i}");
            if (texture != null)
                digitSprites[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
        }
        slashSprite = CreateSlashSprite();
    }

    private void Build()
    {
        GameObject canvasObject = new("Level01HudCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        safeRoot = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        safeRoot.SetParent(canvasObject.transform, false);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

        waveValue = CreateMetric("Wave", "UI/HUD/hud_wave", false, -150f);
        enemyValue = CreateMetric("Enemies", "UI/HUD/hud_enemy", false, -255f);
        lifeValue = CreateMetric("BaseLife", "UI/HUD/hud_base_life", true, -150f);
        defeatedValue = CreateMetric("Defeated", "UI/HUD/hud_defeated", true, -255f);
        coinValue = CreateMetric("Coins", "UI/HUD/hud_coins", false, -360f);
    }

    private DigitStrip CreateMetric(string name, string iconPath, bool rightSide, float y)
    {
        RectTransform metric = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup),
                typeof(HorizontalLayoutGroup))
            .GetComponent<RectTransform>();
        metric.SetParent(safeRoot, false);
        float xAnchor = rightSide ? 1f : 0f;
        metric.anchorMin = metric.anchorMax = new Vector2(xAnchor, 1f);
        metric.pivot = new Vector2(xAnchor, 0.5f);
        metric.anchoredPosition = new Vector2(rightSide ? -70f : 70f, y);
        metric.sizeDelta = new Vector2(220f, 88f);
        metric.GetComponent<CanvasGroup>().alpha = 0.86f;

        HorizontalLayoutGroup metricLayout = metric.GetComponent<HorizontalLayoutGroup>();
        metricLayout.spacing = 1f;
        metricLayout.childAlignment = TextAnchor.MiddleCenter;
        metricLayout.childControlWidth = false;
        metricLayout.childControlHeight = false;
        metricLayout.childForceExpandWidth = false;
        metricLayout.childForceExpandHeight = false;

        DigitStrip value = null;
        if (rightSide) value = CreateDigits(metric);

        RectTransform iconRect = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement))
            .GetComponent<RectTransform>();
        iconRect.SetParent(metric, false);
        iconRect.sizeDelta = new Vector2(86f, 86f);
        LayoutElement iconLayout = iconRect.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 86f;
        iconLayout.preferredHeight = 86f;
        Image icon = iconRect.GetComponent<Image>();
        Texture2D texture = Resources.Load<Texture2D>(iconPath);
        if (texture != null)
            icon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        if (!rightSide) value = CreateDigits(metric);
        return value;
    }

    private DigitStrip CreateDigits(Transform parent)
    {
        RectTransform digits = new GameObject("Digits", typeof(RectTransform), typeof(HorizontalLayoutGroup))
            .GetComponent<RectTransform>();
        digits.SetParent(parent, false);
        digits.sizeDelta = new Vector2(120f, 50f);
        HorizontalLayoutGroup layout = digits.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = -2f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return new DigitStrip(digits, digitSprites, slashSprite);
    }

    private void RefreshValues()
    {
        waveValue.SetValue($"{controller.CurrentWaveDisplay}/{controller.TotalWaves}");
        enemyValue.SetValue(controller.ActiveEnemies.Count.ToString());
        lifeValue.SetValue(controller.BaseLives.ToString());
        defeatedValue.SetValue(controller.Defeated.ToString());
        if (coinValue != null && buildController != null) coinValue.SetValue(buildController.Coins.ToString());
    }

    private void RefreshSafeArea()
    {
        lastSafeArea = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
    }

    private static Sprite CreateSlashSprite()
    {
        const int size = 64;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "HudClaySlash";
        texture.wrapMode = TextureWrapMode.Clamp;
        Color clear = Color.clear;
        Color shadow = new(0.34f, 0.14f, 0.045f, 1f);
        Color face = new(1f, 0.70f, 0.22f, 1f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float line = Mathf.Abs((x + y) - 63f);
            texture.SetPixel(x, y, line < 7f ? face : line < 11f && x > 4 && y < 59 ? shadow : clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private sealed class DigitStrip
    {
        private readonly RectTransform root;
        private readonly Sprite[] digits;
        private readonly Sprite slash;
        private readonly List<GameObject> glyphs = new();
        private string currentValue;

        public DigitStrip(RectTransform root, Sprite[] digits, Sprite slash)
        {
            this.root = root;
            this.digits = digits;
            this.slash = slash;
        }

        public void SetValue(string value)
        {
            if (value == currentValue) return;
            currentValue = value;
            foreach (GameObject glyph in glyphs) Object.Destroy(glyph);
            glyphs.Clear();

            foreach (char character in value)
            {
                Sprite sprite = character == '/' ? slash :
                    char.IsDigit(character) ? digits[character - '0'] : null;
                if (sprite == null) continue;
                GameObject glyph = new($"Glyph_{character}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                glyph.transform.SetParent(root, false);
                RectTransform glyphRect = glyph.GetComponent<RectTransform>();
                glyphRect.sizeDelta = character == '/' ? new Vector2(16f, 38f) : new Vector2(42f, 48f);
                LayoutElement element = glyph.GetComponent<LayoutElement>();
                element.preferredWidth = character == '/' ? 16f : 42f;
                element.preferredHeight = character == '/' ? 38f : 48f;
                Image image = glyph.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                glyphs.Add(glyph);
            }
        }
    }
}
