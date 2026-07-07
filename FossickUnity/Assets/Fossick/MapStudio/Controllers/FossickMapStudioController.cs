using Fossick.Core.Definition.Config;
using Fossick.Core.Definition.Serialization;
using Fossick.Core.Definition.Validation;
using System;
using UnityEngine;

namespace Fossick.MapStudio.Controllers
{
    public sealed class FossickMapStudioController : MonoBehaviour
    {
        [SerializeField] private TextAsset initialMapJson;
        [SerializeField] private int seed = 12345;

        public FossickMapConfig CurrentConfig { get; private set; }
        public FossickValidationResult LastValidation { get; private set; }
        public int Seed => seed;

        private void Awake()
        {
            if (initialMapJson != null)
            {
                LoadJson(initialMapJson.text);
            }
            else if (TryLoadSplitProject())
            {
                Validate();
            }
            else
            {
                CurrentConfig = FossickSampleMapFactory.CreateDefaultConfig();
                Validate();
            }
        }

        public void LoadJson(string json)
        {
            CurrentConfig = FossickMapJsonUtility.FromJson(json);
            Validate();
        }

        public string ExportJson()
        {
            return FossickMapJsonUtility.ToJson(CurrentConfig);
        }

        public void LoadProject(FossickMapProjectConfig project)
        {
            if (project == null)
            {
                CurrentConfig = FossickSampleMapFactory.CreateDefaultConfig();
                Validate();
                return;
            }

            CurrentConfig = project.ToRuntimeConfig();
            if (project.mapDefinition != null)
            {
                seed = project.mapDefinition.seed;
            }

            Validate();
        }

        public void SetSeed(int value)
        {
            seed = value;
        }

        public int RandomizeSeed()
        {
            seed = Math.Abs(Guid.NewGuid().GetHashCode());
            if (seed == 0)
            {
                seed = 1;
            }

            return seed;
        }

        public FossickValidationResult Validate()
        {
            LastValidation = FossickMapValidator.Validate(CurrentConfig);
            return LastValidation;
        }

        private bool TryLoadSplitProject()
        {
            var project = FossickMapProjectFileService.LoadEditableProject();
            if (project == null)
            {
                return false;
            }

            LoadProject(project);
            return true;
        }
    }
}
