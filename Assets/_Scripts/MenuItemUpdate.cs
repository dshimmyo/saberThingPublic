using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuItemUpdate : MonoBehaviour {
    private TextMesh text;
    private enum menuTypes {Handedness, SaberAutoOn, SaberAlwaysInHand}; 
    [SerializeField] private menuTypes menuType;
    public GameObject checkBox;
    private Mesh mesh;

    void Start () {
        text = GetComponent<TextMesh> ();
        mesh = GetComponent<MeshFilter>().sharedMesh;
        mesh.RecalculateBounds();
        mesh.bounds.Expand(100f);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Menu Collider");
    }

    void Update () {
        mesh.RecalculateBounds();

        switch (menuType) 
        {
        case menuTypes.Handedness:
            text.text = "Left-Hand Mode";
            break;
        case menuTypes.SaberAutoOn:
            text.text = "Saber Auto-On";
            break;
        case menuTypes.SaberAlwaysInHand:
            text.text = "Saber In Hand (easy)";
            break;
        default:
            break;
        }


	}
}
