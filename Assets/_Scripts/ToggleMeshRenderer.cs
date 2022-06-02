using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleMeshRenderer : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey;
    private MeshRenderer mr;
    
    // Start is called before the first frame update
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (toggleKey != null)
        {
            if (Input.GetKeyDown(toggleKey))
                mr.enabled = !mr.enabled;
        }
    }
}
