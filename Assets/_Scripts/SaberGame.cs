using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
public class SaberGame : MonoBehaviour {

    [SerializeField] TextMesh deviceText;
    public static bool isQuest;
    public static bool isOculusOpenVR;
    public static bool isInitialized = false;
    void Awake() {
        //DontDestroyOnLoad(this.gameObject);
    }
    // Use this for initialization
    void Start () {
                OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.High;//was HighTop, tried Low

	}

    public static bool QuestCheck()
    {
#if UNITY_ANDROID
        isQuest = true;
#endif
#if UNITY_ANDROID || OCULUS_OPENVR_HACK
        isOculusOpenVR = true;//used for haptics so far
#endif
        if (isQuest)
        {
            Debug.Log("isQuest");
        }

        if (!isInitialized)
        {
            Debug.Log("XRDevice.model: " + UnityEngine.XR.XRDevice.model);

            if (UnityEngine.XR.XRDevice.model.Contains("Quest") )
            {
                isQuest = true;
                OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.HighTop;//was HighTop, tried Low
                // OVRManager.FixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.{Off/LMSLow/LMSMedium/LMSHigh};
                //OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.LMSHigh;

                //deviceText.text += "-foveated";
                
            }   
            isInitialized = true;
        }
        return isQuest;
    }
	
	// Update is called once per frame
	void Update () {
            if (isInitialized && isQuest && !deviceText.text.Contains("-foveated"))
            {
                //isQuest = true;
                //OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.High;//was HighTop, tried Low
                deviceText.text += "-foveated";
                
            }   

        if (!isInitialized) QuestCheck();
        //quit on esc
        if (Input.GetKey("escape")){
            Application.Quit();
        }	
        //if (!isInitialized) isInitialized = true;
	}
}
