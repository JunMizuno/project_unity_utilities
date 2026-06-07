using UnityEngine;
using UnityEngine.InputSystem;
using R3;

[RequireComponent(typeof(Player))]
public class PlayerInputControl : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private PlayerPowerGauge powerGauge;

    private InputAction launchAction;

    /// <summary>
    /// Initializes the player reference and launch input action.
    /// プレイヤー参照と発射用の入力アクションを初期化します。
    /// </summary>
    void Awake()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }

        if (powerGauge == null)
        {
            powerGauge = GetComponent<PlayerPowerGauge>();
        }

        launchAction = new InputAction("Launch", InputActionType.Button, "<Keyboard>/space");
    }

    /// <summary>
    /// Enables the launch input action.
    /// 発射用の入力アクションを有効にします。
    /// </summary>
    void OnEnable()
    {
        launchAction?.Enable();
    }

    /// <summary>
    /// Starts observing the launch input by using R3.
    /// R3を使用して、発射入力の監視を開始します。
    /// </summary>
    void Start()
    {
        SetLaunchInput();
    }

    /// <summary>
    /// Disables the launch input action.
    /// 発射用の入力アクションを無効にします。
    /// </summary>
    void OnDisable()
    {
        launchAction?.Disable();
    }

    /// <summary>
    /// Releases the launch input action.
    /// 発射用の入力アクションを解放します。
    /// </summary>
    void OnDestroy()
    {
        launchAction?.Dispose();
    }

    /// <summary>
    /// Launches the player ball when the space key is pressed.
    /// スペースキーが押されたときにプレイヤーボールを発射します。
    /// </summary>
    private void SetLaunchInput()
    {
        Observable.EveryUpdate()
            .Where(_ => launchAction.WasPressedThisFrame())
            .Subscribe(_ =>
            {
                var powerRate = powerGauge != null ? powerGauge.PowerRate : 0.5f;
                player.AddForceToPlayer(powerRate);
            })
            .AddTo(this);
    }
}
