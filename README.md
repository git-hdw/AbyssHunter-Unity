# 深渊猎手

《深渊猎手》（Abyss Hunter）是个人独立完成的 3D 俯视角动作原型。用 Unity 6 LTS 从空场景做到可运行的 Windows 包，覆盖战斗、AI、资源加载、对象池、背包与流程闭环，作为求职作品集项目。

**开发接手：** 依次阅读 [长期开发规则](AGENTS.md)、[项目长期知识](Docs/PROJECT_CONTEXT.md) 和 [最新任务交接](Docs/HANDOFF.md)，了解架构约束、验证证据及下一步任务。

**当前源码版本为 v0.3.0。** 两关远征支持出战前选择猎剑、重斧或轻弩。首关成功撤离后解锁「裂隙回廊」，挑战守望台与中央浮岛的近战守卫和烬火法师。本机按账号记录通关与最佳用时。[v0.3.0 发布说明](Docs/releases/v0.3.0.md) · [账号与第二关](Docs/accounts-and-depths.md)

**运行源码**：用 Unity Hub 打开工程，选择 Unity 6000.3.23f1，安装 Windows Build Support，等待资源导入与编译。打开 `Assets/Scenes/MainMenu.unity` 进入 Play，联网登录后选择关卡与武器。主要依赖为 URP 17.0.1、Input System 1.16.0、AI Navigation 2.0.9、Addressables 2.9.1 和 UGUI 2.0.0。

**引擎：** Unity 6.3 LTS（URP）  
**平台：** Windows x64  
**输入：** Unity Input System  
**演示视频：** [《深渊猎手》v0.2.0 实机演示（MP4）](https://github.com/git-hdw/AbyssHunter-Unity/releases/download/v0.2.0/AbyssHunter-Demo-v0.2.0.mp4) · [视频与运行包发布页](https://github.com/git-hdw/AbyssHunter-Unity/releases/tag/v0.2.0)

**运行包：** [下载 Windows x64 v0.2.0](https://github.com/git-hdw/AbyssHunter-Unity/releases/download/v0.2.0/AbyssHunter-Windows-v0.2.0.zip) · [版本介绍与校验文件](https://github.com/git-hdw/AbyssHunter-Unity/releases/tag/v0.2.0)。完整解压后运行 `AbyssHunter.exe`，无须安装 Unity。

当前为《深渊猎手》展示版，首关名为“沉没遗迹追猎”，采用兜帽猎手与骷髅敌人，并加入随身护火灯、分层深渊环境及归途点火表现。[版本改动记录](CHANGELOG.md)包含功能说明，[环境与微光说明](Docs/abyss-atmosphere.md)记录实现和验证方式。运行包未包含在源码仓库中。

发布说明与验证范围见 [v0.2.0 发布说明](Docs/releases/v0.2.0.md)。在 Unity 中执行 `Tools > 深渊猎手 > 构建 > 构建 Windows 发布版`，会重建 Addressables 并输出到 `Builds/Windows-v<版本号>/`。

## 玩法

- WASD 移动，鼠标左键使用出战武器，空格闪避
- 菜单、操作说明、战斗提示及结算均为简体中文，内置中文字体
- 沉没遗迹追猎：入口、守卫庭院、核心祭坛三处空间遭遇，通过断桥探索
- 普通敌人与重击敌人：观察扇形前摇、躲开锁向落锤、利用后摇击退反击；祭坛为混合编队
- 清除三处威胁后按 E 取回核心，沿亮起的符文返回入口，首关投入四枚碎片、第二关投入六枚碎片点亮信标，再按 E 撤离
- 每名敌人必掉一枚深渊碎片，靠近自动拾取；生命药水随机掉落，“I”打开背包后左键点击使用
- Esc 暂停，结算时 R 重开本关、M 返回选关
- 本关为单次远征，不提供中途回档；原有玩家数据检查点不适用于关卡任务，已在远征中停用

## 工程结构

```
Assets/_Game/
  Scripts/     角色、战斗、敌人、背包、存档、UI
  Prefabs/     Enemy、拾取物
  Animations/  PlayerAnimator、EnemyAnimator
  Materials/   角色、侵蚀裂隙与遗迹材质
Assets/Scenes/
  MainMenu.unity      启动场景
  SampleScene.unity   沉没遗迹追猎
  AbyssDepths.unity   裂隙回廊
```

敌人 Prefab 走 Addressables Default Local Group。改 Enemy 后需执行：

`Addressables Groups → Build → New Build → Default Build Script`

## 操作

| 按 键 | 功 能 |
| --- | --- |
| WASD | 移动 |
| 1 / 2 / 3 | 出发前选择猎剑 / 重斧 / 轻弩 |
| 鼠标左键 | 使用本局武器攻击 |
| 空格 | 闪避 |
| I | 背包 |
| Esc | 暂停 / 主菜单退出 |
| R | 结算后重开 |
| M | 暂停时返回主菜单，结算时返回选关 |
| Enter | 主菜单开始 |
| E | 取回核心 / 点亮信标 / 撤离 |

角色外观使用 KayKit Adventurers 2.0 FREE 的兜帽猎手与 Skeletons 1.1 FREE 的骷髅斥候、重锤守卫；两套素材均为 CC0，第三方美术来源与接入方式见 [角色模型说明](Docs/character-models.md)。音效使用程序合成。

重击机制、参数和运行验证见 [战斗说明](Docs/heavy-combat.md)。Unity 菜单 `Tools > 深渊猎手 > Verify heavy combat` 可复现集成检查。

关卡流程、素材和验证方式见 [沉没遗迹追猎](Docs/sunken-expedition.md)。

## 后续规划

v0.4.0 拟优先打磨战斗反馈并探索护火灯交互，再验证新关卡与 Boss 原型。阶段目标和验收边界见 [项目愿景](Docs/PROJECT_VISION.md)。
