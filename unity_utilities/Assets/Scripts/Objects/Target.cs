using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBody;

    [SerializeField]
    private Renderer targetRenderer;

    /// <summary>
    /// Initializes the target rigidbody reference.
    /// ターゲットのコンポーネント参照を初期化します。
    /// </summary>
    void Awake()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
    }

    /// <summary>
    /// Sets whether this target is controlled by physics.
    /// このターゲットを物理演算で制御するかどうかを設定します。
    /// </summary>
    public void SetPhysicsEnabled(bool enabled)
    {
        if (rigidBody == null)
        {
            return;
        }

        rigidBody.isKinematic = !enabled;
        if (enabled)
        {
            rigidBody.WakeUp();
        }
        else
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.Sleep();
        }
    }

    /// <summary>
    /// Applies an explosion impulse to this target.
    /// このターゲットへ爆発方向の瞬間的な力を加えます。
    /// </summary>
    public void AddExplosionImpulse(Vector3 explosionPosition, float force, float radius, float upwardsModifier)
    {
        if (rigidBody == null)
        {
            return;
        }

        rigidBody.AddExplosionForce(force, explosionPosition, radius, upwardsModifier, ForceMode.Impulse);
    }

    /// <summary>
    /// Returns whether this target is moving above the velocity threshold.
    /// このターゲットが速度しきい値を超えて動いているかを返します。
    /// </summary>
    public bool IsMoving(float velocityThreshold)
    {
        if (rigidBody == null || rigidBody.isKinematic)
        {
            return false;
        }

        return rigidBody.linearVelocity.sqrMagnitude > velocityThreshold * velocityThreshold
            || rigidBody.angularVelocity.sqrMagnitude > velocityThreshold * velocityThreshold;
    }

    /// <summary>
    /// Returns whether this target is inside the camera viewport used for stop checks.
    /// 停止判定用のカメラ表示範囲内にこのターゲットがあるかを返します。
    /// </summary>
    public bool IsInCameraView(Camera targetCamera, float viewportPadding)
    {
        if (targetCamera == null)
        {
            return true;
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer == null)
        {
            return IsWorldPointInCameraView(targetCamera, transform.position, viewportPadding);
        }

        var bounds = targetRenderer.bounds;
        return IsWorldPointInCameraView(targetCamera, bounds.center, viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.min.x, bounds.min.y, bounds.min.z), viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.min.x, bounds.max.y, bounds.max.z), viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.max.x, bounds.max.y, bounds.min.z), viewportPadding)
            || IsWorldPointInCameraView(targetCamera, new Vector3(bounds.max.x, bounds.max.y, bounds.max.z), viewportPadding);
    }

    /// <summary>
    /// Returns whether the world position is inside the padded camera viewport.
    /// ワールド座標が余白込みのカメラ表示範囲内にあるかを返します。
    /// </summary>
    private bool IsWorldPointInCameraView(Camera targetCamera, Vector3 worldPosition, float viewportPadding)
    {
        var viewportPosition = targetCamera.WorldToViewportPoint(worldPosition);
        return viewportPosition.z > 0.0f
            && viewportPosition.x >= -viewportPadding
            && viewportPosition.x <= 1.0f + viewportPadding
            && viewportPosition.y >= -viewportPadding
            && viewportPosition.y <= 1.0f + viewportPadding;
    }

    /// <summary>
    /// Runs per-frame target processing.
    /// ターゲットのフレームごとの処理を実行します。
    /// </summary>
    void Update()
    {

    }
}
