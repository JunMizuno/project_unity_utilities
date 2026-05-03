using System;
using UnityEngine;
using R3;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBoby;

    void Start()
    {
        Observable.Timer(TimeSpan.FromSeconds(1))
            .Subscribe(_ =>
            {
                rigidBoby.useGravity = true;
                rigidBoby.AddForce(Vector3.forward * 12.0f, ForceMode.Impulse);
            })
            .AddTo(this);
    }

    void Update()
    {
        
    }
}
