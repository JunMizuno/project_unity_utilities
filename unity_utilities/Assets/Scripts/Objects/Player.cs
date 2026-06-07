using UnityEngine;
using R3;

public class Player : MonoBehaviour
{
    public static readonly Subject<Player> LaunchedSubject = new Subject<Player>();

    [SerializeField]
    private Rigidbody rigidBody;

    [SerializeField]
    private float minLaunchForce = 10.0f;

    [SerializeField]
    private float maxLaunchForce = 30.0f;

    /// <summary>
    /// Resets the player ball and applies forward impulse.
    /// プレイヤーボールをリセットし、前方への力を加えます。
    /// </summary>
    public void AddForceToPlayer()
    {
        AddForceToPlayer(0.5f);
    }

    /// <summary>
    /// Resets the player ball and applies forward impulse based on the power rate.
    /// パワー割合に応じてプレイヤーボールをリセットし、前方への力を加えます。
    /// </summary>
    public void AddForceToPlayer(float powerRate)
    {
        AddForceToPlayer(powerRate, Vector3.forward);
    }

    /// <summary>
    /// Resets the player ball and applies impulse in the specified direction.
    /// 指定した方向へプレイヤーボールをリセットして力を加えます。
    /// </summary>
    public void AddForceToPlayer(float powerRate, Vector3 launchDirection)
    {
        this.gameObject.transform.localPosition = new Vector3(0.0f, 2.0f, -8.0f);
        this.gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.useGravity = true;
        rigidBody.mass = 1.0f;
        LaunchedSubject.OnNext(this);
        var launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, Mathf.Clamp01(powerRate));
        rigidBody.AddForce(launchDirection.normalized * launchForce, ForceMode.Impulse);
    }
}
