using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    const string OutputDir = "Builds/WebGL";
    const string DevelopmentOutputDir = "Builds/WebGL-Dev";
    const string ZipPath = "Builds/ShiftScape-WebGL.zip";

    [MenuItem("ShiftScape/Build WebGL for itch.io")]
    public static void BuildForItch()
    {
        if (!Build(OutputDir, BuildOptions.None)) return;

        if (File.Exists(ZipPath)) File.Delete(ZipPath);
        ZipFile.CreateFromDirectory(OutputDir, ZipPath);
        double megabytes = new FileInfo(ZipPath).Length / (1024.0 * 1024.0);
        Debug.Log($"WebGL build ready: {Path.GetFullPath(ZipPath)} ({megabytes:F1} MB). Upload this zip to itch.io as an HTML game.");
        if (!Application.isBatchMode) EditorUtility.RevealInFinder(ZipPath);
    }

    // Development builds show Unity's watermark and enable the F9 unlock-all testing key.
    [MenuItem("ShiftScape/Build WebGL (Development)")]
    public static void BuildDevelopment()
    {
        if (!Build(DevelopmentOutputDir, BuildOptions.Development)) return;
        Debug.Log($"Development WebGL build ready: {Path.GetFullPath(DevelopmentOutputDir)}");
        if (!Application.isBatchMode) EditorUtility.RevealInFinder(DevelopmentOutputDir);
    }

    static bool Build(string outputDir, BuildOptions options)
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Fail("No enabled scenes in Build Settings.");
            return false;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Fail("Could not switch to the WebGL build target. Is the WebGL module installed?");
            return false;
        }

        PlayerSettings.productName = "ShiftScape";
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        // itch.io serves files without Content-Encoding headers, so the loader must decompress gzip itself.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputDir,
            target = BuildTarget.WebGL,
            options = options,
        });

        if (report.summary.result == BuildResult.Succeeded) return true;
        Fail($"WebGL build {report.summary.result} with {report.summary.totalErrors} error(s).");
        return false;
    }

    static void Fail(string message)
    {
        Debug.LogError(message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
