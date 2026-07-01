# Template Editor Layout D Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rework Fossick template editing into layout D: canvas-first editing, right-side template Inspector, top global actions.

**Architecture:** Keep the existing `FossickMapStudioView` rendering model and move template metadata/actions into explicit helper sections. Do not change gameplay/model data; only change template edit presentation and related interaction placement.

**Tech Stack:** Unity UGUI, C#, existing MapStudio helper methods.

---

### Task 1: Split Template Editor Into Canvas And Inspector

**Files:**
- Modify: `FossickUnity/Assets/Fossick/MapStudio/Views/FossickMapStudioView.cs`

- [ ] Move save/undo/redo into a compact top action bar inside the template editor header.
- [ ] Move type/difficulty/height controls into a right-side Inspector panel.
- [ ] Keep brush palette above the canvas, not mixed with template metadata.
- [ ] Keep canvas centered and large.
- [ ] Verify with `dotnet build FossickUnity/Fossick.MapStudio.csproj --no-restore`.
- [ ] Verify with `git diff --check`.
