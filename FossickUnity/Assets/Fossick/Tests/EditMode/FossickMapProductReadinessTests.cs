using System;
using System.Linq;
using Fossick.Core.Definition.Config;
using Fossick.Core.Definition.Serialization;
using Fossick.MapStudio.Validation;
using NUnit.Framework;

namespace Fossick.Tests.EditMode
{
    public sealed class FossickMapProductReadinessTests
    {
        [Test]
        public void ValidatorRequiresDifficultyCountsToMatchGroupSize()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.regularGroupSize = 10;
            config.generation.difficultyCounts.Clear();
            config.generation.difficultyCounts.Add(new FossickDifficultyCount { difficulty = 1, count = 3 });

            var result = FossickMapValidator.Validate(config);

            AssertHasError(result, "Difficulty counts total");
            Assert.That(
                result.issues.Any(issue => issue.category == FossickValidationCategory.GenerationRules),
                Is.True);
        }

        [Test]
        public void ValidatorRejectsBuriedToolsOnEmptyOrBedrockTerrain()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var fragment = config.fragments.First(item => item.type == FossickFragmentType.Regular);
            var cell = fragment.cells[0];
            cell.terrain = FossickTerrainType.Empty;
            cell.hp = 0;
            cell.reward = new FossickElementConfig
            {
                type = FossickElementType.Item,
                id = "pickaxe",
                amount = 1
            };

            var result = FossickMapValidator.Validate(config);

            AssertHasError(result, "Buried reward must be attached to diggable terrain.");
        }

        [Test]
        public void ValidatorRejectsPartialTreasureRoomRegions()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var fragment = config.fragments.First(item => item.type == FossickFragmentType.Reward);

            for (var i = 0; i < fragment.cells.Count; i++)
            {
                fragment.cells[i].rewardBackgroundId = null;
            }

            fragment.cells[0].rewardBackgroundId = "treasure_room_3x2";
            fragment.cells[1].rewardBackgroundId = "treasure_room_3x2";

            var result = FossickMapValidator.Validate(config);

            AssertHasError(result, "Reward background region shape is invalid.");
        }

        [Test]
        public void SplitJsonWithoutVersionIsNormalizedToCurrentVersion()
        {
            const string json = "{\"activity\":\"Fossick\",\"libraryId\":\"normalized\",\"boardWidth\":7,\"fragments\":[]}";

            var library = FossickMapJsonUtility.FragmentLibraryFromJson(json);

            Assert.That(library.version, Is.EqualTo(FossickMapJsonUtility.CurrentVersion));
        }

        [Test]
        public void SplitJsonWithFutureVersionIsRejected()
        {
            const string json = "{\"version\":999,\"activity\":\"Fossick\",\"libraryId\":\"future\",\"boardWidth\":7,\"fragments\":[]}";

            Assert.Throws<InvalidOperationException>(() => FossickMapJsonUtility.FragmentLibraryFromJson(json));
        }

        private static void AssertHasError(FossickValidationResult result, string messagePart)
        {
            Assert.That(result.issues.Any(issue =>
                    issue.severity == FossickValidationSeverity.Error
                    && issue.message.Contains(messagePart)),
                Is.True);
        }
    }
}
