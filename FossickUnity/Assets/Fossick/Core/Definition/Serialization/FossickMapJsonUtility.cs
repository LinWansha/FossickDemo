using Fossick.Core.Definition.Config;
using System;
using System.IO;
using UnityEngine;

namespace Fossick.Core.Definition.Serialization
{
    public static class FossickMapJsonUtility
    {
        public const int CurrentVersion = 1;

        public static string FragmentLibraryToJson(FossickFragmentLibraryConfig library, bool prettyPrint = true)
        {
            NormalizeFragmentLibrary(library);
            return JsonUtility.ToJson(library, prettyPrint);
        }

        public static FossickFragmentLibraryConfig FragmentLibraryFromJson(string json)
        {
            return NormalizeFragmentLibrary(JsonUtility.FromJson<FossickFragmentLibraryConfig>(json));
        }

        public static string GenerationRulesToJson(FossickGenerationRulesConfig rules, bool prettyPrint = true)
        {
            NormalizeGenerationRules(rules);
            return JsonUtility.ToJson(rules, prettyPrint);
        }

        public static FossickGenerationRulesConfig GenerationRulesFromJson(string json)
        {
            return NormalizeGenerationRules(JsonUtility.FromJson<FossickGenerationRulesConfig>(json));
        }

        public static string MapDefinitionToJson(FossickMapDefinitionConfig definition, bool prettyPrint = true)
        {
            NormalizeMapDefinition(definition);
            return JsonUtility.ToJson(definition, prettyPrint);
        }

        public static FossickMapDefinitionConfig MapDefinitionFromJson(string json)
        {
            return NormalizeMapDefinition(JsonUtility.FromJson<FossickMapDefinitionConfig>(json));
        }

        public static FossickMapProjectConfig NormalizeProject(FossickMapProjectConfig project)
        {
            if (project == null)
            {
                return null;
            }

            EnsureSupportedVersion(project.version, "Fossick 项目");
            project.version = NormalizeVersion(project.version);
            project.activity = NormalizeActivity(project.activity);
            project.fragmentLibrary = NormalizeFragmentLibrary(project.fragmentLibrary ?? new FossickFragmentLibraryConfig());
            project.generationRules = NormalizeGenerationRules(project.generationRules ?? new FossickGenerationRulesConfig());
            project.mapDefinition = NormalizeMapDefinition(project.mapDefinition ?? new FossickMapDefinitionConfig());
            project.fragmentLibrary.activity = project.activity;
            project.generationRules.activity = project.activity;
            project.mapDefinition.activity = project.activity;
            return project;
        }

        private static FossickFragmentLibraryConfig NormalizeFragmentLibrary(FossickFragmentLibraryConfig library)
        {
            if (library == null)
            {
                return null;
            }

            EnsureSupportedVersion(library.version, "Fossick 碎片库");
            library.version = NormalizeVersion(library.version);
            library.activity = NormalizeActivity(library.activity);
            return library;
        }

        private static FossickGenerationRulesConfig NormalizeGenerationRules(FossickGenerationRulesConfig rules)
        {
            if (rules == null)
            {
                return null;
            }

            EnsureSupportedVersion(rules.version, "Fossick 生成规则");
            rules.version = NormalizeVersion(rules.version);
            rules.activity = NormalizeActivity(rules.activity);
            rules.generation = rules.generation ?? new FossickGenerationConfig();
            rules.tools = rules.tools ?? new FossickToolRulesConfig();
            rules.visual = rules.visual ?? new FossickVisualConfig();
            if (rules.generation.sequenceOverrides == null)
            {
                rules.generation.sequenceOverrides = new System.Collections.Generic.List<FossickSequenceOverrideConfig>();
            }
            else
            {
                rules.generation.sequenceOverrides.Clear();
            }

            if (rules.generation.rowOverrides == null)
            {
                rules.generation.rowOverrides = new System.Collections.Generic.List<FossickRowOverrideConfig>();
            }
            else
            {
                rules.generation.rowOverrides.Clear();
            }

            return rules;
        }

        private static FossickMapDefinitionConfig NormalizeMapDefinition(FossickMapDefinitionConfig definition)
        {
            if (definition == null)
            {
                return null;
            }

            EnsureSupportedVersion(definition.version, "Fossick 地图定义");
            definition.version = NormalizeVersion(definition.version);
            definition.activity = NormalizeActivity(definition.activity);
            return definition;
        }

        private static int NormalizeVersion(int version)
        {
            return version <= 0 ? CurrentVersion : version;
        }

        private static string NormalizeActivity(string activity)
        {
            return string.IsNullOrEmpty(activity) ? "Fossick" : activity;
        }

        private static void EnsureSupportedVersion(int version, string label)
        {
            if (version > CurrentVersion)
            {
                throw new InvalidOperationException($"{label}版本 {version} 高于当前编辑器支持版本 {CurrentVersion}。");
            }
        }
    }

    public static class FossickMapProjectFileService
    {
        public const string FragmentLibraryFileName = "FossickFragmentLibrary.json";
        public const string GenerationRulesFileName = "FossickGenerationRules.json";
        public const string MapDefinitionFileName = "FossickMapDefinition.json";
        public const string RelativeMapsFolder = "Fossick/MapStudio/Maps";

        public static string GetEditableMapsFolder()
        {
#if UNITY_EDITOR
            return Path.Combine(UnityEngine.Application.dataPath, "Fossick/MapStudio/Maps");
#else
            return GetPlayerWritableMapsFolder();
#endif
        }

        public static string GetPlayerWritableMapsFolder()
        {
            return Path.Combine(UnityEngine.Application.persistentDataPath, RelativeMapsFolder);
        }

        public static string GetBundledPlayerMapsFolder()
        {
            return Path.Combine(UnityEngine.Application.streamingAssetsPath, RelativeMapsFolder);
        }

        public static void EnsurePlayerEditableProject()
        {
#if UNITY_EDITOR
            return;
#else
            var target = GetPlayerWritableMapsFolder();
            if (HasSplitProject(target))
            {
                return;
            }

            var source = GetBundledPlayerMapsFolder();
            if (!HasSplitProject(source))
            {
                return;
            }

            Directory.CreateDirectory(target);
            CopyJson(source, target, FragmentLibraryFileName);
            CopyJson(source, target, GenerationRulesFileName);
            CopyJson(source, target, MapDefinitionFileName);
#endif
        }

        public static FossickMapProjectConfig LoadEditableProject()
        {
#if !UNITY_EDITOR
            EnsurePlayerEditableProject();
#endif
            return LoadSplitProject(GetEditableMapsFolder());
        }

        public static FossickMapProjectConfig LoadSplitProject(string folder)
        {
            var libraryPath = Path.Combine(folder, FragmentLibraryFileName);
            var rulesPath = Path.Combine(folder, GenerationRulesFileName);
            var definitionPath = Path.Combine(folder, MapDefinitionFileName);

            if (!File.Exists(libraryPath) || !File.Exists(rulesPath) || !File.Exists(definitionPath))
            {
                return null;
            }

            return FossickMapJsonUtility.NormalizeProject(new FossickMapProjectConfig
            {
                fragmentLibrary = FossickMapJsonUtility.FragmentLibraryFromJson(File.ReadAllText(libraryPath)),
                generationRules = FossickMapJsonUtility.GenerationRulesFromJson(File.ReadAllText(rulesPath)),
                mapDefinition = FossickMapJsonUtility.MapDefinitionFromJson(File.ReadAllText(definitionPath))
            });
        }

        public static void SaveEditableProject(FossickMapProjectConfig project)
        {
            SaveSplitProject(GetEditableMapsFolder(), project);
        }

        public static void SaveSplitProject(string folder, FossickMapProjectConfig project)
        {
            Directory.CreateDirectory(folder);
            project = FossickMapJsonUtility.NormalizeProject(project);

            File.WriteAllText(
                Path.Combine(folder, FragmentLibraryFileName),
                FossickMapJsonUtility.FragmentLibraryToJson(project.fragmentLibrary));

            File.WriteAllText(
                Path.Combine(folder, GenerationRulesFileName),
                FossickMapJsonUtility.GenerationRulesToJson(project.generationRules));

            File.WriteAllText(
                Path.Combine(folder, MapDefinitionFileName),
                FossickMapJsonUtility.MapDefinitionToJson(project.mapDefinition));
        }

        public static bool HasSplitProject(string folder)
        {
            return !string.IsNullOrEmpty(folder)
                && File.Exists(Path.Combine(folder, FragmentLibraryFileName))
                && File.Exists(Path.Combine(folder, GenerationRulesFileName))
                && File.Exists(Path.Combine(folder, MapDefinitionFileName));
        }

        private static void CopyJson(string sourceDirectory, string targetDirectory, string fileName)
        {
            File.Copy(Path.Combine(sourceDirectory, fileName), Path.Combine(targetDirectory, fileName), true);
        }
    }
}
