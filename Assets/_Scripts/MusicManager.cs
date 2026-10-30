using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager S;

    [Header("Music Settings")]
    public AudioSource musicSource;
    public AudioClip actionMusic;
    public float fadeSpeed = 2f;
    [Range(0f, 1f)] public float maxVolume = 0.7f;

    private HashSet<MonoBehaviour> chasingSources = new HashSet<MonoBehaviour>();
    private Coroutine fadeCoroutine;

    void Awake()
    {
        S = this;
        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.clip = actionMusic;
        }
    }

    public void RegisterChase(MonoBehaviour source, bool isChasing) //starts chase music
    {
        if (isChasing) chasingSources.Add(source);
        else chasingSources.Remove(source);

        bool shouldPlay = chasingSources.Count > 0;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeMusic(shouldPlay ? maxVolume : 0f));
    }

    private IEnumerator FadeMusic(float targetVolume) //ends chase music
    {
        if (targetVolume > 0f && musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }

        while (Mathf.Abs(musicSource.volume - targetVolume) > 0.01f)
        {
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        musicSource.volume = targetVolume;
        if (targetVolume == 0f && musicSource != null)
        {
            musicSource.Stop();
        }
    }
}