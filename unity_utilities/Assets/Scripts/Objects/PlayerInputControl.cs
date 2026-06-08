using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
    private float minVerticalAngle = -45.0f;

    [SerializeField]
    private float maxVerticalAngle = 45.0f;

    [SerializeField]
    private float minHorizontalAngle = -75.0f;

    [SerializeField]
    private float maxHorizontalAngle = 75.0f;

    // Controls how quickly the up/down keys change the vertical launch angle.
    // 上下キーで発射角度を変更する速度を調整します。
    [SerializeField]
    private float verticalAngleChangeSpeed = 45.0f;

    // Controls how quickly the left/right keys change the horizontal launch angle.
    // 左右キーで発射角度を変更する速度を調整します。
    [SerializeField]
    private float horizontalAngleChangeSpeed = 45.0f;

    private InputAction launchAction;

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
    /// Starts observing the launch and angle input by using R3.
    /// R3を使用して、発射入力と角度入力の監視を開始します。
    /// </summary>
    void Start()
    {
        SetLaunchInput();
        SetAngleInput();
        launchDirectionIndicator?.SetAngles(verticalAngle, horizontalAngle);
        launchDirectionIndicator?.SetAngleText(verticalAngle, horizontalAngle);
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
                var keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    return;
                }

                var verticalInput = GetPressedValue(keyboard.upArrowKey) - GetPressedValue(keyboard.downArrowKey);
                var horizontalInput = GetPressedValue(keyboard.rightArrowKey) - GetPressedValue(keyboard.leftArrowKey);

                verticalAngle = Mathf.Clamp(
                    verticalAngle + verticalInput * verticalAngleChangeSpeed * Time.deltaTime,
                    minVerticalAngle,
                    maxVerticalAngle);
                horizontalAngle = Mathf.Clamp(
                    horizontalAngle + horizontalInput * horizontalAngleChangeSpeed * Time.deltaTime,
                    minHorizontalAngle,
                    maxHorizontalAngle);

                launchDirectionIndicator?.SetAngles(verticalAngle, horizontalAngle);
                launchDirectionIndicator?.SetAngleText(verticalAngle, horizontalAngle);
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
    /// Returns one when the key is pressed.
    /// キーが押されている場合に1を返します。
    /// </summary>
    private float GetPressedValue(KeyControl key)
    {
        return key != null && key.isPressed ? 1.0f : 0.0f;
    }
}
