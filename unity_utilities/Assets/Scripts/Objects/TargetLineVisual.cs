using UnityEngine;

using UnityEngine.Serialization;

public class TargetLineVisual : MonoBehaviour
{
    [SerializeField]
    private float size = 1.0f;

    [SerializeField]
    private Color bodyColor = new Color(0.48f, 0.56f, 0.46f, 1.0f);

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

    [FormerlySerializedAs("apexColor")]
    [SerializeField]
    private Color topColor = new Color(1.0f, 0.92f, 0.98f, 1.0f);

    private static Material sharedLineMaterial;
    private static Material sharedBodyMaterial;
    private Mesh bodyMesh;
    private MaterialPropertyBlock bodyMaterialPropertyBlock;

    /// <summary>
    /// Builds the square prism line visual.
    /// 正四角柱のライン表示を構築します。
    /// </summary>
    void Awake()
    {
        CreateBodyMesh();
        CreateVisualLines();
    }

    /// <summary>
    /// Releases the runtime mesh generated for this target visual.
    /// このターゲット表示用に実行時生成したメッシュを解放します。
    /// </summary>
    private void OnDestroy()
    {
        if (bodyMesh != null)
        {
            Destroy(bodyMesh);
            bodyMesh = null;
        }
    }

    /// <summary>
    /// Creates the solid square prism body so back-side lines are hidden by depth.
    /// 奥側のラインが透けて見えすぎないよう、正四角柱の本体メッシュを生成します。
    /// </summary>
    private void CreateBodyMesh()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        var halfSize = size * 0.5f;
        var bottomY = -halfSize;
        var topY = halfSize;
        var vertices = new[]
        {
            new Vector3(-halfSize, bottomY, -halfSize),
            new Vector3(halfSize, bottomY, -halfSize),
            new Vector3(halfSize, bottomY, halfSize),
            new Vector3(-halfSize, bottomY, halfSize),
            new Vector3(-halfSize, topY, -halfSize),
            new Vector3(halfSize, topY, -halfSize),
            new Vector3(halfSize, topY, halfSize),
            new Vector3(-halfSize, topY, halfSize)
        };

        var triangles = new[]
        {
            0, 4, 5,
            0, 5, 1,
            1, 5, 6,
            1, 6, 2,
            2, 6, 7,
            2, 7, 3,
            3, 7, 4,
            3, 4, 0,
            4, 7, 6,
            4, 6, 5,
            3, 0, 1,
            3, 1, 2
        };

        bodyMesh = new Mesh
        {
            name = "TargetLineVisualBodyMesh",
            vertices = vertices,
            triangles = triangles
        };
        bodyMesh.RecalculateNormals();
        bodyMesh.RecalculateBounds();

        meshFilter.sharedMesh = bodyMesh;
        meshRenderer.sharedMaterial = GetBodyMaterial();
        ApplyBodyColor(meshRenderer);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    /// <summary>
    /// Recreates colored line renderers for each square prism edge.
    /// 正四角柱の各辺に色付きLineRendererを再生成します。
    /// </summary>
    private void CreateVisualLines()
    {
        ClearVisualLines();

        var halfSize = size * 0.5f;
        var bottomY = -halfSize;
        var topY = halfSize;
        var bottomFrontLeft = new Vector3(-halfSize, bottomY, -halfSize);
        var bottomFrontRight = new Vector3(halfSize, bottomY, -halfSize);
        var bottomBackRight = new Vector3(halfSize, bottomY, halfSize);
        var bottomBackLeft = new Vector3(-halfSize, bottomY, halfSize);
        var topFrontLeft = new Vector3(-halfSize, topY, -halfSize);
        var topFrontRight = new Vector3(halfSize, topY, -halfSize);
        var topBackRight = new Vector3(halfSize, topY, halfSize);
        var topBackLeft = new Vector3(-halfSize, topY, halfSize);

        CreateLine("Bottom_Front", bottomFrontLeft, bottomFrontRight, baseFrontColor, baseRightColor);
        CreateLine("Bottom_Right", bottomFrontRight, bottomBackRight, baseRightColor, baseBackColor);
        CreateLine("Bottom_Back", bottomBackRight, bottomBackLeft, baseBackColor, baseLeftColor);
        CreateLine("Bottom_Left", bottomBackLeft, bottomFrontLeft, baseLeftColor, baseFrontColor);
        CreateLine("Top_Front", topFrontLeft, topFrontRight, topColor, topColor);
        CreateLine("Top_Right", topFrontRight, topBackRight, topColor, topColor);
        CreateLine("Top_Back", topBackRight, topBackLeft, topColor, topColor);
        CreateLine("Top_Left", topBackLeft, topFrontLeft, topColor, topColor);
        CreateLine("Vertical_FrontLeft", bottomFrontLeft, topFrontLeft, baseFrontColor, topColor);
        CreateLine("Vertical_FrontRight", bottomFrontRight, topFrontRight, baseRightColor, topColor);
        CreateLine("Vertical_BackRight", bottomBackRight, topBackRight, baseBackColor, topColor);
        CreateLine("Vertical_BackLeft", bottomBackLeft, topBackLeft, baseLeftColor, topColor);
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
            if (!child.name.StartsWith("TargetLine_"))
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
        var lineObject = new GameObject($"TargetLine_{lineName}");
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

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        sharedLineMaterial = new Material(shader)
        {
            name = "TargetLineMaterial"
        };
        sharedLineMaterial.SetColor("_BaseColor", Color.white);
        sharedLineMaterial.SetColor("_Color", Color.white);

        return sharedLineMaterial;
    }

    /// <summary>
    /// Returns the shared material used for the solid target body.
    /// ターゲット本体メッシュに使う共有マテリアルを返します。
    /// </summary>
    private Material GetBodyMaterial()
    {
        if (sharedBodyMaterial != null)
        {
            return sharedBodyMaterial;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        sharedBodyMaterial = new Material(shader)
        {
            name = "TargetLineBodyMaterial"
        };
        sharedBodyMaterial.SetColor("_BaseColor", bodyColor);
        sharedBodyMaterial.SetColor("_Color", bodyColor);

        return sharedBodyMaterial;
    }

    /// <summary>
    /// Applies the body color without creating a unique material per target.
    /// ターゲットごとの専用マテリアルを増やさず本体色を適用します。
    /// </summary>
    private void ApplyBodyColor(Renderer meshRenderer)
    {
        if (bodyMaterialPropertyBlock == null)
        {
            bodyMaterialPropertyBlock = new MaterialPropertyBlock();
        }

        meshRenderer.GetPropertyBlock(bodyMaterialPropertyBlock);
        bodyMaterialPropertyBlock.SetColor("_BaseColor", bodyColor);
        bodyMaterialPropertyBlock.SetColor("_Color", bodyColor);
        meshRenderer.SetPropertyBlock(bodyMaterialPropertyBlock);
    }
}
