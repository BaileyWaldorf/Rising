using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour {

    public AudioSource musicSource;
    public static AudioController instance = null;

	// Use this for initialization
	void Awake () {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
	}
	
    public void  PlayAudio(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.Play();
    }
}
