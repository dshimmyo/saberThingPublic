using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightsOut : MonoBehaviour {
    [SerializeField]
    float timeLimit = .111f;//.25f;
    float timeAlive = 0;
    //public bool disableGameObject = false;
    private AudioSource aso;
    private ParticleSystem ps;
    private Light li;
    public bool isFlash = true;
    [SerializeField] private bool playSound = false;
	// Use this for initialization
	void Start () {
        aso = GetComponent<AudioSource>();
        ps = GetComponent<ParticleSystem>();
        li = GetComponent<Light>();
        if (li == null)
            this.enabled = false;
	}
	void OnEnable(){
        timeAlive = 0;
        if (li != null)
            if (!li.enabled && isFlash)
                li.enabled = true;
        if (aso != null)
            if (!aso.isPlaying)
                if (playSound)
                    aso.Play();
        if (ps != null)
            if (!ps.isPlaying)
                ps.Play();
    }
	// Update is called once per frame
	void Update () {
        timeAlive += Time.deltaTime;
        if (timeAlive > timeLimit){
            li.enabled = false;
            //if (disableGameObject)
                if (!aso.isPlaying && ps.particleCount == 0){
                    ps.Stop();
                    gameObject.SetActive(false);
                }
        }
	}
}
