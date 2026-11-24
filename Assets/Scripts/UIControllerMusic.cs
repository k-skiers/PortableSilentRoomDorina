
using UnityEngine;
using UnityEngine.UI;

public class UIControllerMusic : MonoBehaviour
{
    [Header("Music Panel")]
    public GameObject musicPanel;
    public Button volumeButton;
    public Button closeButton;

    [Header("Main UI Icons")]
    public GameObject dayNightButton;
    public GameObject locationButton;

    [Header("Optional: Parent of All Three Icons")]
    public GameObject mainIconsPanel;

    private bool isMusicPanelOpen = false;

    void Start()
    {
        // Initially hide Music Panel
        if (musicPanel != null)
            musicPanel.SetActive(false);

        // Register button actions
        if (volumeButton != null)
            volumeButton.onClick.AddListener(OpenMusicPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMusicPanel);

        // Initially show all main icons
        SetMainIconsVisible(true);
    }

    public void OpenMusicPanel()
    {
        if (isMusicPanelOpen) return;

        isMusicPanelOpen = true;

        // Open Music Panel
        if (musicPanel != null)
            musicPanel.SetActive(true);

        // Hide all three main icons
        SetMainIconsVisible(false);

        Debug.Log("Music Panel opened - icons hidden.");
    }

    public void CloseMusicPanel()
    {
        isMusicPanelOpen = false;

        // Close Music Panel
        if (musicPanel != null)
            musicPanel.SetActive(false);

        // Restore all three main icons
        SetMainIconsVisible(true);

        Debug.Log("Music Panel closed - icons visible.");
    }

    private void SetMainIconsVisible(bool visible)
    {
        // If all icons share one parent, hide/show parent
        if (mainIconsPanel != null)
        {
            mainIconsPanel.SetActive(visible);
        }
        else
        {
            // Otherwise hide/show each icon separately
            if (volumeButton != null)
                volumeButton.gameObject.SetActive(visible);

            if (dayNightButton != null)
                dayNightButton.SetActive(visible);

            if (locationButton != null)
                locationButton.SetActive(visible);
        }
    }

    void OnDestroy()
    {
        if (volumeButton != null)
            volumeButton.onClick.RemoveListener(OpenMusicPanel);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseMusicPanel);
    }
}
