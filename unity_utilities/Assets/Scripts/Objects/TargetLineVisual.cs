using UnityEngine;

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

    [SerializeField]
    private Color apexColor = new Color(1.0f, 0.92f, 0.98f, 1.0f);

    private static Material sharedLineMaterial;
    private static Material sharedBodyMaterial;
    private Mesh bodyMesh;
    private MaterialPropertyBlock bodyMaterialPropertyBlock;

    /// <summary>
    /// Builds the square pyramid line visual.
    /// 正四角錐のライン表示を構築します。
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
    /// Creates the solid square pyramid body so back-side lines are hidden by depth.
    /// 奥側のラインが透けて見えすぎないよう、正四角錐の本体メッシュを生成します。
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
        var baseY = -halfSize;
        var vertices = new[]
        {
            new Vector3(-halfSize, baseY, -halfSize),
            new Vector3(halfSize, baseY, -halfSize),
            new Vector3(halfSize, baseY, halfSize),
            new Vector3(-halfSize, baseY, halfSize),
            new Vector3(0.0f, halfSize, 0.0f)
        };

        var triangles = new[]
        {
            0, 2, 1,
            0, 3, 2,
            0, 1, 4,
            1, 2, 4,
            2, 3, 4,
            3, 0, 4
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
