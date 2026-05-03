using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBoby;

    void Start()
    {
        rigidBoby.useGravity = true;
        rigidBoby.AddForce(Vector3.forward * 8.0f, ForceMode.Impulse);
    }

    void Update()
    {
        
    }
}
