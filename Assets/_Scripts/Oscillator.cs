using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Oscillator : MonoBehaviour
{
    [SerializeField] private double frequencyA = 20.0;
    [SerializeField] private double frequencyB = 180.0;
    [SerializeField] private double frequencyC = 59.3;
    [SerializeField] private double frequencyD = 59.3;
    [SerializeField] private double rippleFrequency = 20;//try 80
    [SerializeField] private float rippleMin = 0f; //was .25 for a long time, trying .1 now to get more variation


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

    [SerializeField] private double actualRippleFrequency;
    private double incrementA;//amount of distance the wave will be moving for each frame
    private double incrementB;//amount of distance the wave will be moving for each frame
    private double incrementC;//amount of distance the wave will be moving for each frame
    private double rippleIncrement;

    private double increment;//amount of distance the wave will be moving for each frame

    private double phaseA; //location on the wave
    private double phaseB;
    private double phaseC;
    private double ripplePhase;
    //private double phaseMove;
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
    private float previousPitch = 1f;
    bool isValidTime = false;
    [SerializeField] float bufferTime = .1f;
    float bufferTimer = 0.1f;
    public float pitchBend = .8f;
    //private bool isSwitchOn = false;//up arrow
    //private bool isSwitchOff = false;//down arrow
    //private bool isSwitch = false;
    private float randomData = 0;
    public float offset = 0;
    public AnimationCurve bladeScaleVolumeCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private bool useRipple = true;
    [SerializeField] private float whooshGainA = 1f;
    [SerializeField] private float whooshGainB = 2.5f;
    [SerializeField] private float whooshGainC = 1.5f;
    //private float[] dataBuffer;
    [SerializeField] bool usePitchSettle = false;
    [SerializeField] float pitchSettleSpeed = 1;
    //[SerializeField] bool useMultipliedAudio = false;
    [SerializeField] AudioClip multiplierClip;
    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (pitch == null)
        pitch = 1f;
        //dataBuffer = new float[1];
    }
    //System.Random rand = new System.Random();

//peeps at vrbar today, Larry (sound) and nick? VR guy

    void OnAudioFilterRead(float[] data, int channels)//~20ms chunks of data
    {
        if (channels > 0)
        {
            if (isValidTime)//makes sure that pausing doesn't break the audio engine
            {            
                double modulatedFrequencyA = frequencyA;//because the frequency will modulate
                double modulatedFrequencyB = frequencyB;
                double modulatedFrequencyC = frequencyC;
                actualRippleFrequency = rippleFrequency;
                modulatedFrequencyA += Mathf.Sin(_deltaTime * modFreqMultA) * modAmplitudeA * frequencyA;
                modulatedFrequencyB += Mathf.Sin(_deltaTime * modFreqMultB) * modAmplitudeB * frequencyB;
                modulatedFrequencyC += Mathf.Sin(_deltaTime * modFreqMultC) * modAmplitudeC * frequencyC;

                actualRippleFrequency *= pitch;
                increment = piX2 / sampling_frequency;//2.0 * Mathf.PI / sampling_frequency;
                incrementA = modulatedFrequencyA * increment;
                incrementB = modulatedFrequencyB * increment;
                incrementC = modulatedFrequencyC * increment;
                rippleIncrement = actualRippleFrequency * increment;

            
                for (int i = 0; i < data.Length; i += channels)
                {
                    float dataA = 0;
                    float dataB = 0;
                    float dataC = 0;
                    phaseA += incrementA;
                    phaseB += incrementB;
                    phaseC += incrementC;
                    ripplePhase += rippleIncrement;
                    //data[i] = (float)(gain * Mathf.Sin((float)phase));
                    if (Mathf.Sin((float)phaseA) >= 0)
                        dataA = (float)gainA * 0.6f;
                    else
                        dataA = (-(float)gainA) * 0.6f;

                    dataB = Mathf.Sin((float)phaseB);
                    dataB = Mathf.Min(.25f,dataB);
                    dataB = Mathf.Max(-.25f,dataB);
                    dataB *= gainB * 4f;

                    if (Mathf.Sin((float)phaseC) >= 0)
                        dataC = (float)gainC * 0.6f;
                    else
                        dataC = (-(float)gainC) * 0.6f;
                    float rippleSignal = Mathf.Lerp(rippleMin, 1, Mathf.Sin((float)ripplePhase) * .5f + .5f);//*.5f + .5f;//+.75f;
                    //if (rippleSignal < 0)
                    //    rippleSignal *= -1;
                    if (pitch < .9)
                    {
                        dataA *= whooshGainA;//brakup thingie
                        dataB *= whooshGainB;// 2.5f;//brakup thingie
                        dataC *= whooshGainC;// 1.5f;//brakup thingie

                        if (useRipple) {
                            dataA *= rippleSignal;//brakup thingie
                            dataB *= rippleSignal;// 2.5f;//brakup thingie
                            dataC *= rippleSignal;// 1.5f;//brakup thingie
                        }
                    }
                    //if (useMultipliedAudio)
                    //{
                    //    data[i] *= (dataA + dataB + dataC) * masterGain * 1.5f;
                    //} 
                    //else 
                    //{
                    data[i] = (dataA + dataB + dataC) * masterGain;
                    //}

                    data[i] = Mathf.Max(-1f, data[i]);
                    data[i] = Mathf.Min(1f, data[i]);

                    if (channels == 2) data[i + 1] = data[i];
                    if (phaseA > (piX2))
                        phaseA -= piX2;
                    if (phaseB > (piX2))
                        phaseB -= piX2;
                    if (phaseC > (piX2))
                        phaseC -= piX2;
                    if (ripplePhase > (piX2))
                        ripplePhase -= piX2;
                }
                //dataBuffer = data;
            }
            //else 
            //{
            //    data = dataBuffer;
            //}
        }//channels > 0
    }

    void Update()
    {
        _deltaTime = Time.deltaTime;
        if (_deltaTime <= 0)
            _deltaTime = 0.001f;

        /*if (useMultipliedAudio && audioSource.clip == null)
        {
            audioSource.clip = multiplierClip;
            audioSource.Stop();
            //audioSource.spatialize = false;
            audioSource.Play();
        } else if (!useMultipliedAudio && audioSource.clip != null)
        {
            audioSource.clip = null;
            audioSource.Stop();
            //audioSource.spatialize = true;
            audioSource.Play();
        }*/
        if (Time.timeScale > 0.001)
        {
            //tested without loop clicked
            //tested commenting out buffer, it needs a buffer even a 0 buffer
            //else the audio still breaks when playing in the editor
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


        if (useBladeScaleForVolume)
            masterGain = bladeScaleVolumeCurve.Evaluate(_bladeTransform.localScale.y );//Mathf.Lerp(0f,1f,_bladeTransform.localScale.y * 2);

        float saberDistance = (_bladeTransform.position - previousPos).sqrMagnitude;
        float tempPitch = Mathf.Lerp(1f, pitchBend, saberDistance / 3f / _deltaTime);

        if (usePitchSettle)
        {
            pitch = Mathf.Lerp(previousPitch,tempPitch,_deltaTime * pitchSettleSpeed);  //tempPitch;
        } 
        else 
        {
            pitch = tempPitch;
        }

        audioSource.volume = Mathf.Lerp(.15f, 1f, (saberDistance/ (_deltaTime /* *_deltaTime*/)) * .1f);
        
        previousPos = _bladeTransform.position;
        previousPitch = pitch;

    }
}
