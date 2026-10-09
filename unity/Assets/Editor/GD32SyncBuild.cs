using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GD32SyncBuild
{
    public static void BuildAndroid()
    {
        GD32SyncTests.RunBatch();
        string path=Path.GetFullPath("Builds/AnimalRace_GD32_V32.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        // Keep the current scene, art and player settings; only rebuild the updated code.
        RK3128Build.BuildGd32SynchronizedApk(path);
        Debug.Log("GD32_V32_APK="+path);
    }
}
