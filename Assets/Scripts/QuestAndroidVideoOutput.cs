
using UnityEngine;
using UnityEngine.Video;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class QuestAndroidVideoOutput : MonoBehaviour
{
    [SerializeField]
    private Material androidScreenMaterial;

    private VideoPlayer videoPlayer;
    private Renderer screenRenderer;
    private RenderTexture videoRenderTexture;
    private Material runtimeMaterial;
    private Material originalMaterial;

    private bool configured;
    private bool firstFrameDecoded;
    private float startTime;
    private float nextLogTime;

    private void Awake()
    {
        // Preserve existing Windows and Editor behaviour.
        if (Application.platform != RuntimePlatform.Android)
            return;

        videoPlayer = GetComponent<VideoPlayer>();
        screenRenderer = GetComponent<Renderer>();

        if (videoPlayer == null ||
            screenRenderer == null ||
            androidScreenMaterial == null ||
            videoPlayer.clip == null)
        {
            Debug.LogError("Quest TV: Missing required component, material or video clip.");
            return;
        }

        originalMaterial = screenRenderer.sharedMaterial;

        videoPlayer.playOnAwake = false;
        videoPlayer.Stop();

        int width = 1280;
        int height = 720;

        if (videoPlayer.clip.width > 0 &&
            videoPlayer.clip.height > 0)
        {
            width = Mathf.Min(
                (int)videoPlayer.clip.width, 1280);

            height = Mathf.Max(2, Mathf.RoundToInt(
                width * (float)videoPlayer.clip.height /
                videoPlayer.clip.width));
        }

        videoRenderTexture = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default
        );

        videoRenderTexture.name = "Quest TV Render Texture";
        videoRenderTexture.antiAliasing = 1;
        videoRenderTexture.useMipMap = false;
        videoRenderTexture.autoGenerateMips = false;
        videoRenderTexture.filterMode = FilterMode.Bilinear;
        videoRenderTexture.wrapMode = TextureWrapMode.Clamp;

        if (!videoRenderTexture.Create())
        {
            Debug.LogError("Quest TV: Render Texture creation failed.");
            return;
        }

        runtimeMaterial = new Material(androidScreenMaterial);

        if (!runtimeMaterial.HasProperty("_BaseMap"))
        {
            Debug.LogError("Quest TV: Material has no Base Map.");
            return;
        }

        runtimeMaterial.SetTexture(
            "_BaseMap", videoRenderTexture);

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", Color.white);

        if (runtimeMaterial.HasProperty("_MainTex"))
            runtimeMaterial.SetTexture(
                "_MainTex", videoRenderTexture);

        screenRenderer.sharedMaterial = runtimeMaterial;

        ClearPropertyBlocks();

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.isLooping = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.frameReady += OnFrameReady;
        videoPlayer.sendFrameReadyEvents = true;

        configured = true;
    }

    private void Start()
    {
        if (!configured)
            return;

        startTime = Time.realtimeSinceStartup;
        nextLogTime = startTime + 3f;

        Debug.Log(
            "Quest TV: Preparing video " +
            videoPlayer.clip.name
        );

        videoPlayer.Prepare();
    }

    private void ClearPropertyBlocks()
    {
        screenRenderer.SetPropertyBlock(null);

        for (int i = 0;
             i < screenRenderer.sharedMaterials.Length;
             i++)
        {
            screenRenderer.SetPropertyBlock(null, i);
        }
    }

    private void OnPrepared(VideoPlayer source)
    {
        ClearPropertyBlocks();

        source.targetTexture = videoRenderTexture;

        runtimeMaterial.SetTexture(
            "_BaseMap", videoRenderTexture);

        Debug.Log("Quest TV: Video prepared successfully.");

        source.Play();
    }

    private void OnFrameReady(
        VideoPlayer source, long frameIndex)
    {
        if (firstFrameDecoded)
            return;

        firstFrameDecoded = true;

        ClearPropertyBlocks();

        runtimeMaterial.SetTexture(
            "_BaseMap", videoRenderTexture);

        Debug.Log(
            "Quest TV: First frame decoded: " +
            frameIndex
        );

        source.sendFrameReadyEvents = false;
    }

    private void OnVideoError(
        VideoPlayer source, string message)
    {
        Debug.LogError("Quest TV VIDEO ERROR: " + message);
    }

    private void Update()
    {
        if (!configured || firstFrameDecoded)
            return;

        if (Time.realtimeSinceStartup >= nextLogTime)
        {
            nextLogTime = Time.realtimeSinceStartup + 3f;

            Debug.Log(
                "Quest TV: Prepared=" +
                videoPlayer.isPrepared +
                " Playing=" + videoPlayer.isPlaying +
                " Frame=" + videoPlayer.frame
            );
        }

        if (Time.realtimeSinceStartup - startTime > 20f)
        {
            Debug.LogError(
                "Quest TV: No video frame after 20 seconds."
            );

            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.frameReady -= OnFrameReady;

            if (videoPlayer.targetTexture == videoRenderTexture)
            {
                videoPlayer.Stop();
                videoPlayer.targetTexture = null;
            }
        }

        if (screenRenderer != null && originalMaterial != null)
            screenRenderer.sharedMaterial = originalMaterial;

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
        }

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
