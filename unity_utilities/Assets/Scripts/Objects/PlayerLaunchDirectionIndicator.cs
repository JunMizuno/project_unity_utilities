using UnityEngine;
using UnityEngine.UI;

public class PlayerLaunchDirectionIndicator : MonoBehaviour
{
    [SerializeField]
    private Vector2 panelAnchoredPosition = new Vector2(-120.0f, -88.0f);

    [SerializeField]
    private Vector2 panelSize = new Vector2(180.0f, 150.0f);

    [SerializeField]
    private Vector2 directionLineSize = new Vector2(10.0f, 92.0f);

    [SerializeField]
    private Vector2 pitchGaugeSize = new Vector2(10.0f, 88.0f);

    [SerializeField]
    private Color indicatorColor = new Color(0.95f, 0.95f, 0.35f, 1.0f);

    [SerializeField]
    private Color baseLineColor = new Color(1.0f, 1.0f, 1.0f, 0.45f);

    [SerializeField]
    private Color textColor = Color.white;

    [SerializeField]
    private int angleTextFontSize = 16;

    [SerializeField]
    private float minVerticalDisplayAngle = -45.0f;

    [SerializeField]
    private float maxVerticalDisplayAngle = 45.0f;

    private RectTransform directionLineRect;

    private RectTransform pitchKnobRect;

    private Text angleText;

    /// <summary>
    /// Creates the screen-space launch direction indicator.
    /// スクリーン空間の発射方向インジケーターを生成します。
    /// </summary>
    void Awake()
    {
        CreateIndicator();
    }

    /// <summary>
    /// Applies the selected launch angles to the 2D indicator.
    /// 選択中の発射角度を2Dインジケーターに反映します。
    /// </summary>
    public void SetAngles(float verticalAngle, float horizontalAngle)
    {
        if (directionLineRect != null)
        {
            directionLineRect.localRotation = Quaternion.Euler(0.0f, 0.0f, -horizontalAngle);
        }

        if (pitchKnobRect != null)
        {
            var pitchRate = Mathf.InverseLerp(minVerticalDisplayAngle, maxVerticalDisplayAngle, verticalAngle);
            var yPosition = Mathf.Lerp(-pitchGaugeSize.y * 0.5f, pitchGaugeSize.y * 0.5f, pitchRate);
            pitchKnobRect.anchoredPosition = new Vector2(0.0f, yPosition);
        }
    }

    /// <summary>
    /// Updates the debug text for the selected launch angles.
    /// 選択中の発射角度のデバッグ表示を更新します。
    /// </summary>
    public void SetAngleText(float verticalAngle, float horizontalAngle)
    {
        if (angleText == null)
        {
            return;
        }

        angleText.text = $"X: {verticalAngle:0.0}°\nY: {horizontalAngle:0.0}°";
    }

    /// <summary>
    /// Creates the runtime UI objects used as the launch direction indicator.
    /// 発射方向インジケーターとして使用する実行時UIオブジェクトを生成します。
    /// </summary>
    private void CreateIndicator()
    {
        var canvasObject = new GameObject("LaunchDirectionIndicatorCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        var panelRect = CreateRect("LaunchDirectionIndicatorPanel", canvasObject.transform);
        panelRect.anchorMin = new Vector2(1.0f, 1.0f);
        panelRect.anchorMax = new Vector2(1.0f, 1.0f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = panelAnchoredPosition;
        panelRect.sizeDelta = panelSize;

        CreateTopDownIndicator(panelRect);
        CreatePitchGauge(panelRect);
        CreateAngleText(panelRect);

        SetAngles(0.0f, 0.0f);
        SetAngleText(0.0f, 0.0f);
    }

    /// <summary>
    /// Creates the top-down horizontal direction display.
    /// 上から見た左右方向の表示を生成します。
    /// </summary>
    private void CreateTopDownIndicator(RectTransform panelRect)
    {
        var baseLineRect = CreateImage("BaseDirectionLine", panelRect, baseLineColor);
        baseLineRect.anchorMin = new Vector2(0.42f, 0.48f);
        baseLineRect.anchorMax = new Vector2(0.42f, 0.48f);
        baseLineRect.pivot = new Vector2(0.5f, 0.0f);
        baseLineRect.anchoredPosition = Vector2.zero;
        baseLineRect.sizeDelta = directionLineSize;

        directionLineRect = CreateImage("LaunchDirectionLine", panelRect, indicatorColor);
        directionLineRect.anchorMin = new Vector2(0.42f, 0.48f);
        directionLineRect.anchorMax = new Vector2(0.42f, 0.48f);
        directionLineRect.pivot = new Vector2(0.5f, 0.0f);
        directionLineRect.anchoredPosition = Vector2.zero;
        directionLineRect.sizeDelta = directionLineSize;

        var centerDotRect = CreateImage("LaunchDirectionCenter", panelRect, indicatorColor);
        centerDotRect.anchorMin = new Vector2(0.42f, 0.48f);
        centerDotRect.anchorMax = new Vector2(0.42f, 0.48f);
        centerDotRect.pivot = new Vector2(0.5f, 0.5f);
        centerDotRect.anchoredPosition = Vector2.zero;
        centerDotRect.sizeDelta = new Vector2(18.0f, 18.0f);
    }

    /// <summary>
    /// Creates the vertical pitch gauge display.
    /// 上下角度のゲージ表示を生成します。
    /// </summary>
    private void CreatePitchGauge(RectTransform panelRect)
    {
        var gaugeBackRect = CreateImage("PitchGaugeBack", panelRect, baseLineColor);
        gaugeBackRect.anchorMin = new Vector2(0.78f, 0.48f);
        gaugeBackRect.anchorMax = new Vector2(0.78f, 0.48f);
        gaugeBackRect.pivot = new Vector2(0.5f, 0.5f);
        gaugeBackRect.anchoredPosition = Vector2.zero;
        gaugeBackRect.sizeDelta = pitchGaugeSize;

        var zeroLineRect = CreateImage("PitchGaugeZeroLine", panelRect, baseLineColor);
        zeroLineRect.anchorMin = new Vector2(0.78f, 0.48f);
        zeroLineRect.anchorMax = new Vector2(0.78f, 0.48f);
        zeroLineRect.pivot = new Vector2(0.5f, 0.5f);
        zeroLineRect.anchoredPosition = Vector2.zero;
        zeroLineRect.sizeDelta = new Vector2(34.0f, 4.0f);

        pitchKnobRect = CreateImage("PitchGaugeKnob", panelRect, indicatorColor);
        pitchKnobRect.anchorMin = new Vector2(0.78f, 0.48f);
        pitchKnobRect.anchorMax = new Vector2(0.78f, 0.48f);
        pitchKnobRect.pivot = new Vector2(0.5f, 0.5f);
        pitchKnobRect.anchoredPosition = Vector2.zero;
        pitchKnobRect.sizeDelta = new Vector2(28.0f, 12.0f);
    }

    /// <summary>
    /// Creates the angle text under the 2D indicator.
    /// 2Dインジケーター下側に角度表示テキストを生成します。
    /// </summary>
    private void CreateAngleText(RectTransform panelRect)
    {
        var textRect = CreateRect("LaunchAngleDebugText", panelRect);
        textRect.anchorMin = new Vector2(0.5f, 0.0f);
        textRect.anchorMax = new Vector2(0.5f, 0.0f);
        textRect.pivot = new Vector2(0.5f, 0.0f);
        textRect.anchoredPosition = new Vector2(0.0f, 0.0f);
        textRect.sizeDelta = new Vector2(panelSize.x, 44.0f);

        angleText = textRect.gameObject.AddComponent<Text>();
        angleText.alignment = TextAnchor.MiddleCenter;
        angleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        angleText.fontSize = angleTextFontSize;
        angleText.color = textColor;
    }

    /// <summary>
    /// Creates a RectTransform child object.
    /// RectTransformを持つ子オブジェクトを生成します。
    /// </summary>
    private RectTransform CreateRect(string objectName, Transform parent)
    {
        var gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }

    /// <summary>
    /// Creates a colored UI image child object.
    /// 色付きUI画像の子オブジェクトを生成します。
    /// </summary>
    private RectTransform CreateImage(string objectName, Transform parent, Color color)
    {
        var rectTransform = CreateRect(objectName, parent);
        var image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }
}
