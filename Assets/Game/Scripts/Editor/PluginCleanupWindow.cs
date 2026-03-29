using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class PluginCleanupWindow : EditorWindow
{
    [Serializable]
    private class AsmdefInfo
    {
        public string name;
        public string[] references;
    }

    private class PluginEntry
    {
        public string Name;
        public string Path;
        public bool UsedByDependencies;
        public bool UsedByAsmdefReference;
        public bool UsedByCodeToken;
        public bool SelectedForDeletion;

        public bool IsCandidateForDeletion
        {
            get { return !UsedByDependencies && !UsedByAsmdefReference && !UsedByCodeToken; }
        }
    }

    private static readonly string[] PluginRoots = { "Assets/Plugins", "Assets/Plagins" };
    private static readonly Regex TokenSplitRegex = new Regex("[^A-Za-z0-9_]+", RegexOptions.Compiled);

    private readonly List<PluginEntry> _entries = new List<PluginEntry>();
    private Vector2 _scroll;
    private bool _scanCompleted;
    private bool _showOnlyCandidates = true;
    private string _status = "Press Scan to find potentially unused plugins.";

    [MenuItem("Tools/Plugin Cleanup")]
    public static void OpenWindow()
    {
        PluginCleanupWindow window = GetWindow<PluginCleanupWindow>("Plugin Cleanup");
        window.minSize = new Vector2(760f, 480f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Plugin Cleanup Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool finds deletion candidates in Plugins/Plagins.\n" +
            "Candidate = no external asset dependencies, no asmdef references, and no code token matches.\n" +
            "Always review the list before deleting.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan", GUILayout.Height(28)))
            {
                RunScan();
            }

            GUI.enabled = _scanCompleted;
            if (GUILayout.Button("Select All Candidates", GUILayout.Height(28)))
            {
                SetSelectionForCandidates(true);
            }

            if (GUILayout.Button("Clear Selection", GUILayout.Height(28)))
            {
                foreach (PluginEntry e in _entries)
                {
                    e.SelectedForDeletion = false;
                }
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = _scanCompleted;
            _showOnlyCandidates = EditorGUILayout.ToggleLeft("Show only candidates", _showOnlyCandidates, GUILayout.Width(250));
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (_scanCompleted)
            {
                int candidateCount = _entries.Count(e => e.IsCandidateForDeletion);
                int selectedCount = _entries.Count(e => e.SelectedForDeletion);
                EditorGUILayout.LabelField(
                    $"Total: {_entries.Count} | Candidates: {candidateCount} | Selected: {selectedCount}",
                    EditorStyles.miniBoldLabel,
                    GUILayout.MaxWidth(420));
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(_status, MessageType.None);
        EditorGUILayout.Space(4);

        DrawList();

        EditorGUILayout.Space(6);
        GUI.enabled = _scanCompleted && _entries.Any(e => e.SelectedForDeletion);
        if (GUILayout.Button("Delete selected", GUILayout.Height(32)))
        {
            DeleteSelected();
        }
        GUI.enabled = true;
    }

    private void DrawList()
    {
        EditorGUILayout.BeginVertical("box");

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Delete", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Plugin", EditorStyles.boldLabel, GUILayout.Width(220));
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel, GUILayout.Width(170));
            EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (!_scanCompleted)
        {
            EditorGUILayout.LabelField("List is empty. Run Scan.");
        }
        else
        {
            IEnumerable<PluginEntry> visible = _entries;
            if (_showOnlyCandidates)
            {
                visible = visible.Where(e => e.IsCandidateForDeletion);
            }

            foreach (PluginEntry entry in visible)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = entry.IsCandidateForDeletion;
                    entry.SelectedForDeletion = EditorGUILayout.Toggle(entry.SelectedForDeletion, GUILayout.Width(60));
                    GUI.enabled = true;

                    EditorGUILayout.LabelField(entry.Name, GUILayout.Width(220));

                    string status = entry.IsCandidateForDeletion ? "Deletion candidate" : "In use";
                    EditorGUILayout.LabelField(status, GUILayout.Width(170));
                    EditorGUILayout.LabelField(BuildReason(entry), EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void RunScan()
    {
        _entries.Clear();
        _scanCompleted = false;
        _status = "Scanning...";
        Repaint();

        try
        {
            List<string> pluginDirectories = GetPluginDirectories();
            foreach (string dir in pluginDirectories)
            {
                _entries.Add(new PluginEntry
                {
                    Name = Path.GetFileName(dir),
                    Path = dir
                });
            }

            if (_entries.Count == 0)
            {
                _status = "Plugins/Plagins folders were not found or are empty.";
                _scanCompleted = true;
                return;
            }

            HashSet<string> dependencyUsedRoots = CollectDependencyUsedRoots(pluginDirectories);
            HashSet<string> asmdefUsedRoots = CollectAsmdefUsedRoots(pluginDirectories);
            HashSet<string> codeTokenUsedRoots = CollectCodeTokenUsedRoots(pluginDirectories);

            foreach (PluginEntry entry in _entries)
            {
                entry.UsedByDependencies = dependencyUsedRoots.Contains(entry.Path);
                entry.UsedByAsmdefReference = asmdefUsedRoots.Contains(entry.Path);
                entry.UsedByCodeToken = codeTokenUsedRoots.Contains(entry.Path);
            }

            _entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            int candidates = _entries.Count(e => e.IsCandidateForDeletion);
            _status = $"Scan complete. Candidates found: {candidates} of {_entries.Count}.";
            _scanCompleted = true;
        }
        catch (Exception ex)
        {
            _status = $"Scan failed: {ex.Message}";
            _scanCompleted = true;
            Debug.LogException(ex);
        }
    }

    private void SetSelectionForCandidates(bool selected)
    {
        foreach (PluginEntry e in _entries)
        {
            if (e.IsCandidateForDeletion)
            {
                e.SelectedForDeletion = selected;
            }
        }
    }

    private void DeleteSelected()
    {
        List<PluginEntry> selected = _entries.Where(e => e.SelectedForDeletion).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        string namesPreview = string.Join("\n", selected.Take(20).Select(e => "- " + e.Name));
        if (selected.Count > 20)
        {
            namesPreview += $"\n... and {selected.Count - 20} more";
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Delete selected plugins",
            "These folders will be deleted:\n\n" + namesPreview + "\n\nContinue?",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        int deleted = 0;
        int failed = 0;
        List<string> failedItems = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (PluginEntry entry in selected)
            {
                bool ok = AssetDatabase.DeleteAsset(entry.Path);
                if (ok)
                {
                    deleted++;
                }
                else
                {
                    failed++;
                    failedItems.Add(entry.Path);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        if (failed > 0)
        {
            _status = $"Deleted: {deleted}. Failed: {failed}. See Console for details.";
            Debug.LogWarning("Plugin cleanup: failed to delete:\n" + string.Join("\n", failedItems));
        }
        else
        {
            _status = $"Deleted: {deleted}.";
        }

        RunScan();
    }

    private static List<string> GetPluginDirectories()
    {
        List<string> result = new List<string>();

        foreach (string root in PluginRoots)
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                continue;
            }

            string fullRoot = ToFullPath(root);
            if (!Directory.Exists(fullRoot))
            {
                continue;
            }

            foreach (string dir in Directory.GetDirectories(fullRoot))
            {
                result.Add(ToAssetPath(dir));
            }
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static HashSet<string> CollectDependencyUsedRoots(IEnumerable<string> pluginDirs)
    {
        List<string> pluginDirList = pluginDirs.ToList();
        HashSet<string> usedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            .Where(p => !IsUnderAnyRoot(p, pluginDirList))
            .ToArray();

        string[] dependencies = AssetDatabase.GetDependencies(allAssetPaths, true);
        foreach (string dep in dependencies)
        {
            string root = FindRootForPath(dep, pluginDirList);
            if (!string.IsNullOrEmpty(root))
            {
                usedRoots.Add(root);
            }
        }

        return usedRoots;
    }

    private static HashSet<string> CollectAsmdefUsedRoots(IEnumerable<string> pluginDirs)
    {
        List<string> pluginDirList = pluginDirs.ToList();
        HashSet<string> usedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> pluginAsmdefs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> pluginAsmdefGuids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string asmdefPath in AssetDatabase.FindAssets("t:asmdef", pluginDirList.ToArray())
                     .Select(AssetDatabase.GUIDToAssetPath))
        {
            if (string.IsNullOrEmpty(asmdefPath))
            {
                continue;
            }

            AsmdefInfo data = ReadAsmdef(asmdefPath);
            if (data == null || string.IsNullOrEmpty(data.name))
            {
                continue;
            }

            string root = FindRootForPath(asmdefPath, pluginDirList);
            if (string.IsNullOrEmpty(root))
            {
                continue;
            }

            pluginAsmdefs[data.name] = root;
            string guid = AssetDatabase.AssetPathToGUID(asmdefPath);
            if (!string.IsNullOrEmpty(guid))
            {
                pluginAsmdefGuids[guid] = root;
            }
        }

        string[] allAsmdefPaths = AssetDatabase.FindAssets("t:asmdef")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            .Where(p => !IsUnderAnyRoot(p, pluginDirList))
            .ToArray();

        foreach (string asmdefPath in allAsmdefPaths)
        {
            AsmdefInfo data = ReadAsmdef(asmdefPath);
            if (data?.references == null)
            {
                continue;
            }

            foreach (string reference in data.references)
            {
                if (string.IsNullOrEmpty(reference))
                {
                    continue;
                }

                string key = reference;
                if (key.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                {
                    key = key.Substring("GUID:".Length);
                    if (pluginAsmdefGuids.TryGetValue(key, out string rootByGuid))
                    {
                        usedRoots.Add(rootByGuid);
                    }
                    continue;
                }

                if (pluginAsmdefs.TryGetValue(key, out string rootByName))
                {
                    usedRoots.Add(rootByName);
                }
            }
        }

        return usedRoots;
    }

    private static HashSet<string> CollectCodeTokenUsedRoots(IEnumerable<string> pluginDirs)
    {
        List<string> pluginDirList = pluginDirs.ToList();
        HashSet<string> usedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, List<string>> tokensByRoot = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in pluginDirList)
        {
            string name = Path.GetFileName(root);
            string[] rawTokens = TokenSplitRegex.Split(name);
            List<string> tokens = rawTokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => t.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tokens.Count == 0)
            {
                tokens.Add(name);
            }

            tokensByRoot[root] = tokens;
        }

        string[] codePaths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(p => !IsUnderAnyRoot(p, pluginDirList))
            .ToArray();

        foreach (string codePath in codePaths)
        {
            string fullPath = ToFullPath(codePath);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(fullPath);
            }
            catch
            {
                continue;
            }

            foreach (KeyValuePair<string, List<string>> pair in tokensByRoot)
            {
                if (usedRoots.Contains(pair.Key))
                {
                    continue;
                }

                foreach (string token in pair.Value)
                {
                    if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        usedRoots.Add(pair.Key);
                        break;
                    }
                }
            }
        }

        return usedRoots;
    }

    private static AsmdefInfo ReadAsmdef(string assetPath)
    {
        string fullPath = ToFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(fullPath);
            return JsonUtility.FromJson<AsmdefInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsUnderAnyRoot(string path, IEnumerable<string> roots)
    {
        foreach (string root in roots)
        {
            if (path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string FindRootForPath(string path, IEnumerable<string> roots)
    {
        foreach (string root in roots)
        {
            if (path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }
        }

        return null;
    }

    private static string BuildReason(PluginEntry entry)
    {
        List<string> reasons = new List<string>();
        if (entry.UsedByDependencies)
        {
            reasons.Add("has external asset dependencies");
        }

        if (entry.UsedByAsmdefReference)
        {
            reasons.Add("has asmdef references");
        }

        if (entry.UsedByCodeToken)
        {
            reasons.Add("code token matches found");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("no usage signals found");
        }

        return string.Join("; ", reasons);
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        string combined = Path.Combine(projectRoot, assetPath);
        return Path.GetFullPath(combined);
    }

    private static string ToAssetPath(string fullPath)
    {
        string normalized = fullPath.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
        {
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        return normalized;
    }
}
