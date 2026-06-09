using UnityEngine;
using UnityEngine.UI;

public class PlayerLaunchDirectionIndicator : MonoBehaviour
{
    [SerializeField]
    private RectTransform directionLineRect;

    [SerializeField]
    private RectTransform pitchKnobRect;

    [SerializeField]
    private RectTransform pitchGaugeRect;

    [SerializeField]
    private Text angleText;

    [SerializeField]
    private float minVerticalDisplayAngle = -45.0f;

    [SerializeField]
    private float maxVerticalDisplayAngle = 45.0f;

    /// <summary>
    /// Initializes the scene-placed launch direction indicator.
    /// シーン配置済みの発射方向インジケーターを初期化します。
    /// </summary>
    void Start()
    {
        SetAngles(0.0f, 0.0f);
        SetAngleText(0.0f, 0.0f);
    }

    /// <summary>
    /// Applies the selected launch angles to the 2D indicator.
    /// 選択中の発射角度を2Dインジケーターに反映します。
    /// </summary>
    public void SetAngles(float verticalAngle, float horizontalAngle)
    {
        if (directionLineRect != null)
        {
            directionLineRect.localRotation = Quaternion.Euler(0.0f, 0.0f, -horizontalAngle);
        }

        if (pitchKnobRect == null || pitchGaugeRect == null)
        {
            return;
        }

        var pitchRate = Mathf.InverseLerp(minVerticalDisplayAngle, maxVerticalDisplayAngle, verticalAngle);
        var yPosition = Mathf.Lerp(-pitchGaugeRect.sizeDelta.y * 0.5f, pitchGaugeRect.sizeDelta.y * 0.5f, pitchRate);
        pitchKnobRect.anchoredPosition = new Vector2(pitchKnobRect.anchoredPosition.x, yPosition);
    }

    /// <summary>
    /// Updates the debug text for the selected launch angles.
    /// 選択中の発射角度のデバッグ表示を更新します。
    /// </summary>
    public void SetAngleText(float verticalAngle, float horizontalAngle)
    {
        if (angleText == null)
        {
            return;
        }

        angleText.text = $"X: {verticalAngle:0.0}°\nY: {horizontalAngle:0.0}°";
    }
}
