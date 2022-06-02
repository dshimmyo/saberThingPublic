using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class OscillatorHeadTailCreate : MonoBehaviour
{
    [SerializeField] private double frequencyA = 20.0;
    [SerializeField] private double frequencyB = 180.0;
    [SerializeField] private double frequencyC = 59.3;
    [Range(0.0f,20.0f)]
    [SerializeField] private float modFreqMultA = .1f; //power/volume of the oscillator
    [Range(0.0f,20.0f)]
    [SerializeField] private float modFreqMultB = 2f; //power/volume of the oscillator
    [Range(0.0f,20.0f)]
    [SerializeField] private float modFreqMultC = 1f; //power/volume of the oscillator
    [Range(0.0f,100f)]
    [SerializeField] private float modAmplitudeA = .1f; //power/volume of the oscillator
    [Range(0.0f,100f)]
    [SerializeField] private float modAmplitudeB = .25f; //power/volume of the oscillator
    [Range(0.0f,100f)]
    [SerializeField] private float modAmplitudeC = .5f; //power/volume of the oscillator

    private double actualFrequencyA;
    private double actualFrequencyB;
    private double actualFrequencyC;
    private double incrementA;//amount of distance the wave will be moving for each frame
    private double incrementB;//amount of distance the wave will be moving for each frame
    private double incrementC;//amount of distance the wave will be moving for each frame

    private double increment;//amount of distance the wave will be moving for each frame

    private double phaseA; //location on the wave
    private double phaseB;
    private double phaseC;
    private double sampling_frequency = 48000.0; //frequency at which unity's audio engine runs by default
    private float _deltaTime;
    private const float piX2 = Mathf.PI * 2;

    [Range(0.0f,0.9f)]
    [SerializeField] private float gainA = .2f; //power/volume of the oscillator
    [Range(0.0f,0.9f)]
    [SerializeField] private float gainB = .2f; //power/volume of the oscillator
    [Range(0.0f,0.9f)]
    [SerializeField] private float gainC = .1f; //power/volume of the oscillator

    [SerializeField] private float volume = 0.1f;

    public bool useBladeScaleForVolume = false;
    [SerializeField] Transform _bladeTransform;
    private float masterGain = 1;
    AudioSource audioSource;
    Vector3 previousPos;
    [SerializeField] private float pitch = 1f;
    bool isValidTime = false;
    float bufferTime = 1;
    float bufferTimer = 0.1f;
    public float pitchBend = .8f;
    private bool isSwitchOn = false;//up arrow
    private bool isSwitchOff = false;//down arrow
    private bool isSwitch = false;
    private float randomData = 0;
    public float offset = 0;
    public AnimationCurve bladeScaleVolumeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    void Start()
    {
        //SteamVR_Events.Event.Listen(SteamVR_Events.InputFocus, SteamVR_Events.Action.action);
        //SteamVR_Events.Event.Listen("input_focus", OnInputFocus);

        audioSource = gameObject.GetComponent<AudioSource>();
    }
    System.Random rand = new System.Random();

    void OnAudioFilterRead(float[] data, int channels)//~20ms chunks of data
    {
        if (isValidTime)
        {
            if (isSwitch)
                 for (int i = 0; i < data.Length; i += channels)
                {               
                    //data[i] = Random.Range(-1f,1f);
                    //dataB = (float)(gainB * Mathf.Sin((float)phaseB));
                    increment = 2.0 * Mathf.PI / sampling_frequency * 2.0;
                    phaseA += increment;
                    data[i] = (float)((rand.NextDouble() * 2.0 - 1.0) * Mathf.Sin((float)phaseA) );
                    //data[i] += offset*Mathf.Sign(data[i]);
                    if (channels == 2) data[i + 1] = data[i];
                    if (phaseA > (piX2))
                        phaseA -= piX2;
                }
            else
            {
                actualFrequencyA = frequencyA;
                actualFrequencyB = frequencyB;
                actualFrequencyC = frequencyC;
                actualFrequencyA += Mathf.Sin(_deltaTime * modFreqMultA) * modAmplitudeA * frequencyA;
                actualFrequencyB += Mathf.Sin(_deltaTime * modFreqMultB) * modAmplitudeB * frequencyB;
                actualFrequencyC += Mathf.Sin(_deltaTime * modFreqMultC) * modAmplitudeC * frequencyC;

                //leave the base sound alone//
                actualFrequencyB *= pitch;
                actualFrequencyC *= pitch;

                increment = 2.0 * Mathf.PI / sampling_frequency;

                incrementA = actualFrequencyA * increment;
                incrementB = actualFrequencyB * increment;
                incrementC = actualFrequencyC * increment;

                float dataA = 0;
                float dataB = 0;
                float dataC = 0;

                for (int i = 0; i < data.Length; i += channels)
                {

                    phaseA += incrementA;
                    phaseB += incrementB;
                    phaseC += incrementC;
                    //data[i] = (float)(gain * Mathf.Sin((float)phase));
                    if (Mathf.Sin((float)phaseA) >= 0)
                        dataA = (float)gainA * 0.6f;
                    else
                        dataA = (-(float)gainA) * 0.6f;

                    dataB = (float)(gainB * Mathf.Sin((float)phaseB));

                    if (Mathf.Sin((float)phaseC) >= 0)
                        dataC = (float)gainC * 0.6f;
                    else
                        dataC = (-(float)gainC) * 0.6f;

                    data[i] = (dataA + dataB + dataC) * masterGain;

                    if (channels == 2) data[i + 1] = data[i];
                    if (phaseA > (piX2))
                        phaseA -= piX2;
                    if (phaseB > (piX2))
                        phaseB -= piX2;
                    if (phaseC > (piX2))
                        phaseC -= piX2;
                }
            }
        }
    }

    IEnumerator SwitchOn(bool isOn)
    {
        isSwitch = true; //OnAudioFilterRead should change behavior when this is true
        float switchTimer = 0;
        audioSource.volume = .25f;
        yield return new WaitForSeconds(.5f);

        while (switchTimer < 2)
        {
            if (isOn){
                audioSource.pitch = Mathf.Lerp (1,20,Mathf.Pow(1f-((2f - switchTimer)/2f),2f));
                offset = Mathf.Lerp( 2.5f,1f,Mathf.Pow((2f - switchTimer)/2f,2f) );
            }
            else{
                audioSource.pitch = Mathf.Lerp (1,25,Mathf.Pow((2f - switchTimer)/2f,2));
                offset = Mathf.Lerp( 2.5f,1f,Mathf.Pow((2f - switchTimer)/2f,2f) );
            }

            switchTimer += _deltaTime;
            yield return null;
        }
        audioSource.volume = 0f;
        yield return new WaitForSeconds(.5f);
        isSwitch = false;
        audioSource.pitch = 1f;
    }

    void Update()
    {
        _deltaTime = Time.deltaTime;

/*
        if (!isSwitch && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)))
        {
            //test on/off switch sound
            if (Input.GetKeyDown(KeyCode.UpArrow))
                StartCoroutine(SwitchOn(true));
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                StartCoroutine(SwitchOn(false));
        }
*/

        if (Time.timeScale > 0.001)
        {
            if (!isValidTime)
                bufferTimer += _deltaTime;
            if (bufferTimer > bufferTime)
                isValidTime = true;
        }
        else
        {
            isValidTime = false;
            bufferTimer = 0;
        }

        if (!isSwitch)
        {
            if (useBladeScaleForVolume)
                masterGain = bladeScaleVolumeCurve.Evaluate(_bladeTransform.localScale.y );//Mathf.Lerp(0f,1f,_bladeTransform.localScale.y * 2);

            float saberDistance = (_bladeTransform.position - previousPos).sqrMagnitude;

            pitch = Mathf.Lerp(1f, pitchBend, saberDistance/Time.deltaTime);//1,1.2
            audioSource.volume = Mathf.Lerp(.75f, 1f, saberDistance/ Time.deltaTime * 2);
        }
        previousPos = _bladeTransform.position;

    }
}
