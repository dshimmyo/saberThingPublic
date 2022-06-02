using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class HeadColliderSetup : MonoBehaviour
{
    public AudioClip headKnockClip;
    [SerializeField] float volume = 1;
    private static bool isSetupOnce = false;
    // Start is called before the first frame update
    void Start()
    {
        if (!isSetupOnce){
            Camera mainCamera = Camera.main;
            GameObject camObject = mainCamera.gameObject;
            SphereCollider col = camObject.AddComponent<SphereCollider>();
            col.radius = .12f;//.09f;
            camObject.layer = LayerMask.NameToLayer("Floor");// 11;//floor
            camObject.AddComponent<AudioSource>();
            AudioSource aud = camObject.GetComponent<AudioSource>();
            if (aud == null)
                aud = camObject.AddComponent<AudioSource>();
            if  (aud != null)
            {
                aud.clip = headKnockClip;
                aud.loop = false;
                aud.playOnAwake = false;
                aud.priority = 1;
                aud.volume = volume;
                AudioMixer mixer = Resources.Load("MasterSaberNoReverb") as AudioMixer;
                string _OutputMixer = "Master";
                if (mixer != null)
                    aud.outputAudioMixerGroup = mixer.FindMatchingGroups(_OutputMixer)[0];
            }
            PlaySoundOnCollision psoc = camObject.AddComponent<PlaySoundOnCollision>();
            isSetupOnce = true;//hopefully prevents multiple cameras getting set up, such as the one that LIV creates
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
