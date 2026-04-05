#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Validates architecture rules at editor time.
    /// Scans C# scripts for violations of the project's design constraints.
    /// Run via: Tools → MiniGame Template → Validate Architecture
    /// </summary>
    public static class ArchitectureValidator
    {
        private static readonly string[] ForbiddenPatterns = new[]
        {
            @"GameObject\.Find\s*\(",
            @"FindObjectOfType\s*[<(]",
            @"FindObjectsOfType\s*[<(]",
            @"FindObjectOfType\s*\(",
            @"FindObjectsOfType\s*\(",
        };

        private static readonly string[] WarningPatterns = new[]
        {
            @"static\s+\w+\s+Instance\s*{",   // Homegrown singletons
            @"DontDestroyOnLoad\s*\(",          // Should only be in Singleton<T> and Bootstrapper
        };

        // Files that are ALLOWED to use these patterns
        private static readonly HashSet<string> Whitelist = new HashSet<string>
        {
            "Singleton.cs",
            "GameBootstrapper.cs",
        };

        [MenuItem("Tools/MiniGame Template/Validate Architecture")]
        public static void RunValidation()
        {
            int errorCount = 0;
            int warningCount = 0;

            var scripts = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);

            foreach (var scriptPath in scripts)
            {
                var fileName = Path.GetFileName(scriptPath);

                // Skip Editor scripts (they can use Find for tooling)
                if (scriptPath.Contains("/Editor/") || scriptPath.Contains("\\Editor\\"))
                    continue;

                // Skip whitelisted files
                if (Whitelist.Contains(fileName))
                    continue;

                var content = File.ReadAllText(scriptPath);

                // Check forbidden patterns
                foreach (var pattern in ForbiddenPatterns)
                {
                    var matches = Regex.Matches(content, pattern);
                    foreach (Match match in matches)
                    {
                        int line = CountLines(content, match.Index);
                        Debug.LogError($"[Architecture] VIOLATION in {scriptPath}:{line} — Found forbidden pattern: {match.Value.Trim()}");
                        errorCount++;
                    }
                }

                // Check warning patterns
                foreach (var pattern in WarningPatterns)
                {
                    var matches = Regex.Matches(content, pattern);
                    foreach (Match match in matches)
                    {
                        int line = CountLines(content, match.Index);
                        Debug.LogWarning($"[Architecture] WARNING in {scriptPath}:{line} — Suspicious pattern: {match.Value.Trim()} (should this use SO events instead?)");
                        warningCount++;
                    }
                }

                // Check file length (SRP indicator)
                int lineCount = content.Split('\n').Length;
                if (lineCount > 200)
                {
                    Debug.LogWarning($"[Architecture] WARNING: {scriptPath} is {lineCount} lines. Consider splitting (SRP guideline: <150 lines).");
                    warningCount++;
                }
            }

            if (errorCount == 0 && warningCount == 0)
            {
                Debug.Log("[Architecture] ✅ All checks passed! No violations found.");
            }
            else
            {
                Debug.Log($"[Architecture] Validation complete: {errorCount} error(s), {warningCount} warning(s).");
            }
        }

        private static int CountLines(string text, int charIndex)
        {
            int lines = 1;
            for (int i = 0; i < charIndex && i < text.Length; i++)
            {
                if (text[i] == '\n') lines++;
            }
            return lines;
        }
    }
}
#endif
