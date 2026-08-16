using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResolutionSelector : MonoBehaviour
{
    public TypeDistinguisher resolutionKey;
    public TypeDistinguisher windowMode;
    public Toggle fullscreen;
    Resolution[] resolutions;
    public TMP_Dropdown resolutionDropdown;

    public void Awake()
    {
        bool fullScreen = PlayerPrefs.GetInt(windowMode.PrefsKey) != 0;
        fullscreen.isOn = fullScreen;
    }

    void Start()
    {
        // Screen.resolutions returns one entry per refresh rate, so a 1920x1080 monitor that can
        // also do 120 and 144 Hz contributes three entries - and since the label is built from
        // width and height alone, all three read "1920 x 1080" and look like duplicates. Collapse
        // them by size, keeping the fastest refresh rate each size can manage.
        resolutions = Screen.resolutions
            .GroupBy(resolution => (resolution.width, resolution.height))
            .Select(group => group.OrderByDescending(resolution => resolution.refreshRate).First())
            .OrderBy(resolution => resolution.width)
            .ThenBy(resolution => resolution.height)
            .ToArray();

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        Screen.fullScreen = PlayerPrefs.GetInt(windowMode.PrefsKey) != 0;
        resolutionDropdown.AddOptions(options);

        // The saved index only means anything if it still addresses this list, and it may not:
        // the player can change monitors, and collapsing the duplicates above renumbers anything
        // an older build saved. The detected current resolution is the better fallback - reading
        // the pref unconditionally used to hand back 0 when nothing was saved, which selected the
        // smallest resolution on the list rather than the one actually in use.
        int savedIndex = PlayerPrefs.GetInt(resolutionKey.PrefsKey, -1);
        if (savedIndex >= 0 && savedIndex < resolutions.Length)
        {
            currentResolutionIndex = savedIndex;
        }

        resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt(windowMode.PrefsKey, isFullScreen.GetHashCode());
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
        {
            Debug.LogWarning($"[{nameof(ResolutionSelector)}] Resolution {resolutionIndex} is not on this screen's list - ignoring.", this);
            return;
        }

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt(resolutionKey.PrefsKey, resolutionIndex);
    }
}