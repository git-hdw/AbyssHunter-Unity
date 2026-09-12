# Abyss Hunter

个人独立完成的 3D 俯视角动作原型。用 Unity 6 LTS 从空场景做到可运行的 Windows 包，覆盖战斗、AI、资源加载、对象池、背包与流程闭环，作为求职作品集项目。

**引擎：** Unity 6.3 LTS（URP）  
**平台：** Windows x64  
**输入：** Unity Input System  
**运行包：** `Builds/WindowsRelease`

## 玩法

- WASD 移动，鼠标左键近战，空格闪避
- 三波敌人清场，通关显示 Victory
- 掉落深渊碎片与生命药水，I 打开背包，右键使用药水
- Esc 暂停，R 重开本局，M 返回主菜单
- F5 保存本局检查点，F9 回滚；退出或重开后不保留进度

## 工程结构

```
Assets/_Game/
  Scripts/     角色、战斗、敌人、背包、存档、UI
  Prefabs/     Enemy、拾取物
  Animations/  PlayerAnimator、EnemyAnimator
  Materials/   角色与竞技场材质
Assets/Scenes/
  MainMenu.unity      启动场景
  SampleScene.unity   战斗关
```

敌人 Prefab 走 Addressables Default Local Group。改 Enemy 后需执行：

`Addressables Groups → Build → New Build → Default Build Script`

## 操作

| 按键 | 功能 |
| --- | --- |
| WASD | 移动 |
| 鼠标左键 | 攻击 |
| 空格 | 闪避 |
| I | 背包 |
| Esc | 暂停 / 主菜单退出 |
| R | 结算后重开 |
| M | 返回主菜单 |
| Enter | 主菜单开始 |
| F5 / F9 | 本局检查点保存 / 读取 |

外观资源来自 KayKit Free Sample Pack。音效目前为程序占位，后续可替换为 Kenney 等 CC0 素材。
