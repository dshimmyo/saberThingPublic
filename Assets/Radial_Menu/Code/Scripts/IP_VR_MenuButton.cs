using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IndiePixel.VR
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Image))]
    public class IP_VR_MenuButton : MonoBehaviour 
    {
        #region Variables
        [Header("Button Properties")]
        public int buttonID;
        public string buttonText;
        public Image buttonIcon;
        public Sprite normalImage;
        public Sprite hoverImage;

        [Header("Events")]
        public UnityEvent OnClick = new UnityEvent();

        private Animator animator;
        private Image currentImage;
        #endregion


        #region Main Methods
    	// Use this for initialization
    	void Start () 
        {
            animator = GetComponent<Animator>();
            currentImage = GetComponent<Image>();
            if(currentImage && normalImage)
            {
                currentImage.sprite = normalImage;
            }
    	}
        #endregion


        #region Custom Methods
        public void Hover(int anID)
        {
            if(currentImage)
            {
                if(anID == buttonID && hoverImage)
                {
                    currentImage.sprite = hoverImage;
                    HandleAnimator(true);
                }
                else if(normalImage)
                {
                    currentImage.sprite = normalImage;
                    HandleAnimator(false);
                }
            }
        }

        public void Click(int anID)
        {
            if(buttonID == anID)
            {
                if(OnClick != null)
                {
                    OnClick.Invoke();
                }
            }
        }

        void HandleAnimator(bool aToggle)
        {
            if(animator)
            {
                animator.SetBool("hover", aToggle);
            }
        }
        #endregion
    }
}
