using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class CreateTargets : MonoBehaviour
{
    public static readonly Subject<CreateTargets> TargetPlacementStartedSubject = new Subject<CreateTargets>();

    public static readonly Subject<TargetPlacementLayerState> TargetPlacementLayerFixedSubject = new Subject<TargetPlacementLayerState>();

    public static readonly Subject<CreateTargets> TargetPlacementCompletedSubject = new Subject<CreateTargets>();

    public static readonly Subject<CreateTargets> AllTargetsStoppedSubject = new Subject<CreateTargets>();

    private static readonly List<CreateTargets> Instances = new List<CreateTargets>();

    [SerializeField]
    GameObject targetPrefab;

    // Controls the interval for fixing each target layer.
    // 各ターゲット段を固定する間隔を調整します。
    [SerializeField]
    private float layerFixIntervalSeconds = 0.2f;

    // Delay before the first target generation starts.
    // Increase to wait longer before blocks appear. Decrease to start placement sooner.
    // 最初のターゲット生成を開始するまでの待ち時間です。
    // 上げるとブロック配置開始が遅くなり、下げると早く開始されます。
    [SerializeField]
    private float initialCreateDelaySeconds = 0.05f;

    // Keeps generated targets affected only by vertical physics until the first ball hit.
    // Turn on to let blocks settle downward without collapsing sideways before impact.
    // 初回のボール衝突まで、生成済みターゲットを縦方向だけ物理影響を受ける状態にします。
    // オンにすると、衝突前に横崩れしにくいまま下方向へ着地させられます。
    [SerializeField]
    private bool useVerticalOnlyPhysicsBeforeHit = true;

    // Moves generated targets together with the field until the first ball hit.
    // Turn on to keep stacked blocks aligned with a horizontally moving field before impact.
    // 初回のボール衝突まで、生成済みターゲットをフィールド移動に合わせて動かします。
    // オンにすると、衝突前に横移動するフィールドと積み上がったブロックの位置を合わせ続けます。
    [SerializeField]
    private bool followFieldMovementBeforeHit = true;

    // Minimum additional force applied when the ball hits targets with weak launch power.
    // Increase to make even weak shots scatter blocks more. Decrease to keep weak shots calmer.
    // 弱い発射威力でターゲットに当たったときに加える追加の最小衝撃力です。
    // 上げると弱いショットでもブロックが散りやすくなり、下げると弱いショットの動きが控えめになります。
    [SerializeField]
    private float minImpactExplosionForce = 4.0f;

    // Maximum additional force applied when the ball hits targets with strong launch power.
    // Increase to make full-power shots blast blocks farther. Decrease to reduce strong-shot scattering.
    // 強い発射威力でターゲットに当たったときに加える追加の最大衝撃力です。
    // 上げると最大威力ショットでブロックがより遠くへ飛び、下げると強いショットの散らばりが抑えられます。
    [SerializeField]
    private float maxImpactExplosionForce = 18.0f;

    // Radius around the hit point that receives the additional impact force.
    // Increase to affect more blocks. Decrease to focus force around the first contact point.
    // 接触点から追加衝撃力が届く範囲です。
    // 上げるとより多くのブロックに力が届き、下げると最初に当たった周辺だけへ力が集中します。
    [SerializeField]
    private float impactExplosionRadius = 3.0f;

    // Upward lift added to the impact force.
    // Increase to pop blocks upward more. Decrease to keep movement flatter along the field.
    // 追加衝撃力に含める上方向の持ち上げ量です。
    // 上げるとブロックが上へ跳ねやすくなり、下げるとフィールド上を横方向に動きやすくなります。
    [SerializeField]
    private float impactExplosionUpwardsModifier = 0.35f;

    // Ball speed that applies the lowest impact force multiplier.
    // Increase to make slowed balls lose impact more easily. Decrease to keep force even at lower speed.
    // 最低衝撃力倍率になるボール速度です。
    // 上げると減速したボールの衝撃が弱くなりやすく、下げると低速でも力が残りやすくなります。
    [SerializeField]
    private float minImpactBallSpeed = 2.0f;

    // Ball speed that applies the highest impact force multiplier.
    // Increase to require faster hits for full scatter. Decrease to reach full force at lower speed.
    // 最大衝撃力倍率になるボール速度です。
    // 上げると最大散らばりに必要な速度が高くなり、下げると低速でも最大に近づきます。
    [SerializeField]
    private float maxImpactBallSpeed = 30.0f;

    // Impact multiplier used when the ball reaches targets at or below minImpactBallSpeed.
    // Increase to let slow balls still scatter blocks. Decrease to make distance loss more severe.
    // ボール速度がminImpactBallSpeed以下でターゲットに届いたときの衝撃倍率です。
    // 上げると低速でもブロックが散りやすくなり、下げると距離による減衰が強くなります。
    [SerializeField]
    private float minImpactSpeedMultiplier = 0.25f;

    // Impact multiplier used when the ball reaches targets at or above maxImpactBallSpeed.
    // Increase above 1 to reward high-speed hits more. Decrease to cap strong hits lower.
    // ボール速度がmaxImpactBallSpeed以上でターゲットに届いたときの衝撃倍率です。
    // 1より上げると高速衝突がより強くなり、下げると強い衝突の上限が抑えられます。
    [SerializeField]
    private float maxImpactSpeedMultiplier = 1.0f;

    // Velocity threshold used to count blocks that are flying enough to affect camera shake.
    // Increase to react only to faster blocks. Decrease to count smaller block movement.
    // カメラシェイクに影響するほど飛んでいるブロックを数える速度しきい値です。
    // 上げると速いブロックだけに反応し、下げると小さなブロックの動きも数えます。
    [SerializeField]
    private float cameraShakeFlyingVelocityThreshold = 0.3f;

    // Delay after impact before checking whether all blocks have stopped.
    // Increase to wait longer before returning the ball. Decrease to make the next shot ready sooner.
    // 衝突後、すべてのブロック停止を確認し始めるまでの待ち時間です。
    // 上げるとボール復帰が遅くなり、下げると次の発射準備が早くなります。
    [SerializeField]
    private float targetStopCheckDelaySeconds = 1.0f;

    // Velocity threshold used to judge whether each block has stopped.
    // Increase to treat slow movement as stopped sooner. Decrease to wait for more complete stillness.
    // 各ブロックが停止したと判定する速度しきい値です。
    // 上げると低速移動中でも停止扱いになりやすく、下げるとより完全な静止を待ちます。
    [SerializeField]
    private float targetStopVelocityThreshold = 0.2f;

    // Y position below which blocks are treated as stopped for return checks.
    // Increase to ignore fallen blocks sooner. Decrease to keep checking blocks until they fall lower.
    // ブロックを復帰判定上の停止扱いにするY座標です。
    // 上げると落下したブロックを早めに無視し、下げるとより下まで落ちるまで判定対象に残します。
    [SerializeField]
    private float targetStoppedYThreshold = -3.0f;

    // Duration all blocks must remain under the velocity threshold.
    // Increase to require more stable stillness. Decrease to return the ball sooner.
    // すべてのブロックが速度しきい値未満を維持する必要がある時間です。
    // 上げるとより安定した静止を待ち、下げるとボール復帰が早くなります。
    [SerializeField]
    private float targetStopStableSeconds = 0.5f;

    [SerializeField]
    private Camera mainCamera;

    // Viewport padding used when deciding which blocks remain on screen for stop checks.
    // Increase to keep near-edge blocks in the stop check longer. Decrease to ignore offscreen blocks sooner.
    // 停止判定で「画面上に残っている」とみなす表示範囲の余白です。
    // 上げると画面端付近のブロックを長く判定対象にし、下げると画面外ブロックを早く除外します。
    [SerializeField]
    private float targetStopViewportPadding = 0.05f;

    private bool trigger = default;

    private readonly List<List<Target>> targetLayers = new List<List<Target>>();

    private bool isLaunchStarted;

    private bool isTargetPhysicsReleased;

    /// <summary>
    /// Sets the stopped Y threshold for every active target generator.
    /// 有効なすべてのターゲット生成器に停止扱いY座標を設定します。
    /// </summary>
    public static void SetTargetStoppedYThresholdForAll(float threshold)
    {
        foreach (var instance in Instances)
        {
            if (instance != null)
            {
                instance.SetTargetStoppedYThreshold(threshold);
            }
        }
    }

    /// <summary>
    /// Sets the Y position below which blocks are treated as stopped.
    /// ブロックを停止扱いにするY座標を設定します。
    /// </summary>
    public void SetTargetStoppedYThreshold(float threshold)
    {
        targetStoppedYThreshold = threshold;
    }

    /// <summary>
    /// Registers this target generator for global target movement checks.
    /// 全体のターゲット停止判定に使うため、この生成器を登録します。
    /// </summary>
    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (!Instances.Contains(this))
        {
            Instances.Add(this);
        }
    }

    /// <summary>
    /// Starts the initial target generation behavior.
    /// ターゲットを最初に一度だけ生成する処理を開始します。
    /// </summary>
    void Start()
    {
        Player.LaunchedSubject
            .Subscribe(_ =>
            {
                isLaunchStarted = true;
                if (!isTargetPhysicsReleased)
                {
                    if (useVerticalOnlyPhysicsBeforeHit)
                    {
                        SetTargetVerticalPhysicsOnly();
                    }
                    else
                    {
                        SetTargetPhysicsEnabled(false);
                    }
                }
            })
            .AddTo(this);

        Player.HitTargetSubject
            .Where(hit => hit.Target != null && hit.Target.transform.IsChildOf(transform))
            .Subscribe(hit =>
            {
                if (isTargetPhysicsReleased)
                {
                    return;
                }

                isTargetPhysicsReleased = true;
                SetTargetPhysicsEnabled(true);
                AddImpactExplosionForceAsync(hit, this.GetCancellationTokenOnDestroy()).Forget();
            })
            .AddTo(this);

        Field.MovementChangedSubject
            .Where(_ => followFieldMovementBeforeHit && !isTargetPhysicsReleased)
            .Subscribe(state => MoveTargetsWithField(state.WorldDelta))
            .AddTo(this);

        Observable.Timer(TimeSpan.FromSeconds(initialCreateDelaySeconds))
            .Subscribe(_ =>
            {
                trigger = true;
                TargetPlacementStartedSubject.OnNext(this);
                CreateTargetObjects();
                SettleTargetLayersAsync(this.GetCancellationTokenOnDestroy()).Forget();
            })
            .AddTo(this);
    }

    /// <summary>
    /// Unregisters this target generator from global target movement checks.
    /// 全体のターゲット停止判定からこの生成器を解除します。
    /// </summary>
    void OnDestroy()
    {
        Instances.Remove(this);
    }

    /// <summary>
    /// Runs per-frame target generator processing.
    /// ターゲット生成オブジェクトのフレームごとの処理を実行します。
    /// </summary>
    void Update()
    {

    }

    /// <summary>
    /// Recreates the target objects in either a depth layout or a flat layout.
    /// ターゲットオブジェクトを奥行きのある配置または平面配置で再生成します。
    /// </summary>
    private void CreateTargetObjects()
    {
        targetLayers.Clear();
        isLaunchStarted = false;
        isTargetPhysicsReleased = false;

        foreach (Transform child in this.gameObject.transform)
        {
            Destroy(child.gameObject);
        }

        var maxWidthCount = (short)0;
        var maxHeightCount = (short)0;
        var maxDepthCount = (short)0;

        maxWidthCount = 11;
        maxHeightCount = 11;
        maxDepthCount = 5;

        var centerX = ((float)maxWidthCount / 2.0f) * 0.5f - 0.25f;
        var centerZ = ((float)maxDepthCount / 2.0f) * 0.5f - 0.25f;

        for (var i = 0; i < maxWidthCount; i++)
        {
            for (var j = 0; j < maxHeightCount; j++)
            {
                while (targetLayers.Count <= j)
                {
                    targetLayers.Add(new List<Target>());
                }

                for (var k = 0; k < maxDepthCount; k++)
                {
                    var instance = Instantiate(targetPrefab, this.gameObject.transform);
                    instance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    var xPos = 0.5f * i - centerX;
                    var yPos = 0.5f * (j + 1);
                    var zPos = 0.5f * k - centerZ;
                    if (!trigger)
                    {
                        zPos = 0.0f;
                    }
                    instance.transform.localPosition = new Vector3(xPos, yPos, zPos);

                    var target = instance.GetComponent<Target>();
                    if (target != null)
                    {
                        target.SetPhysicsEnabled(false);
                        targetLayers[j].Add(target);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Drops and fixes generated targets from the bottom layer upward.
    /// 生成したターゲットを下段から順に落下させて固定します。
    /// </summary>
    private async UniTaskVoid SettleTargetLayersAsync(CancellationToken cancellationToken)
    {
        for (var layerIndex = 0; layerIndex < targetLayers.Count; layerIndex++)
        {
            var layer = targetLayers[layerIndex];
            if (isLaunchStarted || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (useVerticalOnlyPhysicsBeforeHit)
            {
                SetLayerVerticalPhysicsOnly(layer);
            }
            else
            {
                SetLayerPhysicsEnabled(layer, true);
            }

            await WaitForLayerSettledAsync(layer, cancellationToken);

            if (isLaunchStarted || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (useVerticalOnlyPhysicsBeforeHit)
            {
                SetLayerVerticalPhysicsOnly(layer);
            }
            else
            {
                SetLayerPhysicsEnabled(layer, false);
            }

            TargetPlacementLayerFixedSubject.OnNext(new TargetPlacementLayerState(this, layerIndex, layerIndex + 1));
        }

        TargetPlacementCompletedSubject.OnNext(this);
    }

    /// <summary>
    /// Waits until all targets in a layer stop moving or the timeout is reached.
    /// レイヤー内の全ターゲットが停止するか、タイムアウトするまで待機します。
    /// </summary>
    private async UniTask WaitForLayerSettledAsync(List<Target> layer, CancellationToken cancellationToken)
    {
        var elapsedSeconds = 0.0f;
        while (elapsedSeconds < layerFixIntervalSeconds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (isLaunchStarted)
            {
                return;
            }

            elapsedSeconds += Time.fixedDeltaTime;
            await UniTask.WaitForFixedUpdate(cancellationToken);
        }
    }

    /// <summary>
    /// Sets whether a generated target layer is controlled by physics.
    /// 生成済みターゲットのレイヤーを物理演算で制御するかどうかを設定します。
    /// </summary>
    private void SetLayerPhysicsEnabled(List<Target> layer, bool enabled)
    {
        foreach (var target in layer)
        {
            if (target != null)
            {
                target.SetPhysicsEnabled(enabled);
            }
        }
    }

    /// <summary>
    /// Sets a generated target layer to vertical-only physics.
    /// 生成済みターゲットのレイヤーを縦方向だけ物理演算で動ける状態にします。
    /// </summary>
    private void SetLayerVerticalPhysicsOnly(List<Target> layer)
    {
        foreach (var target in layer)
        {
            if (target != null)
            {
                target.SetVerticalPhysicsOnly();
            }
        }
    }

    /// <summary>
    /// Sets whether all generated targets are controlled by physics.
    /// 生成済みのすべてのターゲットを物理演算で制御するかどうかを設定します。
    /// </summary>
    private void SetTargetPhysicsEnabled(bool enabled)
    {
        foreach (Transform child in this.gameObject.transform)
        {
            var target = child.GetComponent<Target>();
            if (target != null)
            {
                target.SetPhysicsEnabled(enabled);
            }
        }
    }

    /// <summary>
    /// Sets all generated targets to vertical-only physics.
    /// 生成済みのすべてのターゲットを縦方向だけ物理演算で動ける状態にします。
    /// </summary>
    private void SetTargetVerticalPhysicsOnly()
    {
        foreach (Transform child in this.gameObject.transform)
        {
            var target = child.GetComponent<Target>();
            if (target != null)
            {
                target.SetVerticalPhysicsOnly();
            }
        }
    }

    /// <summary>
    /// Moves generated targets together with the field before full physics release.
    /// 完全な物理解放前に、生成済みターゲットをフィールドと一緒に移動します。
    /// </summary>
    private void MoveTargetsWithField(Vector3 worldDelta)
    {
        foreach (Transform child in this.gameObject.transform)
        {
            var target = child.GetComponent<Target>();
            if (target != null)
            {
                target.MoveByFieldDelta(worldDelta);
            }
        }
    }

    /// <summary>
    /// Applies additional impact force after target physics is released.
    /// ターゲットの物理解放後に追加の衝突力を適用します。
    /// </summary>
    private async UniTaskVoid AddImpactExplosionForceAsync(PlayerTargetHit hit, CancellationToken cancellationToken)
    {
        var isCanceled = await UniTask.WaitForFixedUpdate(cancellationToken).SuppressCancellationThrow();
        if (isCanceled)
        {
            return;
        }

        var force = CalculateImpactExplosionForce(hit);
        AddImpactExplosionForce(hit.HitPoint, force);

        isCanceled = await UniTask.WaitForFixedUpdate(cancellationToken).SuppressCancellationThrow();
        if (isCanceled)
        {
            return;
        }

        PlayCameraShakeByFlyingTargetCount();
        WaitForAllTargetsStoppedAsync(cancellationToken).Forget();
    }

    /// <summary>
    /// Applies explosion impulse to all generated targets.
    /// 生成済みのすべてのターゲットへ爆発方向の瞬間的な力を加えます。
    /// </summary>
    private void AddImpactExplosionForce(Vector3 hitPoint, float force)
    {
        foreach (Transform child in this.gameObject.transform)
        {
            var target = child.GetComponent<Target>();
            if (target != null)
            {
                target.AddExplosionImpulse(hitPoint, force, impactExplosionRadius, impactExplosionUpwardsModifier);
            }
        }
    }

    /// <summary>
    /// Calculates impact force from launch power and the ball speed at target collision.
    /// 発射威力とターゲット衝突時のボール速度から衝撃力を計算します。
    /// </summary>
    private float CalculateImpactExplosionForce(PlayerTargetHit hit)
    {
        var baseForce = Mathf.Lerp(minImpactExplosionForce, maxImpactExplosionForce, Mathf.Clamp01(hit.PowerRate));
        var safeMaxImpactBallSpeed = Mathf.Max(maxImpactBallSpeed, minImpactBallSpeed + 0.01f);
        var speedRate = Mathf.InverseLerp(minImpactBallSpeed, safeMaxImpactBallSpeed, hit.BallSpeed);
        var speedMultiplier = Mathf.Lerp(minImpactSpeedMultiplier, maxImpactSpeedMultiplier, speedRate);
        return baseForce * speedMultiplier;
    }

    /// <summary>
    /// Plays camera shake based on the number of moving targets.
    /// 動いているターゲット数に応じてカメラシェイクを再生します。
    /// </summary>
    private void PlayCameraShakeByFlyingTargetCount()
    {
        var cameraEffectControl = EffectControl.Instance != null ? EffectControl.Instance.CameraEffect : null;
        if (cameraEffectControl == null)
        {
            return;
        }

        cameraEffectControl.PlayShakeByFlyingObjectCount(CountFlyingTargetsForCameraShake());
    }

    /// <summary>
    /// Counts targets moving fast enough to affect camera shake strength.
    /// カメラシェイクの強さに影響する速度で動いているターゲット数を数えます。
    /// </summary>
    private int CountFlyingTargetsForCameraShake()
    {
        var count = 0;
        foreach (Transform child in this.gameObject.transform)
        {
            var target = child.GetComponent<Target>();
            if (target != null && target.IsMoving(cameraShakeFlyingVelocityThreshold))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Waits until all generated targets stop moving after impact.
    /// 衝突後、生成済みターゲットがすべて停止するまで待機します。
    /// </summary>
    private async UniTaskVoid WaitForAllTargetsStoppedAsync(CancellationToken cancellationToken)
    {
        var isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(targetStopCheckDelaySeconds), cancellationToken: cancellationToken)
            .SuppressCancellationThrow();
        if (isCanceled)
        {
            return;
        }

        var stableSeconds = 0.0f;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (AreAllTargetsStopped())
            {
                stableSeconds += Time.fixedDeltaTime;
                if (stableSeconds >= targetStopStableSeconds)
                {
                    AllTargetsStoppedSubject.OnNext(this);
                    return;
                }
            }
            else
            {
                stableSeconds = 0.0f;
            }

            isCanceled = await UniTask.WaitForFixedUpdate(cancellationToken).SuppressCancellationThrow();
            if (isCanceled)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Returns whether all on-screen generated targets are under the stop velocity threshold.
    /// 画面上に残っている生成済みターゲットがすべて停止速度しきい値未満かを返します。
    /// </summary>
    private bool AreAllTargetsStopped()
    {
        foreach (Transform child in this.gameObject.transform)
        {
            var target = child.GetComponent<Target>();
            if (target == null || !IsTargetInStopCheckView(target))
            {
                continue;
            }

            if (target.transform.position.y < targetStoppedYThreshold)
            {
                continue;
            }

            if (target.IsMoving(targetStopVelocityThreshold))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns whether every on-screen generated target in the scene has stopped moving.
    /// シーン内で画面上に残っている生成済みターゲットが停止しているかを返します。
    /// </summary>
    public static bool AreAllGeneratedTargetsStopped()
    {
        var hasGeneratedTarget = false;
        foreach (var instance in Instances)
        {
            if (instance == null || !instance.HasGeneratedTarget())
            {
                continue;
            }

            hasGeneratedTarget = true;
            if (!instance.AreAllTargetsStopped())
            {
                return false;
            }
        }

        return hasGeneratedTarget;
    }

    /// <summary>
    /// Returns whether the target should be included in stop checks.
    /// 対象ターゲットを停止判定に含めるべきかを返します。
    /// </summary>
    private bool IsTargetInStopCheckView(Target target)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        return target.IsInCameraView(mainCamera, targetStopViewportPadding);
    }

    /// <summary>
    /// Returns whether this generator currently owns generated targets.
    /// この生成器が現在ターゲットを保持しているかを返します。
    /// </summary>
    private bool HasGeneratedTarget()
    {
        foreach (Transform child in this.gameObject.transform)
        {
            if (child.GetComponent<Target>() != null)
            {
                return true;
            }
        }

        return false;
    }
}

public readonly struct TargetPlacementLayerState
{
    public readonly CreateTargets CreateTargets;

    public readonly int LayerIndex;

    public readonly int FixedLayerCount;

    /// <summary>
    /// Stores target placement progress after one layer has been fixed.
    /// 1段分のターゲット固定後の配置進行情報を保持します。
    /// </summary>
    public TargetPlacementLayerState(CreateTargets createTargets, int layerIndex, int fixedLayerCount)
    {
        CreateTargets = createTargets;
        LayerIndex = layerIndex;
        FixedLayerCount = fixedLayerCount;
    }
}
