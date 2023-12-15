/// Written by Ethan Woods -> 016erw@gmail.com
/// The InputManager class is reponsible for handling all controller-specific user interactions and altering gameobjects to reflect these changes

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction;

public class InputManager : MonoBehaviour
{
    #region Variables
    [SerializeField]
    private Color idlePointerColor,
        hoverPointerColor,
        selectPointerColor;

    [Tooltip("Tick layers that you want raycast to ignore")]
    [SerializeField]
    private LayerMask layerMask;

    private GameObject rightPointer,
        leftPointer,
        centerEyeAnchor,
        rightEyeAnchor,
        leftEyeAnchor,
        rightHandActiveController,
        leftHandActiveController,
        environmentController;

    private RenderTexture leftEyeCubemap,
        rightEyeCubemap,
        equirect;

    

    private RaycastState lastInvokeR = RaycastState.unhover,
        lastInvokeL = RaycastState.unhover;

    private PointableCanvasModule pointableModule;
    private InteractableUnityEventWrapper tempWrapperPath = null;
    private RaycastHit hit;

    public FadeController fadeController { get; private set; }
    public FadeController rightFadeController { get; private set; }
    public FadeController leftFadeController { get; private set; }

    private enum RaycastState
    {
        hover,
        unhover
    }

    private enum ActiveState
    {
        nonFunctional,
        partFunctional,
        fullFunctional
    }

    private ActiveState controllerState;

    #endregion
    #region Primary Runtime
    private void Awake()
    {
        // Now deprecated method of changing skybox layout to cubemap, HDRP has a different method of interacting with the skybox which I still need to reimplement at scale
        // RenderSettings.skybox.SetFloat("_Layout", 2);
    }

    private void Start()
    {
        /// Latching onto instances of objects in the scene, especially necessary with scene changes
        /// This is predominately called in the PrimaryMenuController since that controls scene-changes and re-finding instances of objects that change IDs between scenes, such as the VR Rig
        refreshObjects();

        GameObject.Find("Menus and Persistent Objects").transform.Find("Menus").rotation = centerEyeAnchor.transform.rotation;
        environmentController = gameObject.transform.Find("Primary Menu").gameObject;

        // Basic method of setting pointer colors using method below
        setColor(rightPointer, idlePointerColor);
        setColor(leftPointer, idlePointerColor);

        // Deprecated method of creating blur effect that used Built-In Render Pipeline, system needs to be mildy reconfigured to work with HDRP
        //blurInit();
        //equirect = new RenderTexture(1024, 1024, 32, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, -1);
        //wipeSkybox();
        
        //setToController(); // Auto-setting to controller since we're currently on tethered platform that doesn't support hand-tracking
        controllerState = ActiveState.fullFunctional; // Setting state variable to 'fullFunctional' by default
    }

    private void Update()
    {
        /// Following functions are for testing purposes
        // Triggers functionality of top button which brings you to next scene, helpful when starting from preparation or menu scene and want to test scene changes
        if (Input.GetMouseButtonDown(1))
        {
            gameObject.transform.Find("Primary Menu").GetComponent<PrimaryMenuController>().GetUpperButton().GetComponent<InteractableUnityEventWrapper>()._whenUnselect.Invoke();
        }
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(startButtonRoutine());
        }

        if ((int)controllerState >= (int)ActiveState.partFunctional) { // If active state is at least allowing button press
            if (controllerState == ActiveState.fullFunctional) { // Active state must strictly be in 'fullFunctional' if raycasts are going to be registered
                lastInvokeR = PointerCast(rightPointer, OVRInput.RawButton.RIndexTrigger, OVRInput.RawButton.A, lastInvokeR);
                lastInvokeL = PointerCast(leftPointer, OVRInput.RawButton.LIndexTrigger, OVRInput.RawButton.X, lastInvokeL);
            }

            if (OVRInput.GetDown(OVRInput.RawButton.Start))
            {
                StartCoroutine(startButtonRoutine());
            }
        }
    }
    #endregion
    #region Button Functions
    private IEnumerator startButtonRoutine()
    {
        yield return new WaitUntil(() => OVRInput.GetUp(OVRInput.RawButton.Start)); // Waiting until button GetUp is detected
        var menu = gameObject.transform.Find("Primary Menu").transform.Find("Inner Active Container").gameObject; // Using this instead of disabling 'Primary Menu' directly because 'Primary Menu' has scripts attached that are crucial for operation of everything
        EnableControllers_Functional();
        if (menu.activeSelf) // If the menu is currently open
        {
            switch (SceneManager.GetActiveScene().name)
            {
                case ("Menu"):
                    //GameObject.Find("Surrounding Elements").transform.Find("Inner Active Container").gameObject.SetActive(true); // Reactivating all gameobjects in the scene that were hidden when blur was in effect
                    break;
                case ("360Video"):
                    environmentController.GetComponent<PrimaryMenuController>().SetSkyboxVideo();
                    GameObject.Find("Video Controller").GetComponent<VideoManager>().ResumeVideo();
                    break;
            }
        }
        else
        {
            //RenderSettings.skybox.SetColor("_Tint", new Color(128 / 255f, 128 / 255f, 128 / 255f));
            //  processBlur();
            switch (SceneManager.GetActiveScene().name)
            {
                case ("Menu"):
                    //GameObject.Find("Surrounding Elements").transform.Find("Inner Active Container").gameObject.SetActive(false); // Deactivating all gameobjects in the scene so you don't have to worry about objects interfering with blur skybox
                    break;

                case ("360Video"):
                    var videoControllerObject = GameObject.Find("Video Controller");
                    videoControllerObject.GetComponent<VideoManager>().PauseVideo();
                    //RenderSettings.skybox.SetFloat("_Layout", 2);
                    // Need to re-add 
                    break;
            }
        }
        menu.SetActive(!menu.activeSelf); // Toggles menu active state
        yield return new WaitForEndOfFrame();
    }

    // Responsible for handling select button event (which can be triggered by several different buttons, hence the button parameter)
    private IEnumerator selectButtonRoutine(OVRInput.RawButton button, GameObject pointer)
    {
        setColor(pointer, selectPointerColor); // Setting color of pointer to 'Select Pointer' color
        string tempWrapperObject = tempWrapperPath.gameObject.name; // Latching onto tempWrapperPath
        tempWrapperPath._whenSelect.Invoke(); // Invoking 'whenSelect' event on whichever button was pressed
        yield return new WaitUntil(() => OVRInput.GetUp(button)); // Waiting until GetUp on button
        if (tempWrapperPath != null) // Verifying that button is still being hovered over
        {
            setColor(pointer, hoverPointerColor);
            tempWrapperPath._whenUnselect.Invoke();
        }
        else // If the button is no longer being hovered over after GetUp
        {
            setColor(pointer, idlePointerColor); // Resetting to 'Idle Pointer' color if not hovering over selectable
        }
    }

    private IEnumerator thumbPadRightClickRoutine()
    {
        yield return new WaitUntil(() => OVRInput.GetUp(OVRInput.RawButton.RThumbstick));
        centerEyeAnchor.GetComponent<Camera>().fieldOfView = 90;
    }
    #endregion
    #region Supporting Methods
    // Still allows button input, useful in 360 Video scene when user needs the option to bring up menu
    public void DisableControllers_Visual() 
    {
        controllerState = ActiveState.partFunctional;

        rightHandActiveController.transform.Find("RightPointer").GetComponent<MeshRenderer>().enabled = false;
        rightHandActiveController.transform.Find("RightControllerAnchor").Find("OVRControllerPrefab").Find("OculusTouchForQuestAndRiftS_Right").Find("r_controller_ply").GetComponent<SkinnedMeshRenderer>().enabled = false;

        leftHandActiveController.transform.Find("LeftPointer").GetComponent<MeshRenderer>().enabled = false;
        leftHandActiveController.transform.Find("LeftControllerAnchor").Find("OVRControllerPrefab").Find("OculusTouchForQuestAndRiftS_Left").Find("l_controller_ply").GetComponent<SkinnedMeshRenderer>().enabled = false;
    }

    // Supplemenetary enable method for controller visual
    public void EnableControllers_Visual() 
    {
        controllerState = ActiveState.partFunctional;

        rightHandActiveController.transform.Find("RightPointer").GetComponent<MeshRenderer>().enabled = true;
        rightHandActiveController.transform.Find("RightControllerAnchor").Find("OVRControllerPrefab").Find("OculusTouchForQuestAndRiftS_Right").Find("r_controller_ply").GetComponent<SkinnedMeshRenderer>().enabled = true;

        leftHandActiveController.transform.Find("LeftPointer").GetComponent<MeshRenderer>().enabled = true;
        leftHandActiveController.transform.Find("LeftControllerAnchor").Find("OVRControllerPrefab").Find("OculusTouchForQuestAndRiftS_Left").Find("l_controller_ply").GetComponent<SkinnedMeshRenderer>().enabled = true;
    }

    // Useful for strictly narrative or scene changes, for example, where user input during action can affect expected behavior
    public void DisableControllers_Functional() 
    {
        controllerState = ActiveState.nonFunctional;

        rightHandActiveController.SetActive(false);
        leftHandActiveController.SetActive(false);
    }

    // Supplementary enable method for controller functional capability
    public void EnableControllers_Functional() 
    {
        controllerState = ActiveState.fullFunctional;

        rightHandActiveController.SetActive(true);
        leftHandActiveController.SetActive(true);
    }
    
    //Requires GameObject with camera component at object path in var cameraGameObject
    private void processBlur()
    {
        /*
        var cameraGameObject = GameObject.Find("Blur Capture Camera");
        var cameraComponent = cameraGameObject.GetComponent<Camera>();

        cameraGameObject.transform.position = centerEyeAnchor.transform.position;
        cameraGameObject.GetComponent<Camera>().stereoSeparation = 0.064f;

        cameraComponent.enabled = true;
        cameraComponent.RenderToCubemap(leftEyeCubemap, 63, Camera.MonoOrStereoscopicEye.Left);
        cameraComponent.RenderToCubemap(rightEyeCubemap, 63, Camera.MonoOrStereoscopicEye.Right);

        leftEyeCubemap.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Left);
        rightEyeCubemap.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Right);
        cameraComponent.enabled = false;

        //RenderSettings.skybox.SetFloat("_Rotation", 90.5f);
        
        RenderSettings.skybox.mainTexture = equirect;
        if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Menu"))
        {
            GameObject.Find("Surrounding Elements").transform.Find("Inner Active Container").gameObject.SetActive(false);
        }
        */

        var cameraGameObject = GameObject.Find("Menus and Persistent Objects").transform.Find("Blur Capture Camera");
        var cameraComponent = cameraGameObject.GetComponent<Camera>();

        cameraGameObject.transform.position = centerEyeAnchor.transform.position;
        cameraGameObject.GetComponent<Camera>().stereoSeparation = 0.064f;

        cameraComponent.enabled = true;
        cameraComponent.RenderToCubemap(leftEyeCubemap, 63, Camera.MonoOrStereoscopicEye.Left);
        cameraComponent.RenderToCubemap(rightEyeCubemap, 63, Camera.MonoOrStereoscopicEye.Right);

        leftEyeCubemap.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Left);
        rightEyeCubemap.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Right);
        cameraComponent.enabled = false;

        //RenderSettings.skybox.SetFloat("_Rotation", 90.5f);
        
        //RenderSettings.skybox.mainTexture = equirect;
        environmentController.GetComponent<PrimaryMenuController>().SetSkyboxCustom(equirect);
        /*
        if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Menu"))
        {
            GameObject.Find("Surrounding Elements").transform.Find("Inner Active Container").gameObject.SetActive(false);
        }
        */
    }

    private void blurInit()
    {
        leftEyeCubemap = new RenderTexture(1024, 1024, 16);
        rightEyeCubemap = new RenderTexture(1024, 1024, 16);
        leftEyeCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        rightEyeCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
    }

    // Handles raycast detection for interactable panels and other ray-oriented objects in the scene. Could use improvement with event-based management system but I don't think OVR has any native implementation of this 
    private RaycastState PointerCast(GameObject pointer, OVRInput.RawButton controllerSelect, OVRInput.RawButton hybridSelect, RaycastState state)
    {
        // Configuration for raycast with optional layermask for mitigation of gameobjects unexpectedly blocking cast
        if (Physics.Raycast(pointer.transform.position, pointer.transform.up, out hit, layerMask))
        {
            if (hit.transform.GetComponent<InteractableUnityEventWrapper>() != null) // If the raycast is hitting an object not filtered by the layer mask
            {  
                if (hit.transform.GetComponent<InteractableUnityEventWrapper>() != tempWrapperPath) // If the raycast is now pointing at a new gameobject
                {
                    
                    if (tempWrapperPath != null) tempWrapperPath._whenUnhover.Invoke(); // Clears highlight of previously selected button
                    state = RaycastState.hover;
                }
                tempWrapperPath = hit.transform.GetComponent<InteractableUnityEventWrapper>(); // Sets active button to newly pointed at button

                // Switches state over the hover only once when the raycast is initially hovering over the button
                if (state != RaycastState.unhover)
                {
                    state = RaycastState.hover;
                    tempWrapperPath._whenHover.Invoke();
                    setColor(pointer, hoverPointerColor);
                }
            }
        }
        else
        {
            if (state != RaycastState.unhover) // If state is not already unhover
            {
                state = RaycastState.unhover;
                if (tempWrapperPath != null)
                {
                    tempWrapperPath._whenUnhover.Invoke(); // Switching off button graphics if 'tempWrapperPath' (component of button) evaluates to not null
                    tempWrapperPath = null; // Resetting 'tempWrapperPath' to null so buttons can be properly interacted with
                }
                setColor(pointer, idlePointerColor); // Setting pointer color to idle to signify that nothing is selected or hovered over
            }
        }
        if (state == RaycastState.hover) // If player is currently hovering over selectable
        {
            if (tempWrapperPath != null) // If this selectable has component 'InteractableUnityEventWrapper'
            {
                if (OVRPlugin.GetHandTrackingEnabled())
                {
                    selectBehaviorHigh(hybridSelect, null, state); // Hybrid select button (A or X) is what pinching evaluates to in OVR
                }
                else
                {
                    // Beginning coroutine for both options of selecting button
                    selectBehaviorHigh(hybridSelect, pointer, state);
                    selectBehaviorHigh(controllerSelect, pointer, state);
                }
            }
        }
        return state;
    }

    private void selectBehaviorHigh(OVRInput.RawButton button, GameObject pointer, RaycastState state)
    {
        if (OVRInput.GetUp(button))
        {
            StartCoroutine(selectButtonRoutine(button, pointer));
        }
    }

    /// Hands are auto-enabled if active input type, therefore controllers are the only thing that need to be manually hidden
    /// Will likely need to be tweaked if project implements hand-functionality again since this event can happen at random times, resulting in user getting ability to perform input when they aren't supposed to
    private void setToHand() { DisableControllers_Functional(); }
    private void setToController() { EnableControllers_Functional(); }

    // Simple method to change color of pointers
    private void setColor(GameObject pointer, Color setColor) { pointer.GetComponent<Renderer>().material.color = setColor; }

    /*
    public void wipeSkybox()
    {
        RenderSettings.skybox.mainTexture = new RenderTexture(1024, 1024, 32, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, -1);
        processBlur();
        RenderSettings.skybox.SetColor("_Tint", new Color(128 / 255f, 128 / 255f, 128 / 255f));
    }
    */

    public void refreshObjects()
    {
        var tempPath = GameObject.Find("VR Rig").transform.Find("OVRCameraRig").Find("TrackingSpace");
        centerEyeAnchor = tempPath.Find("CenterEyeAnchor").gameObject;
        rightEyeAnchor = tempPath.Find("RightEyeAnchor").gameObject;
        leftEyeAnchor = tempPath.Find("LeftEyeAnchor").gameObject;

        fadeController = centerEyeAnchor.GetComponent<FadeController>();
        rightFadeController = rightEyeAnchor.GetComponent<FadeController>();
        leftFadeController = leftEyeAnchor.GetComponent<FadeController>();

        rightHandActiveController = tempPath.Find("RightHandAnchor").gameObject;
        leftHandActiveController = tempPath.Find("LeftHandAnchor").gameObject;
        rightPointer = rightHandActiveController.transform.Find("RightPointer").gameObject;
        leftPointer = leftHandActiveController.transform.Find("LeftPointer").gameObject;
        pointableModule = gameObject.transform.Find("Pointable Manager").GetComponent<PointableCanvasModule>();
    }
    #endregion
    #region Accessors
    public FadeController GetFadeController()
    {
        return fadeController;
    }
    #endregion
}