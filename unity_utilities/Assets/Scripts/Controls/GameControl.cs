using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using R3;

public class GameControl : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI fpsText;

    private static GameControl instance;

    /// <summary>
    /// Ensures that the GameControl scene is loaded first when play mode starts from another scene in the Unity Editor.
    /// Unity Editorで別シーンから再生を開始した場合に、GameControlシーンを先に読み込むようにします。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void FirstLoad()
    {
#if UNITY_EDITOR
        var gameControlSceneName = SceneControl.GetSceneName(SceneControl.SCENE_NUM.GameControl);
        if (string.Compare(SceneManager.GetActiveScene().name, gameControlSceneName, System.StringComparison.CurrentCulture) != 0)
        {
            SceneManager.LoadScene(gameControlSceneName);
        }
#endif
    }

    /// <summary>
    /// Initializes the game controller and applies the target frame rate.
    /// ゲーム管理オブジェクトを初期化し、目標フレームレートを設定します。
    /// </summary>
    public void Awake()
    {
        if (!this.enabled)
        {
            return;
        }

        GameControl.instance = this;
        Application.targetFrameRate = 60;
    }

    /// <summary>
    /// Starts the FPS display update.
    /// FPS表示の更新を開始します。
    /// </summary>
    public void Start()
    {
        SetCalcFPS();
    }

    /// <summary>
    /// Runs per-frame game controller processing.
    /// ゲーム管理のフレームごとの処理を実行します。
    /// </summary>
    public void Update()
    {

    }

    /// <summary>
    /// Runs fixed-step game controller processing.
    /// ゲーム管理の固定ステップ処理を実行します。
    /// </summary>
    public void FixedUpdate()
    {

    }

    /// <summary>
    /// Clears the static game controller reference when this object is destroyed.
    /// このオブジェクトが破棄されたときに、ゲーム管理の静的参照を解除します。
    /// </summary>
    public void OnDestroy()
    {
        GameControl.instance = null;
    }

    /// <summary>
    /// Handles application focus changes.
    /// アプリケーションのフォーカス変更を処理します。
    /// </summary>
    public void OnApplicationFocus(bool focus)
    {

    }

    /// <summary>
    /// Handles application pause changes.
    /// アプリケーションのポーズ状態の変更を処理します。
    /// </summary>
    public void OnApplicationPause(bool pause)
    {

    }

    /// <summary>
    /// Handles application quit processing.
    /// アプリケーション終了時の処理を実行します。
    /// </summary>
    public void OnApplicationQuit()
    {

    }

    /// <summary>
    /// Updates the FPS text every frame by using R3.
    /// R3を使用して、FPSテキストを毎フレーム更新します。
    /// </summary>
    private void SetCalcFPS()
    {
        Observable.EveryUpdate()
            .Select(_ => 1f / Time.unscaledDeltaTime)
            .Subscribe(fps =>
            {
                fpsText.text = $"FPS:{fps:0.}";
            })
            .AddTo(this);
    }
}
