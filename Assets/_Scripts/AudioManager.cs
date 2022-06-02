using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public GameObject collisionSoundPrefab;
    public GameObject[] collisionSounds;
    private AudioClip[] metalClips;
    //private int currentClipNum = 0;

	// Use this for initialization
	void Start () {
		metalClips = Resources.LoadAll<AudioClip>("_Sounds/metalSounds");//Resources.LoadAll("Sounds/metalSounds", typeof(AudioClip)) as AudioClip[];
        collisionSounds = new GameObject[metalClips.Length];
        for (int i=0; i<metalClips.Length; i++){
            collisionSounds[i] = Instantiate(collisionSoundPrefab);
            collisionSounds[i].name = "collisionSound" + i.ToString();
            collisionSounds [i].GetComponent<AudioSource>().clip = metalClips [i];
        }
	}

    public void PlayMetalClip(GameObject _gameObject, Vector3 pos, float volume,float pitch){
        bool done = false;
        Vector3 _localPos = _gameObject.transform.InverseTransformPoint (pos);
        int clipNum = Random.Range (0, metalClips.Length);
        if (!collisionSounds [clipNum].GetComponent<AudioSource> ().isPlaying) 
        {
            AudioSource aud = collisionSounds [clipNum].GetComponent<AudioSource>();
            //StartCoroutine(FollowObjectWhilePlaying(aud,_gameObject,_localPos,volume,pitch));
            aud.volume = volume;
            aud.pitch = pitch;
            aud.Play();
            aud.transform.localPosition = _gameObject.transform.TransformPoint(_localPos);
        } else {
            for(int i=0; i<metalClips.Length; i++)
            {
                AudioSource aud = collisionSounds [i].GetComponent<AudioSource> ();
                if (!aud.isPlaying) 
                {
                    //StartCoroutine(FollowObjectWhilePlaying(aud,_gameObject,_localPos,volume,pitch));
                    aud.volume = volume;
                    aud.pitch = pitch;
                    aud.Play();
                    aud.transform.localPosition = _gameObject.transform.TransformPoint(_localPos);
                    break;
                }
            }
        }
    }
    IEnumerator FollowObjectWhilePlaying(AudioSource aud, GameObject target, Vector3 localPos, float volume, float pitch)
    {
        aud.volume = volume;
        aud.pitch = pitch;
        aud.Play ();
        bool done = false;
        Transform _audTransform = aud.gameObject.transform;
        Transform _targetTransform = target.transform;
        while (!done) 
        {
            if (aud.isPlaying && _audTransform != null) {
                if (_targetTransform != null)
                {
                    _audTransform.localPosition = _targetTransform.TransformPoint(localPos);
                }
                else
                {
                    done = true;
                }
            } else {
                _audTransform.localPosition = Vector3.zero;
                done = true;
            }
            yield return null;
        }
    }
	// Update is called once per frame
	void Update () {
		
	}
}
