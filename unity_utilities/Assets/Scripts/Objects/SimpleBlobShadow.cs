using UnityEngine;

public class SimpleBlobShadow : MonoBehaviour
{
    [SerializeField]
    private Vector2 shadowScale = new Vector2(1.0f, 1.0f);

    [SerializeField]
    private float groundY = 0.02f;

    [SerializeField]
    private float maxVisibleHeight = 6.0f;

    [SerializeField]
    private float minAlpha = 0.05f;

    [SerializeField]
    private float maxAlpha = 0.28f;

    [SerializeField]
    private Color shadowColor = new Color(0.0f, 0.0f, 0.0f, 0.28f);

    private Transform shadowTransform;

    private Material shadowMaterial;

    private Texture2D shadowTexture;

    /// <summary>
    /// Creates the runtime shadow object.
    /// 実行時に使用する影オブジェクトを生成します。
    /// </summary>
    void Awake()
    {
        var shadowObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadowObject.name = $"{gameObject.name}_BlobShadow";
        shadowObject.transform.SetParent(transform, false);

        var collider = shadowObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        shadowTransform = shadowObject.transform;
        shadowMaterial = CreateShadowMaterial();

        var meshRenderer = shadowObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = shadowMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    /// <summary>
    /// Updates the shadow position and opacity after physics movement.
    /// 物理移動後に影の位置と透明度を更新します。
    /// </summary>
    void LateUpdate()
    {
        if (shadowTransform == null || shadowMaterial == null)
        {
            return;
        }

        var height = Mathf.Max(0.0f, transform.position.y - groundY);
        var visibleRate = 1.0f - Mathf.Clamp01(height / maxVisibleHeight);
        var alpha = Mathf.Lerp(minAlpha, maxAlpha, visibleRate);

        shadowTransform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        shadowTransform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        shadowTransform.localScale = new Vector3(shadowScale.x, shadowScale.y, 1.0f);

        var color = shadowColor;
        color.a = alpha;
        shadowMaterial.SetColor("_BaseColor", color);
        shadowTransform.gameObject.SetActive(alpha > 0.01f);
    }

    /// <summary>
    /// Builds a transparent unlit material for stable floor shadows.
    /// 安定した床影用の透明Unlitマテリアルを構築します。
    /// </summary>
    private Material CreateShadowMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        var material = new Material(shader)
        {
            name = $"{gameObject.name}_BlobShadowMaterial"
        };

        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetFloat("_Surface", 1.0f);
        material.SetFloat("_Blend", 0.0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0.0f);
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetTexture("_BaseMap", CreateShadowTexture());
        material.SetColor("_BaseColor", shadowColor);

        return material;
    }

    /// <summary>
    /// Creates a radial alpha texture for a soft blob shadow.
    /// 柔らかい丸影用の放射状アルファテクスチャを生成します。
    /// </summary>
    private Texture2D CreateShadowTexture()
    {
        const int textureSize = 64;
        shadowTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = $"{gameObject.name}_BlobShadowTexture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        var radius = textureSize * 0.5f;
        var pixels = new Color[textureSize * textureSize];

        for (var y = 0; y < textureSize; y++)
        {
            for (var x = 0; x < textureSize; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                var alpha = Mathf.SmoothStep(1.0f, 0.0f, distance);
                alpha *= alpha;
                pixels[y * textureSize + x] = new Color(1.0f, 1.0f, 1.0f, alpha);
            }
        }

        shadowTexture.SetPixels(pixels);
        shadowTexture.Apply(false, true);

        return shadowTexture;
    }

    /// <summary>
    /// Releases the generated material.
    /// 生成したマテリアルを解放します。
    /// </summary>
    void OnDestroy()
    {
        if (shadowMaterial != null)
        {
            Destroy(shadowMaterial);
        }

        if (shadowTexture != null)
        {
            Destroy(shadowTexture);
        }
    }
}
