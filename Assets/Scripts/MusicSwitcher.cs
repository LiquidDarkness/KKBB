using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicSwitcher : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip startingClip;
    public float fadeoutTime;
    public float timeElapsed = 0;
    public AnimationCurve up, down;


    // What the last switch asked for. A story beat and the level that follows it now request
    // the same track, and so does the ending button, so without remembering the request the
    // second caller would fade the music out and start it again from the top.
    private AudioClip requestedIntro;
    private AudioClip requestedLoop;

    public void Start()
    {
        audioSource.clip = startingClip;
        requestedLoop = startingClip;
        audioSource.Play();
    }

    private bool IsAlreadyPlaying(AudioClip intro, AudioClip loop)
    {
        return requestedIntro == intro && requestedLoop == loop && audioSource.isPlaying;
    }

    public void SwitchAudio(AudioClip audioClip)
    {
        if (IsAlreadyPlaying(null, audioClip))
        {
            return;
        }

        requestedIntro = null;
        requestedLoop = audioClip;
        StartCoroutine(SwitchAudioRoutine(audioClip));
    }

    private IEnumerator SwitchAudioRoutine(AudioClip audioClip)
    {
        timeElapsed = 0;
        float referenceTime = Time.realtimeSinceStartup;
        while (timeElapsed < fadeoutTime)
        {
            float targetVolume = Mathf.Lerp(0, 1, timeElapsed / fadeoutTime);
            audioSource.volume = down.Evaluate(targetVolume);
            timeElapsed = Time.realtimeSinceStartup - referenceTime;
            yield return null;
        }

        audioSource.clip = audioClip;
        timeElapsed = 0;
        yield return null;
        referenceTime = Time.realtimeSinceStartup;
        audioSource.Play();

        while (timeElapsed < fadeoutTime)
        {
            float targetVolume = Mathf.Lerp(0, 1, timeElapsed / fadeoutTime);
            audioSource.volume = up.Evaluate(targetVolume);
            timeElapsed = Time.realtimeSinceStartup - referenceTime;
            yield return null;
        }

    }

    internal void SwitchToSequence(AudioClip first, AudioClip second)
    {
        if (IsAlreadyPlaying(first, second))
        {
            return;
        }

        requestedIntro = first;
        requestedLoop = second;
        StartCoroutine(SwitchSequenceRoutine(first, second));
    }

    private IEnumerator SwitchSequenceRoutine(AudioClip first, AudioClip second)
    {
        timeElapsed = 0;
        float referenceTime = Time.realtimeSinceStartup;
        float firstLength = first.length;

        if (firstLength < 2*fadeoutTime)
        {
            Debug.LogError("FadeOutTime too long.");
        }

        while (timeElapsed < fadeoutTime)
        {
            float targetVolume = Mathf.Lerp(0, 1, timeElapsed / fadeoutTime);
            audioSource.volume = down.Evaluate(targetVolume);
            timeElapsed = Time.realtimeSinceStartup - referenceTime;
            yield return null;
        }

        audioSource.clip = first;
        timeElapsed = 0;
        yield return null;
        referenceTime = Time.realtimeSinceStartup;
        audioSource.Play();

        while (timeElapsed < fadeoutTime)
        {
            float targetVolume = Mathf.Lerp(0, 1, timeElapsed / fadeoutTime);
            audioSource.volume = up.Evaluate(targetVolume);
            timeElapsed = Time.realtimeSinceStartup - referenceTime;
            yield return null;
        }

        while (timeElapsed < firstLength)
        {
            timeElapsed = Time.realtimeSinceStartup - referenceTime;
            yield return null;
        }
        audioSource.Stop();
        audioSource.clip = second;
        audioSource.Play();
    }

    public AudioClip testClip;
    [ContextMenu("Test Switching Audio")]
    public void TestSwitching()
    {
        SwitchAudio(testClip);
    }
}
