# Fossick MapStudio Product Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn Fossick MapStudio from a functional prototype into a planner-facing, robust, maintainable production editor.

**Architecture:** Keep the editor product centered on template library + generation rules + read-only generated mine preview. Avoid reintroducing generated-map direct editing. Split responsibilities into focused data, validation, UI, export, and preview components while keeping runtime gameplay independent from editor UI.

**Tech Stack:** Unity 2022.3, UGUI, C# editor/runtime scripts, Unity `JsonUtility`, existing Fossick core generator and board preview code.

---

## Product Gaps

Current MapStudio already supports template editing, layered brushes, semi-random generation rules, read-only generated preview, split JSON export, Console, and basic documentation.

Before it feels like a product for planners, the remaining work is:

1. Make data ownership and save/load behavior explicit and hard to misuse.
2. Improve validation so planner mistakes are caught before export.
3. Make template library workflows more visual and predictable.
4. Polish generation-rule editing so random output is explainable.
5. Add editor-side QA flows for repeated preview/regression checks.
6. Make export/version compatibility safer.
7. Reduce large view-file risk and finish UI component separation.
8. Add final planner-facing docs and sample content.

## File Structure

Primary files expected to change:

- `FossickUnity/Assets/Fossick/Core/Config/FossickMapConfig.cs`
  - Owns schema objects for template library, generation rules, and map definition.
- `FossickUnity/Assets/Fossick/Core/Generation/FossickFragmentGenerator.cs`
  - Owns deterministic fragment selection behavior.
- `FossickUnity/Assets/Fossick/Core/Generation/FossickMineLayoutBuilder.cs`
  - Owns generated preview layout and lazy row building.
- `FossickUnity/Assets/Fossick/Core/Validation/FossickMapValidator.cs`
  - Should become the single place for product-level validation rules.
- `FossickUnity/Assets/Fossick/Core/Serialization/FossickMapJsonUtility.cs`
  - Owns JSON read/write helpers and schema migration entry points.
- `FossickUnity/Assets/Fossick/MapStudio/ImportExport/FossickMapFileService.cs`
  - Owns editor save/load/export paths.
- `FossickUnity/Assets/Fossick/MapStudio/Controllers/FossickMapStudioController.cs`
  - Coordinates editor data state and validation.
- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`
  - Should keep shrinking toward shell/layout only.
- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickBrushPaletteView.cs`
  - Owns brush rendering and brush selection UI.
- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.Menu.cs`
  - Owns left menu.
- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.Console.cs`
  - Owns Console output.
- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.Dialog.cs`
  - Owns confirmation and warning dialogs.

New files recommended:

- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickTemplateLibraryView.cs`
  - Extract template library window and visual cards.
- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickGenerationRulesView.cs`
  - Extract generation rule editing UI.
- `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMinePreviewView.cs`
  - Extract read-only generated mine preview UI.
- `FossickUnity/Assets/Fossick/MapStudio/Services/FossickMapStudioState.cs`
  - Holds editor mode, selected template, selected brush, dirty flags, and preview seed.
- `FossickUnity/Assets/Fossick/MapStudio/Services/FossickMapStudioAutosaveService.cs`
  - Optional recovery snapshot service for unsaved edits.
- `FossickUnity/Assets/Fossick/Tests/EditMode/FossickMapStudioProductTests.cs`
  - Tests schema split, validation, generator determinism, and export cleanliness.

---

### Task 1: Lock Product Data Boundaries

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Config/FossickMapConfig.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Serialization/FossickMapJsonUtility.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/ImportExport/FossickMapFileService.cs`
- Test: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickSplitStorageTests.cs`

- [ ] **Step 1: Add tests that exported files stay split**

Run: `dotnet test FossickUnity/Fossick.Core.Tests.csproj --no-build`

Expected after implementation: split storage tests pass and confirm:

- fragment library contains templates only.
- generation rules contain random generation settings only.
- map definition contains board/view size only.
- generated mine preview data is not exported.

- [ ] **Step 2: Make file naming and ownership visible in code**

Confirm `FossickMapFileService` exposes only:

```csharp
public const string FragmentLibraryFileName = "FossickFragmentLibrary.json";
public const string GenerationRulesFileName = "FossickGenerationRules.json";
public const string MapDefinitionFileName = "FossickMapDefinition.json";
```

- [ ] **Step 3: Remove remaining draft-map wording**

Search:

```bash
rg -n "Draft|草稿|MapDraft|实例编辑|编辑实例" FossickUnity/Assets/Fossick Docs
```

Expected: no planner-facing text suggests editing generated map instances.

- [ ] **Step 4: Verify**

Run:

```bash
dotnet build FossickUnity/Fossick.MapStudio.csproj --no-restore
dotnet build FossickUnity/Fossick.Core.Tests.csproj --no-restore
git diff --check
```

Expected: builds succeed; existing Unity package warnings are acceptable.

---

### Task 2: Strengthen Product Validation

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Validation/FossickMapValidator.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.Console.cs`
- Test: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickMapStudioProductTests.cs`

- [ ] **Step 1: Add validation cases**

Add or extend tests for these cases:

- regular template must have difficulty >= 1.
- reward template must not enter regular pool.
- ore/tool cannot be placed on empty or bedrock terrain.
- treasure-room region must stay inside template bounds.
- generation rule difficulty counts must match regular group size.
- reward insert min must be <= max.
- map width and visible height must be positive.

- [ ] **Step 2: Improve issue messages**

Each validation issue should include:

- template ID when possible.
- cell coordinate when possible.
- clear Chinese message.
- severity: error or warning.

- [ ] **Step 3: Show grouped Console validation**

In Console, group validation issues by:

- 模板问题
- 生成规则问题
- 地图定义问题

- [ ] **Step 4: Verify**

Run:

```bash
dotnet test FossickUnity/Fossick.Core.Tests.csproj --no-build
dotnet build FossickUnity/Fossick.MapStudio.csproj --no-restore
```

Expected: validation tests pass; Console still opens and displays validation details.

---

### Task 3: Productize Template Library

**Files:**
- Create: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickTemplateLibraryView.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.Dialog.cs`

- [ ] **Step 1: Extract template library UI**

Move template library rendering out of `FossickMapStudioView.cs` into `FossickTemplateLibraryView`.

Keep responsibilities:

- render visual template cards.
- filter by template type.
- select template.
- create from preset.
- copy template.
- delete template with confirmation.

- [ ] **Step 2: Add planner-friendly filters**

Add tabs:

- 全部
- 新手
- 常规
- 奖励

Cards should show:

- preview thumbnail.
- ID.
- type.
- difficulty if regular.
- height.

- [ ] **Step 3: Add safe delete dialog**

Deleting a template should show:

Title: `删除模板`

Message: `删除后该模板不会再参与后续生成。已生成的预览可重新生成。`

Buttons:

- 取消
- 删除

- [ ] **Step 4: Verify**

Manual Unity check:

1. Open MapStudio scene.
2. Open template library.
3. Switch filters.
4. Copy a regular template.
5. Delete copied template.
6. Confirm no generated preview edit UI appears.

---

### Task 4: Productize Template Editing

**Files:**
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickBrushPaletteView.cs`
- Optional create: `FossickUnity/Assets/Fossick/MapStudio/Services/FossickMapStudioState.cs`

- [ ] **Step 1: Make template metadata editing clearer**

Move template metadata into a stable right-side context section when editing a template:

- ID display.
- type selector: 新手 / 常规 / 奖励.
- difficulty selector only visible for 常规.
- height stepper.
- save / discard.

- [ ] **Step 2: Keep brush area visually separate**

Middle top area should contain only:

- editing mode summary.
- layer tabs.
- brush tiles.

Metadata should not mix with brush selection.

- [ ] **Step 3: Preserve current unsaved-change dialog**

Before leaving template editing:

- Continue editing.
- Discard.
- Save.

Button labels should stay short and action-oriented.

- [ ] **Step 4: Verify**

Manual Unity check:

1. Edit a regular template.
2. Change difficulty.
3. Paint terrain and reward.
4. Undo and redo.
5. Try returning without saving.
6. Save and regenerate preview.

---

### Task 5: Productize Generation Rules

**Files:**
- Create: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickGenerationRulesView.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Generation/FossickFragmentGenerator.cs`
- Test: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickMapStudioProductTests.cs`

- [ ] **Step 1: Extract generation-rule UI**

Move generation rule rendering out of `FossickMapStudioView.cs` into `FossickGenerationRulesView`.

- [ ] **Step 2: Add rule explanation preview**

Show a compact explanation:

`每轮 10 段：难度1 x7，难度2 x2，难度3 x1。每隔 4-6 段插入奖励模板。`

- [ ] **Step 3: Add deterministic preview test**

Given same seed and same templates:

- generated sequence must be identical.

Given different seed:

- generated sequence may differ but must follow difficulty counts and reward interval.

- [ ] **Step 4: Verify**

Run:

```bash
dotnet test FossickUnity/Fossick.Core.Tests.csproj --no-build
```

Manual check:

1. Change group size.
2. Change difficulty count.
3. Change reward interval.
4. Generate preview.
5. Open Console and confirm sequence matches the explanation.

---

### Task 6: Improve Generated Mine Preview QA

**Files:**
- Create: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMinePreviewView.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Generation/FossickMineLayoutBuilder.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.Console.cs`

- [ ] **Step 1: Extract preview UI**

Move read-only preview rendering to `FossickMinePreviewView`.

Keep these behaviors:

- scroll preview.
- append rows on demand.
- click cell to inspect source template.
- never mutate template or generated data from preview clicks.

- [ ] **Step 2: Add preview summary**

Show:

- seed.
- previewed row count.
- fragment count.
- reward insert count.
- first/last visible row.

- [ ] **Step 3: Add preview QA actions**

In Console or right panel, add:

- `复制当前拼接顺序`
- `重新生成当前种子`
- `随机种子并生成`

- [ ] **Step 4: Verify**

Manual Unity check:

1. Generate preview.
2. Scroll downward.
3. Click cells from several fragments.
4. Confirm right panel shows correct source template.
5. Confirm no edit mode appears for generated preview.

---

### Task 7: Add Save Recovery and Dirty-State Safety

**Files:**
- Create: `FossickUnity/Assets/Fossick/MapStudio/Services/FossickMapStudioAutosaveService.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Controllers/FossickMapStudioController.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.Dialog.cs`

- [ ] **Step 1: Add temporary recovery snapshots**

Store unsaved template-edit snapshot outside official export files.

Suggested location:

`Library/FossickMapStudio/Recovery/`

This is not source data and should not be committed.

- [ ] **Step 2: Add recovery prompt**

When opening editor and recovery data exists:

Title: `发现未保存编辑`

Buttons:

- 忽略
- 恢复

- [ ] **Step 3: Ensure official JSON stays clean**

Autosave must not write to:

- `FossickFragmentLibrary.json`
- `FossickGenerationRules.json`
- `FossickMapDefinition.json`

- [ ] **Step 4: Verify**

Manual Unity check:

1. Edit template.
2. Do not save.
3. Stop play mode or reload scene.
4. Reopen editor.
5. Confirm recovery prompt appears.

---

### Task 8: Add Export Versioning and Migration Guardrails

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Core/Config/FossickMapConfig.cs`
- Modify: `FossickUnity/Assets/Fossick/Core/Serialization/FossickMapJsonUtility.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/ImportExport/FossickMapFileService.cs`
- Test: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickMapStudioProductTests.cs`

- [ ] **Step 1: Confirm version fields in all exported files**

Each JSON root should include version metadata.

- [ ] **Step 2: Add load-time compatibility check**

If version is missing or newer than editor supports:

- show clear dialog.
- do not silently overwrite.

- [ ] **Step 3: Add migration test**

Load a minimal older JSON fixture and verify it becomes a valid current config.

- [ ] **Step 4: Verify**

Run:

```bash
dotnet test FossickUnity/Fossick.Core.Tests.csproj --no-build
dotnet build FossickUnity/Fossick.MapStudio.csproj --no-restore
```

---

### Task 9: Asset and Brush Robustness

**Files:**
- Modify: `FossickUnity/Assets/Fossick/Runtime/Views/FossickArtLibrary.cs`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickBrushPaletteView.cs`
- Test: `FossickUnity/Assets/Fossick/Tests/EditMode/FossickMapStudioProductTests.cs`

- [ ] **Step 1: Add asset manifest check**

Validate that required sprites exist for:

- dirt smooth tiles.
- stone smooth tiles.
- fog smooth tiles.
- ore attachments.
- ore entities.
- tool attachments.
- tool entities.
- treasure-room backgrounds.

- [ ] **Step 2: Show missing asset errors in Console**

If a required sprite is missing, show:

`资源缺失：<resource-id>，请检查切图导入。`

- [ ] **Step 3: Remove silent fallback visuals from production path**

Do not hide missing assets with pure-color production visuals. Missing assets should be caught as validation/config errors.

- [ ] **Step 4: Verify**

Manual Unity check:

1. Open brush palette.
2. Confirm all brush tiles show icons.
3. Paint terrain, reward, tool, decoration, fog.
4. Confirm no white blocks or placeholder sprites appear.

---

### Task 10: Final Planner Docs and Sample Pack

**Files:**
- Modify: `Docs/FossickMapStudioUserGuide.md`
- Create: `Docs/FossickMapStudioChecklist.md`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Maps/FossickFragmentLibrary.json`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Maps/FossickGenerationRules.json`
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Maps/FossickMapDefinition.json`

- [ ] **Step 1: Add export checklist**

Create planner checklist:

- 保存模板。
- 重新生成预览。
- 打开 Console 检查拼接顺序。
- 点击校验。
- 导出 JSON。

- [ ] **Step 2: Prepare sample templates**

Ensure sample pack includes:

- at least 2 tutorial templates.
- at least 3 regular difficulty 1 templates.
- at least 2 regular difficulty 2 templates.
- at least 1 regular difficulty 3 template.
- at least 2 reward templates.

- [ ] **Step 3: Add one-page handoff note**

Document:

- what planners can edit.
- what planners should not edit.
- who owns asset replacement.
- what files should be submitted.

- [ ] **Step 4: Verify**

Manual check with a non-engineer:

1. Ask them to create a template.
2. Ask them to change generation rules.
3. Ask them to generate preview.
4. Ask them to export.
5. Record any confusing step as follow-up UI work.

---

## Suggested Execution Order

1. Task 1: Lock Product Data Boundaries.
2. Task 2: Strengthen Product Validation.
3. Task 3: Productize Template Library.
4. Task 4: Productize Template Editing.
5. Task 5: Productize Generation Rules.
6. Task 6: Improve Generated Mine Preview QA.
7. Task 8: Add Export Versioning and Migration Guardrails.
8. Task 9: Asset and Brush Robustness.
9. Task 10: Final Planner Docs and Sample Pack.
10. Task 7: Add Save Recovery and Dirty-State Safety.

Task 7 is useful but can wait until core workflows are stable.

## Definition of Done

MapStudio reaches product shape when:

- planners only edit templates and rules, not generated map instances.
- all official data is split into library, rules, and map definition files.
- validation catches common content mistakes before export.
- generated preview is deterministic by seed and explainable through Console.
- template library and template editing are visually understandable without engineer help.
- export files contain version metadata and no temporary preview state.
- missing art assets are reported clearly.
- a planner can follow the guide and export usable JSON without asking how the editor works.

