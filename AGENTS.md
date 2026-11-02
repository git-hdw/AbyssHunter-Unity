# 《深渊猎手》开发规则

## 新任务入口
- 开始工作前必须阅读 [Docs/PROJECT_CONTEXT.md](Docs/PROJECT_CONTEXT.md) 和 [Docs/HANDOFF.md](Docs/HANDOFF.md)，再查看 `git status --short`、当前分支及最近提交。保留已有变更。
- 仓库文档目录的规范大小写是 `Docs/`（用户所称 `docs/`）；引用和新增文件统一使用 `Docs/`，不要创建大小写不同的第二套目录。
- 本文件记录长期规则；设计和历史结论放 PROJECT_CONTEXT；任务进度、验证日期和未完成项放 HANDOFF。根目录 HANDOFF 仅保留导航。任务结束时同步受影响的上下文，不把计划写成已完成。

## 目标与组织
- 单人 3D 俯视角动作求职 Demo，首关“沉没遗迹追猎”，Windows x64；游戏名、玩家 UI 和操作说明使用简体中文。
- Unity 6000.3.23f1、C#、URP、Input System、AI Navigation、Addressables、UGUI；版本以 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json` 为准，不顺手升级依赖。
- 运行时代码放 `Assets/_Game/Scripts/` 对应领域目录，命名空间 `AbyssHunter.<领域>`；编辑器代码放 `Assets/_Game/Editor/`，命名空间 `AbyssHunter.EditorTools`。沿用邻近文件风格，类型/方法/公开属性 PascalCase，字段/局部变量 camelCase；可配置字段优先私有 `[SerializeField]`，不要全项目格式化。
- 组件按职责组合，通过接口、事件和协程协作；订阅与退订、创建与释放成对管理。协程不是多线程。保持现有非分配查询、缓存和池复用路径。

## 必须保持的兼容性
- 物品堆叠、统计、扣除统一调用 `ItemData.HasSameIdentity`：同一对象或非空且区分大小写的稳定 Item ID。不同物品使用不同 ID；数量不足不得部分扣除。
- 玩家攻击由动画事件结算且每次命中去重，闪避可取消待命中攻击；重击由阶段时钟结算，不能再由旧动画事件重复伤害。换动画需核对事件转发、Humanoid Avatar、Root Motion 关闭及重击 `animationContact`。
- 池化敌人必须恢复生命、动画、寻路、攻击阶段和预警，避免重复死亡/回收通知；Addressables 句柄随池生命周期释放。
- `GamePresentation` 是屏幕 UI、输入阻断和暂停的统一入口；保留世界空间血条，勿重新启用旧 UI 控制器。暂停时的展示动画使用受限非缩放时间，离场恢复时间状态。
- 不随意重构 `EnemyController` 战斗时序、`EnemyPool/PooledEnemy` 生命周期、背包身份规则、`GamePresentation/SceneFlow/MatchResult` 流程及 `PortfolioTools` 构建链路。相关需求确需修改时，先核对调用方、序列化引用和回归范围。
- 保留 `.meta`/GUID、Prefab 逻辑根、Addressables 引用、序列化字段及动画事件方法兼容性；改名须迁移引用。旧 UI、检查点、机器人资源不能仅凭“看似不用”删除，先查依赖。
- `Configure`、角色配置和环境应用菜单会重建资源、覆盖手改，不是启动步骤。保留 KayKit 与字体许可。当前远征不支持中途存档，不要启用旧 F5/F9 作为替代。

## 编译、验证与交付
- 用指定版本 Unity 打开工程，等待导入和脚本编译，检查 Console 错误；从 `Assets/Scenes/MainMenu.unity` Play 检查全流程。已有编辑器占用工程时复用该实例，不另开同工程批处理。
- 修改战斗/池运行 `Tools > 深渊猎手 > Verify heavy combat`；任务/背包运行 `Verify sunken expedition`；UI/暂停运行 `Verify Chinese presentation`；环境/镜头运行 `环境 > 验证微光与环境表现`。运行前保存场景，逐项完成后读取报告；细节见 PROJECT_CONTEXT。
- 涉及敌人、掉落、物品身份或资源配置，必须重建 Addressables 并做实际 Player 回归，执行步骤见 `Docs/inventory-bundle-regression.md`。编辑器 Asset Database 通过不能代替此项。
- 发布用 `Tools > 深渊猎手 > 构建 > 构建 Windows 发布版`，检查 `Logs/portfolio-build.txt` 成功结果和错误/警告。新发布使用新版本，不覆盖 v0.2.0 基线。
- 仅文档修改检查事实、路径、互相引用和 `git diff --check`，无需重打游戏。报告必须区分本次验证、历史验证、实现但未验证及计划；现有检查不是 Unity Test Runner 单元测试数量。
- 未经用户授权不提交、推送、合并；提交应包含相关文档、源码和必要 `.meta`，不包含 `Library/`、`Temp/`、`Logs/`、`Builds/` 等忽略产物。运行包经 Release 分发。
