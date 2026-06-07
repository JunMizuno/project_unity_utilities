using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBody;

    /// <summary>
    /// Initializes the target rigidbody reference.
    /// ターゲットのRigidbody参照を初期化します。
    /// </summary>
    void Awake()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody>();
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
    /// Returns whether this target has nearly stopped moving.
    /// ターゲットの移動がほぼ停止しているかどうかを返します。
    /// </summary>
    public bool IsSettled(float velocityThreshold, float angularVelocityThreshold)
    {
        if (rigidBody == null)
        {
            return true;
        }

        return rigidBody.linearVelocity.sqrMagnitude <= velocityThreshold * velocityThreshold
            && rigidBody.angularVelocity.sqrMagnitude <= angularVelocityThreshold * angularVelocityThreshold;
    }

    /// <summary>
    /// Runs per-frame target processing.
    /// ターゲットのフレームごとの処理を実行します。
    /// </summary>
    void Update()
    {

    }
}
