using UnityEngine;
using UnityEngine.Audio;

public class VolumeSetter : MonoBehaviour
{
    public AudioMixerGroup mixer;
    public string parameter;
    public float mixerValue;
    public TypeDistinguisher mixerVolume;

    [Tooltip("Used on a fresh install, before the player has ever moved this slider.")]
    [Range(0f, 1f)] public float defaultVolume = 1f;

    // 0.0001 is not arbitrary: fed through the conversion below it lands on exactly -80 dB, which
    // is the mixer's own floor, so it is the quietest value the slider can usefully ask for.
    private const float MinimumAudibleVolume = 0.0001f;

    public void Start()
    {
        mixerVolume.OnValueChanged += HandleVolumeChanged;
        SetVolumeLevel(ReadSavedVolume());
    }

    // PlayerPrefs.GetFloat hands back 0 for a key nobody has written yet, and TypeDistinguisher's
    // FloatValue passes that straight on - so on a first run every channel asked for silence.
    private float ReadSavedVolume()
    {
        return PlayerPrefs.GetFloat(mixerVolume.PrefsKey, defaultVolume);
    }

    public void LoadLevel()
    {
        SetVolumeLevel(ReadSavedVolume());
    }

    public void HandleVolumeChanged()
    {
        SetVolumeLevel(mixerVolume.FloatValue);
    }

    // The slider reads as a fraction of full volume - 0 silent, 1 as loud as the mix goes - but
    // the mixer takes decibels, where 0 dB is that full volume and quieter means negative. That
    // is what the Log10 is for: 0.5 becomes -6 dB, not -50% of anything, which is why halving the
    // slider barely sounds quieter. The clamp matters because Log10(0) is negative infinity, and
    // a slider pulled all the way down handed the mixer a number it cannot take.
    public void SetVolumeLevel(float sliderVolume)
    {
        float normalized = Mathf.Clamp(sliderVolume, MinimumAudibleVolume, 1f);
        mixerValue = Mathf.Log10(normalized) * 20;
        mixer.audioMixer.SetFloat(parameter, mixerValue);
    }
}