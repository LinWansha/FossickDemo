using Fossick.Core.Actions;
using Fossick.Core.Board;
using Fossick.Core.Config;
using Fossick.Core.Generation;
using Fossick.Core.Rewards;
using Fossick.Core.Save;
using NUnit.Framework;

namespace Fossick.Core.Tests
{
    public sealed class FossickBoardActionTests
    {
        [Test]
        public void Board_AppendsGeneratedMineRowsWithAppliedMapContent()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.rowOverrides.Add(new FossickRowOverrideConfig
            {
                startRow = 7,
                fragment = CreateRewardFragment(9101)
            });
            var mine = FossickMineLayoutBuilder.Build(config, 12345, 12);
            var board = new FossickBoard(config.BoardSpec);

            board.AppendGeneratedMine(mine);

            var cell = board.GetCell(0, 7);
            Assert.That(cell.terrain, Is.EqualTo(FossickTerrainType.Empty));
        }

        [Test]
        public void Pickaxe_WhenDirtBreaks_RevealsBuriedRewardEntityBeforeCollection()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var fragment = CreateRewardFragment(9101);
            fragment.cells[0].terrain = FossickTerrainType.Dirt;
            fragment.cells[0].hp = 1;
            fragment.cells[0].reward = new FossickElementConfig
            {
                type = FossickElementType.Ore,
                id = "test_ore",
                amount = 3
            };
            config.fragments.Clear();
            config.fragments.Add(fragment);
            var mine = FossickMineLayoutBuilder.Build(config, 12345, 6);
            var board = new FossickBoard(config.BoardSpec);
            board.AppendGeneratedMine(mine);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var breakResult = resolver.ResolvePickaxe(board, 0, 0);
            progress.Apply(breakResult);

            Assert.That(breakResult.isApplied, Is.True);
            Assert.That(breakResult.toolConsumed, Is.True);
            Assert.That(breakResult.invalidReason, Is.Null);
            Assert.That(breakResult.rewards, Is.Empty);
            Assert.That(breakResult.steps[0].type, Is.EqualTo(FossickActionStepType.ToolConsumed));
            Assert.That(breakResult.steps[1].type, Is.EqualTo(FossickActionStepType.ObstacleHit));
            Assert.That(breakResult.steps[2].type, Is.EqualTo(FossickActionStepType.ObstacleBroken));
            Assert.That(breakResult.steps[3].type, Is.EqualTo(FossickActionStepType.RewardRevealed));
            Assert.That(board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(0, 0).HasSpawnedReward, Is.True);
            Assert.That(board.GetCell(0, 0).collected, Is.False);
            Assert.That(progress.oreFound, Is.EqualTo(0));
            Assert.That(progress.toolUsed, Is.EqualTo(1));

            var collectResult = resolver.ResolvePickaxe(board, 0, 0);
            progress.Apply(collectResult);

            Assert.That(collectResult.isApplied, Is.True);
            Assert.That(collectResult.toolConsumed, Is.False);
            Assert.That(collectResult.rewards.Count, Is.EqualTo(1));
            Assert.That(collectResult.rewards[0].elementType, Is.EqualTo(FossickElementType.Ore));
            Assert.That(collectResult.rewards[0].amount, Is.EqualTo(3));
            Assert.That(collectResult.steps[0].type, Is.EqualTo(FossickActionStepType.RewardCollected));
            Assert.That(board.GetCell(0, 0).HasSpawnedReward, Is.False);
            Assert.That(board.GetCell(0, 0).collected, Is.True);
            Assert.That(progress.oreFound, Is.EqualTo(3));
            Assert.That(progress.toolUsed, Is.EqualTo(1));
        }

        [Test]
        public void Pickaxe_WhenObstacleBreaks_RemovesFogFromBrokenCell()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            SetFog(board, 0, 0, FossickFogType.None);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(0, 0).fog, Is.EqualTo(FossickFogType.None));
            Assert.That(result.cellDeltas[0].fogBefore, Is.EqualTo(FossickFogType.None));
            Assert.That(result.cellDeltas[0].fogAfter, Is.EqualTo(FossickFogType.None));
        }

        [Test]
        public void Pickaxe_WhenTargetIsCoveredByFog_DoesNotConsumeTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            SetFog(board, 0, 0, FossickFogType.Covered);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var result = resolver.ResolvePickaxe(board, 0, 0);
            progress.Apply(result);

            Assert.That(result.isApplied, Is.False);
            Assert.That(result.toolConsumed, Is.False);
            Assert.That(result.invalidReason, Is.Not.Empty);
            Assert.That(result.steps.Count, Is.EqualTo(1));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
            Assert.That(board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Dirt));
            Assert.That(progress.toolUsed, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_WhenOpeningConnectsCoveredEmptyCavity_RevealsConnectedCavityAndAdjacentObstacles()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillVisibleRow(board, 0, FossickTerrainType.Empty, 0);
            SetCell(board, 0, 1, FossickTerrainType.Dirt, 1);
            SetCell(board, 0, 2, FossickTerrainType.Empty, 0);
            SetCell(board, 1, 2, FossickTerrainType.Empty, 0);
            SetCell(board, 2, 2, FossickTerrainType.Stone, 2);
            SetFog(board, 0, 1, FossickFogType.None);
            SetFog(board, 0, 2, FossickFogType.Covered);
            SetFog(board, 1, 2, FossickFogType.Covered);
            SetFog(board, 2, 2, FossickFogType.Covered);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 1);

            Assert.That(board.GetCell(0, 1).fog, Is.EqualTo(FossickFogType.None));
            Assert.That(board.GetCell(0, 2).fog, Is.EqualTo(FossickFogType.None));
            Assert.That(board.GetCell(1, 2).fog, Is.EqualTo(FossickFogType.None));
            Assert.That(board.GetCell(2, 2).fog, Is.EqualTo(FossickFogType.None));
            Assert.That(CountSteps(result, FossickActionStepType.FogRevealed), Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void RefreshFog_WhenBasicOpenSpaceTouchesVisibleBottom_RevealsBottomRow()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            for (var y = 0; y < board.Spec.visibleHeight - 1; y++)
            {
                FillVisibleRow(board, y, FossickTerrainType.Empty, 0);
            }

            FillVisibleRow(board, board.Spec.visibleHeight - 1, FossickTerrainType.Dirt, 1);
            for (var x = 0; x < board.Spec.width; x++)
            {
                SetFog(board, x, board.Spec.visibleHeight - 1, FossickFogType.Covered);
            }

            board.RefreshFogFromOpenSpace();

            for (var x = 0; x < board.Spec.width; x++)
            {
                Assert.That(board.GetCell(x, board.Spec.visibleHeight - 1).fog, Is.EqualTo(FossickFogType.None));
            }
        }

        [Test]
        public void RefreshFog_WhenScrolledWindowHasVisibleOpenSpaceAboveBottom_RevealsNewBottomRow()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillAbsoluteRow(board, 5, FossickTerrainType.Empty, 0);
            FillAbsoluteRow(board, 6, FossickTerrainType.Empty, 0);
            FillAbsoluteRow(board, 7, FossickTerrainType.Empty, 0);
            for (var x = 0; x < board.Spec.width; x++)
            {
                SetAbsoluteFog(board, x, 5, FossickFogType.None);
                SetAbsoluteFog(board, x, 6, FossickFogType.None);
                SetAbsoluteFog(board, x, 7, FossickFogType.Covered);
            }

            Assert.That(board.TryScrollDown(), Is.True);
            Assert.That(board.TryScrollDown(), Is.True);
            Assert.That(board.TopVisibleRow, Is.EqualTo(2));

            board.RefreshFogFromOpenSpace();

            for (var x = 0; x < board.Spec.width; x++)
            {
                Assert.That(board.GetCellAtAbsoluteRow(x, 7).fog, Is.EqualTo(FossickFogType.None));
            }
        }

        [Test]
        public void Pickaxe_WhenStoneHasHpRemaining_OnlyHitsObstacle()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Stone, 2, null);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);
            var cell = board.GetCell(0, 0);

            Assert.That(cell.terrain, Is.EqualTo(FossickTerrainType.Stone));
            Assert.That(cell.hp, Is.EqualTo(1));
            Assert.That(result.steps.Count, Is.EqualTo(2));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.ToolConsumed));
            Assert.That(result.steps[1].type, Is.EqualTo(FossickActionStepType.ObstacleHit));
            Assert.That(result.rewards, Is.Empty);
        }

        [Test]
        public void Pickaxe_WhenCellIsEmpty_DoesNotConsumeTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var result = resolver.ResolvePickaxe(board, 0, 0);
            progress.Apply(result);

            Assert.That(result.steps.Count, Is.EqualTo(1));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
            Assert.That(progress.toolUsed, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_WhenCellIsUnbreakable_DoesNotConsumeTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Unbreakable, 0, null);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var result = resolver.ResolvePickaxe(board, 0, 0);
            progress.Apply(result);

            Assert.That(result.steps.Count, Is.EqualTo(1));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
            Assert.That(board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Unbreakable));
            Assert.That(progress.toolUsed, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_WhenBottomRowHasNoVisibleEmptyCell_DoesNotScroll()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(result.scrolled, Is.False);
            Assert.That(result.depthAfterAction, Is.EqualTo(0));
            Assert.That(board.TopVisibleRow, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_WhenBottomRowHasVisibleEmptyCell_EmitsScrollStep()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            SetCell(board, 3, 5, FossickTerrainType.Empty, 0);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(result.scrolled, Is.True);
            Assert.That(result.depthAfterAction, Is.EqualTo(1));
            Assert.That(result.steps[result.steps.Count - 1].type, Is.EqualTo(FossickActionStepType.BoardScrolled));
        }

        [Test]
        public void Pickaxe_WhenTargetIsInvalidButBottomRowHasVisibleEmptyCell_DoesNotScroll()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            SetCell(board, 3, 5, FossickTerrainType.Empty, 0);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
            Assert.That(result.scrolled, Is.False);
            Assert.That(board.TopVisibleRow, Is.EqualTo(0));
            Assert.That(result.depthAfterAction, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_WhenTopRowBecomesClearButBottomHasNoVisibleEmptyCell_DoesNotScroll()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillVisibleRow(board, 1, FossickTerrainType.Dirt, 1);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(result.scrolled, Is.False);
            Assert.That(board.TopVisibleRow, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_WhenBottomEmptyCellIsCoveredByFog_DoesNotScroll()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            SetCell(board, 3, 5, FossickTerrainType.Empty, 0);
            SetFog(board, 3, 5, FossickFogType.Covered);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(result.scrolled, Is.False);
            Assert.That(board.TopVisibleRow, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_WhenScrollConditionRemainsTrue_ScrollsUntilBlocked()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillAbsoluteRow(board, 5, FossickTerrainType.Dirt, 1);
            SetAbsoluteCell(board, 3, 5, FossickTerrainType.Empty, 0);
            FillAbsoluteRow(board, 6, FossickTerrainType.Dirt, 1);
            SetAbsoluteCell(board, 3, 6, FossickTerrainType.Empty, 0);
            FillAbsoluteRow(board, 7, FossickTerrainType.Dirt, 1);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(board.TopVisibleRow, Is.EqualTo(2));
            Assert.That(result.depthAfterAction, Is.EqualTo(2));
            Assert.That(CountSteps(result, FossickActionStepType.BoardScrolled), Is.EqualTo(2));
        }

        [Test]
        public void ToolPreview_DynamiteCoversVisibleRow()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            var resolver = new FossickActionResolver();

            var targets = resolver.GetToolPreview(board, FossickToolType.Dynamite, 0, 0);

            Assert.That(targets.Count, Is.EqualTo(board.Spec.width));
            Assert.That(ContainsTarget(targets, 0, 0), Is.True);
            Assert.That(ContainsTarget(targets, 1, 0), Is.True);
            Assert.That(ContainsTarget(targets, 6, 0), Is.True);
            Assert.That(ContainsTarget(targets, 0, 1), Is.False);
        }

        [Test]
        public void Dynamite_WhenCenterIsCoveredByFog_DoesNotConsumeTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            SetFog(board, 0, 0, FossickFogType.Covered);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var result = resolver.ResolveTool(board, FossickToolType.Dynamite, 0, 0);
            progress.Apply(result);

            Assert.That(result.steps.Count, Is.EqualTo(1));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
            Assert.That(progress.toolUsed, Is.EqualTo(0));
        }

        [Test]
        public void Dynamite_WhenCenterIsObstacle_DoesNotConsumeTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            SetFog(board, 0, 0, FossickFogType.None);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var result = resolver.ResolveTool(board, FossickToolType.Dynamite, 0, 0);
            progress.Apply(result);

            Assert.That(result.steps.Count, Is.EqualTo(1));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
            Assert.That(progress.toolUsed, Is.EqualTo(0));
        }

        [Test]
        public void Dynamite_WhenCenterIsObstacleButBottomRowHasVisibleEmptyCell_DoesNotScroll()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            SetCell(board, 3, 5, FossickTerrainType.Empty, 0);
            SetFog(board, 0, 0, FossickFogType.None);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolveTool(board, FossickToolType.Dynamite, 0, 0);

            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
            Assert.That(result.scrolled, Is.False);
            Assert.That(board.TopVisibleRow, Is.EqualTo(0));
            Assert.That(result.depthAfterAction, Is.EqualTo(0));
        }

        [Test]
        public void ToolPreview_TntAtCorner_ClipsToBoard()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            var resolver = new FossickActionResolver();

            var targets = resolver.GetToolPreview(board, FossickToolType.Tnt, 0, 0);

            Assert.That(targets.Count, Is.EqualTo(4));
            Assert.That(ContainsTarget(targets, 0, 0), Is.True);
            Assert.That(ContainsTarget(targets, 1, 0), Is.True);
            Assert.That(ContainsTarget(targets, 0, 1), Is.True);
            Assert.That(ContainsTarget(targets, 1, 1), Is.True);
        }

        [Test]
        public void ToolPreview_RadarDoesNotExposePlacedPreviewTargets()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            var resolver = new FossickActionResolver();

            var targets = resolver.GetToolPreview(board, FossickToolType.Radar, 2, 2);

            Assert.That(targets, Is.Empty);
        }

        [Test]
        public void Dynamite_DamagesVisibleRowButConsumesOneTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            for (var x = 0; x < board.Spec.width; x++)
            {
                SetCell(board, x, 2, FossickTerrainType.Dirt, 1);
            }

            SetCell(board, 3, 2, FossickTerrainType.Empty, 0);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var result = resolver.ResolveTool(board, FossickToolType.Dynamite, 3, 2);
            progress.Apply(result);

            for (var x = 0; x < board.Spec.width; x++)
            {
                Assert.That(board.GetCell(x, 2).terrain, Is.EqualTo(FossickTerrainType.Empty));
            }

            Assert.That(result.cellDeltas.Count, Is.EqualTo(board.Spec.width - 1));
            Assert.That(CountSteps(result, FossickActionStepType.ToolConsumed), Is.EqualTo(1));
            Assert.That(progress.toolUsed, Is.EqualTo(1));
        }

        [Test]
        public void Dynamite_TwoHpObstacleBlocksCellsBehindIt()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            SetCell(board, 2, 2, FossickTerrainType.Dirt, 1);
            SetCell(board, 4, 2, FossickTerrainType.Stone, 2);
            SetCell(board, 5, 2, FossickTerrainType.Dirt, 1);
            SetCell(board, 6, 2, FossickTerrainType.Dirt, 1);
            var resolver = new FossickActionResolver();

            var targets = resolver.GetToolPreview(board, FossickToolType.Dynamite, 3, 2);
            var result = resolver.ResolveTool(board, FossickToolType.Dynamite, 3, 2);

            Assert.That(ContainsTarget(targets, 2, 2), Is.True);
            Assert.That(ContainsTarget(targets, 4, 2), Is.True);
            Assert.That(ContainsTarget(targets, 5, 2), Is.False);
            Assert.That(board.GetCell(2, 2).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(4, 2).terrain, Is.EqualTo(FossickTerrainType.Stone));
            Assert.That(board.GetCell(4, 2).hp, Is.EqualTo(1));
            Assert.That(board.GetCell(5, 2).terrain, Is.EqualTo(FossickTerrainType.Dirt));
            Assert.That(board.GetCell(6, 2).terrain, Is.EqualTo(FossickTerrainType.Dirt));
        }

        [Test]
        public void Tnt_AppliesHitToEveryPreviewTargetButConsumesOneTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            for (var y = 0; y <= 1; y++)
            {
                for (var x = 0; x <= 1; x++)
                {
                    SetCell(board, x, y, FossickTerrainType.Dirt, 1);
                }
            }

            SetCell(board, 0, 0, FossickTerrainType.Empty, 0);
            var resolver = new FossickActionResolver();
            var progress = new FossickProgressState();

            var result = resolver.ResolveTool(board, FossickToolType.Tnt, 0, 0);
            progress.Apply(result);

            Assert.That(board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(1, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(0, 1).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(1, 1).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(result.cellDeltas.Count, Is.EqualTo(3));
            Assert.That(CountSteps(result, FossickActionStepType.ToolConsumed), Is.EqualTo(1));
            Assert.That(progress.toolUsed, Is.EqualTo(1));
        }

        [Test]
        public void Tnt_DamagesThreeByThreeAreaForTwoHpWithoutBlocking()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            SetCell(board, 1, 1, FossickTerrainType.Stone, 2);
            SetCell(board, 2, 1, FossickTerrainType.Stone, 3);
            SetCell(board, 3, 1, FossickTerrainType.Dirt, 1);
            SetCell(board, 2, 2, FossickTerrainType.Empty, 0);
            SetCell(board, 3, 2, FossickTerrainType.Dirt, 1);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolveTool(board, FossickToolType.Tnt, 2, 2);

            Assert.That(board.GetCell(1, 1).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(2, 1).terrain, Is.EqualTo(FossickTerrainType.Stone));
            Assert.That(board.GetCell(2, 1).hp, Is.EqualTo(1));
            Assert.That(board.GetCell(3, 1).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(3, 2).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(CountSteps(result, FossickActionStepType.ObstacleHit), Is.EqualTo(4));
        }

        [Test]
        public void Radar_ClearsFogInPreviewTargetsWithoutDamagingTerrain()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            SetCell(board, 2, 2, FossickTerrainType.Stone, 2);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            for (var y = 0; y < board.Spec.visibleHeight; y++)
            {
                for (var x = 0; x < board.Spec.width; x++)
                {
                    SetFog(board, x, y, FossickFogType.Covered);
                }
            }

            var resolver = new FossickActionResolver();

            var result = resolver.ResolveTool(board, FossickToolType.Radar, 2, 2);

            Assert.That(result.isApplied, Is.True);
            Assert.That(result.toolConsumed, Is.True);
            for (var y = 0; y < board.Spec.visibleHeight; y++)
            {
                for (var x = 0; x < board.Spec.width; x++)
                {
                    Assert.That(board.GetCell(x, y).fog, Is.EqualTo(FossickFogType.None));
                    Assert.That(board.GetCell(x, y).IsContentVisible, Is.True);
                }
            }

            Assert.That(board.GetCell(2, 2).terrain, Is.EqualTo(FossickTerrainType.Stone));
            Assert.That(board.GetCell(2, 2).hp, Is.EqualTo(2));
            Assert.That(result.scrolled, Is.False);
            Assert.That(result.cellDeltas.Count, Is.EqualTo(board.Spec.width * board.Spec.visibleHeight));
            Assert.That(CountSteps(result, FossickActionStepType.ToolConsumed), Is.EqualTo(1));
            Assert.That(CountSteps(result, FossickActionStepType.RadarScanned), Is.EqualTo(board.Spec.width * board.Spec.visibleHeight));
            Assert.That(CountSteps(result, FossickActionStepType.FogRevealed), Is.EqualTo(0));
        }

        [Test]
        public void Radar_DoesNotRequireAPlacedTarget()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            SetFog(board, 3, 3, FossickFogType.Covered);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolveTool(board, FossickToolType.Radar, -1, -1);

            Assert.That(board.GetCell(3, 3).fog, Is.EqualTo(FossickFogType.None));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.ToolConsumed));
        }

        [Test]
        public void Radar_WhenBottomRowBecomesVisible_EmitsScrollStep()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Empty, 0, null);
            FillVisibleRow(board, 5, FossickTerrainType.Dirt, 1);
            FillAbsoluteRow(board, 6, FossickTerrainType.Dirt, 1);
            SetCell(board, 3, 5, FossickTerrainType.Empty, 0);
            SetFog(board, 3, 5, FossickFogType.Covered);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolveTool(board, FossickToolType.Radar, 0, 0);

            Assert.That(result.scrolled, Is.True);
            Assert.That(board.TopVisibleRow, Is.EqualTo(1));
            Assert.That(result.depthAfterAction, Is.EqualTo(1));
            Assert.That(CountSteps(result, FossickActionStepType.BoardScrolled), Is.EqualTo(1));
        }

        [Test]
        public void Pickaxe_WhenTargetIsVisible_CanConsumeTool()
        {
            var board = CreateBoardWithSingleCell(FossickTerrainType.Dirt, 1, null);
            SetFog(board, 0, 0, FossickFogType.None);
            var resolver = new FossickActionResolver();

            var result = resolver.ResolvePickaxe(board, 0, 0);

            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.ToolConsumed));
            Assert.That(board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(board.GetCell(0, 0).fog, Is.EqualTo(FossickFogType.None));
        }

        [Test]
        public void Board_SaveAndRestorePreservesDestroyedCellsCollectedRewardsAndDepth()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var fragment = CreateRewardFragment(9301);
            fragment.cells[0].terrain = FossickTerrainType.Dirt;
            fragment.cells[0].hp = 1;
            fragment.cells[0].reward = new FossickElementConfig
            {
                type = FossickElementType.Ore,
                id = "restore_ore",
                amount = 5
            };
            fragment.cells[2 + 2 * fragment.width].fog = FossickFogType.Covered;
            config.fragments.Clear();
            config.fragments.Add(fragment);
            var mine = FossickMineLayoutBuilder.Build(config, 2468, 8);
            var board = new FossickBoard(config.BoardSpec);
            board.AppendGeneratedMine(mine);
            board.GetCell(2, 2).fog = FossickFogType.None;
            var progress = new FossickProgressState();
            progress.Apply(new FossickActionResolver().ResolvePickaxe(board, 0, 0));

            var save = board.CreateSaveState(2468, progress);
            var restored = new FossickBoard(config.BoardSpec);
            restored.AppendGeneratedMine(mine);
            restored.ApplySaveState(save);

            Assert.That(save.destroyedCells, Does.Contain("0:0"));
            Assert.That(save.collectedCells, Does.Contain("0:0"));
            Assert.That(save.visibleCells, Does.Contain("2:2"));
            Assert.That(save.schemaVersion, Is.EqualTo(FossickSaveState.CurrentSchemaVersion));
            Assert.That(restored.TopVisibleRow, Is.EqualTo(board.TopVisibleRow));
            Assert.That(restored.Depth, Is.EqualTo(board.Depth));
            Assert.That(restored.GetCellAtAbsoluteRow(0, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(restored.GetCellAtAbsoluteRow(0, 0).collected, Is.True);
            Assert.That(restored.GetCellAtAbsoluteRow(2, 2).fog, Is.EqualTo(FossickFogType.None));
            Assert.That(save.oreFound, Is.EqualTo(5));
            Assert.That(save.toolUsed, Is.EqualTo(1));
        }

        private static void SetCell(FossickBoard board, int x, int y, FossickTerrainType terrain, int hp)
        {
            var cell = board.GetCell(x, y);
            cell.terrain = terrain;
            cell.hp = hp;
        }

        private static void SetAbsoluteCell(FossickBoard board, int x, int y, FossickTerrainType terrain, int hp)
        {
            var cell = board.GetCellAtAbsoluteRow(x, y);
            cell.terrain = terrain;
            cell.hp = hp;
        }

        private static void FillVisibleRow(FossickBoard board, int y, FossickTerrainType terrain, int hp)
        {
            for (var x = 0; x < board.Spec.width; x++)
            {
                SetCell(board, x, y, terrain, hp);
            }
        }

        private static void FillAbsoluteRow(FossickBoard board, int y, FossickTerrainType terrain, int hp)
        {
            for (var x = 0; x < board.Spec.width; x++)
            {
                SetAbsoluteCell(board, x, y, terrain, hp);
            }
        }

        private static void SetFog(FossickBoard board, int x, int y, FossickFogType fog)
        {
            var cell = board.GetCell(x, y);
            cell.fog = fog;
        }

        private static void SetAbsoluteFog(FossickBoard board, int x, int y, FossickFogType fog)
        {
            var cell = board.GetCellAtAbsoluteRow(x, y);
            cell.fog = fog;
        }

        private static bool ContainsTarget(System.Collections.Generic.IReadOnlyList<FossickToolTarget> targets, int x, int y)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i].x == x && targets[i].y == y)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountSteps(FossickActionResult result, FossickActionStepType type)
        {
            var count = 0;
            for (var i = 0; i < result.steps.Count; i++)
            {
                if (result.steps[i].type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private static FossickBoard CreateBoardWithSingleCell(FossickTerrainType terrain, int hp, FossickElementConfig reward)
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var fragment = CreateRewardFragment(9201);
            fragment.cells[0].terrain = terrain;
            fragment.cells[0].hp = hp;
            fragment.cells[0].reward = reward;
            config.fragments.Clear();
            config.fragments.Add(fragment);
            var mine = FossickMineLayoutBuilder.Build(config, 12345, 8);
            var board = new FossickBoard(config.BoardSpec);
            board.AppendGeneratedMine(mine);
            return board;
        }

        private static FossickFragmentConfig CreateRewardFragment(int id)
        {
            var fragment = new FossickFragmentConfig
            {
                id = id,
                type = FossickFragmentType.Tutorial,
                width = FossickBoardSpec.DefaultWidth,
                height = FossickBoardSpec.DefaultVisibleHeight
            };

            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    fragment.cells.Add(new FossickCellConfig
                    {
                        x = x,
                        y = y,
                        terrain = FossickTerrainType.Empty,
                        hp = 0,
                        fog = FossickFogType.None
                    });
                }
            }

            return fragment;
        }
    }
}
