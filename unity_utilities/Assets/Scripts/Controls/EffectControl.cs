using UnityEngine;

public class EffectControl : MonoBehaviour
{
    public static EffectControl Instance { get; private set; }

    [SerializeField]
    private PostEffectControl postEffectControl;

    [SerializeField]
    private ObjectEffectControl objectEffectControl;

    [SerializeField]
    private FieldEffectControl fieldEffectControl;

    public PostEffectControl PostEffect => postEffectControl;

    public ObjectEffectControl ObjectEffect => objectEffectControl;

    public FieldEffectControl FieldEffect => fieldEffectControl;

    /// <summary>
    /// Initializes the global effect controller and child effect references.
    /// エフェクト全体の管理インスタンスと子エフェクト参照を初期化します。
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{nameof(EffectControl)} already exists.", this);
            return;
        }

        Instance = this;
        SetEffectControlReferences();
    }

    /// <summary>
    /// Clears the global effect controller reference when this object is destroyed.
    /// このオブジェクトが破棄されたときに、エフェクト管理の静的参照を解除します。
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Completes missing child effect controller references from this hierarchy.
    /// 未設定の子エフェクト管理参照をこの階層から補完します。
    /// </summary>
    private void SetEffectControlReferences()
    {
        if (postEffectControl == null)
        {
            postEffectControl = GetComponentInChildren<PostEffectControl>(true);
        }

        if (objectEffectControl == null)
        {
            objectEffectControl = GetComponentInChildren<ObjectEffectControl>(true);
        }

        if (fieldEffectControl == null)
        {
            fieldEffectControl = GetComponentInChildren<FieldEffectControl>(true);
        }
    }
}
