using UnityEngine;
using UnityEngine.Rendering;
using R3;

public class Player : MonoBehaviour
{
    public static readonly Subject<Player> LaunchedSubject = new Subject<Player>();

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
    private float readyAlpha = 0.35f;

    [SerializeField]
    private float launchedAlpha = 1.0f;

    private Material runtimeMaterial;

    private Vector3 initialLocalPosition;

    private Quaternion initialLocalRotation;

    private bool hasHitTarget;

    private float currentPowerRate;

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

        if (playerRenderer != null)
        {
            runtimeMaterial = playerRenderer.material;
        }

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    /// <summary>
    /// Applies the pre-launch transparent visual.
    /// 発射前の透過表示を適用します。
    /// </summary>
    private void Start()
    {
        SetPlayerAlpha(readyAlpha);
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
        ResetPlayerTransformToInitialPosition();
        this.gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.useGravity = true;
        rigidBody.mass = 1.0f;
        SetPlayerAlpha(launchedAlpha);
        hasHitTarget = false;
        currentPowerRate = Mathf.Clamp01(powerRate);
        LaunchedSubject.OnNext(this);
        var launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, currentPowerRate);
        rigidBody.AddForce(launchDirection.normalized * launchForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Notifies when the player ball first collides with a target after launch.
    /// 発射後にプレイヤーボールが最初にターゲットへ衝突したことを通知します。
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
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

        if (rigidBody == null)
        {
            return;
        }

        rigidBody.position = transform.position;
        rigidBody.rotation = transform.rotation;
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
