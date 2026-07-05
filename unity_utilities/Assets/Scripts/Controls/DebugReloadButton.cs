using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DebugReloadButton : MonoBehaviour
{
    [SerializeField]
    private string buttonText = "RELOAD";

    [SerializeField]
    private Vector2 anchoredPosition = new Vector2(8.0f, -84.0f);

    [SerializeField]
    private Vector2 buttonSize = new Vector2(118.0f, 44.0f);

    [SerializeField]
    private float fontSize = 22.0f;

    [SerializeField]
    private Color buttonColor = new Color(0.08f, 0.08f, 0.08f, 0.72f);

    [SerializeField]
    private Color buttonPressedColor = new Color(0.18f, 0.18f, 0.18f, 0.86f);

    [SerializeField]
    private Color textColor = Color.white;

    /// <summary>
    /// Builds the debug reload button at runtime.
    /// 実行時にデバッグ用リロードボタンを構築します。
    /// </summary>
    private void Awake()
    {
        CreateReloadButton();
    }

    /// <summary>
    /// Creates the canvas and button hierarchy for the reload action.
    /// リロード操作用のCanvasとボタン階層を作成します。
    /// </summary>
    private void CreateReloadButton()
    {
        var canvasObject = new GameObject("DebugReloadCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.layer = GetUiLayer();

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        var buttonObject = new GameObject("ReloadButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        buttonObject.layer = canvasObject.layer;

        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.0f, 1.0f);
        buttonRect.anchorMax = new Vector2(0.0f, 1.0f);
        buttonRect.pivot = new Vector2(0.0f, 1.0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = buttonSize;

        var image = buttonObject.GetComponent<Image>();
        image.color = buttonColor;

        var button = buttonObject.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(ReloadStage);

        CreateButtonText(buttonObject.transform);
    }

    /// <summary>
    /// Creates the visible text for the reload button.
    /// リロードボタンに表示するテキストを作成します。
    /// </summary>
    private void CreateButtonText(Transform parent)
    {
        var textObject = new GameObject("ReloadButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        textObject.layer = parent.gameObject.layer;

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = buttonText;
        text.color = textColor;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    /// <summary>
    /// Reloads the current stage from the beginning.
    /// 現在のステージを最初から読み込み直します。
    /// </summary>
    private void ReloadStage()
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.ReloadGameMainScene();
            return;
        }

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    /// <summary>
    /// Returns the UI layer index when it exists.
    /// UIレイヤーが存在する場合にそのレイヤー番号を返します。
    /// </summary>
    private int GetUiLayer()
    {
        var uiLayer = LayerMask.NameToLayer("UI");
        return uiLayer >= 0 ? uiLayer : gameObject.layer;
    }
}
