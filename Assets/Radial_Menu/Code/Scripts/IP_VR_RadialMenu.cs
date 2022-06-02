using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IndiePixel.VR
{
    public class onHover : UnityEvent<int>{}
    public class onClick : UnityEvent<int>{}

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CanvasGroup))]
    public class IP_VR_RadialMenu : MonoBehaviour 
    {
        #region Variables
        [Header("Controller Properties")]
        public SteamVR_TrackedController controller;

        [Header("UI Properties")]
        public List<IP_VR_MenuButton> menuButtons = new List<IP_VR_MenuButton>();
        public RectTransform m_ArrowContainer;
        public Text m_DebugText;

        [Header("Events")]
        public UnityEvent OnMenuChanged = new UnityEvent();


        private Vector2 currentAxis;
        private SteamVR_Controller.Device controllerDevice;
        private Animator animator;

        private bool menuOpen = false;
        private bool allowNavigation = false;
        private bool isTouching = false;
        private float currentAngle;

        private int currentMenuID = -1;
        private int previousMenuID = -1;

        private onHover OnHover = new onHover();
        private onClick OnClick = new onClick();
        #endregion


        #region Main Methods
    	// Use this for initialization
    	void Start () 
        {
            animator = GetComponent<Animator>();

            if(controller)
            {
                controllerDevice = SteamVR_Controller.Input((int)controller.controllerIndex);
                controller.PadTouched += HandlePadTouched;
                controller.PadUntouched += HandlePadUnTouched;
                controller.PadClicked += HandlePadClicked;
                controller.MenuButtonClicked += HandleMenuActivation;
            }

            if(menuButtons.Count > 0)
            {
                foreach(var button in menuButtons)
                {
                    OnHover.AddListener(button.Hover);
                    OnClick.AddListener(button.Click);
                }
            }
    	}

        void OnDisable()
        {
            if(controller)
            {
                controller.PadTouched -= HandlePadTouched;
                controller.PadUntouched -= HandlePadUnTouched;
                controller.PadClicked -= HandlePadClicked;
                controller.MenuButtonClicked -= HandleMenuActivation;
            }

            if(OnHover != null)
            {
                OnHover.RemoveAllListeners();
            }

            if(OnClick != null)
            {
                OnClick.RemoveAllListeners();
            }
        }
    	
    	// Update is called once per frame
    	void Update () 
        {
            if(controllerDevice != null)
            {
                if(menuOpen && isTouching)
                {
                    UpdateMenu();
                }
            }
    	}
        #endregion


        #region Custom Methods
        void HandlePadTouched(object sender, ClickedEventArgs e)
        {
            isTouching = true;
//            HandleDebugText("Touched Pad");
        }

        void HandlePadUnTouched(object sender, ClickedEventArgs e)
        {
            isTouching = false;
//            HandleDebugText("Un Touched Pad");
        }

        void HandlePadClicked(object sender, ClickedEventArgs e)
        {
//            HandleDebugText("Clicked Pad");
            if(OnClick != null)
            {
                OnClick.Invoke(currentMenuID);
            }
        }

        void HandleMenuActivation(object sender, ClickedEventArgs e)
        {
            menuOpen = !menuOpen;

            HandleDebugText("Menu is: " + menuOpen);

            HandleAnimator();
        }

        void HandleAnimator()
        {
            if(animator)
            {
                animator.SetBool("open", menuOpen);
            }
        }

        void UpdateMenu()
        {
            if(isTouching)
            {
                //Get the Current Axis from the Touch Pad and turn it into and Angle
                currentAxis = controllerDevice.GetAxis();
                currentAngle = Vector2.SignedAngle(Vector2.up, currentAxis);

//                HandleDebugText(currentAngle.ToString());
                float menuAngle = currentAngle;
                if(menuAngle < 0)
                {
                    menuAngle += 360f;
                }
                int updateMenuID = (int)(menuAngle / (360f / 8f));
                HandleDebugText(updateMenuID.ToString());


                //Update Current Menu ID
                if(updateMenuID != currentMenuID)
                {
                    if(OnHover != null)
                    {
                        OnHover.Invoke(updateMenuID);    
                    }

                    if(OnMenuChanged != null)
                    {
                        OnMenuChanged.Invoke();
                    }

                    previousMenuID = currentMenuID;
                    currentMenuID = updateMenuID;
                }


                //Rotate Arrow
                if(m_ArrowContainer)
                {
                    m_ArrowContainer.localRotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);
                }
            }
        }

        void HandleDebugText(string aString)
        {
            if(m_DebugText)
            {
                m_DebugText.text = aString;
            }
        }
        #endregion
    }
}
