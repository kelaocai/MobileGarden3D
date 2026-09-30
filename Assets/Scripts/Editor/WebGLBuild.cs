using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class WebGLBuild
{
    private const string OutputPath = "Builds/WebGL";

    [MenuItem("Tools/Mobile Garden/Build WebGL")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("请先停止播放模式，再构建 WebGL。");
            return;
        }

        Directory.CreateDirectory(OutputPath);
        EditorBuildSettingsScene[] enabledScenes =
            System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
        string[] scenes = System.Array.ConvertAll(enabledScenes, scene => scene.path);
        if (scenes.Length == 0)
        {
            Debug.LogError("WebGL 构建失败：Build Settings 中没有启用的场景。");
            return;
        }

        BuildPlayerOptions options = new()
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"WebGL 构建完成：{OutputPath}，大小 {summary.totalSize / 1048576f:F1} MB，耗时 {summary.totalTime}。");
        else
            Debug.LogError($"WebGL 构建失败：{summary.result}，错误 {summary.totalErrors}。");
    }
}
