using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 第一关的“重新开始”按钮：运行时创建 Screen Space Canvas + EventSystem，
// 使用 Resources/UI/attack_button.png 作为按钮底图，点击后重新加载当前关卡。
public sealed class Level01RestartButton : MonoBehaviour
{
    private const string SpritePath = "UI/attack_button";
    private bool restarting;

    private void Start()
    {
        EnsureEventSystem();
        BuildButton();
    }

    private void BuildButton()
    {
        var canvasGo = new GameObject("RestartCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var buttonGo = new GameObject("RestartButton");
        buttonGo.transform.SetParent(canvasGo.transform, false);
        var rect = buttonGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(72f, 72f);

        var image = buttonGo.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>(SpritePath);
        image.preserveAspect = true;

        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        button.onClick.AddListener(Restart);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(buttonGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -3f);
        labelRect.sizeDelta = new Vector2(88f, 26f);

        var text = labelGo.AddComponent<Text>();
        text.text = "重新开始";
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 18;
        text.color = Color.white;
        text.font = LoadUiFont();
        var shadow = labelGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        if (image.sprite == null)
        {
            Debug.LogError("重新开始按钮缺少贴图：Resources/" + SpritePath);
        }
    }

    private void Restart()
    {
        if (restarting) return;
        restarting = true;
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        int buildIndex = scene.buildIndex >= 0 ? scene.buildIndex : 0;
        SceneManager.sceneLoaded -= OnSceneReloaded;
        SceneManager.sceneLoaded += OnSceneReloaded;
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    private static void OnSceneReloaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneReloaded;
        PolarBearWander.InstallOnScenePolars();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static Font LoadUiFont()
    {
        string[] candidates = { "PingFang SC", "Heiti SC", "Hiragino Sans GB", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" };
        Font font = Font.CreateDynamicFontFromOSFont(candidates, 34);
        if (font != null) return font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
