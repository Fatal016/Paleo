/// Written by Ethan Woods -> 016erw@gmail.com
/// This script is to be attached to any object you want to remain persistent in DDOL (Don't Destroy on Load) between scenes.
/// You will have to manually add a DontDestroy object, GameObject name, and add to the Awake() function for an object to be detected and designated as DDOL

using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    // Static variables that store instances of DontDestroy objects
    private static DontDestroy dontDestroyBuffer,
        dontDestroyContainer;

    // DontDestroy GameObject names (name here must be an exact match to Gameobject name that the script is attached to)
    private const string bufferName = "Video Player Buffer",
        containerName = "Menus and Persistent Objects";

    // Checks attached GameObject and sets static DontDestroy variable to the instance of that object if their names correlate
    private void Awake()
    {
        // Switch case that checks which DDOL object the script is attached to and sets static variable to the instance of that object
        switch (this.name) {
            case bufferName:
                if (dontDestroyBuffer == null) dontDestroyBuffer = this;
                break;
            case containerName:
                if (dontDestroyContainer == null) dontDestroyContainer = this;
                break;
            default:
                Debug.LogError("DDOL object not properly attached, check DontDestroy to ensure that object and string are set for gameobject");
                Destroy(gameObject);
                break;
        }
        DontDestroyOnLoad(gameObject);
    }
}