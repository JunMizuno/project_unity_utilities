using System.Collections.Generic;
using UnityEngine;
using R3;

public class WindForceControl : MonoBehaviour
{
    [SerializeField]
    private bool isWindEnabled = true;

    // World-space wind direction.
    // Change this to create tailwind, headwind, side wind, or diagonal upward wind.
    // ワールド座標上の風向きです。
    // 追い風、向かい風、横風、斜め上方向の風などを作る場合に変更します。
    [SerializeField]
    private Vector3 windDirection = Vector3.right;

    // Continuous wind force applied every FixedUpdate.
    // Increase to make affected objects drift faster. Decrease to make the wind subtle.
    // FixedUpdateごとに加える継続的な風の力です。
    // 上げると対象が風方向へ強く流され、下げると影響が控えめになります。
    [SerializeField]
    private float windForce = 2.0f;

    // Continuous wind force applied to targets after their physics is released.
    // Increase to make hit blocks drift faster. Decrease to keep block movement closer to impact-only motion.
    // 物理解放後のターゲットへ加える継続的な風の力です。
    // 上げるとヒット後のブロックが風方向へ流されやすくなり、下げると衝突力中心の動きになります。
    [SerializeField]
    private float targetWindForce = 4.0f;

    [SerializeField]
    private ForceMode forceMode = ForceMode.Force;

    [SerializeField]
    private bool affectPlayer = true;

    [SerializeField]
    private bool affectTargets = true;

    // Layers affected by automatically registered objects.
    // Remove a layer to prevent automatic player or target wind influence for that layer.
    // 自動登録されたオブジェクトに風を適用するレイヤーです。
    // レイヤーを外すと、そのレイヤーのプレイヤーやターゲットには風が当たりません。
    [SerializeField]
    private LayerMask affectedLayers = ~0;

    [SerializeField]
    private List<Rigidbody> includedRigidbodies = new List<Rigidbody>();

    [SerializeField]
    private List<Rigidbody> excludedRigidbodies = new List<Rigidbody>();

    private readonly Dictionary<Rigidbody, float> runtimeWindForces = new Dictionary<Rigidbody, float>();

    private bool isWindActive;

    /// <summary>
    /// Subscribes to launch, ready, and target physics events that control wind targets.
    /// 風の対象を制御する発射、準備状態、ターゲット物理イベントを購読します。
    /// </summary>
    private void Start()
    {
        Player.LaunchStartedSubject
            .Where(state => state.Player != null)
            .Subscribe(state => StartWindForPlayer(state.Player))
            .AddTo(this);

        Player.ReadyStateChangedSubject
            .Where(state => state.IsReady)
            .Subscribe(_ => StopWind())
            .AddTo(this);

        Target.PhysicsStateChangedSubject
            .Where(state => state.IsPhysicsEnabled)
            .Subscribe(state => RegisterTarget(state.Rigidbody))
            .AddTo(this);
    }

    /// <summary>
    /// Applies wind force to currently affected rigidbodies.
    /// 現在風の影響を受けるRigidbodyへ風の力を加えます。
    /// </summary>
    private void FixedUpdate()
    {
        if (!isWindEnabled || !isWindActive)
        {
            return;
        }

        ApplyWindToIncludedRigidbodies();
        ApplyWindToRuntimeRigidbodies();
    }

    /// <summary>
    /// Enables or disables wind influence.
    /// 風の影響を有効または無効にします。
    /// </summary>
    public void SetWindEnabled(bool enabled)
    {
        isWindEnabled = enabled;
    }

    /// <summary>
    /// Sets the wind direction and force.
    /// 風の方向と力を設定します。
    /// </summary>
    public void SetWind(Vector3 direction, float force)
    {
        windDirection = direction;
        windForce = force;
    }

    /// <summary>
    /// Sets the wind force applied to targets after impact.
    /// 衝突後のターゲットへ加える風の力を設定します。
    /// </summary>
    public void SetTargetWindForce(float force)
    {
        targetWindForce = force;
    }

    /// <summary>
    /// Adds a rigidbody that should receive wind even when it is not player or target.
    /// プレイヤーやターゲット以外でも風を受けるRigidbodyを追加します。
    /// </summary>
    public void AddIncludedRigidbody(Rigidbody targetRigidbody)
    {
        if (targetRigidbody != null && !includedRigidbodies.Contains(targetRigidbody))
        {
            includedRigidbodies.Add(targetRigidbody);
        }
    }

    /// <summary>
    /// Adds a rigidbody that should never receive wind.
    /// 風の影響を受けないRigidbodyを追加します。
    /// </summary>
    public void AddExcludedRigidbody(Rigidbody targetRigidbody)
    {
        if (targetRigidbody != null && !excludedRigidbodies.Contains(targetRigidbody))
        {
            excludedRigidbodies.Add(targetRigidbody);
        }
    }

    /// <summary>
    /// Starts wind influence for the launched player ball.
    /// 発射されたプレイヤーボールへの風の影響を開始します。
    /// </summary>
    private void StartWindForPlayer(Player player)
    {
        runtimeWindForces.Clear();
        isWindActive = true;

        if (!affectPlayer)
        {
            return;
        }

        var playerRigidbody = player.GetComponent<Rigidbody>();
        RegisterRuntimeRigidbody(playerRigidbody, windForce);
    }

    /// <summary>
    /// Stops wind influence and clears runtime targets.
    /// 風の影響を停止し、実行時登録された対象を解除します。
    /// </summary>
    private void StopWind()
    {
        isWindActive = false;
        runtimeWindForces.Clear();
    }

    /// <summary>
    /// Registers a target rigidbody when target physics is released after impact.
    /// 衝突後にターゲットの物理が解放されたとき、対象Rigidbodyを登録します。
    /// </summary>
    private void RegisterTarget(Rigidbody targetRigidbody)
    {
        if (!isWindActive || !affectTargets)
        {
            return;
        }

        RegisterRuntimeRigidbody(targetRigidbody, targetWindForce);
    }

    /// <summary>
    /// Registers a runtime rigidbody for automatic wind application.
    /// 自動風適用用に実行時Rigidbodyを登録します。
    /// </summary>
    private void RegisterRuntimeRigidbody(Rigidbody targetRigidbody, float force)
    {
        if (targetRigidbody != null && !targetRigidbody.isKinematic)
        {
            runtimeWindForces[targetRigidbody] = force;
        }
    }

    /// <summary>
    /// Applies wind to explicitly included rigidbodies.
    /// 明示的に指定されたRigidbodyへ風を適用します。
    /// </summary>
    private void ApplyWindToIncludedRigidbodies()
    {
        for (var i = includedRigidbodies.Count - 1; i >= 0; i--)
        {
            var targetRigidbody = includedRigidbodies[i];
            if (targetRigidbody == null)
            {
                includedRigidbodies.RemoveAt(i);
                continue;
            }

            if (!IsExcluded(targetRigidbody))
            {
                ApplyWind(targetRigidbody, windForce);
            }
        }
    }

    /// <summary>
    /// Applies wind to rigidbodies registered during gameplay.
    /// ゲーム中に登録されたRigidbodyへ風を適用します。
    /// </summary>
    private void ApplyWindToRuntimeRigidbodies()
    {
        RemoveMissingRuntimeRigidbodies();

        foreach (var pair in runtimeWindForces)
        {
            var targetRigidbody = pair.Key;
            if (CanApplyWindToRuntimeRigidbody(targetRigidbody))
            {
                ApplyWind(targetRigidbody, pair.Value);
            }
        }
    }

    /// <summary>
    /// Returns whether wind can be applied to an automatically registered rigidbody.
    /// 自動登録されたRigidbodyへ風を適用できるかを返します。
    /// </summary>
    private bool CanApplyWindToRuntimeRigidbody(Rigidbody targetRigidbody)
    {
        return targetRigidbody != null
            && !targetRigidbody.isKinematic
            && IsInAffectedLayer(targetRigidbody.gameObject)
            && !IsExcluded(targetRigidbody);
    }

    /// <summary>
    /// Removes runtime wind targets that were destroyed.
    /// 破棄済みの実行時風対象を取り除きます。
    /// </summary>
    private void RemoveMissingRuntimeRigidbodies()
    {
        var missingRigidbodies = new List<Rigidbody>();
        foreach (var pair in runtimeWindForces)
        {
            if (pair.Key == null)
            {
                missingRigidbodies.Add(pair.Key);
            }
        }

        foreach (var targetRigidbody in missingRigidbodies)
        {
            runtimeWindForces.Remove(targetRigidbody);
        }
    }

    /// <summary>
    /// Applies the specified wind force to a rigidbody.
    /// 指定された風の力をRigidbodyへ適用します。
    /// </summary>
    private void ApplyWind(Rigidbody targetRigidbody, float force)
    {
        var direction = windDirection.sqrMagnitude > Mathf.Epsilon ? windDirection.normalized : Vector3.zero;
        if (direction == Vector3.zero || Mathf.Approximately(force, 0.0f))
        {
            return;
        }

        targetRigidbody.AddForce(direction * force, forceMode);
    }

    /// <summary>
    /// Returns whether the target object belongs to an affected layer.
    /// 対象オブジェクトが風の影響を受けるレイヤーに属しているかを返します。
    /// </summary>
    private bool IsInAffectedLayer(GameObject targetObject)
    {
        return (affectedLayers.value & (1 << targetObject.layer)) != 0;
    }

    /// <summary>
    /// Returns whether the rigidbody is excluded from wind.
    /// Rigidbodyが風の除外対象かを返します。
    /// </summary>
    private bool IsExcluded(Rigidbody targetRigidbody)
    {
        return excludedRigidbodies.Contains(targetRigidbody);
    }
}
