using Fossick.Core.Definition.Config;
using Fossick.Core.Validation;
using Fossick.Core.Definition.Serialization;
using UnityEngine;

namespace Fossick.MapStudio.Controllers
{
    public sealed class FossickMapStudioController : MonoBehaviour
    {
        public FossickMapConfig CurrentConfig { get; private set; }
        public FossickValidationResult LastValidation { get; private set; }
        public string ActSubType => FossickMapEditorBridge.ActSubType;

        private void Awake()
        {
            if (TryLoadSplitProject())
            {
                Validate();
            }
            else if (TryLoadBundledProject())
            {
                Validate();
            }
            else
            {
                Debug.LogError("Map project config not found.");
                CurrentConfig = new FossickMapConfig();
                Validate();
            }
        }

        public void LoadProject(FossickMapProjectConfig project)
        {
            if (project == null)
            {
                CurrentConfig = new FossickMapConfig();
                Validate();
                return;
            }

            CurrentConfig = project.ToRuntimeConfig();
            Validate();
        }

        public FossickValidationResult Validate()
        {
            LastValidation = FossickMapValidator.Validate(CurrentConfig);
            return LastValidation;
        }

        private bool TryLoadSplitProject()
        {
            var project = FossickMapProjectFileService.LoadEditableProject(ActSubType);
            if (project == null)
            {
                return false;
            }

            LoadProject(project);
            return true;
        }

        private bool TryLoadBundledProject()
        {
            var project = LoadBundledProject();
            if (project == null)
            {
                return false;
            }

            LoadProject(project);
            FossickMapProjectFileService.SaveEditableProject(
                FossickMapProjectConfig.FromRuntimeConfig(CurrentConfig),
                ActSubType);
            return true;
        }

        private FossickMapProjectConfig LoadBundledProject()
        {
            var libraryText = LoadBundledText(FossickMapProjectFileService.FragmentLibraryFileName);
            var rulesText = LoadBundledText(FossickMapProjectFileService.GenerationRulesFileName);
            var definitionText = LoadBundledText(FossickMapProjectFileService.MapDefinitionFileName);
            if (string.IsNullOrEmpty(libraryText) || string.IsNullOrEmpty(rulesText) || string.IsNullOrEmpty(definitionText))
            {
                return null;
            }

            return FossickMapJsonUtility.NormalizeProject(new FossickMapProjectConfig
            {
                fragmentLibrary = FossickMapJsonUtility.FragmentLibraryFromJson(libraryText),
                generationRules = FossickMapJsonUtility.GenerationRulesFromJson(rulesText),
                mapDefinition = FossickMapJsonUtility.MapDefinitionFromJson(definitionText)
            });
        }

        private string LoadBundledText(string fileName)
        {
            if (FossickMapEditorBridge.LoadBundledText == null || string.IsNullOrEmpty(ActSubType))
            {
                return null;
            }

            var assetPath = $"Assets/Art/AbResources/Activity/Fossick/{ActSubType}/Map/Config/{fileName}";
            return FossickMapEditorBridge.LoadBundledText(assetPath);
        }
    }
}
