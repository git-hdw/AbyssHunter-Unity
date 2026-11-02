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

## v0.3.0 多关卡回归

2026-11-01 实际 Windows x64 开发包：第一关 23 项通过、第二关 24 项通过，均 0 失败、退出码 0；两份报告分别为 `Logs/inventory-v030-first.txt` 和 `Logs/inventory-v030-depths.txt`。Addressables 已重建，开发及正式构建均 0 错误、0 警告。此项不涉及真实账号认证。

当前驱动还检查两关导航、暂停、背包、胜利后重开、死亡后重开及返回菜单。第一关沿用上方命令，建议报告命名为 `inventory-v030-first.txt`；第二关在参数数组中增加 `--inventory-level=abyss-depths`，报告使用 `inventory-v030-depths.txt`。第二关验证 9 枚掉落 → 投入 6 枚 → 剩 3 枚，并检查侧廊导航连通。

2026-11-01 23:04 浮空裂隙从零重制后的最新回归：重建 Addressables 与两个 Player 成功，均 0 错误、0 警告。第二关 `Logs/inventory-floating-rift.txt` **24/0**、第一关 `Logs/inventory-floating-first.txt` **23/0**，退出码均 0。此项仍为无图形、受控击杀逻辑回归；新桥实际 WASD、护栏闪避和画面依据为 Editor 40/0 夹具及截图，详见 HANDOFF。

正式启动仍需要 PlayFab 联网认证；只有带显式回归参数的开发包使用测试旁路，且不创建真实账号或本机账号解锁记录。最新实际执行结果见 [HANDOFF.md](HANDOFF.md)，不得用旧版 16 项报告作为当前两关验证证据。

2026-11-02 第一关美术优化后的资源回归：重建 Addressables、开发包和正式包均成功（0 错误、0 警告）。第一关 Logs/inventory-first-art-polish.txt 23/0，第二关 Logs/inventory-depths-after-first-art.txt 24/0，退出码均 0；无图形受控验证，不代替美术人工验收。第二关场景没有修改。

2026-11-02 第二关美术精修后的实际 Player：Logs/inventory-rift-art-polish.txt 24/0、Logs/inventory-first-after-rift-art.txt 23/0，退出码均 0。Addressables 重建、开发及正式构建均成功，0 错误、0 警告。保持每次 Player 构建只生成一次资源的顺序；无图形受控逻辑回归不代替人工画面/战斗验收。

2026-11-02 12:19 第二关第二轮造型精修后的最新构建：Addressables、开发包、正式包均成功，0 错误、0 警告。实际独立 Player 第二关 Logs/inventory-rift-round2.txt 24/0、第一关 Logs/inventory-first-after-round2.txt 23/0，退出码均 0。仍为无图形受控逻辑回归；第二关实际输入/画面依据为本轮 Editor 40/0 和 Game 截图，不代表 Player 人工视觉或帧率验收。
