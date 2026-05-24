using System;
using UnityEngine;
using R3;

public class CreateTargets : MonoBehaviour
{
    [SerializeField]
    GameObject targetPrefab;

    private bool trigger = default;

    void Start()
    {
        Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6))
            .Subscribe(_ =>
            {
                trigger = !trigger;
                CreateTargetObjects();
            })
            .AddTo(this);
    }

    void Update()
    {
        
    }

    private void CreateTargetObjects()
    {
        foreach (Transform child in this.gameObject.transform)
        {
            Destroy(child.gameObject);
        }

        var maxWidthCount = (short)0;
        var maxHeightCount = (short)0;
        var maxDepthCount = (short)0;

        maxWidthCount = 11;
        maxHeightCount = 11;
        maxDepthCount = 5;

        var centerX = ((float)maxWidthCount / 2.0f) * 0.5f - 0.25f;
        var centerZ = ((float)maxDepthCount / 2.0f) * 0.5f - 0.25f;

        for (var i = 0; i < maxWidthCount; i++)
        {
            for (var j = 0; j < maxHeightCount; j++)
            {
                for (var k = 0; k < maxDepthCount; k++)
                {
                    var instance = Instantiate(targetPrefab, this.gameObject.transform);
                    instance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    var xPos = 0.5f * i - centerX;
                    var yPos = 0.5f * (j + 1);
                    var zPos = 0.5f * k - centerZ;
                    if (!trigger)
                    {
                        zPos = 0.0f;
                    }
                    instance.transform.localPosition = new Vector3(xPos, yPos, zPos);

                    var rigidBody = instance.GetComponent<Rigidbody>();
                    if (rigidBody != null)
                    {
                        // change Kinematics
                    }
                }
            }
        }
    }
}
