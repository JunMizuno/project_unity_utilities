using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugScoreDisplay : MonoBehaviour
{
    public static DebugScoreDisplay Instance { get; private set; }

    [SerializeField]
    private string scorePrefix = "SCORE:";

    [SerializeField]
    private Vector2 anchoredPosition = new Vector2(8.0f, -8.0f);

    [SerializeField]
    private Vector2 displaySize = new Vector2(320.0f, 64.0f);

    [SerializeField]
    private float fontSize = 44.0f;

    [SerializeField]
    private Color textColor = Color.white;

    private TextMeshProUGUI scoreText;

    private int currentScore;

    /// <summary>
    /// Registers this display and builds the score UI.
    /// この表示を登録し、スコアUIを構築します。
    /// </summary>
    private void Awake()
    {
        Instance = this;
        CreateScoreDisplay();
        SetScore(currentScore);
    }

    /// <summary>
    /// Clears the global reference when this display is destroyed.
    /// この表示が破棄されたときにグローバル参照を解除します。
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Updates the score value shown on the UI.
    /// UIに表示するスコア値を更新します。
    /// </summary>
    public void SetScore(int score)
    {
        currentScore = Mathf.Max(0, score);
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = $"{scorePrefix}{currentScore}";
    }

    /// <summary>
    /// Creates the canvas and text hierarchy for the score display.
    /// スコア表示用のCanvasとテキスト階層を作成します。
    /// </summary>
    private void CreateScoreDisplay()
    {
        var canvasObject = new GameObject("DebugScoreCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.layer = GetUiLayer();

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        var textObject = new GameObject("ScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        textObject.layer = canvasObject.layer;

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.0f, 1.0f);
        textRect.anchorMax = new Vector2(0.0f, 1.0f);
        textRect.pivot = new Vector2(0.0f, 1.0f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = displaySize;

        scoreText = textObject.GetComponent<TextMeshProUGUI>();
        scoreText.color = textColor;
        scoreText.fontSize = fontSize;
        scoreText.alignment = TextAlignmentOptions.Left;
        scoreText.raycastTarget = false;
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
