# Fossick Template Editing Implementation Plan

Goal: make template editing explicit and safe for designers.

Implementation steps:

1. Add a template edit session to `FossickMapStudioView`: editing works on a draft copy, not the live fragment.
2. Add save, discard, undo, and redo controls to the template editor header.
3. Replace the template metadata row with clear type chips and regular-only difficulty chips.
4. Route all template painting and row resizing through the draft session and undo snapshots.
5. Save writes the draft back into the fragment library and marks the generated mine instance stale.
6. Verify with C# project builds and diff checks.
