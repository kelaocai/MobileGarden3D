using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class FirePigHealthBar : MonoBehaviour
{
    private const float BarWidth = 0.9f;
    private const float BarHeight = 0.11f;

    private FirePigEnemy enemy;
    private Transform barRoot;
    private Transform fill;
    private SpriteRenderer fillRenderer;
    private Camera mainCamera;
    private Vector3 followVelocity;
    private bool positioned;

    public void Initialize(FirePigEnemy owner)
    {
        enemy = owner;
        mainCamera = Camera.main;
        BuildBar();
        Refresh();
    }

    private void BuildBar()
    {
        barRoot = new GameObject("Health Bar").transform;
        barRoot.position = transform.position + Vector3.up * 1.25f;

        CreatePart("Background", barRoot, new Color(0.08f, 0.07f, 0.07f, 0.9f),
            new Vector3(BarWidth + 0.08f, BarHeight + 0.07f, 1f), 20);
        fill = CreatePart("Fill", barRoot, new Color(0.2f, 0.9f, 0.25f, 1f),
            new Vector3(BarWidth, BarHeight, 1f), 21);
        fillRenderer = fill.GetComponent<SpriteRenderer>();
        fill.localPosition = new Vector3(0f, 0f, -0.01f);
    }

    private static Transform CreatePart(string name, Transform parent, Color color, Vector3 scale, int order)
    {
        GameObject part = new(name);
        part.transform.SetParent(parent, false);
        part.transform.localScale = scale;
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeWhiteSprite.Sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return part.transform;
    }

    private void LateUpdate()
    {
        if (enemy == null || barRoot == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 targetPosition = transform.position + Vector3.up * 1.25f;
        if (!positioned)
        {
            barRoot.position = targetPosition;
            positioned = true;
        }
        else
        {
            barRoot.position = Vector3.SmoothDamp(
                barRoot.position,
                targetPosition,
                ref followVelocity,
                0.035f,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }
        if (mainCamera != null) barRoot.rotation = mainCamera.transform.rotation;
        Refresh();
    }

    private void OnDestroy()
    {
        if (barRoot != null) Destroy(barRoot.gameObject);
    }

    private void Refresh()
    {
        float ratio = enemy == null || enemy.MaxHealth <= 0f ? 0f : Mathf.Clamp01(enemy.Health / enemy.MaxHealth);
        if (barRoot != null) barRoot.gameObject.SetActive(ratio > 0f);
        if (fill == null) return;

        fill.localScale = new Vector3(BarWidth * ratio, BarHeight, 1f);
        fill.localPosition = new Vector3(-BarWidth * (1f - ratio) * 0.5f, 0f, -0.01f);
        fillRenderer.color = ratio > 0.6f
            ? new Color(0.2f, 0.9f, 0.25f)
            : ratio > 0.3f ? new Color(1f, 0.75f, 0.12f) : new Color(0.95f, 0.18f, 0.12f);
    }

    private static class RuntimeWhiteSprite
    {
        private static Sprite sprite;
        public static Sprite Sprite
        {
            get
            {
                if (sprite != null) return sprite;
                Texture2D texture = new(4, 4, TextureFormat.RGBA32, false)
                {
                    name = "Runtime Health Bar White",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                return sprite;
            }
        }
    }
}
