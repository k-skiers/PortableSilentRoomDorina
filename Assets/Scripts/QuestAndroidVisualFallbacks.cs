using Oculus.Interaction;
using UnityEngine;

public sealed class QuestAndroidVisualFallbacks : MonoBehaviour
{
    [SerializeField] private Material androidWaterMaterial;
    [SerializeField] private Material androidHandMaterial;

    private void Start()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        MeshRenderer waterRenderer = GetComponent<MeshRenderer>();
        if (waterRenderer == null)
        {
            Debug.LogError("Quest visual fallback could not find the WaterPlane MeshRenderer.", this);
        }
        else if (androidWaterMaterial == null)
        {
            Debug.LogError("Quest visual fallback has no Android water material assigned.", this);
        }
        else
        {
            waterRenderer.sharedMaterial = androidWaterMaterial;
        }

        if (androidHandMaterial == null)
        {
            Debug.LogError("Quest visual fallback has no Android hand material assigned.", this);
            return;
        }

        HandVisual[] handVisuals = FindObjectsOfType<HandVisual>(true);
        int replacedRenderers = 0;
        foreach (HandVisual handVisual in handVisuals)
        {
            if (handVisual == null)
            {
                continue;
            }

            SkinnedMeshRenderer[] renderers = handVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.sharedMaterial = androidHandMaterial;
                    replacedRenderers++;
                }
            }
        }

        if (replacedRenderers < 2)
        {
            Debug.LogWarning(
                $"Quest visual fallback replaced only {replacedRenderers} hand renderers; expected both hands.",
                this);
        }
        else
        {
            Debug.Log($"Quest visual fallback replaced {replacedRenderers} hand renderers.", this);
        }
    }
}
