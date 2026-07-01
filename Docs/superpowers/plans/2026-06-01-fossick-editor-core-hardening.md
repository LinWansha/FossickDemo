# Fossick Editor And Core Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring Fossick from a playable/editor prototype to a production-usable map authoring flow plus a deterministic greybox digging loop.

**Architecture:** Keep the editor, generated mine model, and runtime board model separated. The editor edits templates and explicit mine overrides; the core board consumes generated rows and produces pure action results; preview scenes render those results without owning gameplay rules.

**Tech Stack:** Unity 2022.3, C# asmdefs, uGUI runtime editor, Unity EditMode tests, JSON map config under `Assets/Fossick/MapStudio/Maps`.

---

## File Structure

- Modify `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`: production editor UX, override status, row-range actions, export/report controls.
- Modify `FossickUnity/Assets/Fossick/Core/Generation/FossickMineLayout.cs`: deterministic row override metadata and generated mine row origin.
- Modify `FossickUnity/Assets/Fossick/Core/Board/FossickBoard.cs`: runtime board initialization from generated mine, visible window, scroll/depth.
- Modify `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResolver.cs`: pickaxe and tool result pipeline.
- Modify `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResult.cs`: structured deltas for UI/animation.
- Modify `FossickUnity/Assets/Fossick/Core/Rewards/FossickProgressState.cs`: reward counters and stats.
- Modify `FossickUnity/Assets/Fossick/Core/Save/FossickSaveState.cs`: deterministic restore shape.
- Modify `FossickUnity/Assets/Fossick/Preview/Controllers/FossickPreviewController.cs`: greybox runtime loop using core board/action state.
- Modify `FossickUnity/Assets/Fossick/Preview/Views/FossickPreviewView.cs`: non-OGUI/minimal uGUI preview after core is stable.
- Create `FossickUnity/Assets/Fossick/Tests/EditMode/Fossick.Core.Tests.asmdef`: EditMode test assembly.
- Create `FossickUnity/Assets/Fossick/Tests/EditMode/FossickMineLayoutTests.cs`: generation and override tests.
- Create `FossickUnity/Assets/Fossick/Tests/EditMode/FossickBoardActionTests.cs`: digging, reward, scroll tests.

---

### Task 1: Generated Mine Override Clarity

> Updated 2026-06-02: product direction changed. Once a template is applied into the mine or edited in-place, planners should see it only as map content. Do not expose source-template, row-override, origin metadata, or restore-override language in UI.

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Generation/FossickMineLayout.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`
- Create: `FossickUnity/Assets/Fossick/Tests/EditMode/Fossick.Core.Tests.asmdef`
- Create: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickMineLayoutTests.cs`

- [x] Add tests proving row overrides replace only targeted rows and later overlapping overrides win.
- [x] Keep applied templates as plain map content; do not add row-origin metadata to generated rows.
- [x] Remove source/origin wording from MapStudio UI.
- [x] Remove row override restore/removal action from planner-facing UI.
- [x] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 2: Row Range Batch Editing

**Files:**
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`

- [x] Add explicit “批量行操作” mode separate from normal painting and template application.
- [x] Support select row range, fill layer, clear layer, and duplicate row range downward.
- [x] Do not expose restore-generated-row behavior; edited rows are treated as plain map content.
- [x] Keep normal mode row bar read-only.
- [x] Keep template application using row bar only when applying a template.
- [x] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 3: Runtime Board From Generated Mine

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Board/FossickBoard.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Board/FossickCellState.cs`
- Create: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickBoardActionTests.cs`

- [x] Add tests for board initialization from `FossickGeneratedMine`.
- [x] Preserve absolute row index, visible x/y, terrain hp, reward, decoration, fog, and layer ids.
- [x] Add append API that accepts generated mine rows instead of raw template fragments.
- [x] Make scroll rules explicit and testable.
- [x] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 4: Digging Result Pipeline

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResolver.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResult.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Rewards/FossickProgressState.cs`

- [x] Test pickaxe hit on dirt, multi-hit stone, empty cell, and unbreakable cell.
- [x] Emit ordered result steps: tool consumed, obstacle hit, obstacle broken, reward revealed, reward collected, board scrolled.
- [x] Update progress stats from action results without UI dependency.
- [x] Keep reward collection non-blocking.
- [x] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 5: Tool Preview And Tool Actions

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResolver.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResult.cs`
- Modify: `FossickUnity/Assets/Fossick/Preview/Controllers/FossickPreviewController.cs`
- Modify: `FossickUnity/Assets/Fossick/Preview/Views/FossickPreviewView.cs`

- [x] Add deterministic target preview for pickaxe, dynamite, bomb, and radar.
- [x] Add tests for range masks and edge clipping.
- [x] Apply range hits through the same result pipeline as pickaxe.
- [x] Show preview highlight in greybox preview before action is committed.
- [x] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 6: Save And Restore

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Save/FossickSaveState.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Board/FossickBoard.cs`
- Modify: `FossickUnity/Assets/Fossick/Preview/Controllers/FossickPreviewController.cs`

- [x] Test restoring same seed, same generated rows, same broken cells, same collected rewards, same depth.
- [x] Store seed, top visible row, broken cells, collected rewards, pending rewards, and stats.
- [x] Apply save state after deterministic generation and before first render.
- [x] Add preview buttons for save, reload, and reset.
- [x] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 7: Editor Validation And Reports

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Validation/FossickMapValidator.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`

- [ ] Add validation for buried reward reachability, reward background consistency, layer conflicts, row override overlaps, reward density, and missing tutorial/reward pools.
- [ ] Show validation issues grouped by template id / generated row / cell.
- [ ] Let clicking a validation issue focus the template or generated mine row.
- [ ] Add export preview report text next to JSON export.
- [ ] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 8: Greybox Preview Scene Upgrade

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Preview/Controllers/FossickPreviewController.cs`
- Modify: `FossickUnity/Assets/Fossick/Preview/Views/FossickPreviewView.cs`

- [x] Replace OGUI preview with simple uGUI layout.
- [x] Show depth, visible board window, tool selection, action log, and reward totals.
- [x] Support click/tap digging and refresh after board scrolling.
- [x] Keep all gameplay decisions in core classes.
- [x] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

### Task 9: Documentation And Workflow

**Files:**
- Create or modify: `Docs/FossickImplementation.md`
- Modify: `Docs/FossickCoreArchitecture.md` if present

- [ ] Document template vs generated instance vs row override.
- [ ] Document layer order and where each layer is edited.
- [ ] Document gameplay event pipeline.
- [ ] Document recommended planner workflow for creating a Fossick map.
- [ ] Run `dotnet build FossickUnity/FossickUnity.sln --no-restore`.

---

## Execution Order

Start with Tasks 1, 3, and 4 because they determine whether editor data can feed gameplay. Then complete Tasks 2, 6, and 7 to make content production safe. Finish with Tasks 5, 8, and 9 once the core loop is stable.
