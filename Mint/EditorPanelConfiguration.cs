/// Written by Ethan Woods -> 016erw@gmail.com
/// This code is responsible for altering the colors of the interactable panels
/// As the menu system is going to end up changing with Kacie's custom menu, this won't be useful for all that long, however several of the techniques utilized here will be the same with setting up the new menus
/// Note: OnValidate() was manually made public by me because it's needed for applying changes, if Oculus is updated, this method will need to be made public again since I haven't bothered with making my own identical class and implementing it

using UnityEngine;
using Oculus.Interaction;
using TMPro;
using UnityEngine.UI;

[ExecuteInEditMode]
public class EditorPanelConfiguration : MonoBehaviour
{
    [SerializeField] 
    private Color backgroundColor,
    buttonSelectColor,
    buttonTextColor, 
    buttonBorderColor;
    private void Update()
    {
        updateColors();
    }

    private void updateColors()
    {
        // Latching onto heroscreen and setting background color
        transform.Find("Inner Active Container").Find("Canvas").Find("HeroScreen").Find("Panel").gameObject.GetComponent<Image>().color = backgroundColor;

        // Iterating through each of the buttons attached to the gameobject at the specified path
        foreach (Transform t in gameObject.transform.Find("Inner Active Container").Find("Buttons")) 
        {
            // Setting a bunch of color-related properties of the buttons using custom method which makes it easier to configure opacity of various button elements
            var panelObject = t.Find("Visuals").Find("ButtonVisual").Find("ButtonPanel");
            panelObject.GetComponent<RoundedBoxProperties>().Color = ensureOpacity(buttonSelectColor, 20);
            panelObject.GetComponent<RoundedBoxProperties>().BorderColor = ensureOpacity(buttonBorderColor, 255);
            panelObject.Find("Text (TMP)").GetComponent<TextMeshPro>().color = ensureOpacity(buttonTextColor, 255);
            panelObject.GetComponent<RoundedBoxProperties>().OnValidate(); // Necessary for making the changes take effect

            var tempVisualComponent = panelObject.GetComponent<InteractableColorVisual>();
            tempVisualComponent._normalColorState.Color = ensureOpacity(buttonSelectColor, 100);
            tempVisualComponent._hoverColorState.Color = ensureOpacity(buttonSelectColor, 150);
            tempVisualComponent._selectColorState.Color = ensureOpacity(buttonSelectColor, 200);
        }
    }

    // Makes it easier to control opacity of component without having to re-specify the RGB components
    public static Color ensureOpacity(Color color, int opacity)
    {
        return new Color(color.r, color.g, color.b, opacity);
    }
}
