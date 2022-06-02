using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HudToggle : MonoBehaviour {
    [SerializeField]
    GameObject hud;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.H)) {
            hud.SetActive(!hud.activeInHierarchy);
        }	
	}
}
