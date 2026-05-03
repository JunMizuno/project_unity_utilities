using System;
using UnityEngine;
using R3;

public class CreateTargets : MonoBehaviour
{
    [SerializeField]
    GameObject targetPrefab;

    void Start()
    {
        Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6))
            .Subscribe(_ =>
            {
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

        var maxWithCount = (short)0;
        var maxHeightCount = (short)0;

        maxWithCount = 9;
        maxHeightCount = 9;
        var center = 0;
        var side = maxWithCount / 2;
        var less = maxWithCount % 2;
        if (less > 0)
        {
            center = side + 1;
        }

        for (var i = 0; i < maxWithCount; i++)
        {
            for (var j = 0; j < maxHeightCount; j++)
            {
                var instance = Instantiate(targetPrefab, this.gameObject.transform);
                instance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                var xPos = 0.0f;
                if (center == 0)
                {
                    if (i < side)
                    {
                        xPos = -0.5f * (side - i);
                    }
                    else
                    {
                        xPos = 0.5f * (i - (side - 1));
                    }
                }
                else
                {
                    if (i != center - 1)
                    {
                        if (i < side)
                        {
                            xPos = -0.5f * (side - i);
                        }
                        else
                        {
                            xPos = 0.5f * (i - side);
                        }
                    }
                }
                instance.transform.localPosition = new Vector3(xPos, 0.5f * (j + 1), 0.0f);
            }
        }
    }
}
