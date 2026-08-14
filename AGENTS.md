# 仓库协作指南

## 项目结构与模块边界

FossickDemo 是一个 Unity 2022.3.62f2 项目，用来验证 HOP Fossick 活动原型。Unity 工程位于 `FossickUnity/`。核心玩法逻辑在 `FossickUnity/Assets/Fossick/Core`，正式矿井表现放在 `Runtime`，面向策划的地图编辑器放在 `MapStudio`，Unity Editor 专用工具放在 `Editor`，独立试玩验证放在 `Preview`。正式说明文档放在 `FossickUnity/Assets/Fossick/Docs`。

主要程序集包括 `Fossick.Core`、`Fossick.MapStudio`、`Fossick.Runtime`、`Fossick.Preview` 和 `Fossick.Editor`。纯规则、配置组合、序列化、校验、生成和结算逻辑应优先放在 `Fossick.Core`；UI、场景、MonoBehaviour 和 Unity 工具逻辑不要混进去。

## 构建与开发命令

- 用 Unity `2022.3.62f2` 打开 `FossickUnity/`。
- 仓库级 `.codex/config.toml` 已配置 Unity MCP，使用 `mcpforunityserver==9.7.1`，默认实例为 `FossickUnity`。

## 代码风格与命名

遵循现有 C# 风格和程序集根命名空间，例如 `Fossick.Core`、`Fossick.MapStudio`。类名与文件名保持一致。不要主动新建、修改、修复、移动、删除或重建 Unity `.meta` 文件；让 Unity 编译和导入流程自动生成与维护 `.meta`。

## UI/UX 设计协作

讨论 MapStudio、Preview、Gameplay HUD 等界面布局或复杂交互时，优先先画界面草图再落地代码。若界面相对复杂、包含多区域布局、多个操作状态或需要对比多个方案，应使用 SVG 生成可视化方案图，而不是只用文字描述。临时方案图可以放在 `tmp/`，确认后的正式说明再整理到 `FossickUnity/Assets/Fossick/Docs`。

## 验证要求

当前原型阶段不主动新增测试函数或测试程序集。验证优先使用 Unity 编译、场景试玩、打包结果检查和小范围静态检查。改动地图数据存储时，要保留 `Assets/Fossick/MapStudio/Maps` 下的产品文件概念：`FossickFragmentLibrary.json`、`FossickGenerationRules.json`、`FossickMapDefinition.json`。`FossickUnity/Assets/Fossick/Docs` 下的文档是正式交付说明，不是可执行验证。

## 提交与 PR 习惯

近期提交使用短 scope，例如 `[Core]`、`[Framework][Core]`。提交应聚焦一个系统或一类变更，并在信息里点明影响范围。提交前必须仔细看 dirty tree，因为这个仓库经常同时存在 Unity scene、asset、JSON 和 `.meta` 改动。
