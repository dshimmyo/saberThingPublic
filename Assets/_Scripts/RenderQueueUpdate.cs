using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderQueueUpdate : MonoBehaviour
{
    [SerializeField] int renderQueue = 3000;
    // Start is called before the first frame update
    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        Material mat = rend.material;
        mat.renderQueue = renderQueue;

    }

    // Update is called once per frame
    void Update()
    {
    }
}
