using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Audio;

//gets installed on the camera for head bump sounds in saberThing game
public class PlaySoundOnCollision : MonoBehaviour
{
    private AudioSource aud;
    public bool test;
    private AudioClip clip;
    private float volume;
    // Start is called before the first frame update
    void Start()
    {
        aud = GetComponent<AudioSource>();
        clip = aud.clip;
        volume = aud.volume;
        //AudioMixer mixer = Resources.Load("MasterSaber") as AudioMixer;
        //string _OutputMixer = "MasterSaber";        
        //aud.outputAudioMixerGroup = mixer.FindMatchingGroups(_OutputMixer)[0];
    }

void OnCollisionEnter(Collision col){
    if (col.relativeVelocity.magnitude > .25){
        if (aud != null)
            if (!aud.isPlaying){
                aud.pitch = Random.Range(.25f,1f);
                aud.volume = volume * Mathf.Lerp(.1f,1,col.relativeVelocity.magnitude * .5f);
                float dot = Vector3.Dot(transform.right, col.GetContact(0).normal);//assume both vectors are in worldspace
                    aud.panStereo = -dot;
                    aud.Play();

                    //AudioSource.PlayClipAtPoint(clip, new Vector3(5, 1, 2));

                }
        }

}

    // Update is called once per frame
    void Update()
    {
        if (test)
            if (!aud.isPlaying){
                aud.pitch = Random.Range(.25f,1f);
                aud.Play();
                test = false;
            }

    }
}
