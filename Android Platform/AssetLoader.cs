/// Written by Ethan Woods -> 016erw@gmail.com
/// This script was the start to making an auto-load framework for all streamable assets to the Quest 2
/// The general premise of this works fine, but I ran into issues with actually executing commands in the commandline, which prevented me from getting too far into this functionality
/// This was in addition to the fact that we are no longer primarily developing the app on the Quest 2 (Android)

using UnityEditor;
using UnityEngine;
using System.Diagnostics;

#if (UNITY_EDITOR) 
public class AssetLoader : Editor
{
    static string preparationScenePath = $"{Application.dataPath}/Scenes/Preparation.unity";
    static string preparationSceneName = "Preparation";

    static string assetFolderName = "asset_video";
    static string materialsFolderName = "Render Materials";
    static string texturesFolderName = "Render Textures";

    [MenuItem("Assets/Load Assets")]
    static void loadAssets()
    {
        string[] dirArray = {
        "asset_video"
        };
        Process cmdAssets = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo("C:\\Windows\\System32\\cmd.exe", "/C dir");
        cmdAssets.StartInfo = startInfo;

        //startInfo.Verb = "runas";


        startInfo.WorkingDirectory = "C:\\Users\\Admin\\AppData\\Local\\Android\\Sdk\\platform-tools";
        //startInfo.ArgumentList.Add("adb push --sync ./asset_video /sdcard/Android/data/com.AsapMedia.Clean4H/asset_video");


        /*
        for (int i = 0; i < dirArray.Length; i++)
        {
            cmdAssets.StartInfo.Arguments += $"./{dirArray[i]}";
        }
        */
        //cmdAssets.StartInfo = new ProcessStartInfo("C:\\Windows\\System32\\cmd.exe", "\"echo something");

        //cmdAssets.StartInfo.UseShellExecute = true;
        cmdAssets.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        cmdAssets.Start();
    }


}
#endif