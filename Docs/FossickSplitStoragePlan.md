# Fossick Split Storage Plan

## Goal

Fossick editor data must no longer be stored as one draft map file. Fragment templates, generation rules, and generated map definitions are separate product concepts and must have separate storage and editing boundaries.

## Plan

1. Add formal storage DTOs:
   - `FossickFragmentLibraryConfig`: reusable fragment templates only.
   - `FossickGenerationRulesConfig`: board/rule/tool/visual/gameplay rules only.
   - `FossickMapDefinitionConfig`: seed and generated-instance overrides only.
   - `FossickMapProjectConfig`: editor convenience wrapper that can compose/decompose the three files.

2. Keep `FossickMapConfig` as runtime composed config:
   - Existing generator, validator, board, gameplay, and preview systems continue to consume `FossickMapConfig`.
   - Editors can still work against one in-memory composed object for now.
   - Export splits the composed object into the three formal files.

3. Replace `FossickMapDraft.json` export with product paths:
   - `Assets/Fossick/MapStudio/Maps/FossickFragmentLibrary.json`
   - `Assets/Fossick/MapStudio/Maps/FossickGenerationRules.json`
   - `Assets/Fossick/MapStudio/Maps/FossickMapDefinition.json`

4. Add migration compatibility:
   - Old one-file map JSON can still be parsed into the split project wrapper.
   - The editor no longer exports to the old draft file as the main workflow.

## Verification

- Unit tests cover split export/compose round trip.
- `Fossick.MapStudio` still builds.
- Existing gameplay/session tests continue to use `FossickMapConfig` without migration work.
