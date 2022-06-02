using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndiePixel.VR
{
    [RequireComponent(typeof(MeshRenderer))]
    public class IP_ToggleRenderer : MonoBehaviour 
    {
        #region Variables
        private MeshRenderer mRenderer;
        #endregion

        #region Main Method
    	// Use this for initialization
    	void Start () 
        {
            mRenderer = GetComponent<MeshRenderer>();	
    	}

        public void ToggleRenderer()
        {
            if(mRenderer)
            {
                mRenderer.enabled = !mRenderer.enabled;
            }
        }
        #endregion
    }
}
