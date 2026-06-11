using UnityEngine;

public class TargetLineVisual : MonoBehaviour
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
    private Color topColor = new Color(1.0f, 0.92f, 0.98f, 1.0f);

    [SerializeField]
    private bool isLineColorSliding;

    [SerializeField]
    private float lineColorSlideSpeed = 0.35f;

    private static Material sharedLineMaterial;
    private readonly System.Collections.Generic.List<LineColorState> lineColorStates = new System.Collections.Generic.List<LineColorState>();

    /// <summary>
    /// Builds the square prism line visual.
    /// 正四角柱のライン表示を構築します。
    /// </summary>
    void Awake()
    {
        CreateVisualLines();
    }

    /// <summary>
    /// Updates the edge colors when color sliding is enabled.
    /// 色スライドが有効な場合に辺の色を更新します。
    /// </summary>
    private void Update()
    {
        if (!isLineColorSliding)
        {
            return;
        }

        UpdateLineColors(Time.time * lineColorSlideSpeed);
    }

    /// <summary>
    /// Recreates colored line renderers for each square prism edge.
    /// 正四角柱の各辺に色付きLineRendererを再生成します。
    /// </summary>
    private void CreateVisualLines()
    {
        ClearVisualLines();
        lineColorStates.Clear();

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

        lineColorStates.Add(new LineColorState(lineRenderer, startColor, endColor));
    }

    /// <summary>
    /// Slides each stored line color through the hue circle.
    /// 保存した各ライン色を色相環に沿ってスライドさせます。
    /// </summary>
    private void UpdateLineColors(float hueOffset)
    {
        for (var i = 0; i < lineColorStates.Count; i++)
        {
            var lineColorState = lineColorStates[i];
            if (lineColorState.LineRenderer == null)
            {
                continue;
            }

            lineColorState.LineRenderer.startColor = ShiftHue(lineColorState.StartColor, hueOffset);
            lineColorState.LineRenderer.endColor = ShiftHue(lineColorState.EndColor, hueOffset);
        }
    }

    /// <summary>
    /// Returns a color with its hue shifted while preserving alpha.
    /// アルファ値を保ったまま色相をずらした色を返します。
    /// </summary>
    private Color ShiftHue(Color sourceColor, float hueOffset)
    {
        Color.RGBToHSV(sourceColor, out var hue, out var saturation, out var value);
        var shiftedHue = Mathf.Repeat(hue + hueOffset, 1.0f);
        var shiftedColor = Color.HSVToRGB(shiftedHue, saturation, value);
        shiftedColor.a = sourceColor.a;

        return shiftedColor;
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

    private sealed class LineColorState
    {
        /// <summary>
        /// Stores one line renderer and its base colors.
        /// 1本のLineRendererと基準色を保持します。
        /// </summary>
        public LineColorState(LineRenderer lineRenderer, Color startColor, Color endColor)
        {
            LineRenderer = lineRenderer;
            StartColor = startColor;
            EndColor = endColor;
        }

        public LineRenderer LineRenderer { get; }

        public Color StartColor { get; }

        public Color EndColor { get; }
    }
}
