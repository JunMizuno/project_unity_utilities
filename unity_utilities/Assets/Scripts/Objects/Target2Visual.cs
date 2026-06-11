using UnityEngine;

public class Target2Visual : MonoBehaviour
{
    [SerializeField]
    private float size = 1.0f;

    [SerializeField]
    private float lineWidth = 0.045f;

    [SerializeField]
    private Color baseFrontColor = new Color(1.0f, 0.95f, 0.35f, 1.0f);

    [SerializeField]
    private Color baseRightColor = new Color(0.35f, 1.0f, 0.45f, 1.0f);

    [SerializeField]
    private Color baseBackColor = new Color(0.2f, 0.85f, 1.0f, 1.0f);

    [SerializeField]
    private Color baseLeftColor = new Color(0.75f, 0.2f, 1.0f, 1.0f);

    [SerializeField]
    private Color apexColor = new Color(1.0f, 0.92f, 0.98f, 1.0f);

    private static Material sharedLineMaterial;

    /// <summary>
    /// Builds the square pyramid line visual.
    /// 正四角錐のライン表示を構築します。
    /// </summary>
    void Awake()
    {
        CreateVisualLines();
    }

    /// <summary>
    /// Recreates colored line renderers for each square pyramid edge.
    /// 正四角錐の各辺に色付きLineRendererを再生成します。
    /// </summary>
    private void CreateVisualLines()
    {
        ClearVisualLines();

        var halfSize = size * 0.5f;
        var baseY = -halfSize;
        var apex = new Vector3(0.0f, halfSize, 0.0f);
        var frontLeft = new Vector3(-halfSize, baseY, -halfSize);
        var frontRight = new Vector3(halfSize, baseY, -halfSize);
        var backRight = new Vector3(halfSize, baseY, halfSize);
        var backLeft = new Vector3(-halfSize, baseY, halfSize);

        CreateLine("Base_Front", frontLeft, frontRight, baseFrontColor, baseRightColor);
        CreateLine("Base_Right", frontRight, backRight, baseRightColor, baseBackColor);
        CreateLine("Base_Back", backRight, backLeft, baseBackColor, baseLeftColor);
        CreateLine("Base_Left", backLeft, frontLeft, baseLeftColor, baseFrontColor);
        CreateLine("Side_FrontLeft", frontLeft, apex, baseFrontColor, apexColor);
        CreateLine("Side_FrontRight", frontRight, apex, baseRightColor, apexColor);
        CreateLine("Side_BackRight", backRight, apex, baseBackColor, apexColor);
        CreateLine("Side_BackLeft", backLeft, apex, baseLeftColor, apexColor);
    }

    /// <summary>
    /// Deletes previously generated visual line children.
    /// 以前に生成したライン表示用の子オブジェクトを削除します。
    /// </summary>
    private void ClearVisualLines()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (!child.name.StartsWith("Target2Line_"))
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Creates one colored edge line.
    /// 色付きの辺ラインを1本生成します。
    /// </summary>
    private void CreateLine(string lineName, Vector3 startPosition, Vector3 endPosition, Color startColor, Color endColor)
    {
        var lineObject = new GameObject($"Target2Line_{lineName}");
        lineObject.transform.SetParent(transform, false);

        var lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.sharedMaterial = GetLineMaterial();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    /// <summary>
    /// Returns the shared unlit material used for target lines.
    /// ターゲットライン表示に使う共有Unlitマテリアルを返します。
    /// </summary>
    private Material GetLineMaterial()
    {
        if (sharedLineMaterial != null)
        {
            return sharedLineMaterial;
        }

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        sharedLineMaterial = new Material(shader)
        {
            name = "Target2LineMaterial"
        };
        sharedLineMaterial.SetColor("_BaseColor", Color.white);
        sharedLineMaterial.SetColor("_Color", Color.white);

        return sharedLineMaterial;
    }
}
