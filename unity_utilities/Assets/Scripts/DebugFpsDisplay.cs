using UnityEngine;

public sealed class DebugFpsDisplay : MonoBehaviour
{
    private const int TargetFrameRate = 60;
    private const float UpdateInterval = 0.25f;

    [SerializeField] private Vector2 offset = new(16f, 12f);
    [SerializeField] private Vector2 size = new(132f, 34f);

    private GUIStyle labelStyle;
    private float accumulatedTime;
    private int accumulatedFrames;
    private float currentFps;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }

    private void Awake()
    {
        InitializeFrameRate();
    }

    private void Update()
    {
        accumulatedTime += Time.unscaledDeltaTime;
        accumulatedFrames++;

        if (accumulatedTime < UpdateInterval)
        {
            return;
        }

        currentFps = accumulatedFrames / accumulatedTime;
        accumulatedTime = 0f;
        accumulatedFrames = 0;
    }

    private void OnGUI()
    {
        labelStyle ??= CreateLabelStyle();

        Rect rect = new(
            Screen.width - size.x - offset.x,
            offset.y,
            size.x,
            size.y);

        GUI.Label(rect, $"FPS: {currentFps:0.0}", labelStyle);
    }

    private static GUIStyle CreateLabelStyle()
    {
        return new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = Color.white,
                background = Texture2D.grayTexture
            }
        };
    }
}
