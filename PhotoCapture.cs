/// Written by Ethan Woods -> 016erw@gmail.com
/// The PhotoCapture class is responsible for handling both controller and hand-oriented (custom hand poses) photo-taking in VR
/// This code implements the custom 'Task' system which was nice to a point (cleaned up the evaluation of whether or not a coroutine was running, etc.) but caused issues on Android for some reason
/// Therefore, this class would need to be refactored to work properly
/// However, the actual picture taking, rendering, and storing functionality all works so that's something that can be salvaged
/// This also serves as a decent example of how to handle a local filesystem for storing files generated from within Unity so that's something at least

using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;

[RequireComponent(typeof(PosePackage))]
public class PhotoCapture : MonoBehaviour
{
    [Header("Canvas Elements")]
    [SerializeField] private Image photoDisplayArea;
    [SerializeField] private GameObject photoBackground;
    [SerializeField] private RadialBar photoBar;
    [SerializeField] private Animator animationController;

    [Header("Physical Components")]
    [SerializeField] private Camera cameraComponent;

    private PosePackage posePackage;

    private bool rightCorner = false;
    private bool leftCorner = false;
    private bool thumbsUp = false;
    private bool thumbsDown = false;

    private Texture2D screenshot;
    private Texture2D displayImage;

    private RenderTexture rt;
    private Sprite photoSprite;

    //private Task photoTask = null;
    //private Task renderTask = null;
    //private Task saveTask = null;
    //private Task readTask = null;

    private string pathSpecific;
    private string rootImageDir;
    private string finalDir;
    private string fileName;

    private int photoDirectory;
    private int picCount = 1;

    private void Start()
    {
        posePackage = gameObject.GetComponent<PosePackage>();
        posePackage.poseInit();

        photoBackground.SetActive(false);
        photoBar.viewController.enabled = false;

        //Dynamic filesystem that uses PlayerPrefs among other checks to properly classify files based on what day it is, what run of the day it is, etc.
        rootImageDir = Path.Combine(Application.persistentDataPath + "/PhotoDirectory/");
        pathSpecific = DateTime.Now.ToString("yyyy-MM-dd");
        if (DateTime.Now.Date.Day != PlayerPrefs.GetInt("lastDate")) PlayerPrefs.SetInt("dateMultiple", 1);
        else PlayerPrefs.SetInt("dateMultiple", PlayerPrefs.GetInt("dateMultiple") + 1);

        //Filename is partitioned a couple times for easier reference later on due to the changing ints which comprise the filename
        pathSpecific += " /Run " + PlayerPrefs.GetInt("dateMultiple");
        PlayerPrefs.SetInt("lastDate", DateTime.Now.Date.Day);
        finalDir = Path.Combine(rootImageDir + pathSpecific + "/");
        Directory.CreateDirectory(finalDir);


        //gameObject.GetComponent<dontDestroyManager>().interactable.SetActive(true);

    }

    private void Update()
    {
        /*
        if (photoTask == null)
        {
            if (photoBackground.activeSelf)
            {
                selectorHandler(posePackage.thumbsUp, increaseBarTU, resetBarTUD);
                selectorHandler(posePackage.thumbsDown, increaseBarTD, resetBarTUD);
                if (thumbsUp || thumbsDown)
                {
                    photoBar.viewController.enabled = true;
                    if (thumbsUp) photoBar.FillBar(Color.green);
                    if (thumbsDown) photoBar.FillBar(Color.red);
                }
            }
            else
            {
                selectorHandler(posePackage.rightCorner, rightActivate, rightDeactivate);
                selectorHandler(posePackage.leftCorner, leftActivate, leftDeactivate);
                if (rightCorner && leftCorner)
                {
                    photoBar.viewController.enabled = true;
                    photoBar.FillBar(Color.white);
                }
            }

            if (photoBar.gameObject.GetComponent<Image>().fillAmount == 1)
            {
                if (photoBackground.activeSelf)
                {
                    if (thumbsDown) File.Delete(Path.Combine(finalDir + fileName + ".png"));
                    photoBackground.SetActive(false);
                    resetBarTUD();
                }
                else
                {
                    generalDeactivate();
                    //photoTask = new Task(CapturePhoto());
                }
            }
        }
        */
       // if (Input.GetMouseButtonDown(0)) photoTask = new Task(CapturePhoto());
    }

    private IEnumerator CapturePhoto()
    {
        fileName = ("Pic " + picCount++);
        //renderTask = new Task(PhotoRenderProcess());
        yield return null;
        //saveTask = new Task(photoSaveProcess());
        yield return null;
        photoSprite = Sprite.Create(screenshot, new Rect(0.0f, 0.0f, screenshot.width, screenshot.height), new Vector2(0.5f, 0.5f), 100.0f);
        photoDisplayArea.sprite = photoSprite;
        photoBackground.SetActive(true);
        //screenshot.Apply();
        //photoTask = null;
        yield return new WaitForEndOfFrame();
        cameraComponent.targetTexture = null;
        //RenderTexture.active = null;
       //animationController.SetTrigger("IdleReturn");
        yield return null;
    }

    private IEnumerator PhotoRenderProcess() 
    {
        //photoTask.Pause();
        yield return new WaitForEndOfFrame();
        rt = new RenderTexture(Screen.width, Screen.height, 6, RenderTextureFormat.ARGB32);
        cameraComponent.targetTexture = rt;
        cameraComponent.Render();
        RenderTexture.active = rt;
        yield return new WaitForEndOfFrame();
        //animationController.SetTrigger("FlashTrigger");

        screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false, true);
        //readTask = new Task(photoReadProcess());
        yield return null;
        screenshot.Apply();
        yield return null;
        //photoTask.Unpause();
    }

    private IEnumerator photoSaveProcess()
    {
        //photoTask.Pause();
        yield return new WaitForEndOfFrame();
        File.WriteAllBytes(finalDir + fileName + ".png", ImageConversion.EncodeToPNG(screenshot));
        yield return null;
        //photoTask.Unpause();
    }

    private IEnumerator photoReadProcess()
    {
        //renderTask.Pause();
        yield return new WaitForEndOfFrame();
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        yield return null;
        //renderTask.Unpause();
    }

    private void selectorHandler(ISelector selector, Action select, Action unselect)
    {
        selector.WhenSelected += select;
        selector.WhenUnselected += unselect;
    }

    //Helper Functions
    private void rightActivate()
    {
        if (!rightCorner) rightCorner = true;
    }
    private void rightDeactivate()
    {
        rightCorner = false;
        generalDeactivate();
    }
    private void leftActivate()
    {
        if (!leftCorner) leftCorner = true;
    }
    private void leftDeactivate()
    {
        leftCorner = false;
        generalDeactivate();
    }
    private void increaseBarTU()
    {
        if (!thumbsUp) thumbsUp = true;
    }
    private void increaseBarTD()
    {
        if (!thumbsDown) thumbsDown = true;
    }
    private void resetBarTUD()
    {
        thumbsUp = false;
        thumbsDown = false;
        generalDeactivate();
    }
    private void generalDeactivate()
    {
        photoBar.EmptyBar();
        photoBar.viewController.enabled = false;
    }
}