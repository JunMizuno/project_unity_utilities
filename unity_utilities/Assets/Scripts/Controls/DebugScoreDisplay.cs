using UnityEngine;
using UnityEngine.UI;

public class DebugScoreDisplay : MonoBehaviour
{
    public static DebugScoreDisplay Instance { get; private set; }

    [SerializeField]
    private string scorePrefix = "SCORE:";

    [SerializeField]
    private Text scoreText;

    [SerializeField]
    private int initialScore;

    private int currentScore;

    /// <summary>
    /// Registers this display and initializes the scene-placed score text.
    /// この表示を登録し、シーン配置済みのスコアテキストを初期化します。
    /// </summary>
    private void Awake()
    {
        Instance = this;
        SetScore(initialScore);
    }

    /// <summary>
    /// Clears the global reference when this display is destroyed.
    /// この表示が破棄されたときにグローバル参照を解除します。
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Updates the score value shown on the UI.
    /// UIに表示するスコア値を更新します。
    /// </summary>
    public void SetScore(int score)
    {
        currentScore = Mathf.Max(0, score);
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = $"{scorePrefix}{currentScore}";
    }
}
