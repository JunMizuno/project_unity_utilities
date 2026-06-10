using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [SerializeField]
    private SceneControl.SCENE_NUM initialScene = SceneControl.SCENE_NUM.Title;

    private bool isLoading;

    /// <summary>
    /// Initializes the global scene manager instance.
    /// シーン遷移管理の静的インスタンスを初期化します。
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{nameof(GameSceneManager)} already exists.", this);
            Destroy(this);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Loads the initial scene after the persistent game control scene starts.
    /// 永続化するゲーム管理シーンの開始後に初期シーンを読み込みます。
    /// </summary>
    void Start()
    {
        LoadScene(initialScene);
    }

    /// <summary>
    /// Clears the global scene manager reference when this component is destroyed.
    /// このコンポーネントが破棄されたときに、シーン遷移管理の静的参照を解除します。
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Loads the title scene.
    /// タイトルシーンを読み込みます。
    /// </summary>
    public void LoadTitleScene()
    {
        LoadScene(SceneControl.SCENE_NUM.Title);
    }

    /// <summary>
    /// Loads the main game scene.
    /// メインゲームシーンを読み込みます。
    /// </summary>
    public void LoadGameMainScene()
    {
        LoadScene(SceneControl.SCENE_NUM.GameMain);
    }

    /// <summary>
    /// Loads the requested scene by build index.
    /// 指定したシーン番号のシーンをBuild Settingsの番号で読み込みます。
    /// </summary>
    public void LoadScene(SceneControl.SCENE_NUM scene)
    {
        if (scene == SceneControl.SCENE_NUM.None || isLoading)
        {
            return;
        }

        isLoading = true;
        var asyncOperation = SceneManager.LoadSceneAsync((int)scene, LoadSceneMode.Single);
        if (asyncOperation == null)
        {
            isLoading = false;
            return;
        }

        asyncOperation.completed += _ => isLoading = false;
    }
}
