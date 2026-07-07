using Fossick.Core.Definition.Config;
using Fossick.Core.Definition.Serialization;
using NUnit.Framework;

namespace Fossick.Tests.EditMode
{
    public sealed class FossickSplitStorageTests
    {
        [Test]
        public void SplitProjectExportKeepsGeneratedOverridesOutOfAuthoringFiles()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.sequenceOverrides.Add(new FossickSequenceOverrideConfig
            {
                sequenceIndex = 5,
                fragment = config.fragments[0]
            });
            config.generation.rowOverrides.Add(new FossickRowOverrideConfig
            {
                startRow = 12,
                fragment = config.fragments[1]
            });

            var project = FossickMapProjectConfig.FromRuntimeConfig(config, 2468);
            var recomposed = project.ToRuntimeConfig();

            Assert.That(project.fragmentLibrary.fragments.Count, Is.EqualTo(config.fragments.Count));
            Assert.That(project.generationRules.generation.sequenceOverrides, Is.Empty);
            Assert.That(project.generationRules.generation.rowOverrides, Is.Empty);
            Assert.That(project.mapDefinition.seed, Is.EqualTo(2468));
            Assert.That(recomposed.fragments.Count, Is.EqualTo(config.fragments.Count));
            Assert.That(recomposed.generation.sequenceOverrides, Is.Empty);
            Assert.That(recomposed.generation.rowOverrides, Is.Empty);

            var mapJson = FossickMapJsonUtility.MapDefinitionToJson(project.mapDefinition);
            Assert.That(mapJson, Does.Not.Contain("sequenceOverrides"));
            Assert.That(mapJson, Does.Not.Contain("rowOverrides"));
        }

        [Test]
        public void SplitJsonFilesComposeIntoRuntimeMapConfig()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var project = FossickMapProjectConfig.FromRuntimeConfig(config, 1357);

            var libraryJson = FossickMapJsonUtility.FragmentLibraryToJson(project.fragmentLibrary);
            var rulesJson = FossickMapJsonUtility.GenerationRulesToJson(project.generationRules);
            var mapJson = FossickMapJsonUtility.MapDefinitionToJson(project.mapDefinition);

            var loadedProject = new FossickMapProjectConfig
            {
                fragmentLibrary = FossickMapJsonUtility.FragmentLibraryFromJson(libraryJson),
                generationRules = FossickMapJsonUtility.GenerationRulesFromJson(rulesJson),
                mapDefinition = FossickMapJsonUtility.MapDefinitionFromJson(mapJson)
            };
            var recomposed = loadedProject.ToRuntimeConfig();

            Assert.That(loadedProject.mapDefinition.seed, Is.EqualTo(1357));
            Assert.That(recomposed.boardWidth, Is.EqualTo(config.boardWidth));
            Assert.That(recomposed.visibleHeight, Is.EqualTo(config.visibleHeight));
            Assert.That(recomposed.fragments.Count, Is.EqualTo(config.fragments.Count));
            Assert.That(recomposed.generation.regularGroupSize, Is.EqualTo(config.generation.regularGroupSize));
        }
    }
}
