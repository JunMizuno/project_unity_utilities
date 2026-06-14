using UnityEngine;
using R3;

public class CameraManager : MonoBehaviour
{
    private enum CameraMode
    {
        Manual,
        FollowTarget,
        LaunchChase,
        ReturnToInitialPose,
    }

    [SerializeField]
    private Camera controlledCamera;

    [SerializeField]
    private Transform followTarget;

    [SerializeField]
    private Transform lookAtTarget;

    [SerializeField]
    private Vector3 followOffset = new Vector3(0.0f, 2.0f, -10.0f);

    // Distance kept behind the launched object during launch chase.
    // Increase to keep the camera farther behind the ball. Decrease to move closer.
    // 発射追従中に対象の後ろへ保つ距離です。
    // 上げるとボールの後方から遠く追い、下げると近く追います。
    [SerializeField]
    private float launchFollowDistance = 5.0f;

    // Height offset added while chasing a launched object.
    // Increase to view the ball from higher up. Decrease to follow closer to the ball height.
    // 発射追従中に加える高さです。
    // 上げると高い視点になり、下げるとボールの高さに近い視点になります。
    [SerializeField]
    private float launchFollowHeight = 2.0f;

    [SerializeField]
    private float positionSmoothSpeed = 5.0f;

    [SerializeField]
    private float rotationSmoothSpeed = 8.0f;

    [SerializeField]
    private float returnToInitialSmoothSpeed = 6.0f;

    [SerializeField]
    private float returnToInitialCompleteDistance = 0.02f;

    [SerializeField]
    private float returnToInitialCompleteAngle = 0.5f;

    private CameraMode cameraMode = CameraMode.Manual;

    private Transform launchChaseTarget;

    private Rigidbody launchChaseRigidbody;

    private Vector3 launchChaseDirection = Vector3.forward;

    private Vector3 initialCameraPosition;

    private Quaternion initialCameraRotation;

    /// <summary>
    /// Initializes the managed camera reference and stores the initial camera pose.
    /// 管理対象カメラ参照を初期化し、初期カメラ姿勢を保持します。
    /// </summary>
    private void Awake()
    {
        if (controlledCamera == null)
        {
            controlledCamera = Camera.main;
        }

        if (controlledCamera != null)
        {
            initialCameraPosition = controlledCamera.transform.position;
            initialCameraRotation = controlledCamera.transform.rotation;
        }
    }

    /// <summary>
    /// Subscribes to player launch and hit events for camera chase behavior.
    /// カメラ追従用にプレイヤー発射と衝突イベントを購読します。
    /// </summary>
    private void Start()
    {
        Player.LaunchStartedSubject
            .Where(state => state.Player != null)
            .Subscribe(state => StartLaunchChase(state.Player.transform, state.LaunchDirection))
            .AddTo(this);

        Player.HitTargetSubject
            .Subscribe(_ => ReturnToInitialPose())
            .AddTo(this);
    }

    /// <summary>
    /// Applies camera behavior after target movement has finished.
    /// ターゲットの移動後にカメラ挙動を反映します。
    /// </summary>
    private void LateUpdate()
    {
        if (controlledCamera == null)
        {
            return;
        }

        switch (cameraMode)
        {
            case CameraMode.FollowTarget:
                UpdateCameraPosition();
                UpdateCameraRotation();
                break;
            case CameraMode.LaunchChase:
                UpdateLaunchChase();
                break;
            case CameraMode.ReturnToInitialPose:
                UpdateReturnToInitialPose();
                break;
        }
    }

    /// <summary>
    /// Sets the current follow target.
    /// 現在の追従ターゲットを設定します。
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        cameraMode = followTarget == null && lookAtTarget == null ? CameraMode.Manual : CameraMode.FollowTarget;
    }

    /// <summary>
    /// Sets the current look-at target.
    /// 現在の注視ターゲットを設定します。
    /// </summary>
    public void SetLookAtTarget(Transform target)
    {
        lookAtTarget = target;
        cameraMode = followTarget == null && lookAtTarget == null ? CameraMode.Manual : CameraMode.FollowTarget;
    }

    /// <summary>
    /// Sets both follow and look-at targets.
    /// 追従ターゲットと注視ターゲットをまとめて設定します。
    /// </summary>
    public void SetTargets(Transform nextFollowTarget, Transform nextLookAtTarget)
    {
        followTarget = nextFollowTarget;
        lookAtTarget = nextLookAtTarget;
        cameraMode = followTarget == null && lookAtTarget == null ? CameraMode.Manual : CameraMode.FollowTarget;
    }

    /// <summary>
    /// Moves and rotates the camera immediately to the current target state.
    /// 現在のターゲット状態へカメラを即時反映します。
    /// </summary>
    public void SnapToTargets()
    {
        if (controlledCamera == null)
        {
            return;
        }

        if (followTarget != null)
        {
            controlledCamera.transform.position = followTarget.position + followOffset;
        }

        if (lookAtTarget != null)
        {
            controlledCamera.transform.rotation = GetLookAtRotation(lookAtTarget.position);
        }
    }

    /// <summary>
    /// Starts chasing the launched target from behind its launch direction.
    /// 発射方向の後ろから発射対象を追従します。
    /// </summary>
    public void StartLaunchChase(Transform target, Vector3 launchDirection)
    {
        if (target == null)
        {
            return;
        }

        launchChaseTarget = target;
        launchChaseRigidbody = target.GetComponent<Rigidbody>();
        launchChaseDirection = launchDirection.sqrMagnitude > Mathf.Epsilon
            ? launchDirection.normalized
            : target.forward;
        cameraMode = CameraMode.LaunchChase;
    }

    /// <summary>
    /// Starts returning the camera to the pose captured at scene start.
    /// シーン開始時に記録したカメラ姿勢へ戻り始めます。
    /// </summary>
    public void ReturnToInitialPose()
    {
        launchChaseTarget = null;
        launchChaseRigidbody = null;
        followTarget = null;
        lookAtTarget = null;
        cameraMode = CameraMode.ReturnToInitialPose;
    }

    /// <summary>
    /// Sets the distance kept behind the launched target.
    /// 発射対象の後ろへ保つ距離を設定します。
    /// </summary>
    public void SetLaunchFollowDistance(float distance)
    {
        launchFollowDistance = Mathf.Max(0.0f, distance);
    }

    /// <summary>
    /// Sets the height offset used while chasing the launched target.
    /// 発射対象を追従するときの高さを設定します。
    /// </summary>
    public void SetLaunchFollowHeight(float height)
    {
        launchFollowHeight = height;
    }

    /// <summary>
    /// Updates the camera position when a follow target is assigned.
    /// 追従ターゲットが設定されている場合にカメラ位置を更新します。
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (followTarget == null)
        {
            return;
        }

        var targetPosition = followTarget.position + followOffset;
        controlledCamera.transform.position = Vector3.Lerp(
            controlledCamera.transform.position,
            targetPosition,
            GetFrameLerpRate(positionSmoothSpeed));
    }

    /// <summary>
    /// Updates the camera position and rotation while chasing a launched object.
    /// 発射されたオブジェクトを追従中のカメラ位置と回転を更新します。
    /// </summary>
    private void UpdateLaunchChase()
    {
        if (launchChaseTarget == null)
        {
            ReturnToInitialPose();
            return;
        }

        var followDirection = GetLaunchChaseDirection();
        var targetPosition = launchChaseTarget.position
            - followDirection * launchFollowDistance
            + Vector3.up * launchFollowHeight;
        controlledCamera.transform.position = Vector3.Lerp(
            controlledCamera.transform.position,
            targetPosition,
            GetFrameLerpRate(positionSmoothSpeed));

        var targetRotation = GetLookAtRotation(launchChaseTarget.position);
        controlledCamera.transform.rotation = Quaternion.Slerp(
            controlledCamera.transform.rotation,
            targetRotation,
            GetFrameLerpRate(rotationSmoothSpeed));
    }

    /// <summary>
    /// Updates the camera until it reaches the initial scene pose.
    /// シーン初期姿勢へ到達するまでカメラを更新します。
    /// </summary>
    private void UpdateReturnToInitialPose()
    {
        controlledCamera.transform.position = Vector3.Lerp(
            controlledCamera.transform.position,
            initialCameraPosition,
            GetFrameLerpRate(returnToInitialSmoothSpeed));
        controlledCamera.transform.rotation = Quaternion.Slerp(
            controlledCamera.transform.rotation,
            initialCameraRotation,
            GetFrameLerpRate(returnToInitialSmoothSpeed));

        var distance = Vector3.Distance(controlledCamera.transform.position, initialCameraPosition);
        var angle = Quaternion.Angle(controlledCamera.transform.rotation, initialCameraRotation);
        if (distance <= returnToInitialCompleteDistance && angle <= returnToInitialCompleteAngle)
        {
            controlledCamera.transform.position = initialCameraPosition;
            controlledCamera.transform.rotation = initialCameraRotation;
            cameraMode = CameraMode.Manual;
        }
    }

    /// <summary>
    /// Updates the camera rotation when a look-at target is assigned.
    /// 注視ターゲットが設定されている場合にカメラ回転を更新します。
    /// </summary>
    private void UpdateCameraRotation()
    {
        if (lookAtTarget == null)
        {
            return;
        }

        var targetRotation = GetLookAtRotation(lookAtTarget.position);
        controlledCamera.transform.rotation = Quaternion.Slerp(
            controlledCamera.transform.rotation,
            targetRotation,
            GetFrameLerpRate(rotationSmoothSpeed));
    }

    /// <summary>
    /// Returns the current chase direction from velocity or the original launch direction.
    /// 速度または発射時方向から現在の追従方向を返します。
    /// </summary>
    private Vector3 GetLaunchChaseDirection()
    {
        if (launchChaseRigidbody != null && launchChaseRigidbody.linearVelocity.sqrMagnitude > 0.01f)
        {
            launchChaseDirection = launchChaseRigidbody.linearVelocity.normalized;
        }

        return launchChaseDirection.sqrMagnitude > Mathf.Epsilon
            ? launchChaseDirection.normalized
            : Vector3.forward;
    }

    /// <summary>
    /// Returns the rotation needed to look at the specified world position.
    /// 指定したワールド座標を注視するための回転を返します。
    /// </summary>
    private Quaternion GetLookAtRotation(Vector3 targetPosition)
    {
        var direction = targetPosition - controlledCamera.transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return controlledCamera.transform.rotation;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    /// <summary>
    /// Converts a smoothing speed into a frame-rate independent interpolation rate.
    /// 補間速度をフレームレート非依存の補間率へ変換します。
    /// </summary>
    private float GetFrameLerpRate(float smoothSpeed)
    {
        return 1.0f - Mathf.Exp(-Mathf.Max(0.0f, smoothSpeed) * Time.deltaTime);
    }
}
