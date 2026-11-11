using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    const string OutputDir = "Builds/WebGL";
    const string ZipPath = "Builds/ShiftScape-WebGL.zip";

    [MenuItem("ShiftScape/Build WebGL for itch.io")]
    public static void BuildForItch()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Fail("No enabled scenes in Build Settings.");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Fail("Could not switch to the WebGL build target. Is the WebGL module installed?");
            return;
        }

        PlayerSettings.productName = "ShiftScape";
        // itch.io serves files without Content-Encoding headers, so the loader must decompress gzip itself.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        if (Directory.Exists(OutputDir)) Directory.Delete(OutputDir, true);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            Fail($"WebGL build {report.summary.result} with {report.summary.totalErrors} error(s).");
            return;
        }

        if (File.Exists(ZipPath)) File.Delete(ZipPath);
        ZipFile.CreateFromDirectory(OutputDir, ZipPath);
        double megabytes = new FileInfo(ZipPath).Length / (1024.0 * 1024.0);
        Debug.Log($"WebGL build ready: {Path.GetFullPath(ZipPath)} ({megabytes:F1} MB). Upload this zip to itch.io as an HTML game.");
        if (!Application.isBatchMode) EditorUtility.RevealInFinder(ZipPath);
    }

    static void Fail(string message)
    {
        Debug.LogError(message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
