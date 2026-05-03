using System;
using UnityEngine;
using R3;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBoby;

    void Start()
    {
        AddForceToPlayer();
    }

    void Update()
    {
        
    }

    private void AddForceToPlayer()
    {
        Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6))
            .Subscribe(_ =>
            {
                this.gameObject.transform.localPosition = new Vector3(0.0f, 2.0f, -8.0f);
                rigidBoby.linearVelocity = Vector3.zero;
                rigidBoby.angularVelocity = Vector3.zero;
                rigidBoby.useGravity = true;
                rigidBoby.AddForce(Vector3.forward * 12.0f, ForceMode.Impulse);
            })
            .AddTo(this);
    }
}
