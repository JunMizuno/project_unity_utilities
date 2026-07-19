using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraEffectControl : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera;

    // Duration of the camera shake effect in seconds.
    // Increase to keep shaking longer. Decrease to settle the camera sooner.
    // カメラシェイクが続く秒数です。
    // 上げると揺れが長く続き、下げると早く収まります。
    [SerializeField]
    private float shakeDurationSeconds = 0.35f;

    // Shake amplitude used when only a few objects are moving.
    // Increase to make small impacts more noticeable. Decrease to keep small impacts subtle.
    // 少数のオブジェクトだけが動いているときの揺れ幅です。
    // 上げると小さな衝突でも目立ち、下げると控えめになります。
    [SerializeField]
    private float weakShakeAmplitude = 0.2f;

    // Shake amplitude used for the default impact response.
    // Increase to make normal impacts stronger. Decrease to reduce ordinary shake.
    // 通常の衝突反応に使う揺れ幅です。
    // 上げると通常衝突の揺れが強くなり、下げると控えめになります。
    [SerializeField]
    private float normalShakeAmplitude = 0.45f;

    // Shake amplitude used when many objects are moving.
    // Increase to emphasize heavy impacts. Decrease to reduce strong-impact shake.
    // 多数のオブジェクトが動いているときの揺れ幅です。
    // 上げると大きな衝突をより強調し、下げると強衝突の揺れを抑えます。
    [SerializeField]
    private float strongShakeAmplitude = 1.0f;

    // Maximum moving object count that still uses weak shake.
    // Increase to classify more impacts as weak. Decrease to move impacts into normal shake sooner.
    // 弱い揺れとして扱う動的オブジェクト数の上限です。
    // 上げると弱判定が増え、下げると通常揺れへ移りやすくなります。
    [SerializeField]
    private int weakShakeMaxObjectCount = 2;

    // Minimum moving object count required for strong shake.
    // Increase to require more flying objects for strong shake. Decrease to trigger strong shake more often.
    // 強い揺れとして扱う動的オブジェクト数の下限です。
    // 上げると強揺れに必要な飛散数が増え、下げると強揺れが出やすくなります。
    [SerializeField]
    private int strongShakeMinObjectCount = 12;

    private CancellationTokenSource shakeCancellationTokenSource;

    private Vector3 currentShakeOffset;

    /// <summary>
    /// Initializes the camera reference used for camera effects.
    /// カメラエフェクトに使用するカメラ参照を初期化します。
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    /// <summary>
    /// Releases the active shake cancellation token.
    /// 実行中のシェイク用キャンセルトークンを解放します。
    /// </summary>
    private void OnDestroy()
    {
        StopShake();
    }

    /// <summary>
    /// Plays a camera shake selected by the number of moving objects.
    /// 動いているオブジェクト数に応じたカメラシェイクを再生します。
    /// </summary>
    public void PlayShakeByFlyingObjectCount(int flyingObjectCount)
    {
        // Use normal shake when the impact event fired but the moving object count was not captured yet.
        // 衝突イベントは発生したが動的オブジェクト数をまだ拾えていない場合は通常揺れを使います。
        if (flyingObjectCount <= 0)
        {
            PlayShake(normalShakeAmplitude);
            return;
        }

        // Select weak, normal, or strong shake from the moving object count.
        // 動的オブジェクト数から弱・通常・強の揺れを選択します。
        if (flyingObjectCount <= weakShakeMaxObjectCount)
        {
            PlayShake(weakShakeAmplitude);
            return;
        }

        if (flyingObjectCount >= strongShakeMinObjectCount)
        {
            PlayShake(strongShakeAmplitude);
            return;
        }

        PlayShake(normalShakeAmplitude);
    }

    /// <summary>
    /// Plays a camera shake using the specified amplitude.
    /// 指定した揺れ幅でカメラシェイクを再生します。
    /// </summary>
    public void PlayShake(float amplitude)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        StopShake();
        shakeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        ShakeAsync(Mathf.Max(0.0f, amplitude), shakeCancellationTokenSource.Token).Forget();
    }

    /// <summary>
    /// Stops the active camera shake and removes the current offset.
    /// 実行中のカメラシェイクを停止し、現在の揺れオフセットを取り除きます。
    /// </summary>
    public void StopShake()
    {
        if (shakeCancellationTokenSource != null)
        {
            shakeCancellationTokenSource.Cancel();
            shakeCancellationTokenSource.Dispose();
            shakeCancellationTokenSource = null;
        }

        RemoveCurrentShakeOffset();
    }

    /// <summary>
    /// Runs the camera shake after normal camera updates for each frame.
    /// 通常のカメラ更新後に各フレームのシェイクを実行します。
    /// </summary>
    private async UniTaskVoid ShakeAsync(float amplitude, CancellationToken cancellationToken)
    {
        var elapsedSeconds = 0.0f;
        while (elapsedSeconds < shakeDurationSeconds && !cancellationToken.IsCancellationRequested)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken).SuppressCancellationThrow();
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            elapsedSeconds += Time.deltaTime;
            var shakeRate = 1.0f - Mathf.Clamp01(elapsedSeconds / Mathf.Max(shakeDurationSeconds, 0.0001f));
            ApplyShakeOffset(amplitude * shakeRate);
        }

        RemoveCurrentShakeOffset();
    }

    /// <summary>
    /// Applies a random camera offset on the screen plane.
    /// 画面平面上にランダムなカメラオフセットを適用します。
    /// </summary>
    private void ApplyShakeOffset(float amplitude)
    {
        RemoveCurrentShakeOffset();

        var randomOffset = Random.insideUnitCircle * amplitude;
        currentShakeOffset = targetCamera.transform.right * randomOffset.x
            + targetCamera.transform.up * randomOffset.y;
        targetCamera.transform.position += currentShakeOffset;
    }

    /// <summary>
    /// Removes the previously applied shake offset.
    /// 前回適用したシェイクオフセットを取り除きます。
    /// </summary>
    private void RemoveCurrentShakeOffset()
    {
        if (targetCamera == null || currentShakeOffset == Vector3.zero)
        {
            currentShakeOffset = Vector3.zero;
            return;
        }

        targetCamera.transform.position -= currentShakeOffset;
        currentShakeOffset = Vector3.zero;
    }
}
