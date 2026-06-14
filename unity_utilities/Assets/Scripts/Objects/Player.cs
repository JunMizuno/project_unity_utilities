using UnityEngine;
using UnityEngine.Rendering;
using R3;

public class Player : MonoBehaviour
{
    public static readonly Subject<PlayerReadyState> ReadyStateChangedSubject = new Subject<PlayerReadyState>();

    public static readonly Subject<Player> LaunchedSubject = new Subject<Player>();

    public static readonly Subject<PlayerLaunchState> LaunchStartedSubject = new Subject<PlayerLaunchState>();

    public static readonly Subject<PlayerCollisionState> CollisionSubject = new Subject<PlayerCollisionState>();

    public static readonly Subject<PlayerTargetHit> HitTargetSubject = new Subject<PlayerTargetHit>();

    [SerializeField]
    private Rigidbody rigidBody;

    [SerializeField]
    private float minLaunchForce = 10.0f;

    [SerializeField]
    private float maxLaunchForce = 30.0f;

    [SerializeField]
    private MeshRenderer playerRenderer;

    [SerializeField]
    private SimpleBlobShadow blobShadow;

    [SerializeField]
    private float readyAlpha = 0.35f;

    [SerializeField]
    private float launchedAlpha = 1.0f;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private float offscreenViewportPadding = 0.15f;

    // Velocity threshold used to judge that the launched ball is no longer likely to hit targets.
    // Increase to treat slow movement as finished sooner. Decrease to wait for the ball to slow down more.
    // 発射後のボールがこれ以上ターゲットへ当たりにくいと判定する速度しきい値です。
    // 上げると低速の段階で終了扱いになり、下げるとより停止に近い状態まで待ちます。
    [SerializeField]
    private float returnBallVelocityThreshold = 0.08f;

    // Duration the ball must remain under the velocity threshold before it can return.
    // Increase to avoid returning during brief slowdowns. Decrease to return sooner after the ball stalls.
    // ボールが速度しきい値未満を維持する必要がある時間です。
    // 上げると一瞬の減速では戻りにくくなり、下げると失速後に早く戻ります。
    [SerializeField]
    private float returnBallStableSeconds = 0.5f;

    private Material runtimeMaterial;

    private Vector3 initialLocalPosition;

    private Quaternion initialLocalRotation;

    private Vector3 initialLocalScale;

    private bool hasHitTarget;

    private float currentPowerRate;

    private bool isLaunched;

    private float ballStoppedSeconds;

    public bool IsReady { get; private set; }

    /// <summary>
    /// Caches required components and prepares the runtime-only ball material.
    /// 必要なコンポーネントを保持し、実行時専用のボールマテリアルを準備します。
    /// </summary>
    private void Awake()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        if (playerRenderer == null)
        {
            playerRenderer = GetComponent<MeshRenderer>();
        }

        if (blobShadow == null)
        {
            blobShadow = GetComponent<SimpleBlobShadow>();
        }

        if (playerRenderer != null)
        {
            runtimeMaterial = playerRenderer.material;
        }

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;
    }

    /// <summary>
    /// Hides the ball until target placement completes.
    /// ターゲット配置が完了するまでボールを非表示にします。
    /// </summary>
    private void Start()
    {
        SetLaunchReady(false);
        SetPlayerVisible(false);

        CreateTargets.TargetPlacementCompletedSubject
            .Subscribe(_ => SetLaunchReady(true))
            .AddTo(this);

        CreateTargets.AllTargetsStoppedSubject
            .Subscribe(_ =>
            {
                if (CanReturnToLaunchReady())
                {
                    SetLaunchReady(true);
                }
            })
            .AddTo(this);
    }

    /// <summary>
    /// Checks whether the ball and targets satisfy the return conditions.
    /// ボールとターゲットが復帰条件を満たしているか確認します。
    /// </summary>
    private void Update()
    {
        UpdateBallStoppedSeconds();

        if (!CanReturnToLaunchReady())
        {
            return;
        }

        SetLaunchReady(true);
    }

    /// <summary>
    /// Restores the player ball to the initial position and applies forward impulse.
    /// プレイヤーボールを初期位置へ戻し、前方への力を加えます。
    /// </summary>
    public void AddForceToPlayer()
    {
        AddForceToPlayer(0.5f);
    }

    /// <summary>
    /// Restores the player ball to the initial position and applies forward impulse based on the power rate.
    /// パワー割合に応じてプレイヤーボールを初期位置へ戻し、前方への力を加えます。
    /// </summary>
    public void AddForceToPlayer(float powerRate)
    {
        AddForceToPlayer(powerRate, Vector3.forward);
    }

    /// <summary>
    /// Restores the player ball to the initial position and applies impulse in the specified direction.
    /// プレイヤーボールを初期位置へ戻し、指定した方向へ力を加えます。
    /// </summary>
    public void AddForceToPlayer(float powerRate, Vector3 launchDirection)
    {
        if (!IsReady)
        {
            return;
        }

        SetLaunchReady(false);
        ResetPlayerTransformToInitialPosition();
        this.gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.useGravity = true;
        rigidBody.mass = 1.0f;
        SetPlayerVisible(true);
        SetPlayerAlpha(launchedAlpha);
        hasHitTarget = false;
        ballStoppedSeconds = 0.0f;
        currentPowerRate = Mathf.Clamp01(powerRate);
        isLaunched = true;
        LaunchedSubject.OnNext(this);
        LaunchStartedSubject.OnNext(new PlayerLaunchState(this, launchDirection.normalized, currentPowerRate));
        var launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, currentPowerRate);
        rigidBody.AddForce(launchDirection.normalized * launchForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Notifies when the player ball first collides with a target after launch.
    /// 発射後にプレイヤーボールが最初にターゲットへ衝突したことを通知します。
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (isLaunched)
        {
            var collisionPoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.transform.position;
            CollisionSubject.OnNext(new PlayerCollisionState(this, collision.collider, collisionPoint));
        }

        if (hasHitTarget)
        {
            return;
        }

        var target = collision.collider.GetComponentInParent<Target>();
        if (target == null)
        {
            return;
        }

        hasHitTarget = true;
        var hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.transform.position;
        HitTargetSubject.OnNext(new PlayerTargetHit(target, hitPoint, currentPowerRate));
    }

    /// <summary>
    /// Restores the player ball to the initial scene placement before launching.
    /// 発射前にプレイヤーボールをシーン上の初期配置へ戻します。
    /// </summary>
    private void ResetPlayerTransformToInitialPosition()
    {
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        transform.localScale = initialLocalScale;

        if (rigidBody == null)
        {
            return;
        }

        rigidBody.position = transform.position;
        rigidBody.rotation = transform.rotation;
    }

    /// <summary>
    /// Changes whether the player ball can be launched.
    /// プレイヤーボールを発射可能状態へ切り替えます。
    /// </summary>
    private void SetLaunchReady(bool ready)
    {
        IsReady = ready;
        isLaunched = false;
        ballStoppedSeconds = 0.0f;

        if (ready)
        {
            SetPlayerVisible(true);
            ResetPlayerTransformToInitialPosition();
            if (rigidBody != null)
            {
                rigidBody.linearVelocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;
                rigidBody.useGravity = false;
                rigidBody.Sleep();
            }

            SetPlayerAlpha(readyAlpha);
        }
        else
        {
            SetPlayerAlpha(launchedAlpha);
        }

        ReadyStateChangedSubject.OnNext(new PlayerReadyState(this, ready));
    }

    /// <summary>
    /// Changes whether the player ball and its blob shadow are visible.
    /// プレイヤーボール本体と丸影の表示状態を切り替えます。
    /// </summary>
    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderer != null)
        {
            playerRenderer.enabled = visible;
        }

        if (blobShadow != null)
        {
            blobShadow.SetVisible(visible);
        }
    }

    /// <summary>
    /// Returns whether all blocks are stopped and the launched ball can safely return.
    /// すべてのブロックが停止し、発射後のボールを安全に戻せるかを返します。
    /// </summary>
    private bool CanReturnToLaunchReady()
    {
        if (!isLaunched || !CreateTargets.AreAllGeneratedTargetsStopped())
        {
            return false;
        }

        return IsPlayerOffscreen() || IsPlayerNearlyStopped();
    }

    /// <summary>
    /// Accumulates how long the launched ball has remained nearly stopped.
    /// 発射後のボールがほぼ停止した状態を維持している時間を積算します。
    /// </summary>
    private void UpdateBallStoppedSeconds()
    {
        if (!isLaunched || rigidBody == null)
        {
            ballStoppedSeconds = 0.0f;
            return;
        }

        if (IsPlayerVelocityUnderReturnThreshold())
        {
            ballStoppedSeconds += Time.deltaTime;
            return;
        }

        ballStoppedSeconds = 0.0f;
    }

    /// <summary>
    /// Returns whether the launched ball has been nearly stopped long enough.
    /// 発射後のボールが十分な時間ほぼ停止しているかを返します。
    /// </summary>
    private bool IsPlayerNearlyStopped()
    {
        return ballStoppedSeconds >= returnBallStableSeconds;
    }

    /// <summary>
    /// Returns whether the player ball velocity is under the return threshold.
    /// プレイヤーボールの速度が復帰用しきい値未満かを返します。
    /// </summary>
    private bool IsPlayerVelocityUnderReturnThreshold()
    {
        var velocityThreshold = returnBallVelocityThreshold * returnBallVelocityThreshold;
        return rigidBody.linearVelocity.sqrMagnitude <= velocityThreshold
            && rigidBody.angularVelocity.sqrMagnitude <= velocityThreshold;
    }

    /// <summary>
    /// Returns whether the player ball is outside the camera viewport.
    /// プレイヤーボールがカメラの表示範囲外にあるかを返します。
    /// </summary>
    private bool IsPlayerOffscreen()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return false;
        }

        var viewportPosition = mainCamera.WorldToViewportPoint(transform.position);
        return viewportPosition.z < 0.0f
            || viewportPosition.x < -offscreenViewportPadding
            || viewportPosition.x > 1.0f + offscreenViewportPadding
            || viewportPosition.y < -offscreenViewportPadding
            || viewportPosition.y > 1.0f + offscreenViewportPadding;
    }

    /// <summary>
    /// Sets the runtime material alpha and render mode.
    /// 実行時マテリアルの透明度と描画モードを設定します。
    /// </summary>
    private void SetPlayerAlpha(float alpha)
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        var clampedAlpha = Mathf.Clamp01(alpha);

        if (runtimeMaterial.HasProperty("_BaseColor"))
        {
            var baseColor = runtimeMaterial.GetColor("_BaseColor");
            baseColor.a = clampedAlpha;
            runtimeMaterial.SetColor("_BaseColor", baseColor);
        }

        if (runtimeMaterial.HasProperty("_Color"))
        {
            var color = runtimeMaterial.GetColor("_Color");
            color.a = clampedAlpha;
            runtimeMaterial.SetColor("_Color", color);
        }

        if (clampedAlpha < 1.0f)
        {
            ApplyTransparentRendering();
            return;
        }

        ApplyOpaqueRendering();
    }

    /// <summary>
    /// Switches the runtime material to alpha blending.
    /// 実行時マテリアルをアルファブレンド描画に切り替えます。
    /// </summary>
    private void ApplyTransparentRendering()
    {
        runtimeMaterial.SetOverrideTag("RenderType", "Transparent");
        SetMaterialFloat("_Surface", 1.0f);
        SetMaterialFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        SetMaterialFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetMaterialFloat("_SrcBlendAlpha", (float)BlendMode.One);
        SetMaterialFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        SetMaterialFloat("_ZWrite", 0.0f);
        runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeMaterial.renderQueue = (int)RenderQueue.Transparent;
    }

    /// <summary>
    /// Restores the runtime material to opaque rendering.
    /// 実行時マテリアルを不透明描画に戻します。
    /// </summary>
    private void ApplyOpaqueRendering()
    {
        runtimeMaterial.SetOverrideTag("RenderType", "Opaque");
        SetMaterialFloat("_Surface", 0.0f);
        SetMaterialFloat("_SrcBlend", (float)BlendMode.One);
        SetMaterialFloat("_DstBlend", (float)BlendMode.Zero);
        SetMaterialFloat("_SrcBlendAlpha", (float)BlendMode.One);
        SetMaterialFloat("_DstBlendAlpha", (float)BlendMode.Zero);
        SetMaterialFloat("_ZWrite", 1.0f);
        runtimeMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeMaterial.renderQueue = -1;
    }

    /// <summary>
    /// Sets a material float only when the shader exposes the property.
    /// シェーダーが保持している場合のみマテリアルの float 値を設定します。
    /// </summary>
    private void SetMaterialFloat(string propertyName, float value)
    {
        if (runtimeMaterial.HasProperty(propertyName))
        {
            runtimeMaterial.SetFloat(propertyName, value);
        }
    }
}

public readonly struct PlayerLaunchState
{
    public readonly Player Player;

    public readonly Vector3 LaunchDirection;

    public readonly float PowerRate;

    /// <summary>
    /// Stores player launch data for systems that react to launch direction.
    /// 発射方向に反応するシステム向けのプレイヤー発射情報を保持します。
    /// </summary>
    public PlayerLaunchState(Player player, Vector3 launchDirection, float powerRate)
    {
        Player = player;
        LaunchDirection = launchDirection;
        PowerRate = powerRate;
    }
}

public readonly struct PlayerCollisionState
{
    public readonly Player Player;

    public readonly Collider Collider;

    public readonly Vector3 HitPoint;

    /// <summary>
    /// Stores player collision data while the ball is launched.
    /// ボール発射中のプレイヤー衝突情報を保持します。
    /// </summary>
    public PlayerCollisionState(Player player, Collider collider, Vector3 hitPoint)
    {
        Player = player;
        Collider = collider;
        HitPoint = hitPoint;
    }
}

public readonly struct PlayerTargetHit
{
    public readonly Target Target;

    public readonly Vector3 HitPoint;

    public readonly float PowerRate;

    /// <summary>
    /// Stores data for the player's first target collision.
    /// プレイヤーが最初にターゲットへ衝突した情報を保持します。
    /// </summary>
    public PlayerTargetHit(Target target, Vector3 hitPoint, float powerRate)
    {
        Target = target;
        HitPoint = hitPoint;
        PowerRate = powerRate;
    }
}

public readonly struct PlayerReadyState
{
    public readonly Player Player;

    public readonly bool IsReady;

    /// <summary>
    /// Stores player launch-ready state changes.
    /// プレイヤーの発射準備状態の変更情報を保持します。
    /// </summary>
    public PlayerReadyState(Player player, bool isReady)
    {
        Player = player;
        IsReady = isReady;
    }
}
