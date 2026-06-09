using UnityEngine;
using R3;

public class PlayerPowerGauge : MonoBehaviour
{
    [SerializeField]
    private float gaugeCycleSeconds = 1.5f;

    [SerializeField]
    private Canvas gaugeCanvas;

    [SerializeField]
    private RectTransform gaugeBackgroundRectTransform;

    [SerializeField]
    private RectTransform fillRectTransform;

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

    private float elapsedTime;

    public float PowerRate { get; private set; } = 0.5f;

    /// <summary>
    /// Hides the gauge until the player enters launch-ready state.
    /// プレイヤーが発射準備状態になるまでゲージを非表示にします。
    /// </summary>
    void Awake()
    {
        SetVisible(false);
    }

    /// <summary>
    /// Initializes the launch power gauge UI.
    /// 発射パワーゲージUIを初期化します。
    /// </summary>
    void Start()
    {
        SetUpdateGauge();
        UpdateGaugeFill();
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
                UpdateGaugeFill();
            })
            .AddTo(this);
    }

    /// <summary>
    /// Changes the fill height to match the current power rate.
    /// 現在のパワー割合に合わせてゲージの塗りつぶし高さを更新します。
    /// </summary>
    private void UpdateGaugeFill()
    {
        if (fillRectTransform == null || gaugeBackgroundRectTransform == null)
        {
            return;
        }

        var fillSize = fillRectTransform.sizeDelta;
        fillSize.y = gaugeBackgroundRectTransform.sizeDelta.y * PowerRate;
        fillRectTransform.sizeDelta = fillSize;
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
        if (gaugeCanvas != null)
        {
            gaugeCanvas.enabled = visible;
        }
    }
}
