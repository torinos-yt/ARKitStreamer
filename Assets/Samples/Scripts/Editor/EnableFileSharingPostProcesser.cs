#if UNITY_IOS && UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class EnableFileSharingPostProcessor
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
            BuildForiOS(path);
    }

    private static void BuildForiOS(string path)
    {
        // Get plist
        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Set key and value for UIFileSharingEnabled.
        PlistElementDict rootDict = plist.root;
        rootDict.SetBoolean("UIFileSharingEnabled", true);

        // Write to file
        File.WriteAllText(plistPath, plist.WriteToString());
    }
}

#endif