using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBody;

    /// <summary>
    /// Resets the player ball and applies forward impulse.
    /// プレイヤーボールをリセットし、前方への力を加えます。
    /// </summary>
    public void AddForceToPlayer()
    {
        this.gameObject.transform.localPosition = new Vector3(0.0f, 2.0f, -8.0f);
        this.gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.useGravity = true;
        rigidBody.mass = 1.0f;
        rigidBody.AddForce(Vector3.forward * 20.0f, ForceMode.Impulse);
    }
}
