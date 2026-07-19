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
    public static readonly Subject<FieldMovementState> MovementChangedSubject = new Subject<FieldMovementState>();

    [SerializeField]
    private FieldMovePattern movePattern = FieldMovePattern.SequentialAxes;

    // Maximum movement distance from the initial local position on the X and Z axes.
    // Increase to move the field farther forward/back or left/right. Decrease to keep horizontal movement subtle.
    // X軸とZ軸で初期ローカル座標から移動する最大距離です。
    // 上げると前後左右の移動幅が大きくなり、下げると水平移動が控えめになります。
    [SerializeField]
    private float horizontalMoveDistance = 6.0f;

    // Maximum movement distance from the initial local position on the Y axis.
    // Increase to move the field higher/lower. Decrease to keep vertical movement subtle.
    // Y軸で初期ローカル座標から移動する最大距離です。
    // 上げると上下の移動幅が大きくなり、下げると垂直移動が控えめになります。
    [SerializeField]
    private float verticalMoveDistance = 3.0f;

    // Seconds used to move from zero to one side, back to zero, to the other side, and back again.
    // Increase to make each movement slower. Decrease to make it faster.
    // ゼロから片側、ゼロ、反対側、ゼロへ戻るまでに使う秒数です。
    // 上げると各移動が遅くなり、下げると速くなります。
    [SerializeField]
    private float axisLoopSeconds = 4.0f;

    // Runtime flag that controls whether field movement is active.
    // Keep false during setup presentation, true during launch-ready play, and false after block impact.
    // フィールド移動が有効かどうかを制御する実行時フラグです。
    // 配置演出中はfalse、発射準備中はtrue、ブロック衝突後はfalseにします。
    [SerializeField]
    private bool isMovementEnabled;

    private Vector3 initialLocalPosition;

    private Vector3 lastWorldPosition;

    private float elapsedSeconds;

    /// <summary>
    /// Stores the initial field position.
    /// フィールドの初期位置を保持します。
    /// </summary>
    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        lastWorldPosition = transform.position;
    }

    /// <summary>
    /// Starts field movement updates by using R3.
    /// R3を使用してフィールド移動の更新を開始します。
    /// </summary>
    private void Start()
    {
        SetMovementEvents();

        Observable.EveryUpdate()
            .Where(_ => isMovementEnabled)
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
        SetHorizontalMoveDistance(distance);
        SetVerticalMoveDistance(distance);
    }

    /// <summary>
    /// Sets the maximum horizontal movement distance from the initial local position.
    /// 初期ローカル座標からの水平最大移動距離を設定します。
    /// </summary>
    public void SetHorizontalMoveDistance(float distance)
    {
        horizontalMoveDistance = Mathf.Max(0.0f, distance);
    }

    /// <summary>
    /// Sets the maximum vertical movement distance from the initial local position.
    /// 初期ローカル座標からの垂直最大移動距離を設定します。
    /// </summary>
    public void SetVerticalMoveDistance(float distance)
    {
        verticalMoveDistance = Mathf.Max(0.0f, distance);
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
    /// Changes whether the field movement is active.
    /// フィールド移動が有効かどうかを切り替えます。
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        isMovementEnabled = enabled;
        lastWorldPosition = transform.position;
    }

    /// <summary>
    /// Subscribes to player state events that control field movement.
    /// フィールド移動を制御するプレイヤー状態イベントを購読します。
    /// </summary>
    private void SetMovementEvents()
    {
        Player.ReadyStateChangedSubject
            .Where(state => state.IsReady)
            .Subscribe(_ => SetMovementEnabled(true))
            .AddTo(this);

        Player.HitTargetSubject
            .Subscribe(_ => SetMovementEnabled(false))
            .AddTo(this);
    }

    /// <summary>
    /// Updates the field local position from the selected movement pattern.
    /// 選択された移動パターンからフィールドのローカル座標を更新します。
    /// </summary>
    private void UpdateFieldMovement()
    {
        elapsedSeconds += Time.deltaTime;
        transform.localPosition = initialLocalPosition + CalculateMoveOffset();
        PublishMovementDelta();
    }

    /// <summary>
    /// Publishes the field movement delta for objects that should follow the moving field.
    /// 動くフィールドに追従するべきオブジェクトへフィールドの移動差分を通知します。
    /// </summary>
    private void PublishMovementDelta()
    {
        var currentWorldPosition = transform.position;
        var worldDelta = currentWorldPosition - lastWorldPosition;
        lastWorldPosition = currentWorldPosition;

        if (worldDelta == Vector3.zero)
        {
            return;
        }

        MovementChangedSubject.OnNext(new FieldMovementState(this, worldDelta));
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
                return new Vector3(0.0f, 0.0f, CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance));
            case FieldMovePattern.LeftRight:
                return new Vector3(CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance), 0.0f, 0.0f);
            case FieldMovePattern.UpDown:
                return new Vector3(0.0f, CalculateLoopOffset(elapsedSeconds, verticalMoveDistance), 0.0f);
            case FieldMovePattern.ForwardBackLeftRight:
                return new Vector3(CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance), 0.0f, CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance));
            case FieldMovePattern.ForwardBackUpDown:
                return new Vector3(0.0f, CalculateLoopOffset(elapsedSeconds, verticalMoveDistance), CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance));
            case FieldMovePattern.LeftRightUpDown:
                return new Vector3(CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance), CalculateLoopOffset(elapsedSeconds, verticalMoveDistance), 0.0f);
            case FieldMovePattern.AllAxes:
                return new Vector3(CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance), CalculateLoopOffset(elapsedSeconds, verticalMoveDistance), CalculateLoopOffset(elapsedSeconds, horizontalMoveDistance));
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

        switch (axisIndex)
        {
            case 0:
                return new Vector3(0.0f, 0.0f, CalculateLoopOffset(axisTime, horizontalMoveDistance));
            case 1:
                return new Vector3(CalculateLoopOffset(axisTime, horizontalMoveDistance), 0.0f, 0.0f);
            default:
                return new Vector3(0.0f, CalculateLoopOffset(axisTime, verticalMoveDistance), 0.0f);
        }
    }

    /// <summary>
    /// Calculates eased movement from zero to negative, zero, positive, and back to zero.
    /// ゼロからマイナス、ゼロ、プラス、ゼロへ戻るイージング移動を計算します。
    /// </summary>
    private float CalculateLoopOffset(float time, float distance)
    {
        var safeLoopSeconds = Mathf.Max(axisLoopSeconds, 0.01f);
        var loopRate = Mathf.Repeat(time / safeLoopSeconds, 1.0f);
        if (loopRate < 0.25f)
        {
            return Mathf.Lerp(0.0f, -distance, SmoothStep(loopRate / 0.25f));
        }

        if (loopRate < 0.5f)
        {
            return Mathf.Lerp(-distance, 0.0f, SmoothStep((loopRate - 0.25f) / 0.25f));
        }

        if (loopRate < 0.75f)
        {
            return Mathf.Lerp(0.0f, distance, SmoothStep((loopRate - 0.5f) / 0.25f));
        }

        return Mathf.Lerp(distance, 0.0f, SmoothStep((loopRate - 0.75f) / 0.25f));
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

public readonly struct FieldMovementState
{
    public readonly Field Field;

    public readonly Vector3 WorldDelta;

    /// <summary>
    /// Stores a field movement delta for objects that should follow the field.
    /// フィールドに追従するオブジェクト向けにフィールドの移動差分を保持します。
    /// </summary>
    public FieldMovementState(Field field, Vector3 worldDelta)
    {
        Field = field;
        WorldDelta = worldDelta;
    }
}
