/// Written by Ethan Woods -> 016erw@gmail.com
/// The PosePackage class is responsible for storing all pose-related assets, primarily for the hand-tracking picture-taking functionality
/// This code is pretty terrible but I wanted to clean up the PhotoCapture script and don't know how to make this code more efficient
/// If you want to use this code (which you shouldn't) attach this script to the same object that the PhotoCapture script is on and input the various poses into the inspector fields

using UnityEngine;
using UnityEngine.Assertions;
using Oculus.Interaction;

public class PosePackage : MonoBehaviour
{
    [SerializeField, Interface(typeof(ISelector))]
    public MonoBehaviour _ThumbsUpSelector;
    public ISelector thumbsUp;

    [SerializeField, Interface(typeof(ISelector))]
    public MonoBehaviour _ThumbsDownSelector;
    public ISelector thumbsDown;

    [SerializeField, Interface(typeof(ISelector))]
    public MonoBehaviour _RightCornerSelector;
    public ISelector rightCorner;

    [SerializeField, Interface(typeof(ISelector))]
    public MonoBehaviour _LeftCornerSelector;
    public ISelector leftCorner;

    public void poseInit()
    {
        thumbsUp = _ThumbsUpSelector as ISelector;
        thumbsDown = _ThumbsDownSelector as ISelector;
        rightCorner = _RightCornerSelector as ISelector;
        leftCorner = _LeftCornerSelector as ISelector;

        Assert.IsNotNull(thumbsUp);
        Assert.IsNotNull(thumbsDown);
        Assert.IsNotNull(rightCorner);
        Assert.IsNotNull(leftCorner);
    }
}