# 《深渊猎手》角色模型

当前角色外观使用 Kay Lousberg 的免费 CC0 素材。角色美术为第三方资源；战斗、关卡与界面实现属于本项目工作。

| 游戏角色 | 官方模型 | 版本 |
| --- | --- | --- |
| 兜帽猎手 | Rogue_Hooded、sword_1handed | Adventurers 2.0 FREE |
| 骷髅斥候 | Skeleton_Minion、Skeleton_Blade | Skeletons 1.1 FREE |
| 骷髅重锤守卫 | Skeleton_Warrior，搭配项目已有重锤 | Skeletons 1.1 FREE |

官方来源：[Adventurers](https://kaylousberg.itch.io/kaykit-adventurers)、[Skeletons](https://kaylousberg.itch.io/kaykit-skeletons)。仅使用免费包，不包含付费的 Skeleton Golem、Necromancer 或 EXTRA 贴图。原始许可分别保存在 `Assets/KayKit/Characters/Adventurers/License.txt` 与 `Assets/KayKit/Characters/Skeletons/License.txt`。

## 接入方式

角色使用 Unity 原生 FBX 导入、URP Lit 材质，以及项目已有的 KayKit Humanoid Avatar 和 Animator。FBX 只包含模型与骨架，显示预制体显式添加 Animator 并绑定 Avatar。角色移动由原有 CharacterController / NavMeshAgent 驱动，关闭 Root Motion。

`Assets/_Game/Prefabs/Characters/` 存放独立显示预制体。玩家根对象、两个敌人预制体的 GUID、生命值、碰撞体、掉落和 Addressables 引用继续沿用。动画事件仍由显示模型上的 `AttackAnimationEventRelay` 转发给根对象；重击前摇、方向锁定、伤害与后摇仍由原有重击状态机控制。

旧机器人素材暂时保留在 KayKit Free Sample 目录；当前场景和敌人不再实例化它们。替换前基线已保存在 Git 提交 `40f33a3`，避免在新外观确认前清理仍被动画资源依赖的文件。

## 编辑器工具

- `Tools > 深渊猎手 > 角色 > 配置猎手与骷髅模型`：重新创建显示预制体并替换玩家、敌人的显示子对象，会覆盖这些显示对象的手动修改。
- `Tools > 深渊猎手 > 角色 > 检查模型引用`：检查 Avatar、蒙皮骨骼及伤害事件组件，报告输出到 `Logs/character-model-validation.txt`。
- `Tools > 深渊猎手 > 角色 > 验证动画并导出对比图`：在独立空场景检视待机、奔跑、攻击、受击、死亡和闪避；报告输出到 `Logs/character-model-playtest.txt`，图片输出到 `Logs/character-review/`。这是验证工具，不是正常游戏演示录像。

战斗与任务回归继续使用项目已有的重击、远征和中文界面验证工具。

## 本次验证（2026-10-19）

在 Unity 6000.3.23f1 编辑器 Play Mode 中验证：角色骨骼、材质、武器挂点和动画 44 项通过；重击战斗 41 项通过；远征任务 56 项通过；中文界面交互 49 项通过，均无失败或运行异常。

检查了待机、奔跑、攻击前摇与命中、受击、死亡、玩家闪避的模型截图。界面回归中修复了缩放后弹层标题、结算统计数字被裁切的问题，并补充实际可见字符检查。报告与截图保存在本地 `Logs/`，不纳入版本控制。

角色替换阶段未生成 Windows 包。2026-10-20 后续已完成环境集成、Windows 正式包构建及碎片独立包回归，运行包与最终验证范围见 [v0.2.0 发布说明](releases/v0.2.0.md)。
