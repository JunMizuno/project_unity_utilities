using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameControl : MonoBehaviour
{
    private static GameControl instance;
    private static readonly string CLASS_NAME = "GameControlScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void FirstLoad()
    {
#if UNITY_EDITOR
        // @memo.mizuno string.CompareTo()を使用すると「カルチャ対応」アラートが出たため以下のようにする。
        if (string.Compare(SceneManager.GetActiveScene().name, CLASS_NAME, System.StringComparison.CurrentCulture) != 0)
        {
            SceneManager.LoadScene(CLASS_NAME);
        }
#endif
    }

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

    public void Start()
    {
        // @memo.mizuno ここで最初に起動するシーンを指定する。
        // @memo.mizuno LoadSceneMode.Additiveだとシーンが重複して生成されてしまう。(対象のシーンがDontDestroyOnLoadを含んでいる場合に限る。)
        SceneManager.LoadSceneAsync((int)SceneControl.SCENE_NUM.Test, LoadSceneMode.Single);
    }

    public void Update()
    {

    }

    public void FixedUpdate()
    {

    }

    public void OnDestroy()
    {
        GameControl.instance = null;
    }

    public void OnApplicationFocus(bool focus)
    {

    }

    public void OnApplicationPause(bool pause)
    {

    }

    public void OnApplicationQuit()
    {

    }
}