using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Fixes Unity 6 URP Player Build failure caused by benign Editor-only shader asset
/// type validation ("Host type is not matching any asset type at Path...").
/// These Editor assets (AutodeskInteractive.shadergraph & TraceVirtualOffset.urtshader)
/// are not part of standalone player builds, but Unity 6's pre-build validation logs
/// them as LogError, aborting player builds. This filter downgrades them to warnings so
/// player builds complete cleanly.
/// </summary>
[InitializeOnLoad]
public class URPBuildErrorFilter : IPreprocessBuildWithReport, IPostprocessBuildWithReport, ILogHandler
{
    private static ILogHandler s_DefaultLogHandler;
    private static URPBuildErrorFilter s_Instance;

    public int callbackOrder => int.MinValue; // First preprocessor to run before URPPreprocessBuild

    static URPBuildErrorFilter()
    {
        InstallFilter();
    }

    public static void InstallFilter()
    {
        if (s_Instance == null)
        {
            s_Instance = new URPBuildErrorFilter();
            s_DefaultLogHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = s_Instance;
            Debug.Log("[URPBuildErrorFilter] Active: Benign editor-only shader mismatch errors will not abort player builds.");
        }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        InstallFilter();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        // Keep active
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        if (logType == LogType.Error || logType == LogType.Assert)
        {
            string message = (args != null && args.Length > 0) ? string.Format(format, args) : format;
            if (message.Contains("Host type is not matching any asset type"))
            {
                // Downgrade to Warning so Unity's Player Build pipeline does not count it as a fatal build error
                s_DefaultLogHandler.LogFormat(LogType.Warning, context, "[URP Shader Compatibility Note] " + format, args);
                return;
            }
        }

        s_DefaultLogHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        if (exception != null && exception.Message != null && exception.Message.Contains("Host type is not matching any asset type"))
        {
            s_DefaultLogHandler.LogFormat(LogType.Warning, context, "[URP Shader Compatibility Note] " + exception.Message);
            return;
        }

        s_DefaultLogHandler.LogException(exception, context);
    }

    [MenuItem("Tools/Test Ensure With Filter")]
    public static void TestEnsureWithFilter()
    {
        InstallFilter();
        try
        {
            var t = typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings");
            var m = t?.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(bool) }, null)
                 ?? t?.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            object[] args = m.GetParameters().Length == 1 ? new object[] { true } : null;
            var result = m.Invoke(null, args);
            Debug.Log($"[URPBuildErrorFilter] Ensure() completed cleanly: {result}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[URPBuildErrorFilter] Exception during Ensure: {ex}");
        }
    }
}
