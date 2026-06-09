using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class CreateTargets : MonoBehaviour
{
    [SerializeField]
    GameObject targetPrefab;

    // Controls the interval for fixing each target layer.
    // 各ターゲット段を固定する間隔を調整します。
    [SerializeField]
    private float layerFixIntervalSeconds = 0.2f;

    [SerializeField]
    private float minImpactExplosionForce = 4.0f;

    [SerializeField]
    private float maxImpactExplosionForce = 18.0f;

    [SerializeField]
    private float impactExplosionRadius = 3.0f;

    [SerializeField]
    private float impactExplosionUpwardsModifier = 0.35f;

    private bool trigger = default;

    private readonly List<List<Target>> targetLayers = new List<List<Target>>();

    private bool isLaunchStarted;

    private bool isTargetPhysicsReleased;

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
                    SetTargetPhysicsEnabled(false);
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

        Observable.Timer(TimeSpan.FromSeconds(1))
            .Subscribe(_ =>
            {
                trigger = true;
                CreateTargetObjects();
                SettleTargetLayersAsync(this.GetCancellationTokenOnDestroy()).Forget();
            })
            .AddTo(this);
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
        foreach (var layer in targetLayers)
        {
            if (isLaunchStarted || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            SetLayerPhysicsEnabled(layer, true);
            await WaitForLayerSettledAsync(layer, cancellationToken);

            if (isLaunchStarted || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            SetLayerPhysicsEnabled(layer, false);
        }
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

        var force = Mathf.Lerp(minImpactExplosionForce, maxImpactExplosionForce, Mathf.Clamp01(hit.PowerRate));
        AddImpactExplosionForce(hit.HitPoint, force);
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
}
