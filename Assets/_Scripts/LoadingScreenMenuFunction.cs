using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this object group should stick around and contain all of the menu functionality
// make the controllers visible, make clicking and selecting controls possible
// for now there should be a quit and restart button

public class LoadingScreenMenuFunction : MonoBehaviour
{
    [SerializeField] private GameObject rightController;
    [SerializeField] private GameObject leftController;
    [SerializeField] private int controllerRenderQueue = 0;
    private Component[] lsf;
    private bool foundLf = false;
    private bool foundRt = false;
    private bool isReady = false;
    private Shader shader1;
    private Shader shader2;
    [SerializeField] Light m_light;
    private float justIncaseTimer = 0;
    private bool isCheckingControllers = false;
    private bool isFinallyStarted = false;
    private bool receivedClick = false;
    [SerializeField] private Texture2D debugTexture;
    [SerializeField] private Animator circleSpriteAnim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private bool isInMenu = true;
    private bool previousFrameClicked = false;
    private Light[] allLights;
    [SerializeField] private SteamVR_LaserPointer laserPointerLf;
    [SerializeField] private SteamVR_LaserPointer laserPointerRt;
    private GameObject pointerLf;
    private GameObject pointerRt;
    private bool previousRightRayHit = false;
    private bool previousLeftRayHit = false;
    private bool currentRightRayHit = false;
    private bool currentLeftRayHit = false;
    [SerializeField] private bool enableTimeFreezing = false;

    void Start()
    {
        if (enableTimeFreezing)
            Time.timeScale = 0f;
        shader1 = Shader.Find("Standard");
        shader2 = Shader.Find("Shimmy/Uber");
        StartCoroutine(CheckControllers());//make the controllers visible by modifying their renderQueue
        lsf = GetComponentsInChildren(typeof(LoadScreenFade), true);
        if (m_light != null) {
            m_light.enabled = true;
            m_light.intensity = 1f;
        }
        allLights = FindObjectsOfType<Light>();
        spriteRenderer.enabled = false;
        SetUpLights();
        if (!isInMenu)
        {
            laserPointerLf.enabled = false;
            laserPointerRt.enabled = false;            
        } 
        else
        {
            laserPointerLf.enabled = true;
            laserPointerRt.enabled = true;
        }
        pointerLf = laserPointerLf.pointer;
        pointerRt = laserPointerRt.pointer;
        if (pointerLf != null)
            if (pointerLf.GetComponent<Renderer>().material.renderQueue < 4000)
                pointerLf.GetComponent<Renderer>().material.renderQueue = 4003;
        if (pointerRt != null)
            if (pointerRt.GetComponent<Renderer>().material.renderQueue < 4000)
                pointerRt.GetComponent<Renderer>().material.renderQueue = 4003;
    }

    public void SendClick()
    {
        receivedClick = true;
    }

    private IEnumerator CheckControllers()
    {
        bool done = false;
        //bool foundLf = false;
        //bool foundRt = false;
        isCheckingControllers = true;
        while (!done)
        {
            int modelCount = 0;
            //int mtlCount = 0;
            //int modMtlCount = 0;
            pointerLf = laserPointerLf.pointer;
            pointerRt = laserPointerRt.pointer;
            if (pointerLf != null)
                if (pointerLf.GetComponent<Renderer>().material.renderQueue < 4000)
                    pointerLf.GetComponent<Renderer>().material.renderQueue = 4003;
            if (pointerRt != null)
                if (pointerRt.GetComponent<Renderer>().material.renderQueue < 4000)
                    pointerRt.GetComponent<Renderer>().material.renderQueue = 4003;
            foreach (SteamVR_RenderModel model in leftController.GetComponentsInChildren<SteamVR_RenderModel>())
            {
                foreach (var child in model.GetComponentsInChildren<MeshRenderer>()) {
                    if (controllerRenderQueue < 1)
                        controllerRenderQueue = child.material.renderQueue;// = 4003;
                    if (child.name == "body") 
                        child.material.SetColor("_Color", new Color(.6f, .6f, .6f * .8f));
                    else
                        child.material.SetColor("_Color", new Color(.75f, .75f, .75f * .8f));

                    child.enabled = true;
                    child.material.shader = shader2;
                    child.material.renderQueue = 4003;
                    child.material.SetColor("_OutlineColor", Color.red);
                    child.material.SetFloat("_OutlineWidth", .01f);
                    child.material.SetFloat("_Outline", 1f);
                    child.material.SetTexture("_MainTex", null);

                    if (child.material.renderQueue > 4000)//try to make sure that it becomes visible -it works!
                        modelCount++;
                }

            }
            if (modelCount > 0)
            {
                foundLf = true;
                Debug.Log("left controller found");
            }
            
        
            modelCount = 0;
            foreach (SteamVR_RenderModel model in rightController.GetComponentsInChildren<SteamVR_RenderModel>())
            {
                foreach (var child in model.GetComponentsInChildren<MeshRenderer>()) {
                    if (controllerRenderQueue < 1)
                        controllerRenderQueue = child.material.renderQueue;// = 4003;
                    if (child.name == "body")
                        child.material.SetColor("_Color", new Color(.6f, .6f, .6f * .8f));
                    else
                        child.material.SetColor("_Color", new Color(.75f, .75f, .75f * .8f));

                    child.enabled = true;

                    child.material.shader = shader2;
                    child.material.renderQueue = 4003;
                    //child.material.SetColor("_EmissionColor", new Color(0f, 0f, .25f));
                    child.material.SetColor("_OutlineColor", Color.blue);
                    child.material.SetFloat("_OutlineWidth", .01f);
                    child.material.SetFloat("_Outline", 1f);

                    child.material.SetTexture("_MainTex", null);

                    if (child.material.renderQueue > 4000)//try to make sure that it becomes visible
                    modelCount++;

                }
                //foundRt = true;
            }
            if (modelCount > 0)
            {
                foundRt = true;
                Debug.Log("right ontroller found");
            }
        

            yield return null;

            if (foundRt && foundLf)
                done = true;
        }
        isReady = true;
        isCheckingControllers = false;
        justIncaseTimer = Time.realtimeSinceStartup;
    }

    private void RevertControllers()
        {
        if (leftController.activeInHierarchy)
        {
            foreach (SteamVR_RenderModel model in leftController.GetComponentsInChildren<SteamVR_RenderModel>())
            {
                foreach (var child in model.GetComponentsInChildren<MeshRenderer>())
                {

                    child.material.shader = shader1;

                    child.material.renderQueue = controllerRenderQueue;


                }
            }
        }
        if (rightController.activeInHierarchy)
        {
            foreach (SteamVR_RenderModel model in rightController.GetComponentsInChildren<SteamVR_RenderModel>())
            {
                foreach (var child in model.GetComponentsInChildren<MeshRenderer>())
                {

                    child.material.shader = shader1;

                    child.material.renderQueue = controllerRenderQueue;

                }

            }
        }
    }

    private void MenuControllers()
    {
        //if (leftController.activeInHierarchy)
        {
            foreach (SteamVR_RenderModel model in leftController.GetComponentsInChildren<SteamVR_RenderModel>())
            {
                foreach (var child in model.GetComponentsInChildren<MeshRenderer>())
                {
                    child.enabled = true;

                    child.material.shader = shader2;

                    child.material.renderQueue = 4005;// controllerRenderQueue;


                }
            }
        }
        //if (rightController.activeInHierarchy)
        {
            foreach (SteamVR_RenderModel model in rightController.GetComponentsInChildren<SteamVR_RenderModel>())
            {
                foreach (var child in model.GetComponentsInChildren<MeshRenderer>())
                {
                    child.enabled = true;

                    child.material.shader = shader2;

                    child.material.renderQueue = 4005;// controllerRenderQueue;

                }

            }
        }
    }

    private bool GetTriggerHoldSetAnim()
    {
        bool isClicked = false;
        bool isFlippedHands = HandednessHack.flippedHands;
        if (isFlippedHands)
        {
            if (Input.GetAxis("HTC_VIU_LeftTrigger") > .5 && !currentLeftRayHit) 
            {
                //HapticsTesting.SimplePulse("left");
                isClicked = true;
            }
            else
                isClicked = false;
        }
        else
        {
            if (Input.GetAxis("HTC_VIU_RightTrigger") > .5 && !currentRightRayHit)
            {
                //HapticsTesting.SimplePulse("right");
                isClicked = true;
            }
            else
                isClicked = false;
        }

        if (isClicked)
        {
            if ((circleSpriteAnim.GetBool("triggerDown") && previousFrameClicked) || (!circleSpriteAnim.GetBool("triggerDown") && !previousFrameClicked))
            {
                circleSpriteAnim.SetBool("triggerDown", true);

            }

            if (circleSpriteAnim.GetBool("triggerDown"))
                if (isFlippedHands)
                    HapticsTesting.SimplePulse("left");
                else
                    HapticsTesting.SimplePulse("right");
        }
        else
        {
            circleSpriteAnim.SetBool("triggerDown", false);
            //spriteRenderer.enabled = false;
        }
        return isClicked;
    }

    private bool GetButtonClick()
    {
        if (receivedClick)
        {
            receivedClick = false;
            return true;
        }
        if (Input.GetKeyDown(KeyCode.M))
            return true;
        return false;
    }

    IEnumerator FadeLightOut()
    {
        float fadeStart = Time.realtimeSinceStartup;
        float fadeTime = 2f;//try 2 seconds
        float lightIntensity = 1;

        while (lightIntensity > 0)
        {
            lightIntensity = 1f - ((Time.realtimeSinceStartup - fadeStart) / fadeTime);
            //light.intensity -= .01f;
            m_light.intensity = lightIntensity;
            yield return null;
        }
        m_light.intensity = 0f;
        m_light.enabled = false;
        RevertControllers();

    }

    IEnumerator FadeLightIn()
    {
        m_light.enabled = true;
        m_light.intensity = 0f;
        while (m_light.intensity < 1)
        {
            m_light.intensity += .02f;

            yield return null;
        }
        m_light.intensity = 1f;

    }

    private void SetUpLights()
    {
        allLights = FindObjectsOfType<Light>();

        int cullingMask = 1 << 16;
        cullingMask = ~cullingMask;
        foreach (Light li in allLights)
        {
            if (li != m_light)
                li.cullingMask = cullingMask;
        }
    }

    void Update()
    {

        if (!isInMenu)
        {
            if (pointerLf.activeInHierarchy && !isCheckingControllers)
            {
                StartCoroutine(CheckControllers());
            }

        }

        currentRightRayHit = false;
        currentLeftRayHit = false;

        if (!isFinallyStarted)
        {//game start functionality
            if (!isReady)
            {
                if (enableTimeFreezing)
                {
                    if (Time.timeScale > 0.5)
                        Debug.Log("timescale is fucked");
                    Time.timeScale = 0;
                }
            }
            if (!isCheckingControllers && (Time.realtimeSinceStartup - justIncaseTimer > 3))
            {
                isReady = false;
                StartCoroutine(CheckControllers());
            }

            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(rightController.transform.position, rightController.transform.forward, out hit, Mathf.Infinity))
            {
                if (hit.transform.GetComponent<ICommandable>() != null)
                {
                    currentRightRayHit = true;
                    //Debug.Log("hit a commandable");
                    if (!previousRightRayHit)
                        HapticsTesting.SimplePulse("right");
                    hit.transform.GetComponent<ICommandable>().Interact();
                    if ((Input.GetAxis("HTC_VIU_RightTrigger") > .5))
                        hit.transform.GetComponent<ICommandable>().Command();
                    
                }
            }

            if (Physics.Raycast(leftController.transform.position, leftController.transform.forward, out hit, Mathf.Infinity))
            {
                if (hit.transform.GetComponent<ICommandable>() != null)
                {
                    currentLeftRayHit = true;
                    //Debug.Log("hit a commandable");
                    if (!previousLeftRayHit)
                        HapticsTesting.SimplePulse("left");
                    hit.transform.GetComponent<ICommandable>().Interact();
                    if ((Input.GetAxis("HTC_VIU_LeftTrigger") > .5))
                        hit.transform.GetComponent<ICommandable>().Command();

                }
            }
            previousLeftRayHit = currentLeftRayHit;
            previousRightRayHit = currentRightRayHit;

            if (BLINDED_AM_ME.MeshCut.isDoneInitializing && (isReady || Input.GetKey(KeyCode.M)))
                if (!isReady)
                    isReady = true; //hacky for testing only
                SetUpLights();
                previousFrameClicked = GetTriggerHoldSetAnim();
                if (GetButtonClick())
                {
                    if (pointerLf != null)
                        pointerLf.SetActive(false);
                    if (pointerRt != null)
                        pointerRt.SetActive(false);
                    isInMenu = false;
                    circleSpriteAnim.SetBool("triggerDown", false);
                    foreach (LoadScreenFade comp in lsf)
                    {
                        comp.SetClicked();
                        Collider col = comp.gameObject.GetComponent<Collider>();
                        if (col != null)
                            col.enabled = false;
                    }
                    if (enableTimeFreezing)
                        Time.timeScale = 1;
                    isFinallyStarted = true;
                    StartCoroutine(FadeLightOut());
                    //RevertControllers();

            }
        }

        else

        {
            if (!isInMenu)// in game behavior
            {

                previousFrameClicked = GetTriggerHoldSetAnim();
                if (GetButtonClick())
                {
                    if (pointerLf != null)
                        pointerLf.SetActive(true);
                    if (pointerRt != null)
                        pointerRt.SetActive(true);
                    circleSpriteAnim.SetBool("triggerDown", false);
                    //RevertControllers();
                    foreach (LoadScreenFade comp in lsf)
                    {
                        comp.SetClickBack();
                        Collider col = comp.gameObject.GetComponent<Collider>();
                        if (col != null)
                            col.enabled = true;
                    }
                    MenuControllers();
                    StartCoroutine(FadeLightIn());//fade light in
                    isInMenu = true;

                }
            }
            else
            {

                RaycastHit hit;
                if (Physics.Raycast(rightController.transform.position, rightController.transform.forward, out hit, Mathf.Infinity))
                {
                    if (hit.transform.GetComponent<ICommandable>() != null)
                    {
                        currentRightRayHit = true;
                        //Debug.Log("hit a commandable");
                        if (!previousRightRayHit)
                            HapticsTesting.SimplePulse("right");
                        hit.transform.GetComponent<ICommandable>().Interact();
                        if ((Input.GetAxis("HTC_VIU_RightTrigger") > .5))
                            hit.transform.GetComponent<ICommandable>().Command();

                    }
                }

                if (Physics.Raycast(leftController.transform.position, leftController.transform.forward, out hit, Mathf.Infinity))
                {
                    if (hit.transform.GetComponent<ICommandable>() != null)
                    {
                        currentLeftRayHit = true;
                        //Debug.Log("hit a commandable");
                        if (!previousLeftRayHit)
                            HapticsTesting.SimplePulse("left");
                        hit.transform.GetComponent<ICommandable>().Interact();
                        if ((Input.GetAxis("HTC_VIU_LeftTrigger") > .5))
                            hit.transform.GetComponent<ICommandable>().Command();

                    }
                }
                previousLeftRayHit = currentLeftRayHit;
                previousRightRayHit = currentRightRayHit;


                previousFrameClicked = GetTriggerHoldSetAnim();
                if (GetButtonClick())
                {
                    if (pointerLf != null)
                        pointerLf.SetActive(false);
                    if (pointerRt != null)
                        pointerRt.SetActive(false);
                    isInMenu = false;
                    circleSpriteAnim.SetBool("triggerDown", false);
                    foreach (LoadScreenFade comp in lsf)
                    {
                        comp.SetClicked();
                        Collider col = comp.gameObject.GetComponent<Collider>();
                        if (col != null)
                            col.enabled = false;
                    }
                    if (enableTimeFreezing)
                    Time.timeScale = 1;
                    StartCoroutine(FadeLightOut());
                }
            }

        }
        //        isClicked = true;
    }


}
