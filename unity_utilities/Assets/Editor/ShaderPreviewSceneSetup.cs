using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShaderPreviewSceneSetup
{
    private const string ScenePath = "Assets/Scenes/ShaderPreviewScene.unity";

    [MenuItem("Tools/Shader Utilities/Open Shader Preview Scene")]
    public static void OpenShaderPreviewScene()
    {
        EnsureScene();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    public static void EnsureScene()
    {
        if (File.Exists(ScenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "ShaderPreviewScene";

        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1.5f, -6f), Quaternion.identity);
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        Light light = Object.FindFirstObjectByType<Light>();
        if (light != null)
        {
            light.transform.SetPositionAndRotation(
                new Vector3(0f, 3f, -2f),
                Quaternion.Euler(50f, -30f, 0f));
            light.intensity = 2f;
        }

        GameObject fpsDisplay = new("Debug FPS Display");
        fpsDisplay.AddComponent<DebugFpsDisplay>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Selection.activeGameObject = fpsDisplay;
    }
}
