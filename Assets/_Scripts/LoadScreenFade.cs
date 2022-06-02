using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class LoadScreenFade : MonoBehaviour
{
    private bool fadeStarted = false;
    public AudioMixer masterMixer;
    public float fadeTime = 5f;
    private Material mat;
    Color32 color;
    float fadePercent = 0;
    byte alpha = 255;
    float timeScaler = 0;
    float fadeStartTime = 0;
    [SerializeField] private bool isClicked=false;
    private enum menuButtonStyles { Trigger, TrackpadUp };
    [SerializeField] private menuButtonStyles menuButton;
    [SerializeField] private int renderQueue = 4000;
    [SerializeField] private bool updateRenderQueue = false;
    private bool isLoaded = false; //loadscreen behavior only used once
    private bool isClickBack = false;
    private bool isGameStarted = false;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        color = mat.color;
        //Time.timeScale = 0;
        if (masterMixer != null)
            masterMixer.SetFloat("masterMasterVolume",-80f);
    }

   /* private IEnumerator FadeOut(){
        fadeStarted = true;
        Material mat = GetComponent<Renderer>().material;
        Color32 color = mat.color;
        byte alpha = 255;
        float timeScaler = 0;
        for (int i=0; i<100; i++)
        {
            timeScaler = (100f-i)/100f;
            masterMixer.SetFloat("masterMasterVolume",Mathf.Lerp(0,-80f,timeScaler));

            if (i % 20 == 0)
                Time.timeScale = timeScaler;

            alpha = (byte)(timeScaler * 255f);
            color.a = alpha;
            if (mat != null)
                mat.color = color;
            yield return null;

        }
        Time.timeScale = 1;
        masterMixer.SetFloat("masterMasterVolume",0f);
        gameObject.SetActive(false);

    }*/

    private void FadeOut()
    {


        //fadePercent += Time.deltaTime / fadeTime * 100f;
        fadePercent = ((Time.realtimeSinceStartup - fadeStartTime) / fadeTime * 100f);
        int i = Mathf.CeilToInt(fadePercent);

        //for (int i = 0; i < 100; i++)
        {
            timeScaler = (fadePercent / 100f);
            if (masterMixer != null)
                masterMixer.SetFloat("masterMasterVolume", Mathf.Lerp(0, -80f, 1f - timeScaler));

            //if (i % 20 == 0)
            //    Time.timeScale = timeScaler;

            alpha = (byte)((1f - timeScaler) * 255f);
            color.a = alpha;
            if (mat != null)
                mat.color = color;

        }
        if (i >= 98)
        {
            //Time.timeScale = 1;
            if (masterMixer != null)
                masterMixer.SetFloat("masterMasterVolume", 0f);
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            isClicked = false;
            fadeStarted = false;
            isLoaded = true;
            if (!isGameStarted)
                isGameStarted = true;
        }

    }

    private void FadeIn()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = true;


        //fadePercent += Time.deltaTime / fadeTime * 100f;
        fadePercent = ((Time.realtimeSinceStartup - fadeStartTime) / fadeTime * 100f);
        int i = Mathf.CeilToInt(fadePercent);

        {
            timeScaler = (fadePercent / 100f);
            if (masterMixer != null)
                //masterMixer.SetFloat("masterMasterVolume", Mathf.Lerp(0, -80f, 1f - timeScaler));
                masterMixer.SetFloat("masterMasterVolume", Mathf.Lerp(0, -80f, timeScaler));

            //if (i % 20 == 0)
            //    Time.timeScale = timeScaler;

            alpha = (byte)((timeScaler) * 255f);
            color.a = alpha;
            if (mat != null)
                mat.color = color;

        }
        if (i >= 99)
        {
            color.a = (byte)255;
            if (mat != null)
                mat.color = color;
            if (masterMixer != null)
                masterMixer.SetFloat("masterMasterVolume", -80f);
            isClicked = false;
            isClickBack = false;
            fadeStarted = false;
            Time.timeScale = 0;

            //isLoaded = true;
        }

    }

    /*
    private void FadeIn()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = true;

        int i = Mathf.CeilToInt(fadePercent);

        //fadePercent += Time.deltaTime / fadeTime * 100f;
        fadePercent = ((Time.realtimeSinceStartup - fadeStartTime) / fadeTime * 100f);
        //for (int i = 0; i < 100; i++)
        {
            timeScaler = (fadePercent / 100f);
            if (masterMixer != null)
                masterMixer.SetFloat("masterMasterVolume", Mathf.Lerp(0, -80f, 1f - timeScaler));

            //if (i % 20 == 0)
                //Time.timeScale = timeScaler;

            alpha = (byte)((1f - timeScaler) * 255f);
            color.a = alpha;
            if (mat != null)
                mat.color = color;

        }
        if (i >= 98)
        {
            //Time.timeScale = 1;
            if (masterMixer != null)
                masterMixer.SetFloat("masterMasterVolume", 0f);

        }

    }*/
    private bool GetButtonClick()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            return true;

        if (HandednessHack.flippedHands)
        {
            if (menuButton == menuButtonStyles.Trigger)
                if (Input.GetAxis("HTC_VIU_LeftTrigger") > .5)
                    return true;
            //else if (menuButton == menuButtonStyles.TrackpadUp)
                
            //left
        }
        else
        {
            if (menuButton == menuButtonStyles.Trigger)
                if (Input.GetAxis("HTC_VIU_RightTrigger") > .5)
                    return true;

            //right

        }
        return false;
    }

    public void SetClicked()
    {
        isClicked = true;
    }

    public void SetClickBack()
    {
        isClickBack = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (!isGameStarted)
        {
            if (updateRenderQueue || mat.renderQueue != renderQueue)
            {
                Debug.Log("updating renderQueue " + gameObject.name);
                mat.renderQueue = renderQueue;
                updateRenderQueue = false;
            }

            if (isClicked && !fadeStarted && !isLoaded)
            {
                fadeStarted = true;//StartCoroutine(FadeOut());
                fadeStartTime = Time.realtimeSinceStartup;
            }

            if (fadeStarted /*&& !isLoaded*/ && isClicked)
                FadeOut();
        }

        else
        {
            if (isClickBack)
            {
                if (!fadeStarted)
                {
                    fadeStarted = true;//StartCoroutine(FadeOut());
                    fadeStartTime = Time.realtimeSinceStartup;
                    FadeIn();
                }
                else
                {
                    FadeIn();
                }
            }

            if (isClicked)
            {

                if ( !fadeStarted)
                {
                    fadeStarted = true;
                    fadeStartTime = Time.realtimeSinceStartup;
                    FadeOut();
                }

                if (fadeStarted)
                    FadeOut();

            }
        }



    }
}
