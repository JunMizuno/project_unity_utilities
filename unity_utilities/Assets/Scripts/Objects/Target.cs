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
    /// Runs per-frame target processing.
    /// ターゲットのフレームごとの処理を実行します。
    /// </summary>
    void Update()
    {

    }
}
