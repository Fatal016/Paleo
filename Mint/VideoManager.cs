/// Written by Ethan Woods -> 016erw@gmail.com
/// The VideoManager class is responsible for facilitating all video playback in a designated scene. 
/// The VideoManager class is entirely dependent on the presence of a VideoBufferManager component being present in the scene as that is where it's control structure is drawn from. Therefore, be wary of changing structure as changes in the VideoBufferManager class may also be necessary.

using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class VideoManager : MonoBehaviour
{
    #region Variables 
    private GameObject displayObject, // Will generally be set to CenterEyeAnchor since this is where the VideoPresenter component (which displays 2D video) is attached to 
        sceneObject, // Stores GameObject of current scene (child of VideoPlayerBuffer) that the current instance of the scene correlates to. Refer to VideoPlayerBuffer class for more information
        pathObject360, // Attaches to GameObject of current 360 video clip
        pathObject2D, // Attached to GameObject of current 2D video clip
        screenObject,
        environmentController; // 2D video screen object

    private VideoPlayer player360, // Attaches to VideoPlayer of current 360 video clip
        player2D; // Attaches to VideoPlayer of current 2D video clip

    private VideoClass360 pathClass360; // Set to the path to current 360 object in VideoRepo class
    private VideoClass2D pathClass2D; // Set to the path to current 2D object in VideoRepo class
    private VideoBufferManager bufferManager; // Attaches to local DDOL instance of VideoBufferManager class

    

    private UnityEvent ProperVideo = new UnityEvent();

    #endregion
    #region Primary Runtime
    private void Awake()
    {  
        // Latching onto all needed gameobjects so they don't have to be reflectively found later on (expensive)
        displayObject = GameObject.Find("VR Rig").transform.Find("OVRCameraRig").Find("TrackingSpace").Find("CenterEyeAnchor").gameObject;
        screenObject = displayObject.transform.Find("2D Screen").gameObject;
        bufferManager = GameObject.Find("Menus and Persistent Objects").transform.Find("Video Player Buffer").GetComponent<VideoBufferManager>();
        environmentController = GameObject.Find("Menus and Persistent Objects").transform.Find("Menus").Find("Primary Menu").gameObject;
        sceneObject = bufferManager.transform.GetChild(bufferManager.SceneIndex).gameObject;

        updatePaths(); // Updating paths to physical and class objects based on designations just performed
    }

    private void BeginPlayback() 
    {
        ProperVideo.AddListener(() => StartCoroutine(Video2D())); // Adding listener to UnityEvent handler 'ProperVideo' which is triggered when 360 video that next 2D video is supposed to play over is set to active

        play360Video(); // Autoplays first 360 video because this is only intended behavior of scene
        player360.loopPointReached += Video360Handler; // Adding event handler for when video ends, description of this method is at 'Video360Handler' method below
    }

    private void Start()
    {
        BeginPlayback();
    }
    #endregion
    #region Video Control Methods
    // Handles the playing of 360 video by preparing components and setting class variables
    public void play360Video()
    {
        if (pathClass360.VideoClip.name == pathClass2D.OverlayVideoName) ProperVideo.Invoke(); // Invoking 'ProperVideo' event handler, causing 2D video handler to check whether or not there are 2D videos to monitor 360 video frame for

        // Changing render texture of skybox to render texture attached to VideoPlayer component
        environmentController.GetComponent<PrimaryMenuController>().SetSkyboxVideo();
        
        pathObject360.GetComponent<VideoPlayer>().frame = pathClass360.LastFrame; // Setting to frame, important if video has been paused or completely stopped because of scene change by user
        pathObject360.GetComponent<VideoPlayer>().Play();

        // Handles the playing of optional separate audio source, may need slight alteration to sync up with video
        if (pathObject360.GetComponent<AudioSource>() != null)
        {
            pathObject360.GetComponent<AudioSource>().time = pathClass360.Timestamp; // Same as setting frame for video, important if video has been paused or stopped
            pathObject360.GetComponent<AudioSource>().Play();
        }
    }

    // Handles the playing of 2D video by preparing components and setting class variables
    public void play2DVideo()
    {
        player2D.frame = pathClass2D.LastFrame; // Important if video was paused or stopped
        player2D.Play();
        StartCoroutine(Fade2D(0, 1, 2));

        // Handles the playing of optional separate audio source, like 360 video, may need slight alteration to sync up with video
        if (pathObject2D.GetComponent<AudioSource>() != null)
        {
            pathObject2D.GetComponent<AudioSource>().time = pathClass2D.Timestamp; // In case of pausing or stopping
            pathObject2D.GetComponent<AudioSource>().Play();
        }
    }

    /// Simultaneously resumes both videos from pause
    /// This method is triggered when user exits pause menu
    public void ResumeVideo()
    {
        BeginPlayback(); // Re-initializing all listeners and events, state is automatically restored for 360 video
        if (pathClass2D.LastFrame != -1) play2DVideo(); // If 2D was actively playing, reinstantiate
    }

    /// Simultaneously pauses both videos
    /// This method is triggered when the user enters the pause menu
    /// Could implement a generic method with an interface to clean this up a tiny bit (with handling of frames and audio source) but really not worth the time unless you really want to
    public void PauseVideo()
    {
        // Handling pausing of 360 video and optional audio source
        pathClass360.LastFrame = pathObject360.GetComponent<VideoPlayer>().frame; // Storing last frame in Repo class for later use in resuming video
        player360.Pause();
        if (player360.GetComponent<AudioSource>() != null) {
            pathClass360.Timestamp = pathObject360.GetComponent<AudioSource>().time;
            pathObject360.GetComponent<AudioSource>().Pause();
        }

        // Handling pausing of 2D video and optional audio source
        if (pathClass2D.LastFrame != -1) {
            screenObject.GetComponent<MeshRenderer>().enabled = false;
            pathClass2D.LastFrame = pathObject2D.GetComponent<VideoPlayer>().frame;
            player2D.Pause();
            if (player2D.GetComponent<AudioSource>() != null) {
                pathClass2D.Timestamp = pathObject2D.GetComponent<AudioSource>().time;
                pathObject2D.GetComponent<AudioSource>().Pause();
            }
        }
    }

    /// Simultaneously stops both videos
    /// Same as PauseVideo method, could implement a generic with the same interface but I have to focus on other things at the moment
    public void StopVideo()
    {
        pathClass360.LastFrame = pathObject360.GetComponent<VideoPlayer>().frame;
        pathObject360.GetComponent<VideoPlayer>().Stop();
        if (player360.GetComponent<AudioSource>() != null) {
            pathClass360.Timestamp = pathObject360.GetComponent<AudioSource>().time;
            pathObject360.GetComponent<AudioSource>().Stop();
        }

        pathClass2D.LastFrame = pathObject360.GetComponent<VideoPlayer>().frame;
        pathObject2D.GetComponent<VideoPlayer>().Stop();
        if (player360.GetComponent<AudioSource>() != null) {
            pathClass2D.Timestamp = pathObject360.GetComponent<AudioSource>().time;
            pathObject2D.GetComponent<AudioSource>().Stop();
        }
    }
    #endregion
    #region Supporting Video Methods
    /// Iterates over each videoplayer gameobject until all scene videos have been played, terminates its own calling if no other videos need playing
    private void Video2DHandler(VideoPlayer source) 
    {
        pathClass2D.LastFrame = -1; // Specifying that video has finished playing
        StartCoroutine(Fade2D(1, 0, 2)); // Fading out 2D screen
        if (pathObject2D.transform.GetSiblingIndex() < pathObject2D.transform.parent.childCount - 1) {
            source.gameObject.SetActive(false); // Disabling videoplayer object, specifically not destroying just in case it needs to be played again
            bufferManager.VideoIndex2D++; // Iterating 2D video index if another video has yet to be played in scene
            updatePaths();
            if (pathClass2D.OverlayVideoName == pathObject360.name) StartCoroutine(Video2D()); // Continuing execution of frame watcher if desired scene for next 2D video is already playing
        }
        else ProperVideo.RemoveAllListeners(); // Stops 2D video listener if there are no more videos to be played      
    }

    /// Iterating over each videoplayer gameobject until all scene videos have been played, then loads environment scene
    /// This scene loading functionality may change depending on what's needed, could be improved to be more dynamic but considering we're only going to the 360Video scene once, not much point as of now
    private void Video360Handler(VideoPlayer source)
    {
        if (pathObject360.transform.GetSiblingIndex() < pathObject360.transform.parent.childCount) { // Checking whether or not it is the last video in the scene
            source.gameObject.SetActive(false); // Disabling videoplayer object, specifically not destroying just in case it needs to be played again
            if (pathClass360.FollowingPanel != null) pathClass360.FollowingPanel.SetActive(true); // If there is a following panel to the clip, display it
            else
            {
                bufferManager.VideoIndex360++; // Iterating to next 360 video to be played in the scene
                updatePaths();
                play360Video(); // Playing next video since they are all back-to-back as of now
            }
        } else PrimaryMenuController.loadScene("Environment"); // If all videos have been played, load the next scene
    }

    // Handles the management of playing 2D videos, additional coroutine over 360 video is necessary for minimum overhead caused by constantly checking 360 videoframe
    private IEnumerator Video2D()
    {
        displayObject.transform.Find("2D Screen").GetComponent<MeshRenderer>().enabled = false; // Hiding screen if no 2D video is playing

        /// As long as 2D video isn't already playing, checks once every second to see if appearance frame has been reached
        /// Additionally checks whether or not video has already been played by checking against -1
        while (!player2D.isPlaying) {
            if (player360.frame >= pathClass2D.AppearanceFrame && pathClass2D.AppearanceFrame != -1) play2DVideo();
            yield return new WaitForSeconds(1);
        }
        player2D.loopPointReached += Video2DHandler; // Attaching event handler that manages repo class when video has finished
    }

    // Handles fading in and fading out 2D video
    private IEnumerator Fade2D(float startAlpha, float endAlpha, float fadeTime)
    {
        MeshRenderer screenMat = displayObject.transform.Find("2D Screen").GetComponent<MeshRenderer>(); // Latching onto 2D Screen mesh renderer
        screenMat.material.SetTexture("_RenderTexture", player2D.targetTexture); // Grabbing instance of render texture from videoplayer object for the specific video trying to be played
        screenMat.material.SetFloat("_Alpha", startAlpha); // Ensuring that shader starts at corrent Alpha
        screenMat.enabled = true; // Enabling screen if not already, redundant if fading out, but necessary if fading in since screen is hidden when video isn't played

        float elapsedTime = 0.0f; // Time-keeping variable for Lerp
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            screenMat.material.SetFloat("_Alpha", Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsedTime / fadeTime))); // Lerping Alpha from start to end
            yield return new WaitForEndOfFrame();
        }
        screenMat.material.SetFloat("_Alpha", endAlpha);
        screenMat.enabled = screenMat.material.GetFloat("_Alpha") == 0 ? false : true; // Hiding mesh renderer if just faded out to prevent visual bugs 
    }
    #endregion
    #region Supporting General Methods
    // Updates paths of all relatively volatile paths in the scene, these being on both the object and class level
    private void updatePaths()
    {
        pathObject360 = sceneObject.transform.Find(bufferManager.FolderTitle360).GetChild(bufferManager.VideoIndex360).gameObject;
        pathClass360 = bufferManager.VideoRepo[bufferManager.SceneIndex].videoList360[bufferManager.VideoIndex360];
        player360 = pathObject360.GetComponent<VideoPlayer>();
        if (bufferManager.VideoRepo[bufferManager.SceneIndex].videoList2D[bufferManager.VideoIndex2D] != null)
        {
            pathObject2D = sceneObject.transform.Find(bufferManager.FolderTitle2D).GetChild(bufferManager.VideoIndex2D).gameObject;
            pathClass2D = bufferManager.VideoRepo[bufferManager.SceneIndex].videoList2D[bufferManager.VideoIndex2D];
            player2D = pathObject2D.GetComponent<VideoPlayer>();
        }
    }

    
    
    // Alters skybox layout based on layout of 360 video, needs re-implementing with HDRP skybox system, custom shader property will likely have to added
    /*
    public void setSkybox()
    {
        switch (pathClass360.ImageType)
        {
            case ("None"):
                RenderSettings.skybox.SetFloat("_Layout", 0);
                break;
            case ("Side by Side"):
                RenderSettings.skybox.SetFloat("_Layout", 1);
                break;
            case ("Over Under"):
                RenderSettings.skybox.SetFloat("_Layout", 2);
                break;
        }
    }
    */
    #endregion
    #region Accessors
    public GameObject GetPathObject360() { return pathObject360; } // Strictly used for properly delaying fade effect
    public VideoPlayer GetPlayer360() { return player360; }
    #endregion
}