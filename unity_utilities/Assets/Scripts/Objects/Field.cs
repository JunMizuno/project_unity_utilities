using UnityEngine;
using R3;

public enum FieldMovePattern
{
    None,
    ForwardBack,
    LeftRight,
    UpDown,
    ForwardBackLeftRight,
    ForwardBackUpDown,
    LeftRightUpDown,
    AllAxes,
    SequentialAxes,
}

public class Field : MonoBehaviour
{
    [SerializeField]
    private FieldMovePattern movePattern = FieldMovePattern.SequentialAxes;

    // Maximum movement distance from the initial local position on each active axis.
    // Increase to move the field farther. Decrease to keep the field movement subtle.
    // 有効な各軸で初期ローカル座標から移動する最大距離です。
    // 上げるとフィールドの移動幅が大きくなり、下げると控えめになります。
    [SerializeField]
    private float moveDistance = 3.5f;

    // Seconds used to move from zero to one side, back to zero, to the other side, and back again.
    // Increase to make each movement slower. Decrease to make it faster.
    // ゼロから片側、ゼロ、反対側、ゼロへ戻るまでに使う秒数です。
    // 上げると各移動が遅くなり、下げると速くなります。
    [SerializeField]
    private float axisLoopSeconds = 4.0f;

    private Vector3 initialLocalPosition;

    private float elapsedSeconds;

    /// <summary>
    /// Stores the initial field position.
    /// フィールドの初期位置を保持します。
    /// </summary>
    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    /// <summary>
    /// Starts field movement updates by using R3.
    /// R3を使用してフィールド移動の更新を開始します。
    /// </summary>
    private void Start()
    {
        Observable.EveryUpdate()
            .Subscribe(_ => UpdateFieldMovement())
            .AddTo(this);
    }

    /// <summary>
    /// Sets the field movement pattern.
    /// フィールドの移動パターンを設定します。
    /// </summary>
    public void SetMovePattern(FieldMovePattern pattern)
    {
        movePattern = pattern;
    }

    /// <summary>
    /// Sets the maximum movement distance from the initial local position.
    /// 初期ローカル座標からの最大移動距離を設定します。
    /// </summary>
    public void SetMoveDistance(float distance)
    {
        moveDistance = Mathf.Max(0.0f, distance);
    }

    /// <summary>
    /// Sets the seconds used by one full axis movement loop.
    /// 1軸分の移動ループに使う秒数を設定します。
    /// </summary>
    public void SetAxisLoopSeconds(float seconds)
    {
        axisLoopSeconds = Mathf.Max(0.01f, seconds);
    }

    /// <summary>
    /// Updates the field local position from the selected movement pattern.
    /// 選択された移動パターンからフィールドのローカル座標を更新します。
    /// </summary>
    private void UpdateFieldMovement()
    {
        elapsedSeconds += Time.deltaTime;
        transform.localPosition = initialLocalPosition + CalculateMoveOffset();
    }

    /// <summary>
    /// Calculates the movement offset for the selected pattern.
    /// 選択されたパターンの移動オフセットを計算します。
    /// </summary>
    private Vector3 CalculateMoveOffset()
    {
        switch (movePattern)
        {
            case FieldMovePattern.ForwardBack:
                return new Vector3(0.0f, 0.0f, CalculateLoopOffset(elapsedSeconds));
            case FieldMovePattern.LeftRight:
                return new Vector3(CalculateLoopOffset(elapsedSeconds), 0.0f, 0.0f);
            case FieldMovePattern.UpDown:
                return new Vector3(0.0f, CalculateLoopOffset(elapsedSeconds), 0.0f);
            case FieldMovePattern.ForwardBackLeftRight:
                return new Vector3(CalculateLoopOffset(elapsedSeconds), 0.0f, CalculateLoopOffset(elapsedSeconds));
            case FieldMovePattern.ForwardBackUpDown:
                return new Vector3(0.0f, CalculateLoopOffset(elapsedSeconds), CalculateLoopOffset(elapsedSeconds));
            case FieldMovePattern.LeftRightUpDown:
                return new Vector3(CalculateLoopOffset(elapsedSeconds), CalculateLoopOffset(elapsedSeconds), 0.0f);
            case FieldMovePattern.AllAxes:
                return new Vector3(CalculateLoopOffset(elapsedSeconds), CalculateLoopOffset(elapsedSeconds), CalculateLoopOffset(elapsedSeconds));
            case FieldMovePattern.SequentialAxes:
                return CalculateSequentialMoveOffset();
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// Calculates the sequential forward-back, left-right, and up-down movement offset.
    /// 前後、左右、上下の順に移動するオフセットを計算します。
    /// </summary>
    private Vector3 CalculateSequentialMoveOffset()
    {
        var safeLoopSeconds = Mathf.Max(axisLoopSeconds, 0.01f);
        var totalSeconds = safeLoopSeconds * 3.0f;
        var sequenceTime = Mathf.Repeat(elapsedSeconds, totalSeconds);
        var axisIndex = Mathf.FloorToInt(sequenceTime / safeLoopSeconds);
        var axisTime = sequenceTime - safeLoopSeconds * axisIndex;
        var offset = CalculateLoopOffset(axisTime);

        switch (axisIndex)
        {
            case 0:
                return new Vector3(0.0f, 0.0f, offset);
            case 1:
                return new Vector3(offset, 0.0f, 0.0f);
            default:
                return new Vector3(0.0f, offset, 0.0f);
        }
    }

    /// <summary>
    /// Calculates eased movement from zero to negative, zero, positive, and back to zero.
    /// ゼロからマイナス、ゼロ、プラス、ゼロへ戻るイージング移動を計算します。
    /// </summary>
    private float CalculateLoopOffset(float time)
    {
        var safeLoopSeconds = Mathf.Max(axisLoopSeconds, 0.01f);
        var loopRate = Mathf.Repeat(time / safeLoopSeconds, 1.0f);
        if (loopRate < 0.25f)
        {
            return Mathf.Lerp(0.0f, -moveDistance, SmoothStep(loopRate / 0.25f));
        }

        if (loopRate < 0.5f)
        {
            return Mathf.Lerp(-moveDistance, 0.0f, SmoothStep((loopRate - 0.25f) / 0.25f));
        }

        if (loopRate < 0.75f)
        {
            return Mathf.Lerp(0.0f, moveDistance, SmoothStep((loopRate - 0.5f) / 0.25f));
        }

        return Mathf.Lerp(moveDistance, 0.0f, SmoothStep((loopRate - 0.75f) / 0.25f));
    }

    /// <summary>
    /// Returns a smooth easing value for movement interpolation.
    /// 移動補間用の滑らかなイージング値を返します。
    /// </summary>
    private float SmoothStep(float value)
    {
        var clampedValue = Mathf.Clamp01(value);
        return clampedValue * clampedValue * (3.0f - 2.0f * clampedValue);
    }
}
