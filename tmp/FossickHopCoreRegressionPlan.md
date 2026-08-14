# HOP Fossick Core 回归计划

## 目标

以 HOP 工程当前稳定的 `Assets/Scripts/Activity/Fossick/Core` 为核心业务逻辑权威源，将其回归到 FossickDemo，并保证 MapStudio、Preview、Runtime 与三份地图配置能够继续独立运行，最终产出可交付的 macOS 包。

## 回归边界

- 回归 HOP `Core` 下全部正式 C# 实现，包括新增的生成、重力、校验、奖励提供和动画上下文能力。
- 保留 FossickDemo 自己的程序集定义和独立工程目录结构，不复制 HOP 的 `.meta`。
- 不回归 HOP 的 `Controller`、`Model`、`QuestProvider`、`View`、`Cheat`、活动生命周期、账号存储和正式发奖外壳。
- 三份地图配置以 HOP 当前配置为权威源：
  - `FossickFragmentLibrary.json`
  - `FossickGenerationRules.json`
  - `FossickMapDefinition.json`
- MapStudio、Preview、Runtime 只做适配 HOP Core 所需的修改；HOP 专属资源加载和编辑器桥接不直接进入独立工程。
- 不新增测试函数或测试程序集；使用 Unity 编译、场景依赖检查和 macOS 构建验证。

## 实施阶段

### 1. Core 回归

- 同步 HOP Core 中所有新增和变更的 C# 文件。
- 保留独立工程 `Fossick.Core.asmdef`。
- 审查独立工程仅有的 `FossickPreviewCommand`、`FossickSampleMapFactory`、`BackgroundRegion`，删除已被正式模型替代的类型，保留确属独立试玩入口需要的能力。
- 扫描 HOP Core 是否引用 HOP 活动层类型；若有，通过 Core 接口或独立工程适配实现消除依赖。

### 2. 配置与表现接入

- 用 HOP 当前三份 JSON 覆盖 FossickDemo 产品地图配置。
- 对齐 MapStudio 的序列化、校验、生成规则编辑和预览入口。
- 对齐 Preview/Runtime 对 `FossickActionResult`、`FossickSnapshot`、奖励背景、附件、重力和下移结果的消费。
- 保持本地 `Application.persistentDataPath` 工作流和 Mac 构建时 StreamingAssets 初始化流程。

### 3. 验证与修复

- 运行 Unity 2022.3.62f2 batchmode 编译，清理全部 C# 编译错误。
- 检查 MapStudio 与 Preview 场景引用、Build Settings 场景顺序和 Prefab 丢失引用。
- 静态检查 Core 不反向依赖 MapStudio、Preview、Runtime 或 HOP 活动层。
- 检查三份 JSON 可被组合加载，模板数量和生成规则非空。

### 4. Mac 交付

- 更新策划使用文档中与当前界面不一致的步骤。
- 使用 `FossickMacBuild.BuildPlaytestMac` 构建窗口化 macOS App。
- 检查 `.app` 内两场景与三份初始 JSON 均存在。
- 覆盖 `Delivery/FossickMapStudioPlaytest_Mac.zip`，保留独立工程源码数据不被运行时编辑覆盖。
