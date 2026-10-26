using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// File requests allow CLI tooling to use the existing editor without a second Unity
// instance, changing the open scene, or interacting with desktop windows.
[InitializeOnLoad]
public static class Case13BuildSize
{
    const string RequestPath = "Library/case13-size-request.txt";
    const string ReportDirectory = "Logs/BuildSize";

    [Serializable]
    sealed class DependencyReport
    {
        public string[] scenes;
        public string[] dependencies;
        public string[] resources;
        public string[] gameDependencies;
    }

    static Case13BuildSize() => EditorApplication.update += PollRequest;

    static void PollRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer ||
            !File.Exists(RequestPath)) return;
        string command = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        try
        {
            if (command == "audit") Audit();
            else if (command == "build") BuildAndroid();
            else throw new ArgumentException("Unknown build size command: " + command);
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportDirectory + "/error.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Cases/Build Size/Audit Build Dependencies")]
    public static void Audit()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string[] resources = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                p.Contains("/Resources/") && !p.Contains("/Editor/") && !AssetDatabase.IsValidFolder(p)).ToArray();
        var report = new DependencyReport
        {
            scenes = scenes,
            resources = resources,
            gameDependencies = AssetDatabase.GetDependencies(scenes.Concat(resources.Where(p =>
                    !p.StartsWith("Assets/Photon/PhotonChat/Demos/", StringComparison.Ordinal) &&
                    !p.StartsWith("Assets/Photon/PhotonUnityNetworking/Demos/", StringComparison.Ordinal))).ToArray(), true)
                .OrderBy(p => p, StringComparer.Ordinal).ToArray(),
            dependencies = AssetDatabase.GetDependencies(scenes.Concat(resources).ToArray(), true)
                .OrderBy(p => p, StringComparer.Ordinal).ToArray()
        };
        Directory.CreateDirectory(ReportDirectory);
        File.WriteAllText(ReportDirectory + "/dependencies.json", JsonUtility.ToJson(report, true));
        Debug.Log("Build size dependency report: " + ReportDirectory + "/dependencies.json");
    }

    [MenuItem("Cases/Build Size/Build Optimized Android APK")]
    public static void BuildAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Select the Android build target before building.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building.");
        Directory.CreateDirectory("Builds/Android");
        Directory.CreateDirectory(ReportDirectory);
        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = BuildTarget.Android,
                locationPathName = "Builds/Android/Case13-optimized.apk",
                // Android's default ZIP packaging is smaller than layering LZ4HC
                // inside ZIP. Detailed reporting does not add data to the player.
                options = BuildOptions.DetailedBuildReport
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var lines = report.packedAssets.SelectMany(p => p.contents)
                .GroupBy(a => a.sourceAssetPath)
                .Select(g => new { path = g.Key, bytes = g.Sum(a => (long)a.packedSize) })
                .OrderByDescending(a => a.bytes)
                .Select(a => a.bytes + "\t" + a.path);
            File.WriteAllLines(ReportDirectory + "/packed-assets.tsv", new[] { "bytes\tasset" }.Concat(lines));
            File.WriteAllText(ReportDirectory + "/build-summary.txt",
                "Result: " + report.summary.result + "\nBytes: " + report.summary.totalSize +
                "\nOutput: " + report.summary.outputPath + "\nErrors: " + report.summary.totalErrors + "\n");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed. See the build report and Editor log.");
        }
        finally { EditorUserBuildSettings.buildAppBundle = previousAppBundle; }
    }
}
