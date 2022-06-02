using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitPlane : MonoBehaviour, ICommandable
{
    bool previousFrameInteract = false;
    bool currentFrameInteract = true;

    public void Command()
    {
        Debug.Log("quit");
        Application.Quit();
    }

    public void Interact()
    {
        if (!previousFrameInteract)
        {
            AudioSource.PlayClipAtPoint(Resources.Load("_Sounds/oof") as AudioClip, transform.position);
        }
        currentFrameInteract = true;

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        previousFrameInteract = currentFrameInteract;
        currentFrameInteract = false;
    }
}
