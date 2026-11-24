using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class FullscreenVideo : MonoBehaviour
{
    public UnityEvent onFinished;

    private VideoPlayer vp;

    private void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.renderMode = VideoRenderMode.CameraNearPlane;
        vp.aspectRatio = VideoAspectRatio.FitInside; // mantém a proporção
        vp.loopPointReached += OnEnd;
        vp.Prepare(); // carrega antes, para não travar no primeiro frame
    }

    public void Play()
    {
        vp.targetCamera = Camera.main; // a câmera do XR Rig (precisa da tag MainCamera)
        vp.targetCameraAlpha = 1f;
        vp.Play();
    }

    private void OnEnd(VideoPlayer source)
    {
        vp.Stop();
        onFinished?.Invoke();
    }
}