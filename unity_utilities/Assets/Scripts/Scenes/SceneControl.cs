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
        GameMain,
    }

    public static readonly string[] SceneNames = {
        "None",
        "GameControl",
        "GameMain",
    };
}
