public class SceneControl
{
    /// <summary>
    /// Defines scene build indexes that must match the Build Settings scene order.
    /// Build Settingsのシーン順と一致させる必要があるシーン番号を定義します。
    /// </summary>
    public enum SCENE_NUM
    {
        None = -1,
        GameControl = 0,
        Title,
        GameMain,
    }

    /// <summary>
    /// Returns the Unity scene asset name for the scene number.
    /// シーン番号に対応するUnityシーン名を返します。
    /// </summary>
    public static string GetSceneName(SCENE_NUM scene)
    {
        switch (scene)
        {
            case SCENE_NUM.GameControl:
                return "GameControlScene";
            case SCENE_NUM.Title:
                return "TitleScene";
            case SCENE_NUM.GameMain:
                return "GameMainScene";
            default:
                return string.Empty;
        }
    }
}
