using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaberHiltCollisionSounds : MonoBehaviour {
    //if there's no jointConnection and if the audioSource isn't playing grab one of the gunDrop sounds and play it in the audioSource with some pitchVariation
    private JointConnection jc;
    private AudioClip[] gunDropClips;
    private AudioSource saberAudioSource;
    [SerializeField] private GameObject blade;
    string controllerSide = "right";//use this to toggle haptics buzz sidedness

    //private float soundVolume=1;

    // Use this for initialization
    void Start () {
		gunDropClips = Resources.LoadAll<AudioClip>("_Sounds/gunDropSounds");//Resources.LoadAll("Sounds/metalSounds", typeof(AudioClip)) as AudioClip[];
        saberAudioSource = gameObject.GetComponent<AudioSource>();
	}
    void OnCollisionEnter(Collision collision){
        if (saberAudioSource != null)
        {
            float soundVolume = Mathf.Lerp(.001f,1,(collision.relativeVelocity.magnitude - .05f)/5f);
            if (collision.relativeVelocity.magnitude > .05)
            {
                if (!saberAudioSource.isPlaying)
                {
                    PlayCollisionSound (soundVolume);
                }
            } else {
                jc = GetComponent<JointConnection> ();
                if (jc != null) 
                {
                    if (!blade.activeInHierarchy) 
                    {
                        PlayCollisionSound (.5f);
                        StartCoroutine(HapticsTesting.Pulse(controllerSide, .25f));
                        //StartCoroutine(HapticsTesting.PulseRt (.25f));
                    }
                }
            }
        }
    }	// Update is called once per frame
    private void PlayCollisionSound(float mySoundVolume)
    {
        saberAudioSource.volume = mySoundVolume;
        saberAudioSource.pitch = Random.Range(0.9f,1.1f);
        saberAudioSource.clip = gunDropClips[Random.Range(0,gunDropClips.Length)];
        saberAudioSource.Play();
    }
	void Update () {
        if (HandednessHack.flippedHands)
            controllerSide = "left";
        else
            controllerSide = "right";
    }
}
