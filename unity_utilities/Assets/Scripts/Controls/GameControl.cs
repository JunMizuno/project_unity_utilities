using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using R3;

public class GameControl : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI fpsText;

    private static GameControl instance;
    private static readonly string CLASS_NAME = "GameControlScene";

    /// <summary>
    /// Ensures that the GameControl scene is loaded first when play mode starts from another scene in the Unity Editor.
    /// Unity Editorで別シーンから再生を開始した場合に、GameControlシーンを先に読み込むようにします。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void FirstLoad()
    {
#if UNITY_EDITOR
        if (string.Compare(SceneManager.GetActiveScene().name, CLASS_NAME, System.StringComparison.CurrentCulture) != 0)
        {
            SceneManager.LoadScene(CLASS_NAME);
        }
#endif
    }

    /// <summary>
    /// Initializes the persistent game controller and applies the target frame rate.
    /// 永続化するゲーム管理オブジェクトを初期化し、目標フレームレートを設定します。
    /// </summary>
    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        if (!this.enabled)
        {
            return;
        }

        GameControl.instance = this;
        Application.targetFrameRate = 60;
    }

    /// <summary>
    /// Loads the first playable scene and starts the FPS display update.
    /// 最初にプレイするシーンを読み込み、FPS表示の更新を開始します。
    /// </summary>
    public void Start()
    {
        SceneManager.LoadSceneAsync((int)SceneControl.SCENE_NUM.Test, LoadSceneMode.Single);

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