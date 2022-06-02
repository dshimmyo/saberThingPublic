using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class ControlMappingScreen : MonoBehaviour
{
    [SerializeField] private Texture2D viveControlsImage;
    [SerializeField] private Texture2D riftControlsImage;
    [SerializeField] private Texture2D wmrControlsImage;
    private Material mat;
    private Texture2D tex;
    private bool headsetDetected = false;
    // Start is called before the first frame update
    void Start()
    {
        mat = GetComponent<Renderer>().material;
        tex = mat.mainTexture as Texture2D;
    }

    // Update is called once per frame
    void Update()
    {
        if (!headsetDetected)
            if (UnityEngine.XR.XRDevice.isPresent)
            {
                headsetDetected = true;
                string vrDeviceModel = UnityEngine.XR.XRDevice.model;

                if (vrDeviceModel.StartsWith("Acer") ||
                    vrDeviceModel.Contains("Windows") ||
                    vrDeviceModel.Contains("Lenovo") ||
                    vrDeviceModel.Contains("HP") ||
                    vrDeviceModel.Contains("Samsung"))
                {
                    mat.mainTexture = wmrControlsImage;
                    //isWMR = true;
                }
                else if (vrDeviceModel.StartsWith("Oculus"))//"Oculus Rift CV1")
                {
                    mat.mainTexture = riftControlsImage;

                }
                else if (vrDeviceModel.ToLower().Contains("vive") ||
                    vrDeviceModel.Contains("HTC") ||
                    vrDeviceModel.ToLower().Contains("valve") ||
                    vrDeviceModel.ToLower().Contains("index")
                    )
                {
                    mat.mainTexture = viveControlsImage;

                }
                else
                {
                    mat.mainTexture = viveControlsImage;
                }
            }
            else
            {
                if (mat.mainTexture == null)
                    mat.mainTexture = viveControlsImage;

            }
    }
}
