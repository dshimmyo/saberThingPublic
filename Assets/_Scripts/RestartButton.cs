using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour, ICommandable
{
    bool previousFrameInteract = false;
    bool currentFrameInteract = true;

    public void Command()
    {
        Debug.Log("restart");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Interact()
    {
        if (!previousFrameInteract)
        {
            AudioSource.PlayClipAtPoint(Resources.Load("_Sounds/spawn") as AudioClip, transform.position);
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
