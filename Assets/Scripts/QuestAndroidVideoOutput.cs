using UnityEngine;
using UnityEngine.Video;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class QuestAndroidVideoOutput : MonoBehaviour
{
    private const int MaxRenderTextureWidth = 1024;
    private const float PlaybackStatusInterval = 3f;
    private const float FirstFrameTimeout = 20f;

    [SerializeField] private Material androidScreenMaterial;

    private VideoPlayer videoPlayer;
    private Renderer screenRenderer;
    private RenderTexture videoRenderTexture;
    private Material originalMaterial;
    private Material runtimeMaterial;
    private VideoRenderMode originalRenderMode;
    private RenderTexture originalTargetTexture;
    private bool originalPlayOnAwake;
    private bool originalSendFrameReadyEvents;
    private bool configured;
    private bool firstFrameDecoded;
    private bool firstFrameTimeoutLogged;
    private float prepareRequestedAt;
    private float nextStatusLogAt;
    private int statusLogCount;

    private void Awake()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        videoPlayer = GetComponent<VideoPlayer>();
        screenRenderer = GetComponent<Renderer>();
        if (videoPlayer == null || screenRenderer == null || androidScreenMaterial == null ||
            videoPlayer.clip == null || !androidScreenMaterial.HasProperty("_BaseMap"))
        {
            Debug.LogError(
                "Quest TV output requires a VideoPlayer with a clip, a screen Renderer, and a URP material with _BaseMap.",
                this);
            enabled = false;
            return;
        }

        originalMaterial = screenRenderer.sharedMaterial;
        originalRenderMode = videoPlayer.renderMode;
        originalTargetTexture = videoPlayer.targetTexture;
        originalPlayOnAwake = videoPlayer.playOnAwake;
        originalSendFrameReadyEvents = videoPlayer.sendFrameReadyEvents;

        videoRenderTexture = CreateRenderTexture(videoPlayer.clip);
        if (videoRenderTexture == null)
        {
            Debug.LogError("Quest TV output could not create its video RenderTexture.", this);
            enabled = false;
            return;
        }

        runtimeMaterial = new Material(androidScreenMaterial)
        {
            name = androidScreenMaterial.name + " (Quest Runtime)"
        };
        runtimeMaterial.SetTexture("_BaseMap", videoRenderTexture);

        videoPlayer.playOnAwake = false;
        videoPlayer.Stop();
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.prepareCompleted += OnPrepareCompleted;
        videoPlayer.started += OnStarted;
        videoPlayer.errorReceived += OnErrorReceived;
        videoPlayer.frameReady += OnFrameReady;
        videoPlayer.sendFrameReadyEvents = true;
        screenRenderer.sharedMaterial = runtimeMaterial;
        configured = true;
    }

    private void Start()
    {
        if (configured)
        {
            prepareRequestedAt = Time.realtimeSinceStartup;
            Debug.Log(
                $"Preparing Quest TV clip '{videoPlayer.clip.name}' " +
                $"({videoPlayer.clip.width}x{videoPlayer.clip.height}, " +
                $"{videoPlayer.clip.audioTrackCount} audio tracks).",
                this);
            videoPlayer.Prepare();
        }
    }

    private void Update()
    {
        if (!configured || firstFrameDecoded)
        {
            return;
        }

        float elapsed = Time.realtimeSinceStartup - prepareRequestedAt;
        if (statusLogCount < 7 && elapsed >= nextStatusLogAt)
        {
            statusLogCount++;
            nextStatusLogAt = elapsed + PlaybackStatusInterval;
            Debug.Log(
                $"Quest TV state at {elapsed:F1}s: prepared={videoPlayer.isPrepared}, " +
                $"playing={videoPlayer.isPlaying}, frame={videoPlayer.frame}, " +
                $"time={videoPlayer.time:F2}s, renderMode={videoPlayer.renderMode}, " +
                $"targetMatches={videoPlayer.targetTexture == videoRenderTexture}, " +
                $"screenMaterialMatches={screenRenderer.sharedMaterial == runtimeMaterial}.",
                this);
        }

        if (!firstFrameTimeoutLogged && elapsed >= FirstFrameTimeout)
        {
            firstFrameTimeoutLogged = true;
            Debug.LogError(
                $"Quest TV has not decoded a frame after {FirstFrameTimeout:F0}s. " +
                $"prepared={videoPlayer.isPrepared}, playing={videoPlayer.isPlaying}, " +
                $"frame={videoPlayer.frame}, time={videoPlayer.time:F2}s.",
                this);
        }
    }

    private RenderTexture CreateRenderTexture(VideoClip clip)
    {
        uint sourceWidth = clip.width;
        uint sourceHeight = clip.height;
        int width = sourceWidth == 0u
            ? MaxRenderTextureWidth
            : sourceWidth > MaxRenderTextureWidth
                ? MaxRenderTextureWidth
                : (int)sourceWidth;
        int height = sourceWidth != 0u && sourceHeight != 0u
            ? Mathf.RoundToInt(width * (float)sourceHeight / sourceWidth)
            : 576;
        height = Mathf.Clamp(height, 144, 1024);

        var target = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB)
        {
            name = "Quest TV Video Frame",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        if (target.Create())
        {
            return target;
        }

        Destroy(target);
        return null;
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        Debug.Log(
            $"Quest TV clip prepared ({source.width}x{source.height}, " +
            $"isPrepared={source.isPrepared}); starting playback.",
            this);
        source.Play();
    }

    private void OnStarted(VideoPlayer source)
    {
        if (source == videoPlayer)
        {
            Debug.Log(
                $"Quest TV playback started: isPlaying={source.isPlaying}, " +
                $"isPrepared={source.isPrepared}, frame={source.frame}, time={source.time:F2}s.",
                this);
        }
    }

    private void OnFrameReady(VideoPlayer source, long frameIndex)
    {
        if (source != videoPlayer || firstFrameDecoded)
        {
            return;
        }

        firstFrameDecoded = true;
        source.frameReady -= OnFrameReady;
        source.sendFrameReadyEvents = originalSendFrameReadyEvents;
        string outputSize = source.targetTexture != null
            ? $"{source.targetTexture.width}x{source.targetTexture.height}"
            : "none";
        Debug.Log(
            $"Quest TV decoded first frame {frameIndex}; playback={source.isPlaying}, " +
            $"time={source.time:F2}s, output={outputSize}.",
            this);
    }

    private void OnErrorReceived(VideoPlayer source, string message)
    {
        if (source == videoPlayer)
        {
            Debug.LogError($"Quest TV playback error: {message}", this);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.started -= OnStarted;
            videoPlayer.errorReceived -= OnErrorReceived;
            videoPlayer.frameReady -= OnFrameReady;
            if (configured)
            {
                videoPlayer.sendFrameReadyEvents = originalSendFrameReadyEvents;
                videoPlayer.Stop();
                videoPlayer.renderMode = originalRenderMode;
                videoPlayer.targetTexture = originalTargetTexture;
                videoPlayer.playOnAwake = originalPlayOnAwake;
            }
        }

        if (screenRenderer != null && runtimeMaterial != null &&
            screenRenderer.sharedMaterial == runtimeMaterial)
        {
            screenRenderer.sharedMaterial = originalMaterial;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
        }
    }
}
