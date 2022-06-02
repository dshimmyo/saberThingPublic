using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuToggle : MonoBehaviour {
    [SerializeField]
    private GameObject hud;
    [SerializeField]
    private GameObject menu;
    [SerializeField]
    private GameObject hudLocator;
    private bool hudState = true;

    private float menuButtonTimer = 0;
    public float menuButtonTime = .1f;//if the button is depressed for less than this time
    private bool flippedHands = false;
    private bool isPressing = false;
    private bool bladeOnState = false;
    private bool autoGrabState = false;
    public bool menuToggle = true;
    private bool isMenu = false;
    [SerializeField] private KeyCode toggleKey;

    public enum toggleButtons { UseToggleKey, HTCShoulder };//three major function calls that should be alternated
    [SerializeField] private toggleButtons toggleChoice = toggleButtons.UseToggleKey;
    //[SerializeField] private bool 
    // Use this for initialization
    void Start () {
        menu.SetActive(false);
        hud.SetActive(true);
	}

    void MenuToggler() {
        isMenu = !isMenu;
        if (!isMenu)
        {
            if (menuToggle)
            {
                bladeOnState = gameObject.GetComponent<RemoteGrab>().bladeOn;
                gameObject.GetComponent<RemoteGrab>().PutSaberInHandNow();
                gameObject.GetComponent<RemoteGrab>().bladeOn = true;
                gameObject.GetComponent<RemoteGrab>().alwaysGrab = true;
                gameObject.GetComponent<RemoteGrab>().grabLock = true;
                menu.SetActive(true);
                hudState = hud.activeInHierarchy;
                hud.SetActive(false);
                hudLocator.GetComponent<HudLocator>().enabled = false;
            }
            Time.timeScale = 0f;// .25f;
        }
        else
        {
            if (menuToggle)
            {
                menu.SetActive(false);
                hud.SetActive(hudState);
                hudLocator.GetComponent<HudLocator>().enabled = true;
            }
            Time.timeScale = 1f;

        }
    }

    private bool GetInput(bool isUp)
    {

        if (toggleChoice == toggleButtons.HTCShoulder)
        {
            if (isUp){
                if ((!flippedHands && Input.GetButtonUp("HTCShoulderLf")) || (flippedHands && Input.GetButtonUp("HTCShoulderRt")))
                {
                    return true;
                }
            }
            else
            {
                if ((!flippedHands && Input.GetButton("HTCShoulderLf")) || (flippedHands && Input.GetButton("HTCShoulderRt")))
                {
                    return true;
                }
            }


        } 
        else if (toggleChoice == toggleButtons.UseToggleKey)
        {
            if (isUp)
            {
                if (Input.GetKeyUp(toggleKey))
                {
                    return true;
                }
            }
            else
            {
                if (Input.GetKey(toggleKey))
                {
                    return true;
                }
            }
        }
        return false;
    }

	// Update is called once per frame
	void Update () {
        flippedHands = HandednessHack.flippedHands;

        //if ((!flippedHands && Input.GetButtonUp("HTCShoulderLf")) || (flippedHands && Input.GetButtonUp("HTCShoulderRt")))
        if (GetInput(true))
        {
            menuButtonTimer = 0;

            if (menuButtonTimer < menuButtonTime)
            {
                MenuToggler();
            }

        }
        //if (Input.GetKeyDown(KeyCode.M)){
        //    MenuToggler();
        //}

        //if ((!flippedHands && Input.GetButton("HTCShoulderLf")) || (flippedHands && Input.GetButton("HTCShoulderRt"))) {
        if (GetInput(false))
        {
            menuButtonTimer += Time.deltaTime;
            if (!isPressing)
            {
                isPressing = true;//start a new press
                menuButtonTimer = 0;
            }

        }
        else
        {
            menuButtonTimer = 0;
        }

        /*if (Input.GetKeyDown(KeyCode.H))//what the heck is this?
        {
            if (!menu.activeInHierarchy)
                hud.SetActive(!hud.activeInHierarchy);
        }*/

    }
}
