/// Written by Ethan Woods -> 016erw@gmail.com
/// The PrimaryMenuController class is reponsible for a lot of the high-level persistent control between scenes. It's reponsible for facilitating the re-orientation of all objects and references between scenes as objectID's
/// need to be re-ascertained when DDOL objects are transfered between scenes. As the name suggests, it also handles the fucntions of the primary menu that the player interacts with, however a decent amount of the code
/// associated with this is subject to change since Kacie is making a new UI for the menu scene

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Oculus.Interaction;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using TMPro;

public class PrimaryMenuController : MonoBehaviour
{
    #region Variables
    [SerializeField]
    private TextMeshPro upperButtonText,
        lowerButtonText;

    private Transform upperButton,
        lowerButton,
        panelCanvas;

    private string menuSceneName = "Menu",
        videoSceneName = "360Video",
        environmentSceneName = "Environment";

    private InputManager inputManager; // Latches onto instance of InputManager component on 'Menu' object in DDOL

    private HDRISky hdriSky; // Used as variable for 'out' when messing with the DDOL skybox volume 
    #endregion
    #region Primary Runtime
    private void Awake()
    {
        inputManager = gameObject.transform.parent.GetComponent<InputManager>(); // Locating InputManager custom class (in parent of gameobject) for later use

        // Locating persistent Video Player Buffer gameobject and making it a child of persistent object controller
        try
        {
            GameObject.Find("Video Player Buffer").transform.parent = GameObject.Find("Menus and Persistent Objects").transform;
        }
        catch
        {
            Debug.LogWarning("No persistent buffer detected");
        }

        // Locating transforms
        upperButton = transform.Find("Inner Active Container").Find("Buttons").Find("Upper Button");
        lowerButton = transform.Find("Inner Active Container").Find("Buttons").Find("Lower Button");
        panelCanvas = transform.Find("Inner Active Container").Find("Canvas");
    }

    private void Start()
    {
        // Adjusting unselect behavior of buttons
        upperButtonChange("Begin the Narrative Experience", "360Video");
        lowerButtonChange("Settings", true);

        // Attaches adjustLowerBehavior listener to bottom button to pair with OVRRaycast events triggered by hands or pointers
        lowerButton.GetComponent<InteractableUnityEventWrapper>()._whenUnselect.AddListener(() => adjustLowerBehavior());

        // Creating listener for scene change to ensure that all objects are properly handled
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    // Manages the control of persistent menus between scenes
    void OnSceneChanged(Scene previousScene, Scene newScene)
    {
        inputManager.refreshObjects(); // Reacquiring non-persisent objects
        inputManager.EnableControllers_Functional(); // Re-allowing user input
        if (newScene != SceneManager.GetSceneByName(menuSceneName))
        {
            lowerButtonChange("Return to Menu", false); // Setting lower button to 'Return to Menu' if in any scene other than the Menu
            transform.Find("Inner Active Container").gameObject.SetActive(false); // Instantly hides menu if active
            
            if (newScene == SceneManager.GetSceneByName(videoSceneName))
            {
                inputManager.DisableControllers_Visual(); // Disabling controllers since they're unecessary for when video is playing
                //StartCoroutine(delayFade()); // Delaying fade until 360 video is playing to create a better transition
            }
            //else inputManager.GetFadeController().FadeIn();
        }
        else lowerButtonChange("Settings", true); // Setting lower button to 'Settings' if specifically in the menu scene
    }
    #endregion
    #region Supporting Coroutines
    // High-level controller for managing scene change behavior
    private IEnumerator sceneChanger(string scene)
    {
        inputManager.DisableControllers_Functional(); // Disabling user interaction while scene change is in effect
        //inputManager.GetFadeController().FadeOut();
        
        // Yielding until screen has completely faded to black and then switching to new scene
        //yield return new WaitForSecondsRealtime(inputManager.GetFadeController().fadeTime); 
        yield return new WaitForEndOfFrame();
        StartCoroutine(loadScene(scene));
    }

    // Lower-level scene change code
    public static IEnumerator loadScene(string sceneName)
    {
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene(sceneName);
            yield return new WaitUntil(() => SceneManager.GetSceneByName(sceneName).isLoaded); // Yielding until next scene is completely loaded
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName)); // Performing switch to new scene
        } else Debug.LogWarning("Loading a scene from itself is likely not intended");
    }

    // Giving time for videos to begin before player is able to see everything
    private IEnumerator delayFade()
    {
        yield return new WaitUntil(() => GameObject.Find("Video Controller").GetComponent<VideoManager>().GetPathObject360().GetComponent<VideoPlayer>().isPlaying); // Yielding until video is playing
        yield return new WaitForSeconds(1); // Additional yield since it just feels better if you can hear the audio before you can see anything
        inputManager.GetFadeController().FadeIn();
    }
    #endregion
    #region Supporting Methods
    /// Responsible for managing the behavior of the upper button depending on what scene is selected
    /// This behavior will need to be altered once Kacie finishes up her UI since the scene-scroller that is currently implemented isn't really that good
    public void adjustUpperBehavior()
    {
        upperButton.GetComponent<InteractableUnityEventWrapper>()._whenUnselect.RemoveAllListeners(); // Wiping functionality when the button is selected to prevent bugs
        
        // Need to re-implement imageParent variable in PointableCanvasModule but not really since UI is going to be changing
        /*
        switch (transform.parent.Find("Pointable Manager").GetComponent<PointableCanvasModule>().imageParent.name)
        {
            case "Narrative Panel":
                upperButtonChange("Begin the Narrative Experience", "Environment");
                break;
            case "Primer Lab Panel":
                upperButtonChange("Hop Right Into the Lab", "CameraPose");
                break;
            case "Observational Environment Panel":
                upperButtonChange("Look Around the Environment", "CameraPose");
                break;
            case "Interactive Lab Scene Options Panel":
                upperButtonChange("Learn about the Different Proxies", "CameraPose");
                break;
            case "Interactive Environment Panel":
                upperButtonChange("Interact with the Environment", "CameraPose");
                break;
            case "Interactive Lab Scene Proxies Panel":
                upperButtonChange("Learn More about the Proxies Collected", "CameraPose");
                break;
        }
        */
    }

    // Responsible for managing the behavior of the lower button depending on what scene the user is currently in and what menu they are in
    public void adjustLowerBehavior()
    {
        // Text-based switch case that evaluates what to do based on current text attached to the lower button
        switch (lowerButtonText.text)
        {
            case "Settings":
                lowerButtonChange("Back", false);
                // And then show all settings buttons, like accessibility settings etc.
                break;

            // 'Back' is text when user is in settings menu, therefore returning to scene menu and re-attaching 'Settings' text to button
            case "Back":
                lowerButtonChange("Settings", true);
                break;

            // When the user is mid-scene and wants to return to the main menu
            case "Return to Menu":
                StartCoroutine(sceneChanger("Menu"));

                /// In case the user is in the '360Video' scene, all components must be preserved so the user can go back into the scene and start where they left off
                /// Needs additional implementation on the side of loading into the '360Video' scene that checks what the current video is, and adjusts everything accordingly
                if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName(videoSceneName))
                {
                    GameObject.Find("Video Controller").GetComponent<VideoManager>().PauseVideo();
                }
                //inputManager.wipeSkybox(); // 
                lowerButtonChange("Settings", true);
                break;
        }
    }

    // Changing behavior of upper button in accordance with the scene and situation
    private void upperButtonChange(string buttonText, string scene)
    {
        upperButtonText.text = buttonText;
        upperButton.GetComponent<InteractableUnityEventWrapper>()._whenUnselect.AddListener(() => StartCoroutine(sceneChanger(scene)));
    }

    // Changing behavior of lower button in accordance with the scene and situation
    private void lowerButtonChange(string buttonText, bool defaultMenuState)
    {
        lowerButtonText.text = buttonText;
        upperButton.gameObject.SetActive(defaultMenuState);
        panelCanvas.Find("Scroll View").gameObject.SetActive(defaultMenuState);
        if (SceneManager.GetActiveScene().name != "Menu")
        {
            panelCanvas.Find("Next Scene Graphic").gameObject.SetActive(!defaultMenuState);
        }
    }

    // Public method for setting skybox render texture, used to toggle between blur and video textures
    public void SetSkyboxVideo() 
    {
        // Changing render texture of skybox to render texture attached to VideoPlayer component
        if (GameObject.Find("Menus and Persistent Objects").transform.Find("Skybox Volume").GetComponent<Volume>().profile.TryGet(out hdriSky)) {
            ((CustomRenderTexture)hdriSky.hdriSky).material.SetTexture("_VideoTexture", GameObject.Find("Video Controller").GetComponent<VideoManager>().GetPlayer360().targetTexture); // Grabbing instance of render texture from videoplayer object for the specific video trying to be played
        }
    }

    // Public method for setting skybox to custom render texture, useful for blur effect, etc.
    public void SetSkyboxCustom(RenderTexture customTexture) 
    {
        // Changing render texture of skybox to render texture passed into method
        if (GameObject.Find("Menus and Persistent Objects").transform.Find("Skybox Volume").GetComponent<Volume>().profile.TryGet(out hdriSky)) {
            ((CustomRenderTexture)hdriSky.hdriSky).material.SetTexture("_VideoTexture", customTexture); // Grabbing instance of render texture from videoplayer object for the specific video trying to be played
        }
    }
    #endregion
    #region Accessors
    public GameObject GetUpperButton()
    {
        return upperButton.gameObject;
    }

    public GameObject GetLowerButton()
    {
        return lowerButton.gameObject;
    }
    #endregion
}