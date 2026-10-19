# 深渊猎手

《深渊猎手》（Abyss Hunter）是个人独立完成的 3D 俯视角动作原型。用 Unity 6 LTS 从空场景做到可运行的 Windows 包，覆盖战斗、AI、资源加载、对象池、背包与流程闭环，作为求职作品集项目。

**引擎：** Unity 6.3 LTS（URP）  
**平台：** Windows x64  
**输入：** Unity Input System  
**运行包：** `Builds/Portfolio/AbyssHunter.exe`（包含重击敌人与新版界面）

当前为《深渊猎手》展示版，首关名为“沉没遗迹追猎”，保留角色替换前的原有模型。[版本改动记录](CHANGELOG.md)包含功能说明、验证结果与后续范围。运行包在本地构建，未包含在源码仓库中。

## 玩法

- WASD 移动，鼠标左键近战，空格闪避
- 菜单、操作说明、战斗提示及结算均为简体中文，内置中文字体
- 沉没遗迹追猎：入口、守卫庭院、核心祭坛三处空间遭遇，通过断桥探索
- 普通敌人与重击敌人：观察扇形前摇、躲开锁向落锤、利用后摇击退反击；祭坛为混合编队
- 清除三处威胁后按 E 取回核心，沿亮起的符文返回入口，投入四枚碎片点亮信标，再按 E 撤离
- 每名敌人必掉一枚深渊碎片，靠近自动拾取；生命药水随机掉落，“I”打开背包后左键点击使用
- Esc 暂停，R 重开本局，M 返回主菜单
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
  SampleScene.unity   战斗关
```

敌人 Prefab 走 Addressables Default Local Group。改 Enemy 后需执行：

`Addressables Groups → Build → New Build → Default Build Script`

## 操作

| 按 键 | 功 能 |
| --- | --- |
| WASD | 移动 |
| 鼠标左键 | 攻击 |
| 空格 | 闪避 |
| I | 背包 |
| Esc | 暂停 / 主菜单退出 |
| R | 结算后重开 |
| M | 返回主菜单 |
| Enter | 主菜单开始 |
| E | 取回核心 / 点亮信标 / 撤离 |

外观资源来自 KayKit Free Sample Pack。音效目前为程序占位，后续可替换为 Kenney 等 CC0 素材。

重击机制、参数和运行验证见 [战斗说明](Docs/heavy-combat.md)。Unity 菜单 `Tools > 深渊猎手 > Verify heavy combat` 可复现集成检查。

关卡流程、素材和验证方式见 [沉没遗迹追猎](Docs/sunken-expedition.md)。
