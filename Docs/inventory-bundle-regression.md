# 独立包碎片计数回归

## 问题与修复

Windows 包中，左侧背包显示 6 枚碎片，任务却显示 0 / 4，信标提示还需 4 枚。

场景里的任务物品与 Addressables 敌人掉落的物品会反序列化为两个 `ItemData` 实例。旧逻辑使用对象引用判断物品种类，导致任务计数与扣除匹配不到背包中的碎片。

现在 `ItemData.HasSameIdentity` 使用非空、区分大小写的稳定 `ItemId`，并保留同一对象的匹配。背包堆叠、总量统计与扣除统一使用这一规则。数量不足时仍不扣除任何物品。不同物品必须配置不同 ID。

构建工具还修复了手动生成 Addressables 后，在 Player 构建阶段重复生成资源包的问题：暂时禁用第二次生成，结束后恢复原设置。构建结果不是成功时会明确抛出失败，避免把 `Unknown` 误报为完成。

## 2026-10-20 验证

Windows x64 开发包：16 项通过、0 项失败，退出码 0。

- 不同实例、相同 ID 共享堆叠；跨格汇总和扣除正确。
- 不同 ID、空引用、无效空 ID 不混同；数量不足时保持原数量。
- 加载实际 Addressables 敌人，完成三处遭遇并拾取六枚掉落。
- 确认场景与资源包内的碎片是同 ID 的不同实例。
- 背包和任务均计为 6；取得核心后交互提示允许投入 4 枚。
- 点亮信标后双方均剩 2 枚，再交互完成撤离，进入胜利结算状态。

此项为无图形自动回归，通过受控移动、无敌及直接伤害缩短验证时间；覆盖实际资源包、拾取、任务、交互和结算逻辑，不替代人工操作或画面检查。测试驱动仅在编辑器和开发构建中编译，且须显式命令行参数启动，正式非开发包不包含该驱动。

## 复测

在 Unity 菜单选择 `Tools > 深渊猎手 > 验证 > 构建碎片回归开发包`，然后在项目根目录的 PowerShell 执行：

```powershell
$project = (Get-Location).Path
$report = Join-Path $project 'Logs/inventory-bundle-verification.txt'
$log = Join-Path $project 'Logs/inventory-bundle-player.log'
$runner = Start-Process -FilePath "$project/Builds/InventoryVerification/AbyssHunter.exe" -ArgumentList @('-batchmode', '-nographics', "--inventory-report=`"$report`"", '-logFile', "`"$log`"") -WindowStyle Hidden -PassThru
$runner.WaitForExit()
$runner.ExitCode
Get-Content $report
```

报告末行应为 `DONE | PASSED`，且进程退出码为 0。正式包通过 `Tools > 深渊猎手 > 构建 > 构建 Windows 发布版` 构建，输出至 `Builds/Windows-v<版本号>/`。

## 第一关前厅图形回归（2026-11-09）

在第一关开发包原有 `--inventory-report=<绝对文件>` 参数之外，增加 `--foyer-capture=<绝对目录>` 和 `-screen-fullscreen 0 -screen-width 1600 -screen-height 900`；移除 `-batchmode`、`-nographics`，使用正常桌面图形窗口。开关只用于开发验证，正式非开发包不包含驱动。截图四站位覆盖入口、穿门后、战区西侧、桥前，检查新建筑无碰撞、材质及遮挡淡化；产出 `01-foyer.png` 至 `04-foyer.png`，需实际查看图片，不能只读布尔断言。

11 月 9 日 22:56–22:57 重建 Addressables、开发包及正式配置包均成功，0 错误/0 警告。第一关有图形 `Logs/inventory-foyer-graphical.txt` **29/0**，四张图 `Logs/foyer-player-review/` 已查看；第二关无图形 `Logs/inventory-depths-after-foyer.txt` **24/0**，均退出码 0。第一关 6→投入4→剩2，第二关 9→投入6→剩3，以及取核/撤离/死亡重开/返回菜单通过。使用合成身份和受控位置/击杀，不代表真实认证、完整人工战斗/听音或性能验收。构建日志的 Bee Analytics 文件占用提示及证据边界见 HANDOFF。

## v0.3.0 多关卡回归

2026-11-13 前厅断面与下沉层次精修：11:40 双包/Addressables 构建成功，0 错误/警告；第一关有图形 Logs/inventory-foyer-refine.txt 29/0，第二关无图形 Logs/inventory-depths-after-foyer-refine.txt 24/0，退出码均 0。沿用 --foyer-capture，四张实际图在 Logs/foyer-refine-player/，已查看。原物品身份、掉落、撤离与重开流程通过；受控位置/击杀不代替完整人工战斗、听音或性能评审。

2026-11-01 实际 Windows x64 开发包：第一关 23 项通过、第二关 24 项通过，均 0 失败、退出码 0；两份报告分别为 `Logs/inventory-v030-first.txt` 和 `Logs/inventory-v030-depths.txt`。Addressables 已重建，开发及正式构建均 0 错误、0 警告。此项不涉及真实账号认证。

当前驱动还检查两关导航、暂停、背包、胜利后重开、死亡后重开及返回菜单。第一关沿用上方命令，建议报告命名为 `inventory-v030-first.txt`；第二关在参数数组中增加 `--inventory-level=abyss-depths`，报告使用 `inventory-v030-depths.txt`。第二关验证 9 枚掉落 → 投入 6 枚 → 剩 3 枚，并检查侧廊导航连通。

2026-11-01 23:04 浮空裂隙从零重制后的最新回归：重建 Addressables 与两个 Player 成功，均 0 错误、0 警告。第二关 `Logs/inventory-floating-rift.txt` **24/0**、第一关 `Logs/inventory-floating-first.txt` **23/0**，退出码均 0。此项仍为无图形、受控击杀逻辑回归；新桥实际 WASD、护栏闪避和画面依据为 Editor 40/0 夹具及截图，详见 HANDOFF。

正式启动仍需要 PlayFab 联网认证；只有带显式回归参数的开发包使用测试旁路，且不创建真实账号或本机账号解锁记录。最新实际执行结果见 [HANDOFF.md](HANDOFF.md)，不得用旧版 16 项报告作为当前两关验证证据。

2026-11-02 第一关美术优化后的资源回归：重建 Addressables、开发包和正式包均成功（0 错误、0 警告）。第一关 Logs/inventory-first-art-polish.txt 23/0，第二关 Logs/inventory-depths-after-first-art.txt 24/0，退出码均 0；无图形受控验证，不代替美术人工验收。第二关场景没有修改。

2026-11-02 第二关美术精修后的实际 Player：Logs/inventory-rift-art-polish.txt 24/0、Logs/inventory-first-after-rift-art.txt 23/0，退出码均 0。Addressables 重建、开发及正式构建均成功，0 错误、0 警告。保持每次 Player 构建只生成一次资源的顺序；无图形受控逻辑回归不代替人工画面/战斗验收。

2026-11-02 12:19 第二关第二轮造型精修后的最新构建：Addressables、开发包、正式包均成功，0 错误、0 警告。实际独立 Player 第二关 Logs/inventory-rift-round2.txt 24/0、第一关 Logs/inventory-first-after-round2.txt 23/0，退出码均 0。仍为无图形受控逻辑回归；第二关实际输入/画面依据为本轮 Editor 40/0 和 Game 截图，不代表 Player 人工视觉或帧率验收。

2026-11-02 16:01 入口融合后的最新构建：Addressables、开发与正式 Windows 包均成功，0 错误、0 警告。第二关有图形受控回归 Logs/inventory-entrance-graphical.txt 27/0，新增入口淡化、Shader 和恢复三项；第一关无图形 Logs/inventory-first-after-entrance.txt 23/0，退出码均 0。第二关仍验证真实资源包掉落及 9→投入6→剩3/撤离/重开。

有图形回归从原有第二关命令移除 `-batchmode` 与 `-nographics`，加 `-screen-fullscreen 0 -screen-width 1600 -screen-height 900` 和 `--entrance-capture=<绝对目录>`。三张实际 Player 图在 Logs/entrance-player-review，已查看；受控位置、无敌与击杀仍不代替完整人工操作、战斗、听音或性能评审。正式非开发包无此驱动，也没有新增游客认证入口。

2026-11-02 16:38 中段守望台升级后再次重建 Addressables 与双包，均 0 错误/警告。第二关有图形 Logs/inventory-watch-graphical.txt **32/0**、第一关无图形 Logs/inventory-first-after-watch.txt **23/0**，退出码均 0；原物品身份、真实包掉落/撤离和入口淡化恢复继续通过。第二关在原有有图形参数与入口截图开关外增加 `--watch-capture=<绝对目录>`，在第二遭遇存活时检查中段无新增碰撞、Shader 及三处角色视线；截图 Logs/watch-player-review 已查看。截图位置受控、玩家无敌、击杀受控，不能代替完整人工战斗/听音或帧率评审；正式非开发包不包含此驱动。

2026-11-02 17:11 左侧封印井最终版重建 Addressables 与双包，均 0 错误/警告。第二关有图形 Logs/inventory-well-graphical.txt **37/0**、第一关无图形 Logs/inventory-first-after-well.txt **23/0**，退出码均 0。沿用 `--watch-capture`，另检查北桥遮挡淡化/南桥恢复、两处视线及铭文 URP 发光关键字；五处图存 Logs/well-player-review/，方井和两侧桥图已查看。发光材质需显式设置 GI 发光标记，否则当前 URP 在构建校验时会移除手动启用的 _EMISSION。该回归仍使用受控位置/无敌/击杀，不等同完整人工战斗、听音或性能验收。

2026-11-02 第二关统一地板材质后，17:44 开发包/Addressables 构建成功，0 错误/警告；第二关有图形 Logs/inventory-floor-graphical.txt **37/0**、第一关无图形 Logs/inventory-first-after-floor.txt **23/0**，退出码均 0。未改原回归驱动，沿用已有截图开关，图存 Logs/floor-player-review/ 与 floor-player-entry/；中段/左侧平台与桥面的统一纹理在实际 Player 图中已检查。正式包同次构建遇到配置文件占用后单独重试，17:51 Addressables 与正式包成功，0 错误/警告，报告 Logs/v030-release-build.txt 已更新为本次实际成功结果。


2026-11-03 祭坛圣龛版本：14:09 Addressables、开发和正式包均成功，0 错误/警告；第二关有图形 Logs/inventory-altar-graphical.txt **43/0**、第一关无图形 Logs/inventory-first-after-altar.txt **23/0**，退出码均 0。新增显式 --altar-capture=<目录>，在第三遭遇敌人存活时检查新圣龛材质、无碰撞及四机位猎手视线，截图 Logs/altar-player-review/；原入口/中段检查保留。新建筑可见性通过不代表原核心/紫晶近身遮挡已解决，完整人工战斗与性能仍待验收。


2026-11-03 21:19 晶体遮挡版本：Addressables、开发和正式包均成功，0 错误/警告；第二关有图形 Logs/inventory-crystal-graphical.txt **51/0**、第一关无图形 Logs/inventory-first-after-crystal.txt **23/0**，退出码均 0。沿用 --altar-capture，新增 13 处独立晶体组件、贴近核心淡化/暂停保持/离开恢复及西侧晶簇检查；四张实际图 Logs/crystal-player-review/ 均已查看。首次受限隐藏运行出现 12 次截图失败，记录保留 Logs/inventory-crystal-hidden-attempt.txt；正常桌面可见窗口复跑通过。截图开关需可实际渲染并写出图片的图形 Player；不能以状态布尔检查代替画面验收。完整人工战斗、连续镜头和性能仍待验收。


2026-11-15 桥廊版本：17:34 Addressables/开发包构建成功，0 错误/警告；第一关有图形 Player 39/0、第二关无图形 24/0，退出码均 0。报告 Logs/inventory-bridge.txt、inventory-depths-after-bridge.txt。第一关在 --foyer-capture 外增加 --bridge-capture=<绝对目录>，检查十八处模型与材质、四处角色视线、两座桥的实际胶囊双向连续移动；最终四图 Logs/bridge-player/ 已查看。首次测试以 y=0.1 放置中心原点的胶囊导致四次通行失败，改为按胶囊底部计算安全起点后通过，保留首次报告，未改桥面或玩家控制。受控移动/无敌/击杀不等同人工全流程与性能验收。本批未重建正式配置包。

有图形开发包可加 `--start-weapon=Sword`、`--start-weapon=Axe` 或 `--start-weapon=Crossbow` 走选关和选武器界面。一次运行选择一种武器，配合 `--weapon-review=<绝对目录>` 记录选择页、验证局内 Q 锁定及所选武器的攻击。重斧和轻弩可再加 `--weapon-motion`，按每秒 30 帧录制所选武器的一次攻击，共 27 帧。
