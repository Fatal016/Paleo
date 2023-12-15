/// Written by Ethan Woods -> 016erw@gmail.com
/// The VideoBufferManager class is responsible for the pre-preparation of video clips and their components to create seamless execution of these videos later on without delay.

/// How it Works:
/// On the inspector side, the user is presented with a 'Video Repo' which is an array of 'MasterVideoClass' objects. A 'MasterVideoClass' object contains arrays of 'VideoClass360' and 'VideoClass2D' objects which are what house the actual clips and other necessary components.
/// Serialized inputs are all labeled to indicate whether or not they are required and all elements have tooltips to indicate their purpose.
/// By nature, each element of the 'Video Repo' class represents an instance of the scene in which the videos are played with 'Element 1' being what plays the first time the player enters the scene, 'Element 2' being the second time, etc.
/// Otherwise, the components under each of these elements is pretty self explanatory with each housing a certain type of video

/// Code-wise, a request-oriented retrieval mechanism is used so that the system can be used on all devices, regardless of their filepaths or structure, however additions to detected filestructures may be necessary to facilitate this.

/// Current Issues:
/// For some reason, if there is more than one scene of videos that need processing, RepoIndex runs off of the end of the VideoRepo array, causing an error. As RepoIndex is being incremented in the for loop and nowhere else, it's extremely odd that it's running off.
/// Since I'm calling VideoPreparationRoutine once in Start, it isn't like there's a race condition due to multiple instances running async and both incrementing the variable. I'm still not sure.

/// Improvements to be Made:
/// Custom inspector would be nice, especially when selecting overlay video for 2D videos and having a dropdown of possible videos there, however there may be a smarter way of approaching the whole overlay video concept which would deem this unecessary.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using UnityEngine.Assertions;

public class VideoBufferManager : MonoBehaviour
{
    #region Variables
    [SerializeField]
    private MasterVideoClass[] _videoRepo;
    public MasterVideoClass[] VideoRepo { get { return _videoRepo; } private set { _videoRepo = value; } }

    public string BundleName { get; private set; } = "asset_video"; // File structure can be seen in "Assets/BundleName", must abide by the structure if you want the automation to work
    public string FolderTitle360 { get; private set; } = "360 Videos"; // Name of parent gameobject that will contain all 360 videos under VideoBufferManager
    public string FolderTitle2D { get; private set; } = "2D Videos"; // Name of parent gameobject that will contain all 2D videos under VideoBufferManager
    public string AssetPathHeader { get; private set; } // Platform specific path to video asset folder
    
    public int RepoIndex { get; private set; }
    public int SceneIndex { get; set; }
    public int VideoIndex360 { get; set; }
    public int VideoIndex2D { get; set; }

    private bool Ready360 { get; set; }
    private bool Ready2D { get; set; }
    #endregion
    #region Primary Runtime
    private void Awake()
    {
        /// ApplicationInstallMode
        /// Editor -> Windows testing, this detection will likely need to change since once an SDK is built, the install mode is likely not going to be recognized as Editor anymore
        /// Else -> As of now only used for Android development, but would have to be more specific in final build
        AssetPathHeader = Application.installMode == ApplicationInstallMode.Editor ? $"E:\\Unity Projects\\Final 4H\\Assets\\{BundleName}\\" : $"{Application.persistentDataPath}/{BundleName}/";
    }

    private void Start()
    {
        // Ensuring that all required inputs aren't null
        foreach (MasterVideoClass upper in VideoRepo)
        {
            try
            {
                foreach (VideoClass360 lower360 in upper.videoList360)
                {
                    Assert.IsNotNull(lower360.VideoClip);
                    //Assert.IsNotNull(lower360.ImageType); // Will need to be re-implemented when ImageTypes (layouts) are figured out for HDRP
                }
                foreach (VideoClass2D lower2D in upper.videoList2D)
                {
                    Assert.IsNotNull(lower2D.VideoClip);
                    Assert.IsNotNull(lower2D.OverlayVideoName);
                }
            }
            catch
            {
                Debug.LogError("Value evaluated to null: Ensure that all Video Player Buffer parameters are filled in");
            }
        }
        StartCoroutine(VideoPreparationRoutine());
    }
    #endregion
    #region Supporting Coroutines
    // Primary loop for Video Preparation, iterates through all items inputted by developer and formats gameobjects to contain all the necessary components and values
    private IEnumerator VideoPreparationRoutine()
    {
        yield return new WaitForEndOfFrame();
        for (RepoIndex = 0; RepoIndex < VideoRepo.Length; RepoIndex++) // Iterating over each VideoRepo item (each is treated as an individual scene for multi-scene video distribution)
        {
            tempHandler($"Scene {RepoIndex}", gameObject.transform);

            // Looks complicated but need to use callback to keep track of 'Ready360' and 'Ready2D' booleans because otherwise they don't get updated properly
            createGameObjects(VideoRepo[RepoIndex].videoList360, FolderTitle360, (i) => { Ready360 = i; });
            createGameObjects(VideoRepo[RepoIndex].videoList2D, FolderTitle2D, (i) => { Ready2D = i; });
            yield return new WaitUntil(() => Ready360 && Ready2D); // Waits until all gameobjects, components, and videoclips are prepared
        }
        StartCoroutine(PrimaryMenuController.loadScene("Menu"));
    }

    // Generic function for preparing component, oriented initialization for videos, refer to ClassProps interface for what exactly is being interacted with
    private IEnumerator configureVideos<RepoClass>(RepoClass[] classPath, string folderTitle, Action<bool> callback) where RepoClass : ClassProps
    {   
        for (int localIndex = 0; localIndex < classPath.Length; localIndex++)
        {
            var tempObject = gameObject.transform.GetChild(RepoIndex).Find(folderTitle).GetChild(localIndex);
            var tempDir = classPath[localIndex];
            VideoPlayer tempPlayerPath = tempObject.GetComponent<VideoPlayer>();
            UnityWebRequest www;
            try
            {
                tempPlayerPath.url = $"{AssetPathHeader}Clips/{tempDir.VideoClip.name}.mp4";
                tempPlayerPath.targetTexture = new RenderTexture((int)tempDir.VideoClip.width, (int)tempDir.VideoClip.height, 32, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, -1);
            }
            catch
            {
                Debug.LogError("Issue getting at asset path");
            }
            if (tempDir.AudioClip != null) // Performing a webrequest to retrieve audio from file if clip has separate audio file, otherwise setting mode to 'Direct' so that audio is processed directly from video clip
            {
                tempPlayerPath.audioOutputMode = VideoAudioOutputMode.AudioSource;
                using (www = UnityWebRequestMultimedia.GetAudioClip($"{AssetPathHeader}Audio Clips/{tempDir.AudioClip.name}.wav", AudioType.WAV))
                {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Debug.LogError($"Issue retrieving audio clip: {www.error}");
                    }
                    else
                    {
                        tempObject.GetComponent<AudioSource>().clip = DownloadHandlerAudioClip.GetContent(www);
                    }
                }
            }
            else
            {
                tempPlayerPath.audioOutputMode = VideoAudioOutputMode.Direct;
            }
            tempObject.GetComponent<VideoPlayer>().Prepare();
            yield return new WaitUntil(() => tempObject.GetComponent<VideoPlayer>().isPrepared); // Yielding routine until video is prepared. Ensures that all clips are properly prepared before player is brought in
        }
        callback(true); // Setting passed callback to true (Either 'Ready360' or 'Ready2D')
    }
    #endregion
    #region Supporting Methods
    // Generic method for creating all necessary GameObjects for videoclips and their accompanying components/configurations
    private void createGameObjects<RepoClass>(RepoClass[] classPath, string folderTitle, Action<bool> callback) where RepoClass : ClassProps
    {
        tempHandler(folderTitle, gameObject.transform.GetChild(RepoIndex)); // Building mid-level folder 
        for (int localIndex = 0; localIndex < classPath.Length; localIndex++) // For each element in RepoClass
        {
            GameObject lowestTemp = new GameObject();
            lowestTemp.transform.parent = gameObject.transform.GetChild(RepoIndex).transform.Find(folderTitle); // Making new gameobject a child of "folder" gameobject
            lowestTemp.name = classPath[localIndex].VideoClip.name; // Naming gameobject the same as clip for easy accessing later
            lowestTemp.AddComponent<VideoPlayer>();
            if (classPath[localIndex].AudioClip != null) lowestTemp.AddComponent<AudioSource>(); // If has separate audio clip, attach audio source
            componentInit(lowestTemp.GetComponent<VideoPlayer>());
        }
        StartCoroutine(configureVideos(classPath, folderTitle, (i) => { callback(i); })); // Once gameobjects and their components are built, configure and attach all inputs to necessary components 
    }

    // Init function for passed VideoPlayer component which makes sure that component is set to accept all inputs
    private void componentInit(VideoPlayer player)
    {
        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.isLooping = false;
        player.skipOnDrop = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.source = VideoSource.Url;
        if (player.GetComponent<AudioSource>() != null)
        {
            var temp = player.gameObject.GetComponent<AudioSource>();
            temp.playOnAwake = false;
            temp.loop = false;
        }
    }

    // Convenient method for quickly building GameObject
    private void tempHandler(string name, Transform transform)
    {
        GameObject temp = new GameObject();
        temp.transform.parent = transform;
        temp.name = name;
    }
    #endregion
}
#region Serializable Classes and Interfaces
[Serializable]
public class VideoClass360 : ClassProps
{
    [Header("Required")]
    [SerializeField]
    private VideoClip _videoClip;
    public VideoClip VideoClip { get { return _videoClip; } private set { _videoClip = value; } }

    // Necessary for changing sykbox layout if 360 videos are in different formats, requires additional implementation in HDRP, however, so I haven't gotten around to implementing it again, not a strong priority since all lab footage is the same default format
    //[SerializeField] private string _imageType;
    //public string ImageType { get { return _imageType; } private set { _imageType = value; } }

    [Header("Optional")]
    [SerializeField]
    private AudioClip _audioClip;
    public AudioClip AudioClip { get { return _audioClip; } private set { _audioClip = value; } }

    [Tooltip("Panel which you want to follow the video if necessary for navigation or video-specific interaction")] [SerializeField]
    private GameObject _followingPanel;
    public GameObject FollowingPanel { get { return _followingPanel; } private set { _followingPanel = value; } }

    /// -1 -> Video has been played to finish
    /// 0 -> Video has never been played
    /// Otherwise, variable holds last video frame played
    public long LastFrame { get; set; } = 0;
    public float Timestamp { get; set; } = 0;
}

[Serializable]
public class VideoClass2D : ClassProps
{
    [Header("Required")]
    [SerializeField]
    private VideoClip _videoClip;
    public VideoClip VideoClip { get { return _videoClip; } private set { _videoClip = value; } }

    [Tooltip("Name of 360 video of which this video is to overlay")] [SerializeField]
    private string _overlayVideoName;
    public string OverlayVideoName { get { return _overlayVideoName; } private set { _overlayVideoName = value; } }

    [Tooltip("Frame that this video appears on in the 360 video")] [SerializeField]
    private int _appearanceFrame;
    public int AppearanceFrame { get { return _appearanceFrame; } private set { _appearanceFrame = value; } }

    [Header("Optional")]
    [SerializeField]
    private AudioClip _audioClip;
    public AudioClip AudioClip { get { return _audioClip; } private set { _audioClip = value; } }

    /// -1 -> Video has been played to finish
    /// 0 -> Video has never been played
    /// Otherwise, variable holds last video frame played
    public long LastFrame { get; set; } = 0;
    public float Timestamp { get; set; } = 0;
}

[Serializable]
public class MasterVideoClass
{
    [SerializeField]
    private VideoClass360[] _videoList360;

    [SerializeField]
    private VideoClass2D[] _videoList2D;

    public VideoClass360[] videoList360 { get { return _videoList360; } private set { _videoList360 = value; } }
    public VideoClass2D[] videoList2D { get { return _videoList2D; } private set { _videoList2D = value; } }
}

interface ClassProps
{
    VideoClip VideoClip { get; }
    AudioClip AudioClip { get; }
    long LastFrame { get; }
    float Timestamp { get; }
}
#endregion