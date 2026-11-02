# 项目长期知识

事实核对日期：2026-11-02。仓库根目录为路径基准；本机位置 `F:/UnityProjects/AbyssHunter`，新克隆不依赖该绝对路径。先读根目录 [AGENTS.md](../AGENTS.md)，当前进度见 [HANDOFF.md](HANDOFF.md)。

## 产品与已确定设计

《深渊猎手》（Abyss Hunter）是面向 Unity 游戏客户端求职展示的单人 3D 俯视角动作 Demo。通过源码、Windows 包和实机视频展示完整玩法、重击决策、资源管理及问题排查能力。首关名为“沉没遗迹追猎”，不是游戏名。玩家是兜帽持剑猎手，敌人为普通骷髅与重锤守卫，界面使用简体中文。

- 三段遭遇按顺序接近触发：入口 2 普通 → 庭院 1 重击 → 祭坛 2 普通 + 1 重击。
- 清除全部敌人后 E 取核，返回入口消耗 4 枚碎片点灯，再 E 撤离；清场不直接胜利。6 名敌人各掉 1 枚碎片，消费后剩 2 枚。核心是任务状态，不占背包；药水随机掉落，经背包使用。
- 重击强调识别预警、闪避、后摇反击。`Assets/_Game/Combat/BruteAttack.asset`：蓄力 0.8 秒（追踪 0.3、随后锁向）、落锤 0.12 秒、范围 3 米/110°、伤害 25、后摇 1.1 秒；首次后摇受击可击退并延长 0.25 秒，不能无限延长。橙色预警、青色反击窗口与环境装饰配色保持可辨识。
- 环境装饰不应无意增加阻路碰撞，保留既有行走路线与导航。模型和环境已保存为资产，打开工程无需重新生成。

## 环境与依赖

`ProjectSettings/ProjectVersion.txt`：Unity **6000.3.23f1**；`Packages/manifest.json`：URP **17.0.1**、Input System **1.16.0**、AI Navigation **2.0.9**、Addressables **2.9.1**、UGUI **2.0.0**。目标 Windows x64，`ProjectSettings/ProjectSettings.asset` 当前 bundleVersion 为 **0.3.0**（尚未发布）。

Test Framework、Timeline、Visual Scripting 的包存在不等于已建立对应测试/玩法体系。玩家目前直接读取 Input System 设备状态；敌人池为 `Queue<PooledEnemy>` 自建实现；加载等待使用协程和 AsyncOperationHandle，不应描述成 Task 多线程系统。本机编辑器历史路径为 `D:/6000.3.23f1/Editor/Unity.exe`，换机器须重新定位。

## 核心链路和生命周期

1. `Assets/Scenes/MainMenu.unity` → 邮箱登录 → 选关 → `SceneFlow.LoadLevel()` → SampleScene 或 AbyssDepths。无参 LoadGame 兼容第一关，RestartLevel 重开当前关。`SceneFlow` 切场景前恢复 `Time.timeScale`；菜单编辑态空场景是正常状态，人物与遗迹在 SampleScene。
2. `GamePresentation` 在运行时注册 `sceneLoaded`，为主菜单及关卡目录中的场景创建各自的展示管理器和 Canvas，停用旧屏幕 UI，保留世界空间血条。Login/LevelSelect/Menu/Intro/Playing/Paused/Inventory/Finishing/Result/Transition 统一管理输入和时间；HUD 数据来源仍是玩法组件。场景销毁解除订阅并恢复时间状态，重开重建场景状态。
3. `EnemyPool.Start()` 异步加载 Addressable Prefab，检查结果后预热；`EnemyWaveSpawner` 等两个池就绪再开始遭遇。池加载失败则停止生成。出池启用组件并重置状态；`Health.Died` 触发 `PooledEnemy` 延迟回收，`Defeated` 通知生成器扣减计数，然后停用入队；池销毁释放资源句柄。
4. 玩家输入 → `PlayerMovement`/`PlayerMeleeAttack` → Animator → `AttackAnimationEventRelay` → 物理查询、`HashSet<IDamageable>` 去重 → `Health` 事件 → 受击反馈/UI/死亡。闪避取消待命中攻击。敌人 `EnemyController` 使用 NavMeshAgent + FSM；重击阶段时钟负责伤害，旧动画命中事件被忽略。
5. `EnemyItemDrop` → `PickupItem` → `PlayerInventory`；`InventorySlot`、计数、扣除均使用 `ItemData.HasSameIdentity`。`ItemDatabase` 按 ID 查资产，`HealingItemData.TryUse` 通过多态实现治疗。
6. `AbyssExpedition` 读取遭遇进度和背包碎片，维护 HasCore/BeaconLit/Extracted；交互先检查存活、结算和 UI 阻断。公告/完成事件交给展示层，`MatchResult` 标记对局结束。`AbyssAtmosphere`、`LastLight`、`AbyssSoundscape` 跟随玩法状态驱动归途灯火、镜头和声音。

## 文件地图

下表中目录后的文件名均属于该目录，保持实际大小写。

| 领域 | 真实路径与入口 |
| --- | --- |
| 移动、闪避、镜头 | `Assets/_Game/Scripts/Character/`：`PlayerMovement.cs`、`PlayerAnimationController.cs`、`TopDownCamera.cs` |
| 命中与反馈 | `Assets/_Game/Scripts/Combat/`：`PlayerMeleeAttack.cs`、`AttackAnimationEventRelay.cs`、`IDamageable.cs`、`Health.cs`、`PlayerHitFeedback.cs`、`DamageFlash.cs` |
| AI、池、掉落 | `Assets/_Game/Scripts/Enemy/`：`EnemyController.cs`、`EnemyState.cs`、`EnemyAnimationController.cs`、`EnemyPool.cs`、`PooledEnemy.cs`、`EnemyWaveSpawner.cs`、`EnemyItemDrop.cs`、`HeavyAttackProfile.cs`、`HeavyAttackPresentation.cs` |
| 背包 | `Assets/_Game/Scripts/Inventory/`：`ItemData.cs`、`InventorySlot.cs`、`PlayerInventory.cs`、`PickupItem.cs`、`ItemDatabase.cs`、`HealingItemData.cs` |
| 物品资产 | `Assets/_Game/Items/`：`AbyssShard.asset`、`HealthPotion.asset`、`ItemDatabase.asset` |
| 任务和环境 | `Assets/_Game/Scripts/World/`：`AbyssExpedition.cs`、`AbyssAtmosphere.cs`、`LastLight.cs`、`AbyssOccluder.cs`、`AbyssSoundscape.cs` |
| UI 与场景流程 | `Assets/_Game/Scripts/UI/`：`GamePresentation.cs`、`PresentationTheme.cs`、`SceneFlow.cs`、`MatchResult.cs`、`GameFont.cs` |
| 账号与多关卡 | `Assets/_Game/Scripts/UI/`：`PlayFabAuthentication.cs`、`GamePresentation.Account.cs`、`LevelCatalog.cs`；`Scripts/Save/AccountProgress.cs`；`Resources/PlayFabConfig.json`（均位于 `Assets/_Game/` 下） |
| 第二关 | `Assets/Scenes/AbyssDepths.unity`、`Assets/_Game/Combat/AbyssDepthsNavMesh.asset`、`Assets/_Game/Editor/FloatingRiftMapBuilder.cs`、`Assets/_Game/Art/FloatingRift/`；旧地图迁移工具 `DepthsLevelSetup.cs` 保留 |
| 敌人逻辑根 Prefab | `Assets/_Game/Prefabs/Enemy.prefab`、`Assets/_Game/Prefabs/HeavyEnemy.prefab` |
| 显示 Prefab | `Assets/_Game/Prefabs/Characters/`：`HunterVisual.prefab`、`SkeletonVisual.prefab`、`GuardianVisual.prefab` |
| Animator | `Assets/_Game/Animations/`：`PlayerAnimator.controller`、`EnemyAnimator.controller`、`HeavyAnimator.controller` |
| 环境与导航 | `Assets/_Game/Art/Abyss/`、`Assets/_Game/Shaders/AbyssMist.shader`、`Assets/_Game/Combat/SunkenRuinsNavMesh.asset` |
| 构建配置 | `ProjectSettings/EditorBuildSettings.asset`、`Assets/AddressableAssetsData/`（敌人 Default Local Group） |
| 编辑器工具 | `Assets/_Game/Editor/`：`AbyssLevelSetup.cs`、`HeavyEnemySetup.cs`、`CharacterModelSetup.cs`、`ChinesePresentationSetup.cs`、`AbyssArtSetup.cs`、`PortfolioTools.cs` |
| 视频 | `Assets/_Game/Scripts/UI/PortfolioCapture.cs`、`Tools/Encode-Portfolio.ps1` |
| 遗留检查点 | `Assets/_Game/Scripts/Save/SaveManager.cs`、`Assets/_Game/Scripts/Save/GameSaveData.cs` |

## 技术决策与历史结论

- **物品使用稳定 ID**：场景与 AssetBundle 会生成同一物品的不同 ScriptableObject 实例。曾出现背包 6 枚、任务 0/4 的成品包故障。最终统一按同一对象或非空、Ordinal ID 匹配，覆盖堆叠/统计/消费；不足不扣除。实际包验证了 6 → 投入 4 → 剩 2 → 撤离。不可退回引用相等判断。详见 [独立包回归](inventory-bundle-regression.md)。
- **重击伤害与动画解耦**：重击追踪/锁向/落锤/后摇由时钟控制，避免动画事件重复结算；玩家攻击保留事件驱动。替换模型时保留逻辑根与 Addressables GUID，通过 Humanoid 和事件转发接入；`animationContact` 需随重击动画同步。
- **统一展示与暂停**：旧 UI 并存曾有维护风险；当前入口为 GamePresentation。中文裁切已通过调整文本框并检查实际可见字符修复。开场暂停镜头不动、载入大时间步跳过动画，采用受限非缩放时间解决；菜单相机深色背景解决天空盒闪现。
- **单次远征边界**：旧检查点仅在内存保存玩家与背包，不保存敌人、任务或整场生命周期，因此存在远征时禁止 F5/F9。历史死亡后加载造成状态不一致，现拒绝死亡/结算后回滚。不能宣称已支持中途存档。
- **构建资源只生成一次**：PortfolioTools 先 BuildPlayerContent，临时关闭 Player 阶段重复 Addressables 构建，finally 恢复；非 Succeeded 明确报错，防止将 Unknown 当成功。保留此顺序。
- **美术许可和旧依赖**：KayKit Adventurers/Skeletons 使用 CC0；Noto 字体使用 SIL OFL。许可分别在 `Assets/KayKit/Characters/Adventurers/License.txt`、`Assets/KayKit/Characters/Skeletons/License.txt`、`Assets/_Game/Resources/Fonts/OFL.txt`。旧机器人资源可能仍被动画引用，未完成依赖检查前保留。

## 编译与验证操作

用匹配版本 Unity 打开工程并等待编译，先排除 Console 编译错误；这是 Unity 工程，不以单独 `dotnet build` 代替资产导入和 Player 构建。已有编辑器打开工程时直接使用它。验证前保存场景，以下菜单在编辑器中进入 Play，完成后检查报告及运行异常；不要将异步 Play 验证直接套用 `-executeMethod ... -quit`。

菜单前缀均为 `Tools > 深渊猎手 >`：

| 修改范围 | 菜单 | 报告 |
| --- | --- | --- |
| 战斗、敌人池 | `Verify heavy combat` | `Logs/heavy-combat-verification.txt` |
| 任务、背包、重开 | `Verify sunken expedition` | `Logs/expedition-verification.txt` |
| UI、暂停、转场 | `Verify Chinese presentation` | `Logs/presentation-verification.txt` |
| 环境、灯火、镜头 | `环境 > 验证微光与环境表现` | `Logs/atmosphere-verification.txt` |
| 模型引用 | `角色 > 检查模型引用` | `Logs/character-model-validation.txt` |
| 实际资源包 | `验证 > 构建碎片回归开发包`，再执行 [PowerShell 复测步骤](inventory-bundle-regression.md#复测) | `Logs/inventory-bundle-verification.txt`；末行 `DONE \| PASSED` 且退出码 0 |
| 发布构建 | `构建 > 构建 Windows 发布版` | `Logs/portfolio-build.txt` |

集成驱动位于 `Assets/_Game/Scripts/Enemy/HeavyCombatVerification.cs`、`Assets/_Game/Scripts/World/ExpeditionVerification.cs`、`AtmosphereVerification.cs`（同目录）、`Assets/_Game/Scripts/UI/PresentationVerification.cs`；独立包驱动为 `Assets/_Game/Scripts/Inventory/InventoryBundleVerification.cs`，只在编辑器/开发构建编译且须传入 `--inventory-report=<绝对路径>`。独立回归使用无图形、受控移动、无敌和直接伤害，仅证明逻辑，不能替代人工操作、画面和听音。

发布输出 `Builds/Windows-v<bundleVersion>/AbyssHunter.exe`；碎片开发包输出 `Builds/InventoryVerification/`。`Builds/Portfolio/` 为旧入口产物，不能用作当前修复版依据。重建工具 `Configure sunken expedition`、角色配置和环境应用会覆盖生成区域的手工修改，不要为“启动游戏”执行。

## 限制、证据与维护

当前工作区新增第二关“裂隙回廊”和 PlayFab 邮箱认证入口；第一关成功撤离后按账号在本机解锁第二关，无整场持久化存档。Title ID 已配置为 1302F5，真实账号与恢复邮件仍待验收。屏幕 UI 集中于运行时创建，旧 UI/检查点代码仍留存；尚未做大规模拆分或清理。战斗时序、池复用、物品身份、暂停/结算、场景重建和 GUID 迁移均是高风险区域。

发布历史：`40f33a3` 为角色替换前基线，`e41e6b4` 发布 v0.2.0，`c31aed2` 补充视频入口。既有交付记录确认上传完成，参见 [v0.2.0 Release](https://github.com/git-hdw/AbyssHunter-Unity/releases/tag/v0.2.0)；本次未重新联网核验。用户原视频历史路径 `F:/AbyssHunter演示视频.mp4` 不属于仓库依赖。

`Builds/`、`Releases/`、`Logs/`、`Recordings/`、`Library/`、`Temp/` 等被忽略，新克隆不会获得本地报告、视频和运行包。关键验证结论须写入可提交文档，原始产物按需经 Release 分发。人工完整听音、视频评审及带硬件/分辨率/固定路线的 Player 性能报告尚缺记录；旧编辑器采样不能视为成品性能保证。当前证据和待办以 HANDOFF 为准。

详细设计按需阅读：`Docs/heavy-combat.md`、`Docs/sunken-expedition.md`、`Docs/character-models.md`、`Docs/abyss-atmosphere.md`、`Docs/presentation.md`；发布说明 `Docs/releases/v0.2.0.md`，历史变更 `CHANGELOG.md`。

## v0.3.0 工作区增量（2026-10-31）

- 账号和第二关的配置、职责及限制见 [accounts-and-depths.md](accounts-and-depths.md)。PlayFabConfig.json 已配置公开 Title ID 1302F5；缺少配置时不允许正式登录；不提供游客入口。
- 第二关独立场景/导航，2026-11-01 从零重制为浮空裂隙：三层浮岛、八米宽升空桥、水平转台、中央祭坛与西侧补给悬台。3 普通 → 1 普通+1 重锤 → 2 普通+2 重锤，9 枚碎片投入 6、剩 3；侧路固定药水。第一关原始地图、共享角色/敌人资产及战斗参数保留。
- GamePresentation.Account 负责账号表单和选关，PlayFabAuthentication 负责 HTTPS Client API 与进程内身份，AccountProgress 按 Title ID/PlayFabId 隔离本机进度，LevelCatalog 管理关卡定义。
- HUD 碎片数量现在直接读取远征的稳定 ID 统计，修正旧版本将所有非治疗物品视为碎片的扩展风险。
- 新增字段保持第一关默认值：任务文案、声源坐标、镜头区域和归途点亮顺序。第二关创建工具不会重写第一关；新场景有单独 NavMesh 资产。
- 复制并移动关卡时，不能只移动预合并环境的 Transform：网格顶点可能仍使用第一关坐标。旧版第二关曾因此错位并重合批；该布局现已被全新多边形地图替代，新资产位于 `Assets/_Game/Art/FloatingRift/`。旧迁移工具遇到浮空地图标记会跳过，不可覆盖第一关环境合批资产。
- 验证结果以最新 HANDOFF 为准；账号夹具不是真实 PlayFab 认证，逻辑回归不代表完整人工听音或操作验收。

## 第一关美术增量（2026-11-02）

SampleScene 的光照、独立 Volume、石板/柱体显示网格、入口拱券、火盆、桥侧细节、祭坛断环与深渊岩层已优化。生成源为 AbyssArtSetup 及其 Portfolio partial，新资产在 Assets/_Game/Art/SunkenPolish/；不修改第二关或共享旧材质。326 个原碰撞体及其 Transform、第一关 NavMesh 和玩法配置保留。美术细节见 [abyss-atmosphere.md](abyss-atmosphere.md)，本轮运行证据见 HANDOFF。

## 第二关美术增量（2026-11-02）

AbyssDepths 已增加独立 RiftArtPolish 显示层：区域石材/裂纹、缺角护墙、倒悬岩瓣与碎片、三层深渊背景/雾/低量浮尘、起航青蓝晶簇/铜罗盘、中段紫色尖碑、终点橙金核心/仪式断环及独立 RiftVolume。新资产位于 Assets/_Game/Art/RiftPolish/，地图生成器同步调用精修流程。407 个原碰撞体和 Transform、独立 NavMesh、关卡 ID/流程保留，第一关场景未改。详见 accounts-and-depths 与 HANDOFF。

第二轮造型由 RiftArtPolish.Architecture.cs 同步生成：带斜切截面的分层石环、八角晶体设施与弧肋、肩切晶体、祭坛浮雕/开放悬冠、中段断续放射铺石及原井口内衬、沉积岩层与六处远景断拱。地面改用周期不规则石铺纹理。所有新增建筑为无碰撞显示网格；行走区仅有浅浮雕，较高祭坛悬冠前半部留空。继续按材质与空间区块合批，不增加动态灯或粒子预算；修改前后绘制采样及验收边界记录在 HANDOFF。
