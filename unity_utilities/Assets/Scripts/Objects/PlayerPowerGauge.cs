using UnityEngine;
using UnityEngine.UI;
using R3;

public class PlayerPowerGauge : MonoBehaviour
{
    [SerializeField]
    private float gaugeCycleSeconds = 1.5f;

    [SerializeField]
    private Vector2 gaugeSize = new Vector2(28.0f, 240.0f);

    private RectTransform fillRectTransform;
    private float elapsedTime;

    public float PowerRate { get; private set; } = 0.5f;

    /// <summary>
    /// Creates the launch power gauge UI.
    /// 発射パワーゲージのUIを生成します。
    /// </summary>
    void Start()
    {
        CreateGaugeUI();
        SetUpdateGauge();
    }

    /// <summary>
    /// Creates the gauge canvas and fill image at the left edge of the screen.
    /// 画面左端にゲージ用Canvasと塗りつぶしImageを生成します。
    /// </summary>
    private void CreateGaugeUI()
    {
        var canvasObject = new GameObject("PowerGaugeCanvas");
        canvasObject.transform.SetParent(this.transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var backgroundObject = new GameObject("PowerGaugeBackground");
        backgroundObject.transform.SetParent(canvasObject.transform, false);

        var backgroundRectTransform = backgroundObject.AddComponent<RectTransform>();
        backgroundRectTransform.anchorMin = new Vector2(0.0f, 0.5f);
        backgroundRectTransform.anchorMax = new Vector2(0.0f, 0.5f);
        backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);
        backgroundRectTransform.anchoredPosition = new Vector2(32.0f, 0.0f);
        backgroundRectTransform.sizeDelta = gaugeSize;

        var backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.05f, 0.05f, 0.05f, 0.75f);

        var fillObject = new GameObject("PowerGaugeFill");
        fillObject.transform.SetParent(backgroundObject.transform, false);

        fillRectTransform = fillObject.AddComponent<RectTransform>();
        fillRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
        fillRectTransform.anchorMax = new Vector2(1.0f, 0.0f);
        fillRectTransform.pivot = new Vector2(0.5f, 0.0f);
        fillRectTransform.anchoredPosition = Vector2.zero;
        fillRectTransform.sizeDelta = new Vector2(0.0f, gaugeSize.y * PowerRate);

        var fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.9f, 0.35f, 0.95f);
    }

    /// <summary>
    /// Updates the gauge value with eased ping-pong motion by using R3.
    /// R3を使用して、イージングされた往復運動でゲージ値を更新します。
    /// </summary>
    private void SetUpdateGauge()
    {
        Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                elapsedTime += Time.deltaTime;
                var phase = Mathf.PingPong(elapsedTime / gaugeCycleSeconds, 1.0f);
                PowerRate = Mathf.SmoothStep(0.0f, 1.0f, phase);

                if (fillRectTransform != null)
                {
                    fillRectTransform.sizeDelta = new Vector2(0.0f, gaugeSize.y * PowerRate);
                }
            })
            .AddTo(this);
    }
}
