using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WaveSceneSetup
{
    private const string ScenePath = "Assets/Scenes/ShaderPreviewScene.unity";
    private const string ShaderName = "ShaderUtilities/WaveEquationWater";
    private const string MaterialPath = "Assets/Materials/WaveEquationWater.mat";
    private const string MeshPath = "Assets/Meshes/WavePlaneMesh.asset";
    private const string TexturePath = "Assets/Textures/WaterRippleNoise.png";

    [MenuItem("Tools/Shader Utilities/Setup Wave Preview")]
    public static void SetupWavePreview()
    {
        EnsureFolders();
        EnsureWaterTexture();
        Mesh mesh = EnsureWaveMesh();
        Material material = EnsureWaveMaterial();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject objectsRoot = GameObject.Find("Objects") ?? new GameObject("Objects");

        Transform existingWave = objectsRoot.transform.Find("Wave");
        if (existingWave != null)
        {
            Object.DestroyImmediate(existingWave.gameObject);
        }

        GameObject waveRoot = new("Wave");
        waveRoot.transform.SetParent(objectsRoot.transform, false);

        GameObject wavePlane = new("Wave Plane");
        wavePlane.transform.SetParent(waveRoot.transform, false);
        wavePlane.transform.localPosition = Vector3.zero;
        wavePlane.transform.localRotation = Quaternion.identity;
        wavePlane.transform.localScale = Vector3.one;

        MeshFilter meshFilter = wavePlane.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = wavePlane.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        PositionCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = waveRoot;
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Shaders");
        CreateFolder("Assets", "Materials");
        CreateFolder("Assets", "Meshes");
        CreateFolder("Assets", "Textures");
    }

    private static void CreateFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{folder}"))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void EnsureWaterTexture()
    {
        if (File.Exists(TexturePath))
        {
            return;
        }

        const int size = 256;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float rippleA = Mathf.Sin((u * 18f + v * 7f) * Mathf.PI * 2f);
                float rippleB = Mathf.Sin((u * -9f + v * 15f) * Mathf.PI * 2f);
                float noise = Mathf.PerlinNoise(u * 9.5f, v * 9.5f);
                float value = Mathf.Clamp01(0.52f + rippleA * 0.12f + rippleB * 0.08f + (noise - 0.5f) * 0.35f);
                texture.SetPixel(x, y, new Color(value * 0.55f, value * 0.85f, value, 1f));
            }
        }

        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TexturePath);
    }

    private static Mesh EnsureWaveMesh()
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null)
        {
            return existing;
        }

        const int xSegments = 160;
        const int zSegments = 96;
        const float width = 18f;
        const float depth = 10f;

        Vector3[] vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];

        int vertex = 0;
        for (int z = 0; z <= zSegments; z++)
        {
            for (int x = 0; x <= xSegments; x++)
            {
                float u = x / (float)xSegments;
                float v = z / (float)zSegments;
                vertices[vertex] = new Vector3((u - 0.5f) * width, 0f, (v - 0.5f) * depth);
                uvs[vertex] = new Vector2(u * 3f, v * 3f);
                vertex++;
            }
        }

        int index = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int row = z * (xSegments + 1);
                int nextRow = (z + 1) * (xSegments + 1);
                triangles[index++] = row + x;
                triangles[index++] = nextRow + x;
                triangles[index++] = row + x + 1;
                triangles[index++] = row + x + 1;
                triangles[index++] = nextRow + x;
                triangles[index++] = nextRow + x + 1;
            }
        }

        Mesh mesh = new()
        {
            name = "WavePlaneMesh",
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        AssetDatabase.CreateAsset(mesh, MeshPath);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static Material EnsureWaveMaterial()
    {
        Shader shader = Shader.Find(ShaderName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "WaveEquationWater"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
        material.SetColor("_DeepColor", new Color(0.015f, 0.18f, 0.28f, 1f));
        material.SetColor("_ShallowColor", new Color(0.12f, 0.64f, 0.78f, 1f));
        material.SetColor("_FoamColor", new Color(0.85f, 0.98f, 1f, 1f));
        material.SetFloat("_Amplitude", 0.32f);
        material.SetFloat("_WaveSpeed", 2.35f);
        material.SetFloat("_WaveScale", 1.75f);
        material.SetFloat("_FoamThreshold", 0.76f);
        material.SetFloat("_TextureStrength", 0.32f);
        material.SetFloat("_GlossHighlight", 0.85f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void PositionCamera()
    {
        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            return;
        }

        camera.transform.SetPositionAndRotation(
            new Vector3(0f, 5.2f, -7.2f),
            Quaternion.Euler(53f, 0f, 0f));
        camera.fieldOfView = 62f;
        camera.backgroundColor = new Color(0.05f, 0.12f, 0.18f, 1f);
    }
}
