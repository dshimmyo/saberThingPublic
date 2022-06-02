using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckboxToggle : MonoBehaviour {
    public GameObject XMark;
    public bool defaultState;
    [SerializeField]
    private bool toggleState=false;
    private enum menuTypes { Handedness, SaberAutoOn, SaberAlwaysInHand };
    [SerializeField]
    private menuTypes menuType;
    // Use this for initialization
    void Start () {
        XMark.SetActive(defaultState);
        toggleState = defaultState;

    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 1)
        {
            toggleState = !toggleState;
            XMark.SetActive(toggleState);
            Debug.Log("Checkbox Collider");
        }
    }
    void Awake()
    {
        switch (menuType)
        {
            case menuTypes.Handedness:
                toggleState = HandednessHack.flippedHands;
                break;
            case menuTypes.SaberAutoOn:
                toggleState = gameObject.GetComponent<RemoteGrab>().bladeOn;
                break;
            case menuTypes.SaberAlwaysInHand:
                toggleState = gameObject.GetComponent<RemoteGrab>().alwaysGrab;
                break;
            default:
                break;
        }
    }
    void ToggleStuff()
    {
        switch (menuType)
        {
            case menuTypes.Handedness:
                HandednessHack.SetLeftHanded(toggleState);
                break;
            case menuTypes.SaberAutoOn:
                gameObject.GetComponent<RemoteGrab>().bladeOn = toggleState;
                break;
            case menuTypes.SaberAlwaysInHand:
                gameObject.GetComponent<RemoteGrab>().alwaysGrab = toggleState;
                break;
            default:
                break;
        }

    }
    // Update is called once per frame
    void Update ()
    {
	}
}
