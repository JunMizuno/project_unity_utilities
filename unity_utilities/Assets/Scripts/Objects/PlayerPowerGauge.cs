using UnityEngine;
using UnityEngine.UI;
using R3;

public class PlayerPowerGauge : MonoBehaviour
{
    [SerializeField]
    private float gaugeCycleSeconds = 1.5f;

    [SerializeField]
    private Vector2 gaugeSize = new Vector2(28.0f, 240.0f);

    // Adds a secondary wave to make the gauge motion less regular.
    // Increase to make the gauge more unpredictable. Decrease to make it closer to a simple sine wave.
    // ゲージの動きを不規則にするための補助波の強さです。
    // 上げると動きが読みづらくなり、下げると単純なサイン波に近づきます。
    [SerializeField]
    private float irregularWaveAmount = 0.12f;

    // Frequency multiplier for the secondary wave.
    // Increase to change speed more often. Decrease to make the irregular motion broader.
    // 補助波の周波数倍率です。
    // 上げると速度変化が細かくなり、下げると大きなうねりになります。
    [SerializeField]
    private float irregularWaveMultiplier = 2.7f;

    // Slows the gauge more strongly near the minimum value.
    // Increase to make the gauge linger near low power. Decrease to make low power pass faster.
    // ゲージが最小値付近で遅くなる強さです。
    // 上げると低威力付近で粘り、下げると低威力を速く通過します。
    [SerializeField]
    private float minimumSlowdownPower = 2.2f;

    private RectTransform fillRectTransform;
    private float elapsedTime;
    private GameObject canvasObject;

    public float PowerRate { get; private set; } = 0.5f;

    /// <summary>
    /// Creates the launch power gauge UI.
    /// 発射パワーゲージのUIを生成します。
    /// </summary>
    void Start()
    {
        CreateGaugeUI();
        SetVisible(false);
        SetUpdateGauge();
    }

    /// <summary>
    /// Creates the gauge canvas and fill image at the left edge of the screen.
    /// 画面左端にゲージ用Canvasと塗りつぶしImageを生成します。
    /// </summary>
    private void CreateGaugeUI()
    {
        canvasObject = new GameObject("PowerGaugeCanvas");
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
    /// Updates the gauge value with irregular sine-based motion by using R3.
    /// R3を使用して、不規則なサイン波ベースの動きでゲージ値を更新します。
    /// </summary>
    private void SetUpdateGauge()
    {
        Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                elapsedTime += Time.deltaTime;
                PowerRate = CalculatePowerRate();

                if (fillRectTransform != null)
                {
                    fillRectTransform.sizeDelta = new Vector2(0.0f, gaugeSize.y * PowerRate);
                }
            })
            .AddTo(this);
    }

    /// <summary>
    /// Calculates the current power rate from a sine wave with secondary modulation.
    /// 補助波を混ぜたサイン波から現在のパワー割合を計算します。
    /// </summary>
    private float CalculatePowerRate()
    {
        var cyclePosition = Mathf.Repeat(elapsedTime / Mathf.Max(gaugeCycleSeconds, 0.01f), 1.0f);
        var primaryWave = (Mathf.Sin(cyclePosition * Mathf.PI * 2.0f - Mathf.PI * 0.5f) + 1.0f) * 0.5f;
        var secondaryWave = Mathf.Sin(cyclePosition * Mathf.PI * 2.0f * irregularWaveMultiplier) * irregularWaveAmount;
        var centerWeightedWave = primaryWave + secondaryWave * Mathf.Sin(primaryWave * Mathf.PI);
        return Mathf.Pow(Mathf.Clamp01(centerWeightedWave), Mathf.Max(minimumSlowdownPower, 0.01f));
    }

    /// <summary>
    /// Changes whether the launch power gauge is visible.
    /// 発射パワーゲージの表示状態を切り替えます。
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (canvasObject != null)
        {
            canvasObject.SetActive(visible);
        }
    }
}
