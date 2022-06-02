using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendClick : MonoBehaviour
{

    public LoadingScreenMenuFunction lsmf;
    // Start is called before the first frame update
    void Start()
    {
       
    }
    public void SendClicks()
    {
        lsmf.SendClick();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
