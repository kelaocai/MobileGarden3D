using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MobileMobaControls : MonoBehaviour
{
    public static Vector2 MoveInput { get; private set; }
    private static Sprite circleSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<MobileMobaControls>() != null) return;

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        GameObject root = new("Mobile MOBA Controls");
        root.AddComponent<MobileMobaControls>().BuildUi();
    }

    private void OnDisable() => MoveInput = Vector2.zero;

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1170f, 2532f);
        scaler.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform joystickBase = CreateImage("Movement Joystick", transform, LoadUiSprite("UI/joystick_base"));
        Anchor(joystickBase, new Vector2(0f, 0f), new Vector2(255f, 285f), new Vector2(360f, 360f));
        RectTransform handle = CreateImage("Handle", joystickBase, LoadUiSprite("UI/joystick_handle"));
        handle.sizeDelta = new Vector2(175f, 175f);
        VirtualJoystick joystick = joystickBase.gameObject.AddComponent<VirtualJoystick>();
        joystick.Initialize(handle, value => MoveInput = value);

        RectTransform attack = CreateImage("Attack Button", transform, LoadUiSprite("UI/attack_button"));
        Anchor(attack, new Vector2(1f, 0f), new Vector2(-225f, 280f), new Vector2(270f, 270f));
        Button button = attack.gameObject.AddComponent<Button>();
        button.targetGraphic = attack.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        button.onClick.AddListener(Attack);
    }

    private static void Attack()
    {
        KnightClickMover knight = FindAnyObjectByType<KnightClickMover>();
        if (knight != null) knight.Attack();
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(Image));
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.sprite = sprite != null ? sprite : GetCircleSprite();
        image.preserveAspect = true;
        return item.GetComponent<RectTransform>();
    }

    private static Sprite LoadUiSprite(string path) => Resources.Load<Sprite>(path);

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 128;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime UI Circle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.49f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float edge = radius - Vector2.Distance(new Vector2(x, y), center);
                byte alpha = (byte)(Mathf.Clamp01(edge + 0.5f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
        circleSprite.name = "Runtime UI Circle";
        return circleSprite;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RectTransform area;
    private RectTransform handle;
    private System.Action<Vector2> onValueChanged;
    private float radius;

    public void Initialize(RectTransform joystickHandle, System.Action<Vector2> callback)
    {
        area = (RectTransform)transform;
        handle = joystickHandle;
        onValueChanged = callback;
        radius = area.sizeDelta.x * 0.32f;
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateValue(eventData);
    public void OnDrag(PointerEventData eventData) => UpdateValue(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        onValueChanged?.Invoke(Vector2.zero);
    }

    private void UpdateValue(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
        Vector2 value = Vector2.ClampMagnitude(local / radius, 1f);
        handle.anchoredPosition = value * radius;
        onValueChanged?.Invoke(value);
    }
}
