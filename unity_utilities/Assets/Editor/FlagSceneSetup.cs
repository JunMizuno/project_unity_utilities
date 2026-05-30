using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FlagSceneSetup
{
    private const string ScenePath = "Assets/Scenes/ShaderPreviewScene.unity";
    private const string ShaderName = "ShaderUtilities/FlagWave";
    private const string FlagMaterialPath = "Assets/Materials/FlagWave.mat";
    private const string PoleMaterialPath = "Assets/Materials/FlagPole.mat";
    private const string MeshPath = "Assets/Meshes/FlagClothMesh.asset";
    private const string TexturePath = "Assets/Textures/FlagPattern.png";

    [MenuItem("Tools/Shader Utilities/Setup Flag Preview")]
    public static void SetupFlagPreview()
    {
        EnsureFolders();
        EnsureFlagTexture();
        Mesh clothMesh = EnsureFlagMesh();
        Material flagMaterial = EnsureFlagMaterial();
        Material poleMaterial = EnsurePoleMaterial();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject objectsRoot = GameObject.Find("Objects") ?? new GameObject("Objects");

        Transform existingFlag = objectsRoot.transform.Find("Flag");
        if (existingFlag != null)
        {
            Object.DestroyImmediate(existingFlag.gameObject);
        }

        GameObject flagRoot = new("Flag");
        flagRoot.transform.SetParent(objectsRoot.transform, false);
        flagRoot.transform.localPosition = new Vector3(-2.35f, 0.55f, 0f);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Flag Pole";
        pole.transform.SetParent(flagRoot.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.25f, 0.02f);
        pole.transform.localScale = new Vector3(0.075f, 1.55f, 0.075f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = poleMaterial;

        GameObject cloth = new("Flag Cloth");
        cloth.transform.SetParent(flagRoot.transform, false);
        cloth.transform.localPosition = new Vector3(0f, 2.02f, 0f);
        cloth.transform.localRotation = Quaternion.identity;

        MeshFilter meshFilter = cloth.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = clothMesh;

        MeshRenderer meshRenderer = cloth.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = flagMaterial;

        PositionCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = flagRoot;
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

    private static void EnsureFlagTexture()
    {
        if (File.Exists(TexturePath))
        {
            return;
        }

        const int width = 512;
        const int height = 320;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        Color top = new(0.88f, 0.12f, 0.10f, 1f);
        Color middle = new(1f, 0.96f, 0.82f, 1f);
        Color bottom = new(0.08f, 0.24f, 0.78f, 1f);
        Color emblem = new(1f, 0.82f, 0.18f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = y / (float)(height - 1);
                Color color = v > 0.66f ? top : v > 0.33f ? middle : bottom;
                float dx = x / (float)width - 0.5f;
                float dy = v - 0.5f;
                float star = Mathf.Abs(dx) + Mathf.Abs(dy) * 1.3f;
                if (star < 0.085f || (Mathf.Abs(dx) < 0.022f && Mathf.Abs(dy) < 0.18f))
                {
                    color = Color.Lerp(color, emblem, 0.9f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TexturePath);
    }

    private static Mesh EnsureFlagMesh()
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null)
        {
            return existing;
        }

        const int xSegments = 96;
        const int ySegments = 48;
        const float width = 4.6f;
        const float height = 2.4f;

        Vector3[] vertices = new Vector3[(xSegments + 1) * (ySegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * ySegments * 6];

        int vertex = 0;
        for (int y = 0; y <= ySegments; y++)
        {
            for (int x = 0; x <= xSegments; x++)
            {
                float u = x / (float)xSegments;
                float v = y / (float)ySegments;
                vertices[vertex] = new Vector3(u * width, (v - 0.5f) * height, 0f);
                uvs[vertex] = new Vector2(u, v);
                vertex++;
            }
        }

        int index = 0;
        for (int y = 0; y < ySegments; y++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int row = y * (xSegments + 1);
                int nextRow = (y + 1) * (xSegments + 1);
                triangles[index++] = row + x;
                triangles[index++] = row + x + 1;
                triangles[index++] = nextRow + x;
                triangles[index++] = row + x + 1;
                triangles[index++] = nextRow + x + 1;
                triangles[index++] = nextRow + x;
            }
        }

        Mesh mesh = new()
        {
            name = "FlagClothMesh",
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

    private static Material EnsureFlagMaterial()
    {
        Shader shader = Shader.Find(ShaderName);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(FlagMaterialPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "FlagWave"
            };
            AssetDatabase.CreateAsset(material, FlagMaterialPath);
        }

        material.shader = shader;
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_WaveAmplitude", 0.28f);
        material.SetFloat("_WaveSpeed", 2.4f);
        material.SetFloat("_WaveScale", 1.45f);
        material.SetFloat("_VerticalFlutter", 0.06f);
        material.SetFloat("_HoistStiffness", 1.7f);
        material.SetFloat("_LightBoost", 0.65f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static Material EnsurePoleMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(PoleMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = "FlagPole"
            };
            AssetDatabase.CreateAsset(material, PoleMaterialPath);
        }

        material.SetColor("_BaseColor", new Color(0.62f, 0.62f, 0.60f, 1f));
        material.SetFloat("_Smoothness", 0.55f);
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
            new Vector3(0.55f, 2.9f, -8.4f),
            Quaternion.Euler(12f, -2f, 0f));
        camera.fieldOfView = 46f;
        camera.backgroundColor = new Color(0.05f, 0.12f, 0.18f, 1f);
    }
}
