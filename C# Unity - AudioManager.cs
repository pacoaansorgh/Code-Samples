using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class AudioManager : MonoBehaviourSingleton<AudioManager> {

    // Our dictionary of audiosources, each with its own unique key.
    private Dictionary<int, AudioSourceExtension> audioPool = new Dictionary<int, AudioSourceExtension>();
    // The key for our next audiosource. Incremented by one every time we use it.
    private int index;

    public List<AudioClip> BGMClips = new List<AudioClip>();
    private Dictionary<string, AudioClip> BGMDict = new Dictionary<string, AudioClip>();

    [Range(0.0f, 1.0f)]
    public float SFXVolume;
    [Range(0.0f, 1.0f)]
    public float BGMVolume;
    
    void Awake() {
        // Reset the audiomanager, then add one new audio source so we have at least one.
        ResetAudioManager();
        GetIdleSource();
        LoadVolumeSettings();
        CreateBGMDictionary();
    }

    void Update() {
        foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
            if (pair.Value.isFadingOut) {
                pair.Value.currentTime += Time.deltaTime;
                if (pair.Value.currentTime > pair.Value.fadeTime && pair.Value.idleOnFadeComplete) MakeSourceIdle(pair.Key);
                else if (pair.Value.currentTime > pair.Value.fadeTime && !pair.Value.idleOnFadeComplete) pair.Value.StopFade();
                else {
                    float newVolume = ((pair.Value.currentTime / pair.Value.fadeTime) * pair.Value.targetVolume) + ((1 - (pair.Value.currentTime / pair.Value.fadeTime)) * pair.Value.startingVolume);
                    pair.Value.source.volume = newVolume;
                }
            }
            else if (pair.Value.isFadingIn) {
                pair.Value.currentTime += Time.deltaTime;
                if (pair.Value.currentTime > pair.Value.fadeTime) {
                    pair.Value.StopFade();
                    pair.Value.source.volume = pair.Value.targetVolume;
                }
                else {
                    float newVolume = ((pair.Value.currentTime / pair.Value.fadeTime) * pair.Value.targetVolume) + ((1 - (pair.Value.currentTime / pair.Value.fadeTime)) * pair.Value.startingVolume);
                    pair.Value.source.volume = newVolume;
                }
            }
        }
    }

	// Returns true if we are casting, false if we are not.
	private bool CastingCheck() {
		if ( PlatformSetup.currentPlatform == platform.desktop || PlatformSetup.currentPlatform == platform.console ) {
			return true;
		} else if ( PauseMenu.Instance.isCasting ) {
			return true;
		} else {
			return false;
		}
	}

    #region VOLUME
    private void LoadVolumeSettings() {
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume"));
        SetBGMVolume(PlayerPrefs.GetFloat("BGMVolume"));
    }
    
    public void SetSFXVolume(float volume) {
        if (volume < 0) volume = 0;
        else if (volume > 1.0f) volume = 1.0f;
        SFXVolume = volume;
        foreach(KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
            if (!pair.Value.isBGMSource) {
                if (pair.Value.isFadingIn)
                    pair.Value.targetVolume = SFXVolume;
                else if (pair.Value.isFadingOut)
                    pair.Value.startingVolume = SFXVolume;
                else
                    pair.Value.source.volume = SFXVolume;
            }
        }
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float volume) {
        if (volume < 0) volume = 0;
        else if (volume > 1.0f) volume = 1.0f;
        BGMVolume = volume;
        foreach(KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
            if (pair.Value.isBGMSource) {
                Debug.Log("Found the BGM source. Setting volume.");
                if (pair.Value.isFadingIn)
                    pair.Value.targetVolume = pair.Value.playVolume*BGMVolume;
                else if (pair.Value.isFadingOut)
                    pair.Value.startingVolume = pair.Value.playVolume * BGMVolume;
                else
                    pair.Value.source.volume = pair.Value.playVolume * BGMVolume;
            }
        }
        PlayerPrefs.SetFloat("BGMVolume", BGMVolume);
        PlayerPrefs.Save();
    }

    public float GetSFXVolume() {
        return SFXVolume;
    }

    public float GetBGMVolume() {
        return BGMVolume;
    }

    public void SFXVolumeSlider(Slider slider) {
        SetSFXVolume(slider.value);
    }

    public void BGMVolumeSlider(Slider slider) {
        SetBGMVolume(slider.value);
    }
    #endregion VOLUME

    #region POOLING
    // Remove all audiosources from this object and the dictionary, and reset the index to 0. Use on awake to reset everything, or when resetting the game.
    private void ResetAudioManager() {
        foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
            Destroy(pair.Value.source);
        }
        audioPool.Clear();
        index = 0;
    }

    // Remove all idle audio sources from this object. Don't know if we need this, but useful tool to have if audiosources get out of hand.
    private void RemoveIdleSources() {
        foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
            AudioSource source = pair.Value.source;
            if (pair.Value.IsIdle()) {
                Destroy(pair.Value.source);
                audioPool.Remove(pair.Key);
            }
        }
    }

    // If we have an audio source that isnt playing at the moment, return it. Otherwise create a new one.
    private AudioSourceExtension GetIdleSource() {
        foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
            if (pair.Value.IsIdle()) return pair.Value;
        }
        AudioSourceExtension newSource = AddNewSource();
        audioPool.Add(index++, newSource);
        return newSource;
    }

    // If we have an audio source that isnt playing at the moment, return its keyvaluepair. otherwise create a new one.
    private KeyValuePair<int, AudioSourceExtension> GetIdlePair() {
        foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
            if (pair.Value.IsIdle()) return pair;
        }
        AudioSourceExtension newSource = AddNewSource();
        KeyValuePair<int, AudioSourceExtension> newPair = new KeyValuePair<int, AudioSourceExtension>(index++, newSource);
        audioPool.Add(newPair.Key, newPair.Value);
        return newPair;
    }

    // Add a new audiosource to this object, and set it to not play on awake, then return the new audiosource.
    private AudioSourceExtension AddNewSource() {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        AudioSourceExtension newSource = new AudioSourceExtension(source);
        return newSource;
    }

    private void MakeSourceIdle(int key) {
        // Set the volume to 1, stop playing, reset the "loop" setting.
        if (audioPool.ContainsKey(key)) {
            audioPool[key].MakeIdle(SFXVolume);
        }
    }

    private void CreateBGMDictionary() {
        for (int i = 0; i < BGMClips.Count; i++) {
            BGMDict.Add(BGMClips[i].name, BGMClips[i]);
        }
    }
    #endregion POOLING

    #region PLAYONESHOT
    // Plays one shot of the given clip, with optional volume modifier. Only plays when we are not casting.
    public void PlayLocalOneshot(AudioClip clip, float optionalVolume = -1.0f) {
        if (!CastingCheck()) {
            if (optionalVolume < 0.0f) optionalVolume = SFXVolume;
            GetIdleSource().source.PlayOneShot(clip, optionalVolume);
        }
    }

    // Plays one shot of the given clip, with optional volume modifier. Only plays when we are casting.
    public void PlayRemoteOneshot(AudioClip clip, float optionalVolume = -1.0f) {
        if (CastingCheck()) {
            if (optionalVolume < 0.0f) optionalVolume = SFXVolume;
            GetIdleSource().source.PlayOneShot(clip, optionalVolume);
        }
    }
    #endregion PLAYONESHOT

    #region PLAY
    // Plays the given clip, and returns the key of the audioSource used to play it. Only plays when we are not casting.
    public int PlayLocal(AudioClip clip) {
        if (!CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = clip;
            pair.Value.source.Play();
            pair.Value.source.volume = SFXVolume;
            return pair.Key;
        }
        return -1;
    }

    // Plays the given clip, and returns the key of the audioSource used to play it. Only plays when we are casting.
    public int PlayRemote(AudioClip clip, Action optionalOnComplete = null) { 
        if (CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = clip;
            pair.Value.source.Play();
            pair.Value.source.volume = SFXVolume;
            if (optionalOnComplete != null) {
                StartCoroutine(OnComplete(clip.length, optionalOnComplete));
            }
            return pair.Key;
        }
        return -1;
    }

    private IEnumerator OnComplete(float delay, Action action) {
        yield return new WaitForSeconds(delay);
        action();
    }
    #endregion PLAY

    // NOTE: Playdelayed currently leaves audiosources as "idle" while they wait for their delay to end.
    #region PLAYDELAYED
    public int PlayLocalDelay(AudioClip clip, float delay) {
        if (!CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = clip;
            pair.Value.source.PlayDelayed(delay);
            pair.Value.source.volume = SFXVolume;
            return pair.Key;
        }
        return -1;
    }

    public int PlayRemoteDelay(AudioClip clip, float delay) {
        if (CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = clip;
            pair.Value.source.PlayDelayed(delay);
            pair.Value.source.volume = SFXVolume;
            return pair.Key;
        }
        return -1;
    }
    #endregion PLAYDELAYED

    #region PLAYLOOPED
    public int PlayLocalLooped(AudioClip clip) {
        if (!CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = clip;
            pair.Value.source.loop = true;
            pair.Value.source.Play();
            pair.Value.source.volume = SFXVolume;
            return pair.Key;
        }
        return -1;
    }

    public int PlayRemoteLooped(AudioClip clip) {
        if (CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = clip;
            pair.Value.source.loop = true;
            pair.Value.source.Play();
            pair.Value.source.volume = SFXVolume;
            return pair.Key;
        }
        return -1;
    }
    #endregion PLAYLOOPED

    #region STOPPLAY
    // Stop the audiosource with the given key from playing (if any).
    public void Stop(int key) {
        if (audioPool.ContainsKey(key)) {
            if (audioPool[key].source.isPlaying)
                MakeSourceIdle(key);
        }
    }

    // Pause the audiosource with the given key (if any).
    public void Pause(int key) {
        if (audioPool.ContainsKey(key)) {
            if (audioPool[key].source.isPlaying) {
                audioPool[key].Pause();
            }
        }
    }

    // Unpauses the audiosource with the given key (if any). Note that it is not possible to check if it is paused, so we assume the user knows what they are doing.
    public void UnPause(int key) {
        if (audioPool.ContainsKey(key)) {
                audioPool[key].UnPause();
        }
    }

    public void StopAll() {
        if (audioPool != null) {
            foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
                MakeSourceIdle(pair.Key);
            }
        }
    }
    #endregion STOPPLAY

    #region FADE
    // Fades the audiosource with given key into a new audioclip on a new source, and returns the new source.
    public int CrossFade(int key, AudioClip clip, float optionalFadeTime = 1.0f, float optionalTargetVolume = -1.0f, bool optionalLoop = false) {
        if (CastingCheck()) {
            if (optionalTargetVolume < 0.0f) optionalTargetVolume = SFXVolume;
            if (audioPool.ContainsKey(key)) {
                if (audioPool[key].source.isPlaying) {
                    FadeOut(key, optionalFadeTime);
                }
            }
            return (FadeIn(clip, optionalFadeTime, optionalTargetVolume, optionalLoop));
        }
        return -1;
    }

    // Fades out audiosource with the given key over the given time.
    public void FadeOut(int key, float fadeTime) {
        if (CastingCheck()) {
            if (audioPool.ContainsKey(key)) {
                audioPool[key].isFadingOut = true;
                audioPool[key].FadeOut(fadeTime);
            }
        }
    }

    // Fades in a new audiosource with the given clip over the given time, and returns the new audiosource's key.
    public int FadeIn(AudioClip clip, float fadeTime, float targetVolume, bool optionalLoop = false) {
        if (CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.FadeIn(targetVolume, fadeTime);
            AudioSource source = pair.Value.source;
            source.clip = clip;
            source.volume = 0.0f;
            source.loop = optionalLoop;
            source.Play();
            return pair.Key;
        }
        return -1;
    }
    #endregion FADE
    
    #region BACKGROUNDMUSIC
    public int PlayRemoteBGM(AudioClip clip) {
        if (CastingCheck()) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = clip;
            pair.Value.playVolume = BGMVolume;
            pair.Value.source.volume = BGMVolume;
            pair.Value.isBGMSource = true;
            pair.Value.source.loop = true;
            pair.Value.source.Play();
            return pair.Key;
        }
        return -1;
    }

    public int PlayRemoteBGM(string clipName) {
        if (CastingCheck() && BGMDict.ContainsKey(clipName)) {
            KeyValuePair<int, AudioSourceExtension> pair = GetIdlePair();
            pair.Value.source.clip = BGMDict[clipName];
            pair.Value.playVolume = BGMVolume;
            pair.Value.source.volume = BGMVolume;
            pair.Value.isBGMSource = true;
            pair.Value.source.loop = true;
            pair.Value.source.Play( );
            return pair.Key;
        }
        return -1;
    }

    // Crossfades an already playing BGM source into a new BGM source with a different audioclip, given by the function call
    public int CrossfadeBGM(AudioClip clip, float optionalFadeTime = 1.0f, float optionalTargetVolume = -1.0f, bool optionalLoop = false) {
        if (CastingCheck()) {
            if (optionalTargetVolume < 0.0f) optionalTargetVolume = BGMVolume;
            foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) { 
                if (pair.Value.isBGMSource && !pair.Value.IsIdle()) {
                    FadeOut(pair.Key, optionalFadeTime);
                }
            }
            int newKey = FadeIn(clip, optionalFadeTime, optionalTargetVolume*BGMVolume, optionalLoop);
            audioPool[newKey].isBGMSource = true;
            audioPool[newKey].playVolume = optionalTargetVolume;
            return newKey;
        }
        return -1;
    }

    // Crossfades an already playing BGM source into a new BGM source with a different audioclip, taken from the known clips
    public int CrossfadeBGM(string clipName, float optionalFadeTime = 1.0f, float optionalTargetVolume = -1.0f, bool optionalLoop = false) {
		if (CastingCheck() && BGMDict.ContainsKey(clipName)) {
            if (optionalTargetVolume < 0.0f) optionalTargetVolume = BGMVolume;
            foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
                if (pair.Value.isBGMSource && !pair.Value.IsIdle()) {
                    FadeOut(pair.Key, optionalFadeTime);
                }
            }
            int newKey = FadeIn(BGMDict[clipName], optionalFadeTime, optionalTargetVolume*BGMVolume, optionalLoop);
            audioPool[newKey].isBGMSource = true;
            audioPool[newKey].playVolume = optionalTargetVolume;
            return newKey;
        }
        return -1;
    }

    // Fade out an already playing BGM source
    public void FadeOutBGM(float optionalFadeTime = 1.0f, float optionalFadeVolume = 0.0f, bool optionalIdleOnFadeComplete = true) {
        if (CastingCheck()) {
            foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
                if (pair.Value.isBGMSource) {
                    pair.Value.FadeOut(optionalFadeTime, optionalFadeVolume*BGMVolume, optionalIdleOnFadeComplete);
                    pair.Value.playVolume = optionalFadeVolume;
                }
            }
        }
    }

    // Fade in an already playing BGM source
    public void FadeInBGM(float fadeVolume, float optionalFadeTime = 1.0f) {
        if (CastingCheck()) {
            foreach (KeyValuePair<int, AudioSourceExtension> pair in audioPool) {
                if (pair.Value.isBGMSource) {
                    pair.Value.FadeIn(fadeVolume*BGMVolume, optionalFadeTime);
                    pair.Value.playVolume = fadeVolume;
                }
            }
        }
    }
    #endregion BACKGROUNDMUSIC
}
