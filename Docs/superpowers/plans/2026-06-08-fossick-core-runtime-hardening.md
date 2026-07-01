# Fossick Core Runtime Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Fossick's core gameplay loop complete, stable, testable, and portable before adding production UI, animation, lifecycle, or reward-exchange systems.

**Architecture:** Keep Fossick.Core as a pure gameplay model with deterministic generation, explicit action results, state snapshots, and no dependency on Unity UI. Preview and MapStudio may render and edit data, but they must not own rules for digging, fog, scrolling, rewards, or inventory.

**Tech Stack:** Unity 2022.3, C# asmdefs, pure C# core classes, Unity EditMode tests, uGUI preview as a thin adapter.

---

## Priority Order

1. **Action result contract:** make every operation explicitly say whether it was applied, whether a tool was consumed, why it failed, what changed, and how depth changed.
2. **Visibility and scrolling stability:** make fog removal and board scrolling deterministic after every successful FossickTool action, including radar.
3. **Tool rules completeness:** lock pickaxe, dynamite, TNT, and radar semantics behind tests so Preview cannot drift from design.
4. **Portable save snapshots:** preserve enough state for restart, future server sync, and future HOP integration without leaking UI/editor concepts into save data.
5. **Preview adapter cleanup:** make Preview consume the core contract only, with no copied gameplay rules.
6. **Documentation:** keep the runtime contract documented so future UI, animation, and HOP integration know exactly which layer owns which responsibility.

---

### Task 1: Explicit Action Result Contract

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResult.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResolver.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Gameplay/FossickGameplaySession.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Rewards/FossickProgressState.cs`
- Modify: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickBoardActionTests.cs`

- [x] Add `isApplied`, `toolConsumed`, `invalidReason`, `depthBeforeAction`, and `depthAfterAction` to `FossickActionResult`.
- [x] Set these fields inside `FossickActionResolver` instead of forcing consumers to infer state from `steps`.
- [x] Update `FossickGameplaySession` to consume inventory only when `action.toolConsumed` is true.
- [x] Update `FossickProgressState` to count tool usage from `action.toolConsumed`.
- [x] Add tests for valid action flags, invalid action flags, and radar direct-use flags.
- [x] Run `dotnet build FossickUnity/Fossick.Core.Tests.csproj --no-restore`.

### Task 2: Visibility And Scroll Policy

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Board/FossickBoard.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResolver.cs`
- Modify: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickBoardActionTests.cs`

- [ ] Keep fog as the only visibility gate: `fog == Covered` means not visible; `fog == None` means visible.
- [ ] Ensure only successful tool operations can trigger scroll attempts.
- [ ] After a successful non-radar tool operation, reveal connected visible open space and adjacent obstacles.
- [ ] After radar, clear fog in the visible window and then run the same scroll policy.
- [ ] Keep scroll condition as: current visible window's last row contains at least one visible empty cell.
- [ ] Add tests for radar-driven scroll, non-scroll on invalid action, and scroll chains after successful operations.
- [ ] Run `dotnet build FossickUnity/Fossick.Core.Tests.csproj --no-restore`.

### Task 3: Tool Rules Lockdown

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Actions/FossickActionResolver.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Config/FossickMapConfig.cs`
- Modify: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickBoardActionTests.cs`

- [ ] Pickaxe: only usable on visible breakable terrain; single target; one damage.
- [ ] Dynamite: only usable on visible empty cell; affects same row; one damage; blocked by unbreakable terrain or by breakable terrain with more than one hp before damage.
- [ ] TNT: only usable on visible empty cell; affects clipped 3x3 area; two damage; no row blocking.
- [ ] Radar: direct-use button action; no placement preview; clears fog in current visible window.
- [ ] Keep all tool shapes configurable where practical, but default to document behavior.
- [ ] Run `dotnet build FossickUnity/Fossick.Core.Tests.csproj --no-restore`.

### Task 4: Portable Save Snapshot

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Save/FossickSaveState.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Board/FossickBoard.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Gameplay/FossickGameplaySession.cs`
- Modify: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickGameplaySessionTests.cs`

- [x] Add a save schema version field.
- [x] Persist seed, top visible row, depth, inventory, reward totals, collections, destroyed cells, collected cells, visible cells, and progress stats.
- [x] Keep template/editor metadata out of runtime save.
- [x] Restore by deterministic generation first, then apply runtime mutations.
- [ ] Add tests that restore after tool usage, radar reveal, scroll, and collected rewards.
- [x] Run `dotnet build FossickUnity/Fossick.Core.Tests.csproj --no-restore`.

### Task 5: Preview As Thin Adapter

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Preview/Controllers/FossickPreviewController.cs`
- Modify: `FossickUnity/Assets/Fossick/Preview/Views/FossickPreviewView.cs`

- [ ] Preview reads tool preview from `FossickActionResolver.GetToolPreview`.
- [ ] Preview performs actions through `FossickGameplaySession.UseTool`.
- [ ] Preview renders `FossickActionResult` and never reimplements tool legality, fog, reward, or scroll rules.
- [ ] Radar button directly uses radar and does not enter placement/preview mode.
- [ ] Run `dotnet build FossickUnity/Fossick.Preview.csproj --no-restore`.

### Task 6: Core Runtime Documentation

**Files:**
- Modify: `docs/FossickCorePlan.md`
- Create or modify: `docs/FossickRuntimeContract.md`

- [ ] Document module ownership: generation, board, action resolver, gameplay session, reward state, save state, preview adapter.
- [ ] Document the action result event contract for future animation/UI.
- [ ] Document tool rules in gameplay terms, not UI terms.
- [ ] Document save/restore order and fields.
- [ ] Run both core and preview builds.
