/// Written by Ethan Woods -> 016erw@gmail.com
/// The RadialBar class is responsible for handling a radial bar sprite that fills at a constant rate if whatever calling it is true, this is therefore a bare class and is only instantiated by and has function calls made outside of itself
/// This was used as a status indicator in taking pictures with hand-tracking in VR but has pretty limitless potential for implementation since it actually doesn't look that bad

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RadialBar : MonoBehaviour
{
    public Canvas viewController; // Canvas on which the radial bar graphic is displayed

    private Image fill; // Radial bar graphic

    private bool running; // Whether or not the radial bar is already being filled or not. Note: A boolean isn't necessary here, you could just evaluate the 'fill' of the image to determine whether or not it's already running, if you end up using this class, that'd be a good improvement
    private float maxValue; // Is only ultimately latched onto screen refresh rate (since the coroutine runs every frame, if the maxValue of the fill is set to the framerate, the fill time is almost exactly 1 second). This variable is fine but implementing an additional time-scalar in the inspector would be good

    private void Awake()
    {
        fill = gameObject.GetComponent<Image>();
    }

    private void Start()
    {
        fill.fillAmount = 0; // Setting radial bar fill amount to 0
        maxValue = Screen.currentResolution.refreshRate; // Should have the radial bar fill up at a relatively decent speed, though this could be scaled with a SerializeField in inspector which would be good

        running = false;
    }

    public void FillBar(Color color)
    {
        if (!running) StartCoroutine(Filling(color));
    }

    // Resets radial bar to 0 fill (invisible)
    public void EmptyBar()
    {
        StopAllCoroutines();
        running = false;
        fill.fillAmount = 0;
    }

    // Fills radial bar at a constant rate based on framerate
    public IEnumerator Filling(Color color) {
        gameObject.GetComponent<Image>().color = color; // Assigning color filter to image, allowing user to create radial bar of any color
        running = true;
        while (running)
        {
            yield return new WaitForEndOfFrame();
            fill.fillAmount += 1 / maxValue;
        } 
    }
}