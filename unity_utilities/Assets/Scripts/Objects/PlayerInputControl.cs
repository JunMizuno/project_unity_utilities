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

    [SerializeField]
    private PlayerLaunchDirectionIndicator launchDirectionIndicator;

    [SerializeField]
    private float minVerticalAngle = 0.0f;

    [SerializeField]
    private float maxVerticalAngle = 75.0f;

    [SerializeField]
    private float minHorizontalAngle = -75.0f;

    [SerializeField]
    private float maxHorizontalAngle = 75.0f;

    [SerializeField]
    private float angleChangeSpeed = 45.0f;

    private InputAction launchAction;

    private InputAction upAction;

    private InputAction downAction;

    private InputAction leftAction;

    private InputAction rightAction;

    private float verticalAngle;

    private float horizontalAngle;

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

        if (launchDirectionIndicator == null)
        {
            launchDirectionIndicator = GetComponent<PlayerLaunchDirectionIndicator>();
        }

        launchAction = new InputAction("Launch", InputActionType.Button, "<Keyboard>/space");
        upAction = new InputAction("LaunchAngleUp", InputActionType.Button, "<Keyboard>/upArrow");
        downAction = new InputAction("LaunchAngleDown", InputActionType.Button, "<Keyboard>/downArrow");
        leftAction = new InputAction("LaunchAngleLeft", InputActionType.Button, "<Keyboard>/leftArrow");
        rightAction = new InputAction("LaunchAngleRight", InputActionType.Button, "<Keyboard>/rightArrow");
    }

    /// <summary>
    /// Enables the launch input action.
    /// 発射用の入力アクションを有効にします。
    /// </summary>
    void OnEnable()
    {
        launchAction?.Enable();
        upAction?.Enable();
        downAction?.Enable();
        leftAction?.Enable();
        rightAction?.Enable();
    }

    /// <summary>
    /// Starts observing the launch input by using R3.
    /// R3を使用して、発射入力の監視を開始します。
    /// </summary>
    void Start()
    {
        SetLaunchInput();
        SetAngleInput();
    }

    /// <summary>
    /// Disables the launch input action.
    /// 発射用の入力アクションを無効にします。
    /// </summary>
    void OnDisable()
    {
        launchAction?.Disable();
        upAction?.Disable();
        downAction?.Disable();
        leftAction?.Disable();
        rightAction?.Disable();
    }

    /// <summary>
    /// Releases the launch input action.
    /// 発射用の入力アクションを解放します。
    /// </summary>
    void OnDestroy()
    {
        launchAction?.Dispose();
        upAction?.Dispose();
        downAction?.Dispose();
        leftAction?.Dispose();
        rightAction?.Dispose();
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
                player.AddForceToPlayer(powerRate, CreateLaunchDirection());
            })
            .AddTo(this);
    }

    /// <summary>
    /// Updates the launch angles with the arrow keys.
    /// 矢印キーで発射角度を更新します。
    /// </summary>
    private void SetAngleInput()
    {
        Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                var verticalInput = GetPressedValue(upAction) - GetPressedValue(downAction);
                var horizontalInput = GetPressedValue(rightAction) - GetPressedValue(leftAction);

                verticalAngle = Mathf.Clamp(
                    verticalAngle + verticalInput * angleChangeSpeed * Time.deltaTime,
                    minVerticalAngle,
                    maxVerticalAngle);
                horizontalAngle = Mathf.Clamp(
                    horizontalAngle + horizontalInput * angleChangeSpeed * Time.deltaTime,
                    minHorizontalAngle,
                    maxHorizontalAngle);

                launchDirectionIndicator?.SetAngles(verticalAngle, horizontalAngle);
            })
            .AddTo(this);
    }

    /// <summary>
    /// Converts the selected launch angles into a launch direction.
    /// 選択中の発射角度を発射方向へ変換します。
    /// </summary>
    private Vector3 CreateLaunchDirection()
    {
        return Quaternion.Euler(-verticalAngle, horizontalAngle, 0.0f) * Vector3.forward;
    }

    /// <summary>
    /// Returns one when the action key is pressed.
    /// アクションキーが押されている場合に1を返します。
    /// </summary>
    private float GetPressedValue(InputAction action)
    {
        return action != null && action.IsPressed() ? 1.0f : 0.0f;
    }
}
