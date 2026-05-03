using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBoby;

    void Start()
    {
        rigidBoby.AddForce(Vector3.forward * 5.0f, ForceMode.Impulse);
    }

    void Update()
    {
        
    }
}
