using UnityEngine;
using UnityEngine.Audio;

public class VolumeSetter : MonoBehaviour
{
    public AudioMixerGroup mixer;
    public string parameter;
    public float mixerValue;
    public TypeDistinguisher mixerVolume;

    public void Start()
    {
        mixerVolume.OnValueChanged += HandleVolumeChanged;
        SetVolumeLevel(mixerVolume.FloatValue);
    }

    public void LoadLevel()
    {
        SetVolumeLevel(PlayerPrefs.GetFloat(mixerVolume.PrefsKey, 1));
    }

    public void HandleVolumeChanged()
    {
        SetVolumeLevel(mixerVolume.FloatValue);
    }

    public void SetVolumeLevel(float sliderVolume)
    {
        mixerValue = Mathf.Log10(sliderVolume) * 20;
        mixer.audioMixer.SetFloat(parameter, mixerValue);
    }
}