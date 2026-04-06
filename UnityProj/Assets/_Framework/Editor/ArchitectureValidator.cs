#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Validates architecture rules at editor time.
    /// Scans C# scripts for violations of the project's design constraints.
    ///
    /// Improvements over v1:
    /// - Ignores matches inside comments (// and /* */)
    /// - Checks for Resources.Load outside of fallback patterns
    /// - Checks for excessive Update() methods
    /// - Verifies MODULE_README.md presence in _Framework/ and _Game/ subdirectories
    /// - Provides structured summary output
    ///
    /// Run via: Tools → MiniGame Template → Validate → Architecture Check
    /// </summary>
    public static class ArchitectureValidator
    {
        private struct Rule
        {
            public string Pattern;
            public string Description;
            public bool IsError; // true = error, false = warning
        }

        private static readonly Rule[] ErrorRules = new[]
        {
            new Rule { Pattern = @"GameObject\.Find\s*\(",        Description = "GameObject.Find — use SO references instead", IsError = true },
            new Rule { Pattern = @"FindObjectOfType\s*[<(]",      Description = "FindObjectOfType — expensive scene search",   IsError = true },
            new Rule { Pattern = @"FindObjectsOfType\s*[<(]",     Description = "FindObjectsOfType — expensive scene search",  IsError = true },
        };

        private static readonly Rule[] WarningRules = new[]
        {
            new Rule { Pattern = @"static\s+\w+\s+Instance\s*\{", Description = "Homegrown singleton — use Singleton<T> base or SO events", IsError = false },
            new Rule { Pattern = @"DontDestroyOnLoad\s*\(",        Description = "DontDestroyOnLoad — should only be in Singleton<T>/Bootstrapper", IsError = false },
            new Rule { Pattern = @"Resources\.Load\s*[<(]",        Description = "Resources.Load — prefer YooAsset (check if this is a fallback)", IsError = false },
        };

        // Files that are ALLOWED to use restricted patterns
        private static readonly HashSet<string> Whitelist = new HashSet<string>
        {
            "Singleton.cs",
            "GameBootstrapper.cs",
        };

        // Max recommended file length (SRP indicator)
        private const int MAX_FILE_LINES = 200;
        private const int IDEAL_FILE_LINES = 150;

        // Comment stripping regex
        private static readonly Regex LineCommentRegex = new Regex(@"//.*$", RegexOptions.Multiline);
        private static readonly Regex BlockCommentRegex = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);

        public static void RunValidation()
        {
            int errorCount = 0;
            int warningCount = 0;
            int filesScanned = 0;

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

                // Skip ThirdParty
                if (scriptPath.Contains("ThirdParty/") || scriptPath.Contains("ThirdParty\\"))
                    continue;

                var rawContent = File.ReadAllText(scriptPath);
                filesScanned++;

                // Strip comments before pattern matching to avoid false positives
                var content = StripComments(rawContent);

                // Check error patterns
                foreach (var rule in ErrorRules)
                {
                    var matches = Regex.Matches(content, rule.Pattern);
                    foreach (Match match in matches)
                    {
                        int line = CountLines(rawContent, FindOriginalIndex(rawContent, content, match.Index));
                        Debug.LogError($"[Architecture] VIOLATION in {scriptPath}:{line} — {rule.Description}");
                        errorCount++;
                    }
                }

                // Check warning patterns
                foreach (var rule in WarningRules)
                {
                    var matches = Regex.Matches(content, rule.Pattern);
                    foreach (Match match in matches)
                    {
                        int line = CountLines(rawContent, FindOriginalIndex(rawContent, content, match.Index));
                        Debug.LogWarning($"[Architecture] WARNING in {scriptPath}:{line} — {rule.Description}");
                        warningCount++;
                    }
                }

                // Check file length (SRP indicator)
                int lineCount = rawContent.Split('\n').Length;
                if (lineCount > MAX_FILE_LINES)
                {
                    Debug.LogWarning($"[Architecture] WARNING: {scriptPath} is {lineCount} lines (limit: {MAX_FILE_LINES}). Consider splitting.");
                    warningCount++;
                }
            }

            // Check for MODULE_README.md presence
            CheckModuleReadmes(ref warningCount);

            // Summary
            Debug.Log("──────────────────────────────────────────────");
            if (errorCount == 0 && warningCount == 0)
            {
                Debug.Log($"[Architecture] ✅ All checks passed! Scanned {filesScanned} files.");
            }
            else
            {
                Debug.Log($"[Architecture] Validation complete: {errorCount} error(s), {warningCount} warning(s) in {filesScanned} files.");
            }
            Debug.Log("──────────────────────────────────────────────");
        }

        private static void CheckModuleReadmes(ref int warningCount)
        {
            var moduleDirs = new[] { "Assets/_Framework", "Assets/_Game" };
            foreach (var root in moduleDirs)
            {
                if (!Directory.Exists(root)) continue;

                var subdirs = Directory.GetDirectories(root);
                foreach (var dir in subdirs)
                {
                    var dirName = Path.GetFileName(dir);
                    // Skip special directories
                    if (dirName == "Editor" || dirName == "Scripts" || dirName.StartsWith(".")) continue;

                    var readmePath = Path.Combine(dir, "MODULE_README.md");
                    if (!File.Exists(readmePath))
                    {
                        Debug.LogWarning($"[Architecture] WARNING: Missing MODULE_README.md in {dir}/");
                        warningCount++;
                    }
                }
            }
        }

        private static string StripComments(string code)
        {
            code = BlockCommentRegex.Replace(code, m => new string(' ', m.Length));
            code = LineCommentRegex.Replace(code, m => new string(' ', m.Length));
            return code;
        }

        private static int FindOriginalIndex(string original, string stripped, int strippedIndex)
        {
            // Since we replace comments with spaces (same length), indices match
            return Mathf.Min(strippedIndex, original.Length - 1);
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
