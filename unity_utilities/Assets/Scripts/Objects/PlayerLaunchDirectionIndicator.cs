using UnityEngine;

public class PlayerLaunchDirectionIndicator : MonoBehaviour
{
    [SerializeField]
    private Vector3 viewportPosition = new Vector3(0.86f, 0.82f, 4.0f);

    [SerializeField]
    private Vector3 indicatorScale = new Vector3(0.08f, 0.08f, 1.2f);

    [SerializeField]
    private Color indicatorColor = new Color(0.95f, 0.95f, 0.35f, 1.0f);

    private Transform indicatorTransform;

    private Material indicatorMaterial;

    /// <summary>
    /// Creates the 3D launch direction indicator.
    /// 3Dの発射方向インジケーターを生成します。
    /// </summary>
    void Awake()
    {
        CreateIndicator();
    }

    /// <summary>
    /// Updates the indicator position so it stays near the upper-right screen area.
    /// 画面右上付近に残るようにインジケーター位置を更新します。
    /// </summary>
    void LateUpdate()
    {
        if (indicatorTransform == null || Camera.main == null)
        {
            return;
        }

        indicatorTransform.position = Camera.main.ViewportToWorldPoint(viewportPosition);
    }

    /// <summary>
    /// Applies the selected launch angles to the indicator.
    /// 選択中の発射角度をインジケーターに反映します。
    /// </summary>
    public void SetAngles(float verticalAngle, float horizontalAngle)
    {
        if (indicatorTransform == null || Camera.main == null)
        {
            return;
        }

        indicatorTransform.rotation = Camera.main.transform.rotation
            * Quaternion.Euler(-verticalAngle, horizontalAngle, 0.0f);
    }

    /// <summary>
    /// Creates the runtime 3D object used as the direction indicator.
    /// 方向表示として使用する実行時3Dオブジェクトを生成します。
    /// </summary>
    private void CreateIndicator()
    {
        var indicatorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicatorObject.name = "LaunchDirectionIndicator";

        var collider = indicatorObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        indicatorTransform = indicatorObject.transform;
        indicatorTransform.localScale = indicatorScale;

        indicatorMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            name = "LaunchDirectionIndicatorMaterial"
        };
        indicatorMaterial.SetColor("_BaseColor", indicatorColor);

        var meshRenderer = indicatorObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = indicatorMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    /// <summary>
    /// Releases the generated indicator material.
    /// 生成したインジケーターマテリアルを解放します。
    /// </summary>
    void OnDestroy()
    {
        if (indicatorMaterial != null)
        {
            Destroy(indicatorMaterial);
        }
    }
}
