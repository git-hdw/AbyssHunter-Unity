# v0.3.0 账号与裂隙回廊

## 账号服务配置

PlayFab 是托管游戏账号后台；Title ID 是游戏项目的公开编号，不是密码。当前已配置用户提供的公开 Title ID `1302F5`。2026-11-01 对真实服务的不存在账号探测返回 AccountNotFound，证明端点可达；尚不代表真实注册、登录或邮件投递通过。

1. 由项目所有者进入 [PlayFab Game Manager](https://developer.playfab.com/)，用自己的账户创建工作室和游戏项目。
2. 在该游戏的 Settings / Game Properties 中查找 Title ID。无需提供 Secret Key。
3. 将公开编号填入 `Assets/_Game/Resources/PlayFabConfig.json` 的 `titleId`，然后重新构建。不要把管理密钥放进客户端。
4. 使用自己的测试邮箱完成注册、退出、重新登录和密码找回；检查收件箱及垃圾邮件。邮件发送配置若被服务端拒绝，需要在 Game Manager 完成对应配置，不能只凭 API 请求发送成功认定邮件可用。

接口为 PlayFab Client 的 RegisterPlayFabUser、LoginWithEmailAddress、SendAccountRecoveryEmail。注册使用邮箱与密码，RequireBothUsernameAndEmail=false。账号密码规则以服务端为准；客户端不保存密码、邮件或认证票据，不上传关卡成绩。重置密码由邮件链接里的托管页面完成。

每次启动重新联网登录；同一次进程登录后可以断网游玩。主菜单退出账号会清除当前身份。登录只验证身份，不是单机进度的防篡改系统。

官方参考：[注册](https://learn.microsoft.com/en-us/rest/api/playfab/client/authentication/register-playfab-user?view=playfab-rest)、[登录](https://learn.microsoft.com/en-us/rest/api/playfab/client/authentication/login-with-email-address?view=playfab-rest)、[密码找回](https://learn.microsoft.com/en-us/rest/api/playfab/client/account-management/send-account-recovery-email?view=playfab-rest)。

## 本机进度与关卡

账号档案位于 Application.persistentDataPath/Accounts，以 Title ID 与 PlayFabId 的 SHA-256 组合作为文件名，避免把邮箱写进路径。保存版本、关卡 ID、通关标记和最佳成功用时。临时文件替换成功后才提交内存状态；写入失败不解锁下一关。损坏文件保留副本，有可用备份时读取备份，否则使用默认档案并显示提示。

第一关成功撤离解锁第二关；仅清场或死亡不解锁。每次远征使用新的生命和空背包，仍不支持中途存档。重开加载当前关；结果页可以返回选关，第一关胜利后可以直接进入第二关。

第二关仍使用稳定关卡 ID `abyss-depths`，场景 `Assets/Scenes/AbyssDepths.unity` 与独立导航 `Assets/_Game/Combat/AbyssDepthsNavMesh.asset` 的 GUID 保留。2026-11-01 根据用户选择，从零制作“浮空裂隙”地图，替换原先由第一关派生的庭院地图；不复制第一关地形或预合并网格。

地图由三个高度的浮岛组成：起航浮岛（0 米）、悬浮守望台（1.5 米）、上层中央祭坛（3 米）。主路采用八米宽升空桥和水平转台，桥头齐平，外围设实体护栏；西侧补给悬台有两条五米宽侧桥和一瓶固定生命药水，主路不依赖侧路。新多边形地形、倒悬晶岩、晶体、材质保存在 `Assets/_Game/Art/FloatingRift/`，装饰无碰撞。

三段遭遇仍为 3 普通 → 1 普通+1 重锤 → 2 普通+2 重锤。共 9 枚保证掉落的碎片，取核后消耗 6 枚点灯，再次交互撤离，全部收集时余量为 3。任务文案、风声位置、14 处归航光标顺序和镜头区域按新地图重配。第一关、共享角色/敌人 Prefab、物品参数和全局导航代理设置保留。

`Tools > 深渊猎手 > 关卡 > 制作全新浮空裂隙地图` 是明确的重建工具，会覆盖第二关的地图子物体并重烘焙独立导航，执行前将旧场景备份至 Logs。它不是启动步骤。旧桥梁修复与显示网格迁移工具检测到浮空地图标记后直接跳过，避免破坏新地图。`DepthsLevelSetup` 和 `Art/Abyss/Depths/` 保留作旧版迁移历史，当前场景不使用旧庭院地形。

## 验证入口与限制

`Tools > 深渊猎手 > 验证 > 运行 v0.3.0 回归` 先检查本机档案，再依次运行原有四项 Play 验证和账号/选关流程夹具。需要普通 Editor 和 Game 窗口；原有测试包含 WaitForEndOfFrame，不适合 batchmode。测试夹具使用独立 Logs 目录和合成身份，不发 PlayFab 请求，不代表真实账号或邮件验证。

`账号与第二关操作回归` 可单独重跑账号夹具，包含 WASD 驱动的侧廊、遭遇区、祭坛和入口往返。使用无敌及受控击杀，只验证操作路径和状态流转；完整战斗手感仍需人工验收。`关卡 > 验证裂隙回廊导航与补给` 对浮空地图只检查入口至祭坛、补给悬台的连通性，不执行旧地图迁移。账号夹具还检查新地图材质、随身灯、悬浮晶体、暂停、取核与归途依次苏醒；不代表完整人工听音。

开发包原有 `--inventory-report=<绝对路径>` 回归可增加 `--inventory-level=abyss-depths` 来验证第二关；省略则检查第一关。其受控击杀、无敌和认证旁路只存在于显式启动的开发回归，正式非开发版没有该旁路。报告分别输出到指定路径，不应覆盖混淆两关结果。

Title ID 已配置；真实注册登录、重置邮件和改密后新旧密码仍需实际测试账号验收。完整手动战斗、人工听音和发布视频验收仍需另行记录。具体执行结果见 `Docs/HANDOFF.md`。

## 第二关美术精修（2026-11-02）

`RiftArtPolish` 为第二关提供独立的显示层精修，入口为 `Tools > 深渊猎手 > 环境 > 精修第二关浮空裂隙`。它在编辑态打开并保存 AbyssDepths，不重建地形、不重烘焙导航；原有 407 个碰撞体及对应 Transform 保留。`FloatingRiftMapBuilder.Build` 在完成原有地图/导航生成后调用同一精修流程，避免重建丢失美术。

新资产在 `Assets/_Game/Art/RiftPolish/`：

- 三种区域石材配色，独立显示网格带平面 UV；256×256 程序石材含 16 个色差/裂纹分布，减少整齐重复。MeshCollider 继续引用原地形网格。
- 护墙仅替换缺角显示网格，底部增加不规则岩瓣、脱落碎片、桥下桥脊和悬片；新增装饰不带碰撞。护墙与静态装饰按材质、空间区域合批，交互物、归途光标和动态晶体保持独立。
- 起航区：青蓝晶簇与铜金罗盘；守望台：紫色晶簇与断裂尖碑；终点：更大橙金核心、双层仪式断环与紫色晶簇。中央战斗区域保持通畅。
- 远、中、近三层漂浮残岛，三层低透明度深渊雾，两处各最多 18 粒的缓落浮尘；仍保留原有动态晶体与归途苏醒。
- 冷蓝灰环境补光、软主阴影、无阴影的局部晶体光与橙色祭坛光。第二关独立 RiftVolume 使用 ACES、+0.45 EV、轻量 Bloom/Vignette、关闭 Motion Blur，相机 SMAA；不修改第一关或全局 URP/旧共享材质。

重复执行只替换“裂隙精修美术”子物体和自己的资产，原碰撞/交互/遭遇配置保留；执行前备份场景到 Logs。它是重建美术的工具，不是启动步骤。具体运行证据与人工待验收范围见 HANDOFF。

第二轮精修增加 `RiftArtPolish.Architecture.cs`，仍由同一入口自动调用：

- 入口改为有斜切截面和破损缺口的浅层石环；蓝晶使用肩切多面体，配八角石座、雕刻、抱箍和弧形支肋。
- 中段使用断续放射铺石与原开口内部的断层衬壁；原地面、方形井口护栏及侧路碰撞保留。512 像素周期不规则石铺代替规则矩形地砖纹理。
- 核心使用三层石/银/铜金环座、独立符文网格和后半悬冠；橙色肩切晶体保持原任务引用，紫晶采用两种切面及不同尺度。行走区浮雕高度不超过约 0.2 米，无新碰撞。
- 深渊残岛采用扁平不规则沉积岩模型，增加六处不同明度的断层遗迹和断拱。沿用原雾及两处各最多 18 粒的粒子；不增加动态灯，蓝/橙自发光略收敛。

专用建筑与符文均为低面数网格，继续按材质/区域合批；模型文件和 `.meta` 保存在 RiftPolish，重建保留已有资产 GUID。具体 Game 视图、结构比对和 Player 回归见最新 HANDOFF，编辑器绘制批次不能换算为成品帧率。
