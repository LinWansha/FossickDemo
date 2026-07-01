# 仓库协作指南

## 项目结构与模块边界

FossickDemo 是一个 Unity 2022.3.62f2 项目，用来验证 HOP Fossick 活动原型。Unity 工程位于 `FossickUnity/`。核心玩法逻辑在 `FossickUnity/Assets/Fossick/Core`，按 board、config、serialization、generation、gameplay、actions、validation、save、rewards、visual 等模块拆分。运行时表现放在 `Runtime`，面向策划的地图编辑器放在 `MapStudio`，Unity Editor 专用工具放在 `Editor`，灰盒验证放在 `Preview`。EditMode 测试位于 `FossickUnity/Assets/Fossick/Tests/EditMode`。

主要程序集包括 `Fossick.Core`、`Fossick.MapStudio`、`Fossick.Runtime`、`Fossick.Preview`、`Fossick.Editor` 和 `Fossick.Core.Tests`。纯规则、配置组合、序列化、校验、生成和结算逻辑应优先放在 `Fossick.Core`；UI、场景、MonoBehaviour 和 Unity 工具逻辑不要混进去。

## 构建、测试与开发命令

- 用 Unity `2022.3.62f2` 打开 `FossickUnity/`。
- 运行 EditMode 测试：
  `/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/mengruiqing/WorkSpace/UnityWork/FossickDemo/FossickUnity -runTests -testPlatform EditMode -testResults /tmp/fossick-editmode-results.xml -quit`
- 仓库级 `.codex/config.toml` 已配置 Unity MCP，使用 `mcpforunityserver==9.7.1`，默认实例为 `FossickUnity`。

## 代码风格与命名

遵循现有 C# 风格和程序集根命名空间，例如 `Fossick.Core`、`Fossick.MapStudio`。类名与文件名保持一致。不要随意移动、删除或重建 Unity `.meta` 文件；新增 Unity 资产或 C# 文件时，要保留 Unity 生成的对应 `.meta` 文件。

## UI/UX 设计协作

讨论 MapStudio、Preview、Gameplay HUD 等界面布局或复杂交互时，优先先画界面草图再落地代码。若界面相对复杂、包含多区域布局、多个操作状态或需要对比多个方案，应使用 SVG 生成可视化方案图，而不是只用文字描述。方案图可以临时放在 `Docs/DesignMockups/`，待用户确认后再进入实现。

## 测试要求

核心逻辑、存储 DTO、地图校验、地图生成、玩法 session 行为优先补 EditMode 测试。改动 split storage 时，要保留 `Assets/Fossick/MapStudio/Maps` 下的产品文件概念：`FossickFragmentLibrary.json`、`FossickGenerationRules.json`、`FossickMapDefinition.json`。`Docs/` 下的计划文档是设计上下文，不是可执行验证。

## 提交与 PR 习惯

近期提交使用短 scope，例如 `[Core]`、`[Framework][Core]`。提交应聚焦一个系统或一类变更，并在信息里点明影响范围。提交前必须仔细看 dirty tree，因为这个仓库经常同时存在 Unity scene、asset、JSON 和 `.meta` 改动。
