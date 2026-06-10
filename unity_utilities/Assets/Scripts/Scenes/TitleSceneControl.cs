using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using R3;

public class TitleSceneControl : MonoBehaviour
{
    [SerializeField]
    private Graphic tapToStartGraphic;

    [SerializeField]
    private float blinkSpeed = 2.5f;

    [SerializeField]
    private float minAlpha = 0.2f;

    [SerializeField]
    private float maxAlpha = 1.0f;

    private bool isTransitionRequested;

    /// <summary>
    /// Starts title text blinking and tap input observation.
    /// タイトル文字の点滅とタップ入力の監視を開始します。
    /// </summary>
    void Start()
    {
        SetBlinkText();
        SetStartInput();
    }

    /// <summary>
    /// Updates the title text alpha with a smooth blinking wave by using R3.
    /// R3を使用して、滑らかな点滅波でタイトル文字の透明度を更新します。
    /// </summary>
    private void SetBlinkText()
    {
        Observable.EveryUpdate()
            .Where(_ => tapToStartGraphic != null)
            .Subscribe(_ =>
            {
                var wave = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1.0f) * 0.5f;
                var color = tapToStartGraphic.color;
                color.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
                tapToStartGraphic.color = color;
            })
            .AddTo(this);
    }

    /// <summary>
    /// Watches click and tap input, then requests transition to the main game scene.
    /// クリックとタップ入力を監視し、メインゲームシーンへの遷移を要求します。
    /// </summary>
    private void SetStartInput()
    {
        Observable.EveryUpdate()
            .Where(_ => !isTransitionRequested && IsStartInputPressed())
            .Subscribe(_ =>
            {
                isTransitionRequested = true;
                GameSceneManager.Instance?.LoadGameMainScene();
            })
            .AddTo(this);
    }

    /// <summary>
    /// Returns whether the title start input was pressed this frame.
    /// タイトル開始入力がこのフレームで押されたかを返します。
    /// </summary>
    private bool IsStartInputPressed()
    {
        return IsMousePressed() || IsTouchPressed();
    }

    /// <summary>
    /// Returns whether the left mouse button was pressed this frame.
    /// マウス左ボタンがこのフレームで押されたかを返します。
    /// </summary>
    private bool IsMousePressed()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    /// <summary>
    /// Returns whether the primary touch was pressed this frame.
    /// プライマリタッチがこのフレームで押されたかを返します。
    /// </summary>
    private bool IsTouchPressed()
    {
        return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
    }
}
