using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraEffectControl : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private float shakeDurationSeconds = 0.35f;

    [SerializeField]
    private float weakShakeAmplitude = 0.05f;

    [SerializeField]
    private float normalShakeAmplitude = 0.12f;

    [SerializeField]
    private float strongShakeAmplitude = 0.22f;

    [SerializeField]
    private int weakShakeMaxObjectCount = 2;

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
        if (flyingObjectCount <= 0)
        {
            PlayShake(normalShakeAmplitude);
            return;
        }

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
