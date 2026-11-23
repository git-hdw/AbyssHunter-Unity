# 项目长期知识

事实核对日期为 2026-11-21，本次核对两关远征前选武器。仓库根目录为路径基准；本机位置 `F:/UnityProjects/AbyssHunter`，新克隆不依赖该绝对路径。先读根目录 [AGENTS.md](../AGENTS.md)，长期目标与阶段验收见 [PROJECT_VISION.md](PROJECT_VISION.md)，当前详细进度见 [HANDOFF.md](HANDOFF.md)。

## 产品与已确定设计

选关后在出发页点击武器，或按 1、2、3 选择猎剑、重斧、轻弩。选择才会加载关卡，整局锁定。重开当前关沿用本次选择，结算页进入下一关时可重新选择。SceneFlow 仅在内存保留本局武器，HunterVisual 初始化时装备。猎剑每次伤害 25、间隔 0.5 秒。重斧使用 KayKit 双手长柄斧和双手横扫，右手握住柄底，斧柄随左手握点校正。横扫剪辑以 1.25 倍速播放，剪辑第 0.44 秒触发伤害事件，每目标去重结算 42 点，间隔 0.92 秒、判定半径 1.75 米、普通敌人击退 1.8 米。轻弩使用单手射击动画，按下攻击时确定瞄准点，发射事件所在帧在骨骼更新后从弩口生成弩矢。角色与弩口之间及后续飞行路径均检查实体遮挡。射程 12 米、速度 19 米/秒、首目标伤害 20、间隔 0.85 秒。两种武器在接触或发射前保持攻击朝向，闪避可取消待结算攻击。两关共用 HunterVisual；角色生成器调用 HunterWeaponSetup 更新挂点、剪辑、播放速度和动画事件。

遭遇期间，两关 HUD 显示已出场且仍能攻击的敌人数。普通骷髅提示挥砍，重锤守卫提示躲锤反击，烬火法师提示观察杖头聚火，重锤与法师同场时并列提示。敌人开始死亡动画就移出威胁提示，掉落、回收和遭遇结算仍走原时序。遭遇开始后隐藏触发点方位，清场后恢复下一目标的方向。

第二关持核时，GamePresentation 每 0.25 秒使用既有 NavMesh 查询到信标的完整可走路线和剩余距离。在有序归航符文连线三米内，方向采用符文路线前方的可通行点，临近转角再提示转弯。离开符文路线或前方被阻挡时，使用导航路径拐点。暂时取不到完整路径时显示“沿桥返回归途信标”。到达提示和点灯交互共用信标距离判断。

第二关守望台清场时，若玩家受伤且西侧悬台的固定药水仍在，清场横幅提示绕行取药和北桥返回路线。其余情况继续提示前往祭坛。固定拾取物的引用由浮岛地图生成器绑定，使用既有背包和治疗规则。

《深渊猎手》（Abyss Hunter）是面向 Unity 游戏客户端求职展示的单人 3D 俯视角动作 Demo。通过源码、Windows 包和实机视频展示完整玩法、重击决策、资源管理及问题排查能力。首关名为“沉没遗迹追猎”。玩家是兜帽持剑猎手，两关均有普通骷髅与重锤守卫，第二关另有烬火法师，界面使用简体中文。

第二关的 `RiftCaster.prefab` 是普通骷髅的 Addressables 变体，沿用原掉落与回收流程。法师在 8 米内且视线畅通时锁向，蓄力 0.9 秒，火球以每秒 8 米直线飞行，单次命中造成 12 点伤害。实体掩体可挡弹，击退打断蓄力，死亡与回收清除在途火球。守望台与中央祭坛共用一个预热实例。烬火法师使用 Skeleton_Mage、Skeleton_Staff 和独立的 CinderMageAnimator。举杖时火种随杖头移动，发射时切换到 Ranged_Magic_Shoot。CinderSpell 着色器在复用的面片上绘制火舌、轨迹与命中火星，伤害沿原弹道结算。独立配置菜单与完整地图生成器均调用 `RiftCasterSetup.Configure`。

- 三段遭遇按顺序接近触发：入口 2 普通 → 庭院 1 重击 → 祭坛 2 普通 + 1 重击。
- 清除全部敌人后 E 取核，返回入口消耗 4 枚碎片点灯，再 E 撤离；清场不直接胜利。6 名敌人各掉 1 枚碎片，消费后剩 2 枚。核心是任务状态，不占背包；药水随机掉落，经背包使用。
- 重击强调识别预警、闪避、后摇反击。`Assets/_Game/Combat/BruteAttack.asset`：蓄力 0.8 秒（追踪 0.3、随后锁向）、落锤 0.12 秒、范围 3 米/110°、伤害 25、后摇 1.1 秒；首次后摇受击可击退并延长 0.25 秒，不能无限延长。橙色预警、青色反击窗口与环境装饰配色保持可辨识。
- 环境装饰不应无意增加阻路碰撞，保留既有行走路线与导航。模型和环境已保存为资产，打开工程无需重新生成。
- 第二关起航浮岛入口的石拱门位于玩家往返路线上。左右拱墩各占门宽四分之一，中央门洞净宽 5.9 米。两墩与外侧两根高柱的实体由 `KayKitEntranceSetup` 随模型生成，各用一个盒形碰撞体和同尺寸的导航障碍。遮挡淡化仍由显示模型负责。运行包的入口回归同时覆盖角色与导航，回程从较窄引桥的可走区域斜向检查两侧拱墩。
- 重锤的地面扇形从蓄力起就显示完整危险区。HeavyAttackPresentation 在 LateUpdate 按控制器的 PhaseElapsed / windup 计算内弧半径，通过已有 MaterialPropertyBlock 传入 AttackTelegraph。锁向后保持暖红外框，落锤时隐藏内弧，后摇时整个扇形关闭。美术表现沿用同一个网格和材质，暂停与池复用沿用同一阶段时钟。
- 已造成伤害且重锤在后摇接受本次击退时，PlayerMeleeAttack 将该目标的迎剑侧火花改为青色，并在原 HitConfirmed 通知前汇总本刀是否命中过破绽。CombatSfxPlayer 每刀选择一次命中音，击杀优先于反击。HeavyAttackPresentation 读取控制器已有的后摇击退状态，在本次后摇余下时间显示“反击命中”。闪避不是反击的额外触发条件，侧移避锤后的有效反击同样生效。

左键按下时记录相机射线最先命中的非触发碰撞体。实际出刀时重新核对存活、启用、目标层和近战自动瞄准距离；点中有效近身目标则优先朝向它，点击空地或目标失效时沿用原近邻查询。选敌只控制朝向，伤害仍由动画事件的范围查询结算。重锤锁向末段的头顶文字改为浅金色“即将落锤”，与地面内弧共用阶段时钟，持续时间取 0.2 秒与锁向时长一半中的较小值。

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
- 第二关使用独立场景与导航，主路连接三层浮岛，西侧补给悬台有固定药水。起航区 3 名普通骷髅，守望台 1 重锤与 1 火法师，中央祭坛 2 重锤、1 普通与 1 火法师。清场可收集 9 枚碎片，信标消耗 6 枚，余下 3 枚。
- GamePresentation.Account 负责账号表单、启动时恢复登录和选关。Windows Player 通过 WindowsLoginTicket 在当前系统用户的凭据管理器中保存 PlayFab 会话票据，PlayerPrefs 只保存自动恢复开关。PlayFabAuthentication 启动时向服务验证票据，按返回的 PlayFabId 打开 AccountProgress。退出账号清除登录记忆，通关进度仍按 Title ID/PlayFabId 隔离。LevelCatalog 管理关卡定义。
- HUD 碎片数量现在直接读取远征的稳定 ID 统计，修正旧版本将所有非治疗物品视为碎片的扩展风险。
- 新增字段保持第一关默认值：任务文案、声源坐标、镜头区域和归途点亮顺序。第二关创建工具不会重写第一关；新场景有单独 NavMesh 资产。
- 复制并移动关卡时，不能只移动预合并环境的 Transform：网格顶点可能仍使用第一关坐标。旧版第二关曾因此错位并重合批；该布局现已被全新多边形地图替代，新资产位于 `Assets/_Game/Art/FloatingRift/`。旧迁移工具遇到浮空地图标记会跳过，不可覆盖第一关环境合批资产。
- 验证结果以最新 HANDOFF 为准；账号夹具不是真实 PlayFab 认证，逻辑回归不代表完整人工听音或操作验收。

## 第一关美术增量（2026-11-02）

SampleScene 的光照、独立 Volume、石板/柱体显示网格、入口拱券、火盆、桥侧细节、祭坛断环与深渊岩层已优化。生成源为 AbyssArtSetup 及其 Portfolio partial，新资产在 Assets/_Game/Art/SunkenPolish/；不修改第二关或共享旧材质。326 个原碰撞体及其 Transform、第一关 NavMesh 和玩法配置保留。美术细节见 [abyss-atmosphere.md](abyss-atmosphere.md)，本轮运行证据见 HANDOFF。

## 第二关美术增量（2026-11-02）

AbyssDepths 已增加独立 RiftArtPolish 显示层：区域石材/裂纹、缺角护墙、倒悬岩瓣与碎片、三层深渊背景/雾/低量浮尘、起航青蓝晶簇/铜罗盘、中段紫色尖碑、终点橙金核心/仪式断环及独立 RiftVolume。新资产位于 Assets/_Game/Art/RiftPolish/，地图生成器同步调用精修流程。407 个原碰撞体和 Transform、独立 NavMesh、关卡 ID/流程保留，第一关场景未改。详见 accounts-and-depths 与 HANDOFF。

第二轮造型由 RiftArtPolish.Architecture.cs 同步生成：带斜切截面的分层石环、八角晶体设施与弧肋、肩切晶体、祭坛浮雕/开放悬冠、中段断续放射铺石及原井口内衬、沉积岩层与六处远景断拱。地面改用周期不规则石铺纹理。所有新增建筑为无碰撞显示网格；行走区仅有浅浮雕，较高祭坛悬冠前半部留空。继续按材质与空间区块合批，不增加动态灯或粒子预算；修改前后绘制采样及验收边界记录在 HANDOFF。

## KayKit 第二关入口资源接入（2026-11-02）

免费建筑资源为作者官方 KayKit Dungeon Remastered 1.0 仓库的七模型子集（包含后续新增门洞），固定版本与原始 CC0 许可记录在 Assets/ThirdParty/KayKitDungeon/SOURCE.md、LICENSE.txt、SHA256SUMS.txt。生成资产为 Assets/_Game/Art/KayKitEntrance，编辑器入口为 KayKitEntranceSetup.cs；Prefab 采用逻辑根/VisualRoot 分离且不添加碰撞。RiftArtPolish 在合批后调用入口显示生成；Architecture 在模型存在时略过旧入口石环/程序底座。第一关及既有导航不变，重建第二关仍保留此示范。当前覆盖入口与中段局部建筑，未迁移全关。已验证范围与人工/Player 待验项见 HANDOFF 最新记录。

入口资源后续扩大可见轮廓：KayKitEntranceSetup 生成开放石拱门、四根高低柱、31 块缺角铺石，仍只操作第二关入口显示层；七模型来源清单见 ThirdParty/KayKitDungeon/SOURCE.md。门洞模型自带木门在显示 Prefab 实例中移除。

入口融合迭代现由 KayKitEntranceSetup.Polish.cs 生成：71 块错缝石铺合成两个显示网格，边缘缺损与石缝底层衔接，使用低色差灰蓝石材；原起航地面仅更换显示材质。旧入口罗盘刻线停用显示，不修改归途符文引用。拱门与高柱从已导入 FBX 裁切出带封口的斜断面网格，原 FBX/导入设置保留；拱门、高柱和侧墙复用 AbyssOccluder，共七个遮挡组件，独立淡化，不能合并进铺石批次。生成器结构检查允许该显示子树新增淡化组件，其余原始玩法组件和碰撞仍严格比对。具体 Game、Player 与性能证据见 HANDOFF 最新记录。

中段由 KayKitWatchSetup.cs 单独生成，资产放 Assets/_Game/Art/KayKitWatch，场景显示根为“KayKit守望遗迹”。复用入口已有模型及断面 Prefab，在中段增加两个合批错缝铺石网格、东缘悬空残拱廊和原西侧受护井口内的石柱/衬墙；原守望台只更换显示材质。14 个启用 Renderer、10 个遮挡淡化组件，不新增碰撞、灯或粒子。独立菜单“环境 > 精修第二关守望战区”（Ctrl+Alt+Shift+J）只更新中段；入口和完整 Rift 生成会覆盖更大范围，不是启动步骤。完整 Rift 生成已在入口生成后调用同一中段源，重复应用检测原碰撞、玩法与入口/祭坛显示。祭坛于 2026-11-03 接入独立外围圣龛，见下文；中段工具仍不重建该区域。

后续左侧精修由 KayKitWatchSetup.Well.cs 生成“裂隙封印井”子树：后侧高残拱、西侧半拱、四根高低断柱、深色井壁与两层下沉拱券、三道断续封印和高处铭文。显示范围严格限制在原受护井口 x[-33,-28]、z[27.5,32.5] 内，保持两条侧桥及朝玩家方向开敞，不封盖井口、不添加灯/粒子/碰撞。中段总量因此变为 23 个启用 Renderer、15 个遮挡组件（上段 14/10 为初版）。铭文双面网格分别使用正反面顶点以避免法线抵消，生成时检查法线有效；不调整全局 Volume 掩盖网格异常。原 KayKitWatch 材质/铺石网格及入口/祭坛资源均保留，仅新增井专用资产。

井铭文的 URP/Lit 材质显式设置 BakedEmissive 标记并调用 FixupEmissiveFlag，再启用 _EMISSION；仅手动 EnableKeyword 会被当前 URP 的导入/构建校验按 GI 标记清除。此标记用于正确保留发光变体，本轮没有烘焙光照或新增灯。开发 Player 检查铭文材质的 _EMISSION 状态，避免 Shader 无报错却丢失发光表现。

## 第二关地板材质统一（2026-11-02）

RiftFloorSetup.cs 为第二关全部平台/桥面及入口、中段模块铺石统一冷灰石铺材质和世界坐标 UV（3.56 米重复单元，含两行/列错缝石纹）。20 个地面 Renderer 的上表面共享同一 Material，取消各区旧岩板纹理与尺度差异；已有模块石板造型保留。仅复制显示 Mesh 并改 UV/材质分组，平台竖侧/底面沿用深层玄武岩材质；不修改原 MeshCollider 网格、导航、Transform 或玩法组件，不替换符文/祭坛金属饰条。专用资产在 Assets/_Game/Art/RiftFloor，原美术资产和 GUID 保留。

独立菜单“环境 > 统一第二关地板材质”（Ctrl+Alt+Shift+U）重复应用并比对物理/玩法序列化和导航。RiftArtPolish、KayKitEntranceSetup、KayKitWatchSetup 在各自显示生成结束时调用同一地板源，局部重建也会恢复统一材质。正常打开工程无需重新执行生成器；本轮运行证据见 HANDOFF。

2026-11-03 审查收紧生成范围：只接受任务根下 16 个已知平台/桥面及入口、中段指定显示根下的铺石，不再仅凭存在 MeshCollider 判断地面；相同输出名称在写入前报错，避免不同对象覆盖同一显示 Mesh。当前 20 处地板资产保持不变。

## 第二关核心圣龛（2026-11-03）

KayKitAltarSetup.cs 复用既有 CC0 模型和断面 Prefab，独立生成“KayKit核心圣龛”。后方高残拱/旧铜浮雕、非对称长柱、下沉拱券与两侧低残柱位于原祭坛半径 14 米以外，中央战斗区、统一地板、核心交互、晶体及原仪式环全部保留。9 个 Renderer、4582 个模型三角形、6 个既有遮挡组件；没有新增碰撞、灯光或粒子。新材质独立位于 Assets/_Game/Art/KayKitAltar，不修改入口/中段资产或源 FBX。

独立菜单“环境 > 精修第二关核心圣龛”（Ctrl+Alt+Shift+L）只重建该显示根，重复执行并对比全部原场景组件，逐个 Renderer 检查不侵入平台；完整 RiftArtPolish 在入口/中段生成后调用同一源。原入口全场重建工具的快照只为该新显示根的 AbyssOccluder 增加例外，不放宽原玩法保护。AccountFlowVerification 增加实际 WASD 绕坛四点与视线检查；开发 Player 的 InventoryBundleVerification 新增显式 --altar-capture 参数，在第三遭遇仍有敌人时截图与验证材质/角色可见性，正式非开发构建不含该驱动。当前证据和完整人工品质待验项见 HANDOFF。


## 第二关晶体近身遮挡（2026-11-03）

RiftCrystalVisibility.cs 在原环境合批之后，将祭坛两侧六块“边缘晶簇”从两个显示批次中拆出，恢复源 Renderer 并添加 AbyssOccluder；原核心和六枚浮动晶体同样独立淡化，共 13 处。新批次 Mesh 单独保存于 Assets/_Game/Art/RiftVisibility，旧 RiftPolish Mesh、材质、晶体造型及变换保留，不增加碰撞、灯或粒子。六个独立 Renderer 增加少量绘制开销，实际性能仍须目标硬件评审。

AbyssOccluder 新增序列化 targetHeight，旧对象默认保持 0.35 米，祭坛晶体使用 1.1 米对准猎手上身；沿用缓存材质副本、遮挡射线和非缩放时间平滑恢复。生成器在 RiftArtPolish 完整生成末尾调用；局部菜单“环境 > 修正第二关晶体遮挡”（Ctrl+Alt+Shift+O）只改显示，重复应用并核对原碰撞/导航。正常打开场景不需要运行生成菜单。

开发 Player 的 --altar-capture 检查十三处组件、贴近核心淡化、暂停保持、离开恢复和西侧晶簇独立淡化；取核/撤离及死亡重开沿用现有真实资源包回归。正式非开发包不包含驱动。本轮最终验证状态见 HANDOFF，不能仅凭代码或历史报告宣称画面问题已解决。


## 第一关前厅模块化试用（2026-11-04 应用，2026-11-09 集中验证）

SunkenFoyerSetup.cs 仅接受 SampleScene，复用已导入且许可记录完整的 KayKitEntrance 显示 Prefab，通过独立显示根“沉没前厅模块建筑”替换入口旧柱/拱的显示；地板、原物理和任务对象保留。局部入口 Ctrl+Alt+Shift+F，完整 AbyssArtSetup 末尾接入同一源；完整生成前移除该显示根，避免被旧环境遍历/合批重复处理。场景及 SunkenFoyer 专用材质/门洞 Mesh 已实际保存；重复局部生成、原碰撞与入口至桥面的导航检查已执行，当前运行证据见 HANDOFF。

门廊、六根高低残柱、两侧下沉墙与拱券及旧铜浮雕共 12 个 Renderer，沿用 AbyssOccluder，不增加碰撞、灯或粒子。进口门洞的粗侧墩曾在实际镜头下造成道路视觉收窄；WidenGate 仅重映射该实例的显示网格横向顶点，保存独立宽门洞 Mesh，保留源 FBX/Prefab 与原通道碰撞。隐藏 21 个旧 Renderer，不删除逻辑对象或旧资产。

环境专项的恢复检查站位从 z=-5 移至 z=-7.5：原站位处于新门廊整块 Renderer 包围盒内，本应仍淡化；新站位完全离开门廊再验证恢复，没有放宽检查条件或修改运行时遮挡算法。2026-11-09 环境专项 23/0；首次失败记录保留在 Logs/foyer-environment-first-attempt.txt。

InventoryBundleVerification 新增第一关可选 --foyer-capture=<绝对目录>，需要有图形的开发 Player，记录入口/穿门后/战区侧面/通往桥面的四张图及材质、碰撞和视线检查。复用原真实包掉落、撤离和重开回归；不增加正式产品入口或免认证玩法。

前厅第二轮（11 月 9 日实现，11 月 13 日继续验收）：SunkenFoyerSetup 在宽门洞基础上以两个不同倾角的平面裁切原导入模型，补封断面并保留 UV，输出仍使用原专用 Mesh 的 GUID。侧面上层实墙改为开放门洞，前沿地板下方增加错位的浅/深两层拱券；显示量由 12 增至 14 个 Renderer，仍无新增灯、粒子或碰撞。浅层潮蚀石采用低亮度发光补足平台阴影下的可见性，深层维持暗色，未调整全局 Volume 或地板。生成 Mesh 名称与文件名保持一致以避免导入警告。实际运行结果及审美边界见 HANDOFF 最新条目。


## 第一关桥廊模块化显示层（2026-11-15）

SunkenBridgeSetup.cs 只接受 SampleScene，复用已有 CC0 KayKit 显示 Prefab，在 z=12、36 两座桥外设置下沉承重残拱、错落桥头断柱、旧铜浮雕及交替位于远侧的残拱。18 个 Renderer、四份独立材质位于 Assets/_Game/Art/SunkenBridge；无新增碰撞、灯光或粒子。桥面、护栏、地板材质、符文、环境声源与所有原场景组件保留；沿用 AbyssOccluder 处理近身遮挡，浅层微弱材质发光用于阴影轮廓。

独立菜单“环境 > 精修第一关桥廊”（Ctrl+Alt+Shift+B）重复应用并检查单一显示根、原物理与双桥三通行线的双向 NavMesh 路径。AbyssArtSetup 在完整生成开始移除旧桥廊根，在合批和前厅处理后接入相同源，避免下次重建丢失或混入旧环境合批。没有运行完整 Configure 或重建第二关。

“环境 > 桥廊集中验证与开发包”（Ctrl+Alt+Shift+H）先应用，再执行既有环境专项，通过后沿用 PortfolioTools 构建开发包。开发 Player 增加显式 --bridge-capture=<绝对目录>：第二段遭遇期间记录四处桥上画面、材质与视线，并以 CharacterController 连续移动检查两座桥的双向通行。受控移动/无敌并非人工操作或完整战斗验收，正式非开发包不包含该驱动。详细结果见 HANDOFF。

## 第一关终点祭坛（2026-11-18）

`SunkenAltarSetup.cs` 的局部菜单“环境 > 精修第一关终点祭坛”（Ctrl+Alt+Shift+K）打开 SampleScene，连续生成两次，检查单一显示根、11 个 Renderer 和零碰撞后保存。完整 `AbyssArtSetup` 在环境合批前清除旧祭坛显示根，在桥廊生成后重建祭坛。门洞和端柱位于末端护栏外，近景残石、旧铜纹及琥珀铜承印贴合既有仪式环与铜盘。材质沿用 KayKit 图集，只有承印使用专用的微发光材质。战斗与核心状态仍由原组件驱动。

在第一关有图形的碎片回归开发包命令中增加 `--sunken-altar-capture=<绝对目录>`，可保留 HUD，记录桥口、第三遭遇侧面、清场后取核前以及取核后走回桥口四处画面。回程使用玩家现有 CharacterController，截图检查打包材质及祭坛模型对猎手的视线。运行命令基础参数见 [独立包回归](inventory-bundle-regression.md)。
