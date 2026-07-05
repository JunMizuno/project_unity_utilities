using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    // Temporary in-game development setting.
    // Restore this to SceneControl.SCENE_NUM.Title when the title flow is needed again.
    // インゲーム実装確認用の一時設定です。
    // タイトル導線を戻すときは SceneControl.SCENE_NUM.Title に戻してください。
    [SerializeField]
    private SceneControl.SCENE_NUM initialScene = SceneControl.SCENE_NUM.GameMain;

    private SceneControl.SCENE_NUM currentContentScene = SceneControl.SCENE_NUM.None;

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
    /// Loads the initial content scene after the game control scene starts.
    /// ゲーム管理シーンの開始後に初期コンテンツシーンを読み込みます。
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
    /// Reloads the main game scene from its initial state.
    /// メインゲームシーンを初期状態から読み込み直します。
    /// </summary>
    public void ReloadGameMainScene()
    {
        ReloadScene(SceneControl.SCENE_NUM.GameMain);
    }

    /// <summary>
    /// Reloads the currently active content scene.
    /// 現在のコンテンツシーンを読み込み直します。
    /// </summary>
    public void ReloadCurrentContentScene()
    {
        if (currentContentScene == SceneControl.SCENE_NUM.None)
        {
            LoadScene(initialScene);
            return;
        }

        ReloadScene(currentContentScene);
    }

    /// <summary>
    /// Starts loading the requested content scene.
    /// 指定したコンテンツシーンの読み込みを開始します。
    /// </summary>
    public void LoadScene(SceneControl.SCENE_NUM scene)
    {
        if (scene == SceneControl.SCENE_NUM.None || isLoading)
        {
            return;
        }

        LoadSceneAsync(scene).Forget();
    }

    /// <summary>
    /// Starts reloading the requested content scene.
    /// 指定したコンテンツシーンの再読み込みを開始します。
    /// </summary>
    public void ReloadScene(SceneControl.SCENE_NUM scene)
    {
        if (scene == SceneControl.SCENE_NUM.None || isLoading)
        {
            return;
        }

        ReloadSceneAsync(scene).Forget();
    }

    /// <summary>
    /// Loads a content scene additively and unloads the previous content scene.
    /// コンテンツシーンを加算読み込みし、直前のコンテンツシーンをアンロードします。
    /// </summary>
    private async UniTaskVoid LoadSceneAsync(SceneControl.SCENE_NUM scene)
    {
        isLoading = true;
        try
        {
            var sceneName = SceneControl.GetSceneName(scene);
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            var previousContentScene = currentContentScene;
            var loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                var loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    return;
                }

                await loadOperation.ToUniTask();
                loadedScene = SceneManager.GetSceneByName(sceneName);
            }

            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }

            currentContentScene = scene;
            await UnloadPreviousContentSceneAsync(previousContentScene, scene);
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Unloads and loads the requested content scene to rebuild it from the beginning.
    /// 指定したコンテンツシーンをアンロードしてから読み込み、最初から作り直します。
    /// </summary>
    private async UniTaskVoid ReloadSceneAsync(SceneControl.SCENE_NUM scene)
    {
        isLoading = true;
        try
        {
            var sceneName = SceneControl.GetSceneName(scene);
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            var loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                SetActiveSceneIfLoaded(SceneControl.SCENE_NUM.GameControl);
                var unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                if (unloadOperation != null)
                {
                    await unloadOperation.ToUniTask();
                }
            }

            var loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                return;
            }

            await loadOperation.ToUniTask();
            loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }

            currentContentScene = scene;
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Unloads the previous content scene while keeping the game control scene alive.
    /// ゲーム管理シーンを残したまま、直前のコンテンツシーンをアンロードします。
    /// </summary>
    private async UniTask UnloadPreviousContentSceneAsync(
        SceneControl.SCENE_NUM previousScene,
        SceneControl.SCENE_NUM nextScene)
    {
        if (previousScene == SceneControl.SCENE_NUM.None
            || previousScene == nextScene
            || previousScene == SceneControl.SCENE_NUM.GameControl)
        {
            return;
        }

        var previousSceneName = SceneControl.GetSceneName(previousScene);
        if (string.IsNullOrEmpty(previousSceneName))
        {
            return;
        }

        var loadedPreviousScene = SceneManager.GetSceneByName(previousSceneName);
        if (!loadedPreviousScene.IsValid() || !loadedPreviousScene.isLoaded)
        {
            return;
        }

        var unloadOperation = SceneManager.UnloadSceneAsync(loadedPreviousScene);
        if (unloadOperation != null)
        {
            await unloadOperation.ToUniTask();
        }
    }

    /// <summary>
    /// Sets the specified loaded scene as active when it is available.
    /// 指定したシーンが読み込み済みの場合にアクティブシーンへ設定します。
    /// </summary>
    private void SetActiveSceneIfLoaded(SceneControl.SCENE_NUM scene)
    {
        var sceneName = SceneControl.GetSceneName(scene);
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        var loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }
    }
}
