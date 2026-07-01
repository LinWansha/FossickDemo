---
name: fossick-unity-workflow
description: 在 FossickDemo Unity 项目中工作时使用，尤其是修改 Fossick Core、MapStudio、Preview、Runtime、Editor 工具、Unity 资产、地图 JSON 存储、EditMode 测试或 HOP Fossick 活动原型时。这个 skill 会帮助 Codex 读取正确设计上下文、保护 Unity 资产边界，并使用项目专属的 EditMode 验证流程。
---

# Fossick Unity 工作流

在 `FossickDemo` 内修改代码、测试、JSON、场景、Prefab、文档时使用这个流程。

## 先定位上下文

1. 编辑前先检查 git status。这个项目经常有正在进行中的 Unity 资产、scene、JSON 和 `.meta` 改动。
2. 阅读仓库根目录的 `AGENTS.md`，了解长期项目规则。
3. 只读取与当前任务匹配的设计文档：
   - 核心规则和运行时结构：`Docs/FossickCorePlan.md`
   - split storage：`Docs/FossickSplitStoragePlan.md`
   - MapStudio 用户行为：`Docs/FossickMapStudioUserGuide.md`
   - 模板编辑：`Docs/FossickTemplateEditingPlan.md`
   - 活跃实现计划：`Docs/superpowers/plans/`
4. 决定改哪里之前，先查看最近的 C# 文件、asmdef、JSON 配置和测试。

## 架构边界

- 纯玩法、配置、序列化、校验、生成、action 结算逻辑放在 `Fossick.Core`。
- UI、scene、MonoBehaviour、Unity view 逻辑按职责放在 `Runtime`、`MapStudio`、`Preview` 或 `Editor`。
- 面向策划的 MapStudio 行为要和程序调试用 EditorWindow 分开。
- 除非任务明确要求改 split storage 模型，否则运行时系统继续消费组合后的 `FossickMapConfig`。
- 保留 split storage 的三个产品文件概念：
  - `FossickFragmentLibrary.json`
  - `FossickGenerationRules.json`
  - `FossickMapDefinition.json`

## Unity 资产注意事项

- 不要随意删除、重建或重命名 Unity `.meta` 文件。
- 新增 Unity 资产或 C# 文件时，如果 Unity 已生成 `.meta`，要一起保留。
- scene 和 prefab diff 对用户很敏感；声称它们是预期改动前，必须先检查。

## 验证方式

逻辑改动优先使用 `FossickUnity/Assets/Fossick/Tests/EditMode` 下的 EditMode 测试。

需要 Unity 验证时使用：

```bash
/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/mengruiqing/WorkSpace/UnityWork/FossickDemo/FossickUnity -runTests -testPlatform EditMode -testResults /tmp/fossick-editmode-results.xml -quit
```

如果 Unity 已打开或命令无法运行，要说明阻塞原因，并尽量运行可用的小范围静态检查。

## 最终汇报

汇报时说明：

- 改了什么
- 触碰了哪条架构边界
- 跑了什么验证
- 是否有 Unity 资产或 `.meta` 文件需要人工注意
