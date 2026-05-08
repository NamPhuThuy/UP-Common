using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

// ─────────────────────────────────────────────────────────────────────────────
// Build Settings Data (Serializable for EditorPrefs persistence)
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class BuildSettings
{
    public string productName        = "YourGameName";
    public string companyName        = "YourCompany";
    public string version            = "1.0.0";
    public string outputFolder       = "Builds/Windows";
    public BuildTarget buildTarget   = BuildTarget.StandaloneWindows64;
    public bool   openFolderOnDone   = true;
    public bool   runAfterBuild      = false;
    public bool   cleanBeforeBuild   = false;

    // Preset flags
    public bool isDevelopment        = false;
    public bool isCompressed         = false;
    public bool isDeepProfile        = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// Core Build Script — MenuItem entry points
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 
/// </summary>
public class WindowsBuildScript : EditorWindow
{
    private const string PREFS_KEY = "WindowsBuildScript_Settings";

    // ── Quick Menu Builds ────────────────────────────────────────────────────

    [MenuItem("NamPhuThuy/Build (untested)/⚡ Windows (Release)", priority = 1)]
    public static void BuildRelease()
    {
        var s = LoadSettings();
        s.isDevelopment = false;
        s.isCompressed  = false;
        ExecuteBuild(s);
    }

    [MenuItem("NamPhuThuy/Build (untested)/🐛 Windows (Development)", priority = 2)]
    public static void BuildDevelopment()
    {
        var s = LoadSettings();
        s.isDevelopment = true;
        s.isCompressed  = false;
        ExecuteBuild(s);
    }

    [MenuItem("NamPhuThuy/Build (untested)/📦 Windows (Compressed)", priority = 3)]
    public static void BuildCompressed()
    {
        var s = LoadSettings();
        s.isDevelopment = false;
        s.isCompressed  = true;
        ExecuteBuild(s);
    }

    [MenuItem("NamPhuThuy/Build (untested)/⚙️ Build Configuration...", priority = 20)]
    public static void ShowBuildWindow()
        => GetWindow<WindowsBuildScript>("Build Configuration");

    // ── Core Build Logic ─────────────────────────────────────────────────────

    public static void ExecuteBuild(BuildSettings settings)
    {
        // ── Validate scenes ──────────────────────────────────────────────────
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[Build] No enabled scenes found in Build Settings.");
            EditorUtility.DisplayDialog("Build Failed",
                "No enabled scenes found in Build Settings.", "OK");
            return;
        }

        // ── Resolve output path ──────────────────────────────────────────────
        string root     = Path.Combine(Application.dataPath, "..");
        string outDir   = Path.GetFullPath(Path.Combine(root, settings.outputFolder));
        string exePath  = Path.Combine(outDir, $"{settings.productName}.exe");

        // ── Optional clean ───────────────────────────────────────────────────
        if (settings.cleanBeforeBuild && Directory.Exists(outDir))
        {
            Debug.Log($"[Build] Cleaning output folder: {outDir}");
            Directory.Delete(outDir, recursive: true);
        }

        if (!Directory.Exists(outDir))
            Directory.CreateDirectory(outDir);

        // ── Stamp player settings ────────────────────────────────────────────
        string prevProduct = PlayerSettings.productName;
        string prevCompany = PlayerSettings.companyName;
        string prevVersion = PlayerSettings.bundleVersion;

        PlayerSettings.productName  = settings.productName;
        PlayerSettings.companyName  = settings.companyName;
        PlayerSettings.bundleVersion = settings.version;

        // ── Compose BuildOptions ─────────────────────────────────────────────
        BuildOptions opts = BuildOptions.None;
        if (settings.isDevelopment)  opts |= BuildOptions.Development | BuildOptions.AllowDebugging;
        if (settings.isCompressed)   opts |= BuildOptions.CompressWithLz4HC;
        if (settings.isDeepProfile)  opts |= BuildOptions.EnableDeepProfilingSupport;

        // ── Run build ────────────────────────────────────────────────────────
        var sw = Stopwatch.StartNew();
        Debug.Log($"[Build] Starting → {exePath}");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = exePath,
            target           = settings.buildTarget,
            options          = opts
        });

        sw.Stop();

        // ── Restore player settings ──────────────────────────────────────────
        PlayerSettings.productName   = prevProduct;
        PlayerSettings.companyName   = prevCompany;
        PlayerSettings.bundleVersion = prevVersion;

        // ── Report result ────────────────────────────────────────────────────
        LogBuildSummary(report, sw.Elapsed.TotalSeconds);

        if (report.summary.result == BuildResult.Succeeded)
        {
            if (settings.openFolderOnDone)
                EditorUtility.RevealInFinder(exePath);

            if (settings.runAfterBuild)
                Process.Start(exePath);
        }
        else
        {
            // Surface every error in the console
            foreach (var step in report.steps)
                foreach (var msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError($"[Build] {msg.content}");

            EditorUtility.DisplayDialog("Build Failed",
                $"Build failed with {report.summary.totalErrors} error(s).\nCheck the Console for details.", "OK");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string[] GetEnabledScenes() =>
        EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();

    private static void LogBuildSummary(BuildReport report, double seconds)
    {
        var sum = report.summary;
        string status = sum.result == BuildResult.Succeeded ? "✅ SUCCESS" : "❌ FAILED";

        Debug.Log(
            $"[Build] {status} | " +
            $"Time: {seconds:F1}s | " +
            $"Size: {sum.totalSize / 1_048_576f:F1} MB | " +
            $"Warnings: {sum.totalWarnings} | " +
            $"Errors: {sum.totalErrors}"
        );
    }

    // ── Settings persistence via EditorPrefs ─────────────────────────────────

    public static BuildSettings LoadSettings()
    {
        if (EditorPrefs.HasKey(PREFS_KEY))
        {
            try
            {
                return JsonUtility.FromJson<BuildSettings>(EditorPrefs.GetString(PREFS_KEY));
            }
            catch { /* fall through to defaults */ }
        }
        return new BuildSettings();
    }

    public static void SaveSettings(BuildSettings settings)
        => EditorPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(settings));
}

// ─────────────────────────────────────────────────────────────────────────────
// Editor Window — Build Configuration UI
// ─────────────────────────────────────────────────────────────────────────────
public class BuildConfigWindow : EditorWindow
{
    private BuildSettings _settings;
    private Vector2       _scroll;
    private string[]      _sceneNames;

    private void OnEnable()
    {
        _settings   = WindowsBuildScript.LoadSettings();
        RefreshSceneList();
    }

    private void RefreshSceneList()
    {
        _sceneNames = EditorBuildSettings.scenes
            .Select((s, i) =>
            {
                string name = Path.GetFileNameWithoutExtension(s.path);
                return $"{i}: {name}{(s.enabled ? "" : " (disabled)")}";
            })
            .ToArray();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // ── Header ───────────────────────────────────────────────────────────
        GUILayout.Space(6);
        GUILayout.Label("🛠  Build Configuration", EditorStyles.boldLabel);
        DrawHorizontalLine();

        // ── Identity ─────────────────────────────────────────────────────────
        GUILayout.Label("Identity", EditorStyles.boldLabel);
        _settings.productName = EditorGUILayout.TextField("Product Name", _settings.productName);
        _settings.companyName = EditorGUILayout.TextField("Company Name", _settings.companyName);
        _settings.version     = EditorGUILayout.TextField("Version",      _settings.version);

        GUILayout.Space(8);

        // ── Output ───────────────────────────────────────────────────────────
        GUILayout.Label("Output", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _settings.outputFolder = EditorGUILayout.TextField("Output Folder", _settings.outputFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string abs = EditorUtility.OpenFolderPanel("Select Output Folder",
                Path.Combine(Application.dataPath, ".."), "");
            if (!string.IsNullOrEmpty(abs))
            {
                // Store relative to project root
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _settings.outputFolder = abs.Replace(root, "").TrimStart('/', '\\');
            }
        }
        EditorGUILayout.EndHorizontal();

        _settings.buildTarget = (BuildTarget)
            EditorGUILayout.EnumPopup("Build Target", _settings.buildTarget);

        GUILayout.Space(8);

        // ── Build Presets ─────────────────────────────────────────────────────
        GUILayout.Label("Presets", EditorStyles.boldLabel);
        _settings.isDevelopment = EditorGUILayout.Toggle("Development Build",   _settings.isDevelopment);
        _settings.isCompressed  = EditorGUILayout.Toggle("Compress (LZ4 HC)",   _settings.isCompressed);
        _settings.isDeepProfile = EditorGUILayout.Toggle("Deep Profiling",      _settings.isDeepProfile);

        GUILayout.Space(8);

        // ── Post-Build ────────────────────────────────────────────────────────
        GUILayout.Label("Post-Build", EditorStyles.boldLabel);
        _settings.openFolderOnDone = EditorGUILayout.Toggle("Open Folder When Done", _settings.openFolderOnDone);
        _settings.runAfterBuild    = EditorGUILayout.Toggle("Run After Build",        _settings.runAfterBuild);
        _settings.cleanBeforeBuild = EditorGUILayout.Toggle("Clean Before Build",     _settings.cleanBeforeBuild);

        GUILayout.Space(8);

        // ── Scene overview ────────────────────────────────────────────────────
        GUILayout.Label("Scenes in Build Settings", EditorStyles.boldLabel);
        if (_sceneNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No scenes in Build Settings.", MessageType.Warning);
        }
        else
        {
            foreach (var s in _sceneNames)
                GUILayout.Label($"  • {s}", EditorStyles.miniLabel);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", EditorStyles.miniButton))
            RefreshSceneList();
        if (GUILayout.Button("Open Build Settings", EditorStyles.miniButton))
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        DrawHorizontalLine();

        // ── Action Buttons ───────────────────────────────────────────────────
        Color prevBg = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("▶  Build Now", GUILayout.Height(36)))
        {
            WindowsBuildScript.SaveSettings(_settings);
            WindowsBuildScript.ExecuteBuild(_settings);
        }

        GUI.backgroundColor = prevBg;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💾 Save Settings"))
            WindowsBuildScript.SaveSettings(_settings);
        if (GUILayout.Button("↩ Reset to Defaults"))
        {
            if (EditorUtility.DisplayDialog("Reset",
                "Reset all settings to defaults?", "Reset", "Cancel"))
                _settings = new BuildSettings();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private static void DrawHorizontalLine()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f));
        GUILayout.Space(4);
    }
}
#endif