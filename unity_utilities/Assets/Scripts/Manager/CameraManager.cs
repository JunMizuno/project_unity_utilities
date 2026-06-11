using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField]
    private Camera controlledCamera;

    [SerializeField]
    private Transform followTarget;

    [SerializeField]
    private Transform lookAtTarget;

    [SerializeField]
    private Vector3 followOffset = new Vector3(0.0f, 2.0f, -10.0f);

    [SerializeField]
    private float positionSmoothSpeed = 5.0f;

    [SerializeField]
    private float rotationSmoothSpeed = 8.0f;

    /// <summary>
    /// Initializes the managed camera reference.
    /// 管理対象カメラの参照を初期化します。
    /// </summary>
    private void Awake()
    {
        if (controlledCamera == null)
        {
            controlledCamera = Camera.main;
        }
    }

    /// <summary>
    /// Applies camera follow and look-at behavior after target movement has finished.
    /// ターゲットの移動後にカメラの追従と注視を反映します。
    /// </summary>
    private void LateUpdate()
    {
        if (controlledCamera == null)
        {
            return;
        }

        UpdateCameraPosition();
        UpdateCameraRotation();
    }

    /// <summary>
    /// Sets the current follow target.
    /// 現在の追従ターゲットを設定します。
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    /// <summary>
    /// Sets the current look-at target.
    /// 現在の注視ターゲットを設定します。
    /// </summary>
    public void SetLookAtTarget(Transform target)
    {
        lookAtTarget = target;
    }

    /// <summary>
    /// Sets both follow and look-at targets.
    /// 追従ターゲットと注視ターゲットをまとめて設定します。
    /// </summary>
    public void SetTargets(Transform nextFollowTarget, Transform nextLookAtTarget)
    {
        followTarget = nextFollowTarget;
        lookAtTarget = nextLookAtTarget;
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
