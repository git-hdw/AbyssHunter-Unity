# 最新任务交接

更新时间：**2026-11-23**。入口：[AGENTS.md](../AGENTS.md) → [PROJECT_VISION.md](PROJECT_VISION.md) → [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) → 本文件。

## 2026-11-23 v0.3.0 本地发布候选

- 远端只读核对发现 v0.1.0、v0.2.0 两个正式 Release。上一版标签 v0.2.0 解引用到 e41e6b4，远端 main 为 c31aed2，开发备份分支为 e7b47c1。整理时远端无 v0.3.0 标签或 Release。本地候选分支为 codex/release-v0.3.0，远程操作等项目所有者确认。
- 已核对三场景构建顺序、版本 0.3.0、依赖、资源 meta 和 GUID。扫描场景、游戏资产、Addressables 配置等文本序列化引用，在项目及当前 PackageCache 中可解析；Dungeon 免费子集校验清单十项吻合。角色与地下城来源、字体许可随源码保留。历史 v0.2.0 的下载与演示仍可访问原发布页。
- 本次 Unity 6000.3.23f1 在新目录 Builds/RC030-1123 重建 Addressables、Windows 开发及正式配置，两次 Player 构建报告均 0 错误、0 警告。正式包图形窗口启动有响应，可正常退出。两关流程使用同源码开发包及回归身份。第二关 72/0；第一关初次 35/1（猎剑 25 点伤害断言），相同构建固定采样率复测 36/0。首次失败原因待确认，不视为人工战斗或真实认证验收。
- 日志在 Logs/rc030/。发布附件在 Releases/，ZIP 含运行所需整个目录与素材许可，已排除 Burst 调试目录，已执行 ZIP CRC 校验并生成独立 SHA-256 文件。11 月 21 日的混战闪避及界面自动输入检查曾失败，键鼠回放在第一关击败三名敌人后死亡。完整真人平衡、联网邮件及目标机性能待验收。
- 发布标题为「AbyssHunter v0.3.0 — 游戏基础版本与核心系统整合」。正文见 [发布说明](releases/v0.3.0.md)。发布时再次核对远端空位，完成后核对远端标签解引用提交与 Release 附件校验值。

## 2026-11-21 远征前选定武器

- 选关后点击猎剑、重斧或轻弩即可出发，也可按 1、2、3。返回会取消此次选择。关卡内锁定武器，暂停、背包和战斗界面均无换装入口。重开沿用本局武器；结算后选下一关会再次显示武器选择页。
- Unity 6000.3.23f1 编译并重建 Addressables 与 Windows 开发包，构建 0 错误、0 警告。三次有图形 Player 从主菜单出发，第一关猎剑 30 项、重斧 32 项、第二关轻弩 66 项均通过，覆盖实际攻击、Q 锁定、撤离、胜利和死亡后的重开。编辑器重击 46 项、中文展示 60 项均通过。检查了选择页中文、按钮点击、返回和 HUD；合成账号流程验证了第一关结算后取消下一关选择，以及按 3 带轻弩进入第二关。
- 合成账号与第二关连续操作专项最终 84 项通过。初次测试从门口斜切西侧，在引桥边缘超时。修正测试路线，先走到桥中转台再向西转，场景与碰撞无需调整。测试中一次热重载打断了运行，完整复测在编译结束后重新进入 Play。
- 本地试玩包 `Builds/ExpeditionLoadout/AbyssHunter.exe`。本次报告及选择页截图在 `Logs/departure/`。验证使用本地测试身份与受控战斗。

## 2026-11-21 重斧与轻弩攻击动作

- 旧战斧使用单手斧和单手横切，旧轻弩在发射帧的弩身与射向相差约 90°。改用 KayKit 双手长柄斧和双手横扫，在右手挂点校正左手握柄，按斧刃经过身前的时刻触发伤害。轻弩已校正手部挂点，动画事件触发后在同一帧读取弩口位置并发射，弩口伸过薄墙时也会被截停。
- 重新生成两次，猎手预制体、控制器和两段攻击剪辑的文件内容相同。主菜单、两关场景、两份 NavMesh 的 SHA-256 与本批起点一致。Unity 6000.3.23f1 重建 Addressables 与 Windows 开发包，构建 0 错误、0 警告。试玩入口为 `Builds/WeaponMotion/AbyssHunter.exe`。
- 第一关有图形 Player 38 项通过。六米靶位弩矢起点与弩口重合，飞行方向与弩身夹角 1.9°。横扫命中时左手握点距斧柄轴线小于 1 厘米，柄轴与角色前向点积 0.95，双靶各扣 42 点。两段攻击各录 27 帧，实机动图为 `Logs/weapon-motion/attacks.gif`。第二关无图形 Player 55 项通过，两关均完成取核、撤离与重开。移动敌群中的自由瞄准与连续换装手感尚待玩家试玩。
- 编辑器重击专项首轮有 3 项鼠标输入检查失败，一轮中文界面复测有 14 项输入及后续流程检查失败。将 Unity 置于前台并单独复跑后，重击 46 项、中文界面 50 项全部通过。各轮报告均保留于 `Logs/weapon-motion/`。

## 2026-11-21 猎手轻弩

- 两关均从猎剑开始，按 Q 依次切换战斧、轻弩和猎剑。轻弩按鼠标方向射击，发射时播放弦声，弩矢最多飞行 12 米，命中第一个目标造成 20 点伤害。沿用 KayKit Adventurers 2.0 FREE 单手弩、弩矢及猎手图集。首轮截图中箭身偏细，最终版本加入短暖色箭尾。
- 实际 Windows 开发包第一关 35/0，第二关 55/0，编辑器重击 46/0、中文展示 50/0。第一关用实际输入测试六米外受控靶、后排靶、临时实体掩体、闪避取消发射和换回猎剑，随后完成撤离与重开。重复应用生成器后预制体及控制器文件一致。两关场景与导航的批次起点哈希一致。移动敌人混战中的瞄准手感仍待玩家试玩。
- 本地试玩包 `Builds/LightCrossbow/AbyssHunter.exe`。重建 Addressables 与 Windows Player，构建 0 错误、0 警告。报告见 `Logs/light-crossbow/{build,heavy,ui,final-first,final-depths}.txt`，最终射击截图在 `final-screens/crossbow.png`。沿用有图形开发包的 `--weapon-review` 专项入口。

## 2026-11-21 猎手双武器

- 两关每次开局持猎剑。Q 在猎剑与单手战斧间切换，左键使用当前武器。战斧横扫每目标 42 点、攻击间隔 0.92 秒，猎剑每目标 25 点、间隔 0.5 秒。攻击冷却期间、闪避或暂停时阻止换装。生命条与操作说明同步展示装备和按键。战斧模型和贴图取自已授权的 KayKit Adventurers 2.0 FREE。
- 重建 Addressables 和 Windows 开发包，0 错误、0 警告。第一关有图形 Player 30/0，验证了实际按键、暂停阻断、前摇锁定、双目标各受一次伤害、切回剑的伤害与撤离。第二关无图形 Player 55/0，重击专项 46/0。中文展示在编辑器原视图复现 4 项结算点击失败，1600×900 独立复测 50/0。竖窄窗口点击问题和两种武器的实战平衡仍待试玩。
- 两次应用生成器后，猎手预制体与动画控制器的文件内容相同。两关场景和导航资产与本批起点哈希一致。试玩包 `Builds/HunterWeapons/AbyssHunter.exe`，报告与战斧实机截图位于 `Logs/hunter-weapons/`。有图形开发包沿用碎片回归入口，加 `--weapon-review=<截图目录>` 可复测 Q 键与横扫。

## 2026-11-20 第二关烬火法师

- **已完成并验证**。按试玩反馈将裂晶术士改为烬火法师。使用本机已有的 KayKit Skeletons 1.1 FREE 骷髅法师、骨杖与原版骨骼图集，以及已导入的举杖和发射动画。杖头蓄火，沿锁定线发出有焰尾的直线火球。首轮实机画面中命中闪光容易被猎手遮挡，最终版本加入向外飞散的余烬与随火球移动的局部暖光。火星仅作表现，伤害仍为单次 12 点。
- **实机与回归**。在 1280×720 Windows Player 逐帧记录施法，输出 24 帧/秒的 2 秒片段。第二关 63 项、第一关 23 项、编辑器重击 46 项、中文展示 50 项均通过。覆盖受击、横移、掩体、击退与死亡打断、同一法师实例复用、取核撤离与重开。生成配置执行两次，第二关场景与法师 Prefab 字节一致。第一关场景、普通与重锤 Prefab、两关导航的 SHA-256 与本批开始一致。
- **交付**。本地开发包 `Builds/FireMage/AbyssHunter.exe`，已重建 Addressables，构建报告 0 错误、0 警告。`Logs/fire-mage/` 包含 `build.txt`、`generation.txt`、`heavy.txt`、`ui.txt`、`final-first.txt`、`final-depths.txt`、原分辨率录像 `fireball.mp4` 和裁切动图 `fireball-preview.gif`。有图形开发包沿用下方「裂晶术士」条目中的三个命令行参数，`--caster-review` 的输出目录中增加 `cast/` 连续帧。录像为受控站位，祭坛双锤混战下的首次读招体验仍待玩家确认。

## 2026-11-20 第二关远程敌人「裂晶术士」

- **已完成并验证**。守望台和中央祭坛各用一名术士替换一名普通骷髅。守望台教玩家辨认肩晶、预警线和直线弹，祭坛再与两名重锤混编。术士进入 8 米射程后锁向，0.9 秒后放弹。实测站定时单次扣 12 点生命，锁向后按 D 横移可躲过，实体掩体可截弹，击退和死亡可中断蓄力。祭坛复用了守望台回收的术士。第一关场景、普通与重锤 Prefab、两关 NavMesh 的 SHA-256 与本批开始时一致。第二关连续两次应用配置后场景文件逐字节一致。
- **实机画面**。首轮截图中预警线在蓝灰地面上很暗。已为肩晶、预警线和晶弹配置独立自发光材质，加宽预警并放大在途弹体，更新混合遭遇的开场与右上角提示。已查看最终 Windows Player 的守望台同场、锁向及晶弹飞行三张 1280×720 截图。
- **验证与复现**。Unity 6000.3.23f1 重建 Addressables 与 Windows x64 开发包，构建报告 0 错误、0 警告。第一关 Player 23 项通过，第二关有图形 Player 61 项通过，包含 9 枚掉落、点灯投入 6 枚、撤离、重开与返回菜单。编辑器重击 46/0、远征 56/0。中文展示在狭窄 Game 视图首次跑出 4 个结算按钮相关失败，使用项目目标的 1600×900 Game 视图独立复测 50/0。首轮报告已保留，竖窄编辑器视图中的结算点击稳定性仍待确认。
- **试玩与证据**。本地开发包 `Builds/RiftCaster/AbyssHunter.exe`。证据位于 `Logs/rift-caster/`，包括 `build-final.txt`、`first.txt`、`final-depths.txt`、`heavy.txt`、`expedition.txt`、`ui-first.txt`、`ui.txt`、`scene.txt` 与 `final-screens/`。有图形开发包使用 `--inventory-level=abyss-depths`、`--inventory-report=<报告绝对路径>`、`--caster-review=<截图目录绝对路径>` 可复测。专项使用受控清场、键盘横移与测试掩体。玩家首次遇敌时能否独立读招、祭坛双锤加术士的实际难度仍待试玩。

## 2026-11-20 第二关连续回程与守望台补给

- **回程**。11 月 19 日连续录制发现在 z=53.25 已提示向左，实际转角在 z=44。修正后，同一路线在 z=44.50 才提示向左。上层横桥故意反走约六米时，剩余路程由 58 米增加到 63 米，指引仍指回左方。角色从祭坛按 E 取核，沿桥步行，到信标按 E 点灯，再按 E 撤离。方向复用既有地面符文和可行走路径。
- **补给**。受伤清理守望台且固定药水仍在时，清场横幅提示西侧补给和北桥归路。有图形 Windows Player 验证了走南桥拾药、背包用药恢复 30 点生命、经过北桥跨中并重回主路。首次联合回归的返程终点断言失败。增加桥跨中检查并走到完整的双轴终点后，两个位置均通过。第二关场景只新增固定药水的序列化引用及其 Prefab 引用记录，生成源已同步。第一关场景与两关导航资源的批次起点哈希一致。
- **交付与证据**。11 月 19 日最终开发包构建 0 错误、0 警告，第二关联合回归 57/0。11 月 20 日用同一包复测第一关 23/0，并编码核对最终录像。试玩包为 `Builds/ReturnWalk/AbyssHunter.exe`。证据目录 `Logs/return-walk/` 包含 `delivery.txt`、`delivery-first.txt`、`build-d.txt` 和连续录像 `delivery.mp4`（1280×720、24 帧/秒、760 帧、无音轨）。`first-frames/route.tsv` 与 `delivery-frames/route.tsv` 可比较逐帧位置和方向。`combined.txt` 保留首次补给终点失败记录。回程修正批次另跑过中文展示 50/0 与环境表现 23/0。
- **复现**。开发包使用既有 `--inventory-report=<报告路径>`、`--inventory-level=abyss-depths`，增加 `--supply-walk` 走可选支路，增加 `--return-walk=<空目录>` 在有图形 Player 逐帧保存回程。录像为受控清场后的模拟键盘操作，首次玩家自主发现补给、辨认路线与战斗听感仍待实际试玩。

## 2026-11-19 阶段三首项 · 裂隙回廊取核回程

- **已完成并验证**。修正第二关取核后右上角目标提示跨越浮岛间隙指向信标的问题。实际 Windows 游戏画面中，上层横桥测试点显示“向左 58 米”，下层横桥显示“向右 23 米”，进入信标交互范围后显示“已抵达归途信标”。两关场景与各自导航资产均与本批开始时的哈希相同。
- Unity 6000.3.23f1 本地开发包构建 0 错误、0 警告。第二关有图形 Player 53 项通过，第一关无图形 Player 23 项通过，覆盖取核、点灯、撤离及重开。中文展示专项 50 项通过。报告位于 `Logs/return-guide/`，三个站位的游戏截图位于 `screens/`。第二关开发回归可加 `--return-guide-capture=<目录>` 在有图形 Player 重拍。验证使用实际导航和游戏界面，采用定点放置，整段人工寻路体验待评审。试玩包为 `Builds/ReturnGuide/AbyssHunter.exe`。

## 2026-11-19 Windows 重启后保持登录

- 邮箱登录或注册成功后，PlayFab 会话票据存入当前 Windows 用户的凭据管理器。重启时自动联网验证，使用服务端确认的玩家 ID 加载原账号进度。“退出游戏”保留会话，“退出账号”或恢复页的“使用其他账号”清除登录记忆。临时服务故障可以重试，票据过期回到邮箱表单；旧版升级后的首次启动需要手动登录。
- 本地测试服务配合 Windows 测试包连续启动 11 次，23 项界面断言通过，涵盖首次登录、重启恢复、临时断线重试、主动退出、注册另一账号、票据过期及取消进行中的恢复。每次冷启动同时核对请求类型；使用两个模拟身份和隔离的 Windows 凭据名称。报告在 `Logs/remember-session/restart-suite.txt`。未使用玩家密码，真实 PlayFab 账号跨次启动待验收。

- 实际试玩包为 `Builds/RememberLogin/AbyssHunter.exe`，使用正式 PlayFab 地址。构建 0 错误、0 警告，已核对运行程序集移除了本地模拟服务入口。该包第二关独立运行回归 48 项通过，覆盖拱门通行、取核、撤离、死亡重开与返回菜单。报告见 `Logs/remember-session/build.txt` 和 `final-player.txt`。

- 编辑器验证了账号操作 81 项、账号档案 8 项、中文展示 50 项、远征 57 项和环境表现 23 项。首次整套运行时重击专项的五项模拟输入检查失败；关闭构建报告并聚焦 Game 窗口后独立复测 46 项全部通过。首次整套报告与复测报告分别保留在 `Logs/remember-session/full-suite-first.txt` 和 `heavy-combat-verification.txt`。

## 当前工作区

- 当前 HEAD 为 e7b47c1，模型导入前备份已推送；下方“基于 main / c31aed2”指备份起点。后续 KayKit 入口、场景和美术改动仍未提交。本次新增长期目标文档，不把后续资源改动视为已包含在远程备份中。
- 引入新模型前备份分支为 `codex/backup-before-model-import-20261102`，基于 main / c31aed2。该次备份已获授权并完成；后续美术修改未获新的提交/推送授权，不创建版本标签、GitHub Release 或发布运行包。旧发布基线为 v0.2.0 / e41e6b4，未覆盖旧包。
- 当前实现版本为 0.3.0：PlayFab 邮箱注册/登录/密码恢复入口、本机账号进度、选关与第一关撤离解锁第二关、独立的“裂隙回廊”场景及导航。
- 初始已有 README、Docs/presentation、Docs/sunken-expedition 的修改及未跟踪入口文档，已保留。现在工作区还包含本次源码、新资产和版本/构建场景配置；不再是只有文档改动。
- 2026-11-02 第一关 Scene 已进行显示层美术优化；原布局、碰撞、导航、共享角色/敌人 Prefab、现有物品资产和包依赖保留。第二关通过场景覆盖明确绑定侧廊药水，避免旧拾取 Prefab 默认碎片引用造成误掉落。
- 用户已提供公开 Title ID `1302F5`，已填入 `Assets/_Game/Resources/PlayFabConfig.json`。配置方法见 [accounts-and-depths.md](accounts-and-depths.md)。

2026-11-04 收尾核对：以下运行证据产生于 11 月 3 日；本次复核报告、保护文件哈希与新增资源元文件，修正愿景中仍引用旧遮挡问题的摘要，未重复构建，也不将旧时间写为今日运行。

## 2026-11-18 玩家反馈 · 第二关起航拱门穿柱

- **已完成并验证**。可从正面、背面穿过起航拱门两墩的原因是场景原来只有显示模型。已按模型基部的尺寸给两墩与外侧两根高柱配置实体和导航障碍，门洞净宽 5.9 米。生成源为 `Assets/_Game/Editor/KayKitEntranceSetup.cs`。连续应用两次入口实体生成，场景文件逐字节一致。与修复前的第二关场景比较，原有对象只在任务根下新增一条子节点引用。主菜单与第一关场景哈希和本批起点一致。
- **本批集中回归**。Unity 6000.3.23f1 编译及本地 Windows 开发包构建成功，构建报告 0 错误、0 警告。第二关有图形 Player 52 项通过，覆盖正反两向撞墩、中央与内沿通行、门内横移、两侧高柱、对应导航、虚化与恢复、取核及撤离。门体虚化时，从引桥对着右墩一次移动 2.4 米，角色在 z=8.46 停下。同一包第一关混战反击与远征 34 项通过；编辑器环境与微光专项 23 项通过。
- **复测细节**。初次回程导航取点在窄桥外缘，改为从桥面斜向拱墩后通过。首次虚化冲撞检查在桥外沿未通过，当时未分别记录虚化和位移。最终从桥面可站立位置同时记录淡化状态、侧向接触和实际终点。最终报告在 `Logs/depths-gate/` 的 `generation.txt`、`build.txt`、`environment.txt`、`depths-verified.txt` 和 `first.txt`；入口实机图在 `screens/`。之前的 `depths.txt` 和 `final-depths.txt` 是排查记录。
- **待人工验收**。本批修复包为 `Builds/DepthsGateVerification/AbyssHunter.exe`。阶段二仍待玩家通过正常菜单连续游玩，确认读招、闪避反击和听感。

## 2026-11-18 阶段二 · 混战选敌、锁向提示与集中验收

- **已完成并验证**。祭坛两名普通骷髅均存活时，闪避中点击重锤可朝它出刀；点击空地继续朝最近活敌出刀。开发包在侧翼安置一名仍运行 AI 的普通骷髅，稳定复现更近的竞争目标。两个用例的实际朝向误差均为 0.0°。落锤触发时玩家处于闪避无敌窗口，随后重锤受一次 25 点伤害并被击退。原三份场景、普通与重锤 Prefab、攻击配置、敌人控制器与扇形 Shader 通过本批基线哈希比对。
- **已完成并验证**。贴身角色会遮住部分地面内弧，沿用已有头顶文字，在最后 0.2 秒扩大字号并显示“即将落锤”。已查看实机锁向、末段及反击图。首次用例取碰撞体中心时，实际出刀偏离重锤 147.2°；该次没有记录射线命中对象。改点可见上身并断言射线实际命中后通过。另一次取景跨过落锤帧，图中已显示“重击！”。改为在渲染帧末同时检查提示与阶段，最终图通过。
- **集中回归**。Unity 6000.3.23f1，重击 46/0、远征 56/0、中文展示 50/0；Addressables 与 Windows 开发包构建成功，0 错误/0 警告。第一关有图形 Player 34/0、第二关无图形 Player 24/0，均覆盖取核、点灯、撤离及重开。第一关在既有 `--counter-capture=<目录>` 参数外增加 `--mixed-counter`。报告在 `Logs/mixed-counter/` 的 `editor.txt`、`expedition.txt`、`presentation.txt`、`build.txt`、`first.txt`、`depths.txt`。图在 `player/`，首次失败和取景偏帧记录在 `attempt1/`、`timing-attempt/`。
- **待人工验收**。已打开 `Builds/InventoryVerification/AbyssHunter.exe`，由正常菜单进入，没有携带自动回归参数。待玩家完整体验第一关，确认普通敌人与重锤的读招、闪避后的选敌、命中反馈和听感。取得反馈前阶段二保持待验收。未提交、推送或发布。

## 2026-11-18 阶段二 · 闪避后第一刀（已完成并验证）

- 重锤后摇首次接受玩家击退后，提示从“破绽 · 趁机反击”切换为“反击命中”，命中点使用青色火花，本刀播放缓存的 0.20 秒金属音。沿用原闪避、动画事件扣血及击退结果，多目标单刀只结算一次命中音，击杀仍优先。原三份场景、重锤 Prefab 和攻击配置通过文件哈希复核。
- 复用 Unity 6000.3.23f1，重击专项 **45/0**，新增反击提示与对象池复用后清理检查。首轮开发包确认连招与撤离通过后，因贴身火花被角色遮挡，将反击火花取点调整至迎剑侧。最终版重建 Addressables 和开发包，**0 错误 / 0 警告**。最终第一关有图形 Player **27/0**，第二关无图形 **24/0**，包括两关取核、点灯、撤离及重开。
- 第一关碎片回归命令额外传入 `--counter-capture=<绝对目录>`。到祭坛后先受控清掉两名普通骷髅，再把猎手放到重锤近身处。测试关闭常驻无敌，通过 Input System 输入 W+空格，在闪避期间单击鼠标。玩家避开落锤、实际攻击动画命中一次，重锤失去 25 生命、后摇延至 1.35 秒并被击退。开发包验证当帧的中文提示、反击音频生成及玩家音源播放状态。没有进行人工听音或三敌混战手感验收。
- 本机证据在 `Logs/recovery-counter/`。Editor 报告为 `editor.txt`，最终构建和两关报告在 `final/`，命中及击退图为 `final/player/01-counter.png`、`02-recoil.png`。贴身火花在俯视镜头下较小，“反击命中”、伤害数字与后退动作更易辨认。未提交、推送或发布。

## 2026-11-18 阶段二首项 · 重锤蓄力读招（已完成并验证）

- 将本次“下一阶段”落实为核心战斗手感的首项迭代。基线开发包在第一关祭坛复测 28/0。原预警能看到整块危险扇形及锁向变色，锁向期间主要靠透明度闪烁。现保留原完整边界，用一条浅暖色内弧从锤兵脚边向外推进，给出距落锤的连续时间线索。无需新增 Renderer、网格或材质实例。三份场景、BruteAttack 和 EnemyController 的 SHA-256 与本轮基线一致。
- 复用 Unity 6000.3.23f1，重击专项 **43/0**，包括暂停时内弧半径与阶段时钟一致、后摇预警关闭、早闪受伤、适时闪避免伤、后摇单次延长、动画事件去重及复用清理。Addressables 与 Windows 开发包构建成功，0 错误、0 警告。
- 第一关有图形 Player **30/0**。在原碎片回归参数上增加 `--heavy-capture=<绝对目录>`，记录庭院重锤蓄力约 25% 和 82% 时的画面，核对打包材质参数与阶段时钟。配合原祭坛开关复核桥口接敌、侧面、取核和归桥，6 枚投入 4 枚余 2 枚并撤离。第二关无图形 Player **24/0**，含 9 枚投入 6 枚余 3 枚及撤离，两关退出码均为 0。
- 本机证据目录 `Logs/heavy-readability/`。报告为 `editor.txt`、`build.txt`、`first.txt`、`depths.txt`，改前祭坛图在 `before/`，新包跟踪与锁向图在 `heavy/`，四处祭坛图在 `route/`。已查看实机画面，内弧接近外圈时仍能分辨危险边界。前段内弧在贴身时会被人物遮住一部分。Player 使用受控站位、无敌和击杀；时机与反击由 Editor 输入夹具核对。连续人工战斗、听音、目标硬件性能和阶段一整体验收尚待完成。仅本地开发构建，未提交、推送或发布。

## 2026-11-18 第一关终点祭坛（已完成并验证）

- 在第一关末端护栏外以现有 CC0 KayKit 模型组成缺角主龛、高低侧拱与端柱。既有仪式环两翼加旧铜纹与低矮残石，中央铜盘上叠加琥珀铜承印。新增 11 个 Renderer，没有新增灯、粒子或碰撞，也没有改动原核心。局部与完整生成共用 `SunkenAltarSetup`，只运行了局部工具。
- 复用 Unity 6000.3.23f1 编辑器完成编译、两次连续生成与保存。与本批开始时的 SampleScene 对比，4778 个原序列化记录全部保留，唯一差异为 Sunken Ruins 多一个直接子节点。2182 份既有 Assets、Packages、ProjectSettings 文件中，除第一关场景、完整生成入口和开发包回归驱动外全部通过 SHA-256 比对，涵盖地板、碰撞资源、两关 NavMesh、第二关及未提交美术。证据在 `Logs/sunken-altar-review/final/preservation.txt`。
- 首轮图中任务横幅遮住部分后墙，紫色核心与暗色底座对比较弱。最终补暖色承印，截图等正常横幅淡出，携核回程由角色胶囊从核心连续走至桥口。保留 HUD、原跟随镜头及战斗提示。最终四张 1600×900 Player 图在 `Logs/sunken-altar-review/final/player/`。桥口可辨核心与暖铜底座，侧面可见重锤范围，近身保留 E 提示，回程可见空承座和已亮起的符文。桥口远景裁切了后墙上缘，暗调侧拱在侧面和近身更易辨认。
- 最终环境专项 **23/0**，已查看 Game 的战区和取核表现。Addressables 重建、Windows 开发包构建成功，**0 错误 / 0 警告**。第一关有图形 Player **28/0**，含第三遭遇、摄取真实掉落 6 枚、取核、携核归桥、信标投入 4 枚余 2 枚、撤离结算及重开。第二关无图形 Player **24/0**，含 9 枚投入 6 枚余 3 枚及撤离。两次退出码均为 0。最终报告分别见同目录下 `atmosphere-verification.txt`、`development-build.txt`、`player.txt` 和 `depths.txt`。本轮未重跑旧桥廊专用的 39 项组合。
- 测试使用受控站位、无敌与受控击杀。完整人工战斗、连续听音及目标硬件性能仍待验收。D3D12 日志仍有 info queue 查询提示，四张图均正常写出。Unity 自动序列化的空 YAML 字段带行末空格，保留原生输出。只重建本机开发包，未提交、推送或发布。

## 2026-11-15 第一关桥廊模块化（已实现并完成集中技术验证）

- 本批只操作第一关两座桥的显示层：SunkenBridgeSetup 在桥外增加四座下沉承重残拱、八根高低桥头断柱、四处旧铜浮雕及两座远层残拱，共 18 个 Renderer。四份独立材质在 Assets/_Game/Art/SunkenBridge，源模型与许可沿用已有 CC0 KayKit；无新增碰撞、灯或粒子，不改地板、护栏、符文、环境声源、前厅和第二关。真实生成源已接入 AbyssArtSetup，未运行完整 Configure。
- 原编辑器 6000.3.23f1 已复用。受限查询最初未显示原 Unity，尝试启动的进程自动提前退出、未执行项目方法；通过桌面进程核对后恢复原实例操作。新增集中验证快捷键最初与守望台冲突，修正为 Ctrl+Alt+Shift+H，取消冲突窗口后未执行第二关工具。
- 局部重复应用已保存 SampleScene；Logs/bridge-structure.txt 确认单一显示根、原物理不变，两座桥各三条通行线双向 NavMesh 完整。基线 Logs/bridge-baseline/ 保存原场景和 2143 个既有资源哈希；Logs/bridge-preservation.txt 确认除两个有意修改的源码外，其余 2141 个资源不变，包括第二关、两关导航、源模型和既有显示资产。全部原场景组件与姿态保持，仅根的子物体列表新增桥廊。
- 17:29 环境专项 **23/0**（Logs/bridge-atmosphere.txt）；编辑器 450 帧 Main Thread 中位 3.24 ms、P95 3.83 ms，五处峰值 1038 批次/321357 三角形/400 MB。此为 Editor 采样，不是目标硬件 Player 性能验收，不能据此声称成品性能达标。
- 首次开发 Player 通行检查四次失败：测试将中心在原点、高 2 米的胶囊放到 y=0.1，与地面重叠。已按胶囊底部计算测试起点，并保留终点坐标检查；只修正开发验证驱动，未改角色或桥面碰撞。失败报告 Logs/inventory-bridge-first-attempt.txt 与对应 Player 日志保留。
- 17:34 针对驱动修正重建 Addressables/开发包，Succeeded、0 错误/0 警告（Logs/bridge-development-build.txt），没有重复环境全套或构建正式包。17:35 第一关有图形 Player **39/0**、第二关无图形 **24/0**，退出码均 0；Logs/inventory-bridge.txt、inventory-depths-after-bridge.txt。含前厅回归、桥廊材质/四处视线、四次实际 CharacterController 往返及原 6→4→2 / 9→6→3、取核撤离、暂停背包、死亡重开与返回菜单。首次前桥前进终点有敌人避碰偏移，仍在 0.35 米终点容差内；其余三次抵达预期终点，均未掉落。
- 最终四张 1600×900 Player 桥廊图 Logs/bridge-player/01-bridge.png 至 04-bridge.png 均已查看：桥头轮廓、交替远景残拱与原火光引导可见，玩家/道路保持清晰；桥下近层拱券受俯视与阴影限制，完整拱洞轮廓仍不突出，不能宣称达到最终作品集品质。前厅截图在 Logs/bridge-foyer-regression/。受控移动、无敌、击杀不代替完整人工战斗、听音与目标硬件性能评审；未进行真实账号认证。
- 本轮编译与构建通过，Player 无 Unity 异常；图形日志含 D3D12 info queue 查询失败诊断，未阻止渲染或回归。原有工作区改动保留，无提交、推送或发布。最新桥廊只在 Builds/InventoryVerification 开发包；正式配置包仍为 11 月 13 日版本。下一独立美术批次为第一关祭坛，阶段一整体仍在进行。

## 2026-11-13 第一关前厅断面与下沉层次（已实现并完成集中技术验证）

- 本批 11 月 9 日已实现并保存、11 月 13 日恢复验证：SunkenFoyerSetup 将原宽门洞裁切成两段不同倾角且封闭断面的残门楣，保留原 UV 与专用 Mesh GUID；上层侧墙改为开放拱券，前沿地板下方增加错位的浅/深两层门洞。浅层潮蚀石低亮度发光改善平台阴影下的轮廓，深层仍为暗色。显示量由 12 增至 14 个 Renderer，无新增灯、粒子或碰撞，地板/玩法/第二关未改。
- 生成源及场景同步；11 月 9 日 23:25 重复应用与入口至桥面的导航通过（Logs/foyer-structure.txt），23:26 环境专项 23/0（Logs/atmosphere-verification.txt），属于本批前段证据，不记为 11 月 13 日重新运行。修复新 Mesh 内部名称与文件名不一致产生的导入警告。改造前场景、源码、四站位图保留于 Logs/foyer-refine-baseline/。
- 11 月 13 日再次复核原 326 个碰撞体、Transform 姿态及玩法组件；1252 个保护资源哈希不变，包括第二关、两关 NavMesh、旧美术和源模型。结构依据 Logs/foyer-refine-preservation.txt；本批变动限 SampleScene、SunkenFoyerSetup、SunkenFoyer 显示资产及文档。
- 11:40 重建 Addressables、开发和正式配置 Windows 本地包均 Succeeded、0 错误/0 警告（Logs/v030-development-build.txt、v030-release-build.txt）。每次 Player 单次资源构建顺序不变。Editor.log 再次出现 Analytics.IBP 读取 Bee/tundra.log.json 的文件占用异常，后续两次 Build Finished 均成功，未更改构建链或清空 Library；此工具问题保留观察。
- 本次第一关有图形开发 Player **29/0**，第二关无图形回归 **24/0**，退出码均 0；报告 Logs/inventory-foyer-refine.txt、inventory-depths-after-foyer-refine.txt。实际资源包掉落、6→4→2 / 9→6→3、取核/撤离、暂停背包、死亡重开和返回菜单通过，Player 日志无异常。没有把合成身份用于真实 PlayFab 验收。
- 本次四张 1600×900 实机图 Logs/foyer-refine-player/01-foyer.png 至 04-foyer.png 已逐张查看：门楣断面开口落地，浅层下沉结构轮廓更可辨认，战区玩家/敌人及桥前路线可见。深层拱券在俯视镜头仍较含蓄；前厅只是局部试用完成，连续人工移动/完整战斗、听音和目标硬件性能仍待评审。下一批按美术路线推进第一关桥梁过渡区，不扩大到战斗系统或第二关。
- 新本地包位于 Builds/Windows-v0.3.0，未覆盖 v0.2.0，未提交、推送或发布。

## 2026-11-09 第一关前厅模块化试用（已应用并完成集中技术验证）

- 本批场景与资产于 11 月 4 日实际应用保存；此前交接仍写“源码完成、尚未应用”，与真实场景不符，本次核对修正。11 月 9 日复用指定版本 Unity 6000.3.23f1 完成剩余集中验证，没有启动第二个编辑器或重做其他关卡。
- SunkenFoyerSetup.cs 生成独立根“沉没前厅模块建筑”：复用 CC0 KayKit 门洞、六根高低残柱、下沉侧墙/拱券及旧铜浮雕，共 12 个 Renderer；隐藏 21 个旧柱/拱 Renderer，保留地板、原柱脚、灯火、布局和玩法。三份材质及一份独立宽门洞 Mesh 位于 Assets/_Game/Art/SunkenFoyer，不修改共享模型、第二关或原美术资产。
- 已接入 AbyssArtSetup 完整生成末尾，生成开始移除上次模块根以免混入旧合批。局部菜单“环境 > 试用第一关模块前厅”（Ctrl+Alt+Shift+F）于 11 月 4 日重复应用通过，Logs/foyer-structure.txt 确认单一根、原碰撞/Transform 与入口至桥面的导航。11 月 9 日再次比较基线：原 326 个碰撞体、原 Transform 姿态及玩法不变；73 个旧淡化组件仅补序列化默认 targetHeight；1252 个保护资源哈希无变化，包含第二关 Scene、两关 NavMesh、既有美术与 ThirdParty。基线 Logs/foyer-baseline/、结果 Logs/foyer-preservation.txt。
- 本批发现并修复：进口门洞侧墩过粗导致视觉收窄，生成专用宽门洞 Mesh，未改通道碰撞；局部变量同名造成 CS0136，重命名后编译通过；环境恢复测试原 z=-5 站位处于新门廊包围盒内，移至 z=-7.5 完全离开后再检查，没有放宽断言或改运行时算法。首次失败证据 Logs/foyer-environment-first-attempt.txt 保留。
- **11 月 9 日 22:54** 环境专项 **23/0**，含建筑淡化/恢复、微光、取核归途、信标及无运行错误。Logs/atmosphere-verification.txt。450 帧 Editor Main Thread 中位 3.75 ms、P95 5.59 ms，五处峰值 956 批次、276885 三角形、405 MB；不是 Player 性能验收，也不能与历史不同运行状态数据直接推断提速。
- **22:56–22:57** Addressables、开发及正式配置 Windows 本地构建均成功，报告 0 错误/0 警告；每次 Player 单次 Addressables 顺序不变。报告 Logs/v030-development-build.txt、v030-release-build.txt、portfolio-build.txt；本地包 Builds/Windows-v0.3.0，未覆盖 v0.2.0、未发布。Editor.log 有一次 Analytics.IBP 读取 Library/Bee/tundra.log.json 的文件占用异常，后续构建仍成功；最终 Console 0 错误/0 警告、两关 Player 无运行错误。未为该工具日志问题更改构建链或清空 Library，仍需留意复发。
- **22:58** 第一关有图形开发 Player **29/0**、退出码 0，Logs/inventory-foyer-graphical.txt；显式 --foyer-capture 生成的四张实际 1600×900 图片 Logs/foyer-player-review/01-foyer.png 至 04-foyer.png 已逐张查看。门廊淡出后战区玩家/敌人可见，原地板保留，通往桥面的画面清晰。新增建筑无碰撞、打包材质及四站位遮挡检查通过；真实资源包掉落 6→投入4→剩2、取核撤离、暂停背包、死亡/重开与返回菜单继续通过。
- **22:59** 未修改的第二关进行无图形独立包回归 **24/0**、退出码 0，Logs/inventory-depths-after-foyer.txt；含侧廊导航及 9→投入6→剩3。该项是本次逻辑回归，不是第二关新画面验收。
- 仍需人工品质评审：门楣轮廓偏平直、下沉建筑暗部层次较弱；前厅试用尚未代表全关美术完成。四点受控截图/无敌击杀不代替连续人工移动与完整战斗、听音或目标硬件性能；真实 PlayFab 与恢复邮件仍未验证。下一轮优先细化前厅门楣破损轮廓和暗部可见性，再逐区推进第一关桥/祭坛。没有提交、推送或发布。

## 2026-11-03 第二关晶体近身遮挡（已完成实现与集中技术验证）

- 按当前路线只处理上一轮实际 Player 中的核心/西侧紫晶遮挡。RiftCrystalVisibility 将祭坛两侧六块晶簇从两个环境批次拆出，连同原核心和六枚浮动晶体接入独立淡化，共 13 处；AbyssOccluder 可配置射线目标高度，晶体设为 1.1 米，旧建筑默认仍为 0.35 米。材质副本随组件销毁释放，保留现有非缩放时间平滑恢复。没有改角色、战斗、AI、交互或流程。
- 已实际应用并保存 AbyssDepths，生成源同步接入完整 RiftArtPolish；局部菜单“环境 > 修正第二关晶体遮挡”（Ctrl+Alt+Shift+O）重复执行通过。新 Mesh 仅在 Assets/_Game/Art/RiftVisibility。Logs/crystal-visibility-structure.txt 记录重复生成与主/侧路导航检查；Logs/crystal-preservation.txt 确认原 407 个碰撞体、全部原 Transform、1274 个既有资源哈希、第一关场景与第二关 NavMesh 不变。原有一处淡化组件仅补写默认 targetHeight，新增 13 处显示组件、六个源 Renderer 启用和两个批次 Mesh 引用替换。
- 本次 Unity 6000.3.23f1 编译与运行无项目错误；Editor 账号/第二关操作夹具 **81/0**、原环境专项 **23/0**，报告 Logs/account-flow-verification.txt、atmosphere-verification.txt。前者使用合成身份和受控战斗；后者固定第一关且未保存第一关，不当作第二关视觉证据。
- **21:19** 完成 Addressables、开发及正式 Windows v0.3.0 本地构建，均 Succeeded、0 错误、0 警告；Logs/v030-development-build.txt、v030-release-build.txt。沿用每次 Player 单次 Addressables 构建顺序，未发布或覆盖 v0.2.0。
- 第二关有图形 Player **51/0**、第一关无图形 Player **23/0**，退出码均 0；报告 Logs/inventory-crystal-graphical.txt、inventory-first-after-crystal.txt。新增核心淡化、暂停保持、离开恢复、西侧晶簇及 13 处组件检查；原真实包掉落、9→投入6→剩3 / 6→投入4→剩2、取核撤离、暂停/背包、死亡/成功重开继续通过。
- 四张祭坛实机图 Logs/crystal-player-review/01-altar.png 至 04-altar.png 均已查看：核心近身透出猎手头部/身体，西侧仅遮挡晶簇淡化，离开后恢复；其余环境保持显示。入口与中段截图在 crystal-player-entry/、crystal-player-watch/。前图与场景基线在 Logs/crystal-visibility-baseline/。图为受控站位、无敌和受控击杀，不代替完整人工战斗。
- 验证环境问题：原编辑器窗口一度无法访问，用户恢复指定版本后继续复用。首个受限隐藏 Player 的 12 次截图失败，失败记录保留 Logs/inventory-crystal-hidden-attempt.txt；改为正常桌面可见窗口后重跑，所有截图与检查通过，无须修改玩法或重新构建。最终运行没有该截图错误。
- 本批完成“代码/生成接入→集中编译/资源/Editor/Player→失败项复验”，未反复全量验证单项。源码/文档差异检查通过；Unity 场景序列化空字段存在惯常行尾空格，未为此全场格式化。未提交、推送或发布。
- 剩余风险：拆分增加六个启用 Renderer 和 13 个缓存淡化组件，尚无目标硬件 GPU/帧率专项结论；完整人工战斗、连续镜头、暗部/听音与真实 PlayFab 仍待验收。下一个独立美术迭代候选为第一关前厅的小范围模块化试用，本批未开始、未修改第一关。

## 2026-11-03 审查既有美术并推进祭坛（历史）

- 核对当前场景、地板/入口/中段/封印井生成源与上一轮报告。地板当前已统一，没有将旧报告写成本次运行证据；审查发现原选择器仅按 MeshCollider 判断地面，未来导入建筑可能被误换材质。已收紧到原 16 个平台/桥面及两处指定显示根中的铺石，并拒绝重名输出；重跑仍为 20 处，既有地板资产哈希不变。
- 新增 KayKitAltarSetup.cs 与三份独立材质，复用已有 CC0 FBX/断面 Prefab；后方残拱、旧铜浮雕、非对称长柱、下沉拱券及外围残柱形成核心圣龛。全部建筑在原半径 14 米以外，保留中央核心、仪式环、地板、晶体、行走区及入口/中段；9 个 Renderer、6 个淡化组件、4582 个模型三角形，无新增碰撞/灯/粒子。
- 完整 Rift 生成已接入同一源，独立菜单“环境 > 精修第二关核心圣龛”（Ctrl+Alt+Shift+L）只重建圣龛。重复应用检查原场景全部组件不变（任务根 child 列表允许增加显示根），边界/导航通过，报告 Logs/altar-art-verification.txt。初次边界检查拦下柱座越界，已外移并通过；初次实际 Game 发现拱顶裁切，随后降低高拱/柱轮廓并增加雕刻饰板，不修改镜头参数。
- 改造前场景、核心截图、绘制采样及 1284 个资源哈希存 Logs/altar-review-baseline/。Logs/altar-review-structure.txt 记录原 407 个碰撞体、原场景序列化及既有资源保持不变；第一关 Scene、第二关 NavMesh、旧材质/模型、Prefab 与依赖保留。
- 最终 Unity 6000.3.23f1 编译通过；账号/第二关 Editor 夹具 **81/0**（Logs/account-flow-verification.txt），新增四侧实际 WASD 绕坛与新建筑视线检查；原环境专项 **23/0**（Logs/atmosphere-verification.txt），固定第一关，未保存第一关，不代替第二关画面。身份合成、战斗受控，非真实认证或完整人工战斗。
- 本轮 **14:09** 重建 Addressables、开发与正式 Windows v0.3.0 包，均 Succeeded、0 错误/警告（Logs/v030-development-build.txt、v030-release-build.txt、portfolio-build.txt）；保留每次 Player 仅构建一次 Addressables。第二关有图形 Player **43/0**，第一关无图形 **23/0**，退出码均 0（Logs/inventory-altar-graphical.txt、inventory-first-after-altar.txt）。覆盖真实包掉落、9→投入6→剩3 / 6→投入4→剩2、取核撤离、暂停、死亡/成功重开；入口、中段、封印井及新圣龛材质/视线检查通过。
- 实际 Player 图在 Logs/altar-player-review/（四处祭坛机位均已查看）、altar-player-entry/、altar-player-watch/；Game 图 Logs/030-depths-altar*.png 已查看。固定核心 Editor 单帧由前轮 324 批次/95893 三角形变为 353/105725，动态对象时序不同；新增模型本身为 4582 三角形，不把单帧差值当精确开销或成品性能验收。
- 剩余可读性问题：Player 图中紧贴原核心或西侧原紫晶时，角色会局部被这些旧装饰遮住；本轮视线检查仅覆盖新圣龛，不能据此宣称全部装饰遮挡已解决。列为下一批局部优化候选；完整人工战斗、连续画面/暗部/听音、目标硬件 GPU 性能及真实 PlayFab 仍未验收。未提交、推送或发布，不覆盖 v0.2.0。
- 用户于本轮收尾明确采用“批量开发、集中验证”：后续多项任务先计划、连续开发集成，批末统一编译/资源/功能/必要回归，发现问题集中修复并按影响范围复验；只有阻塞错误才在开发中局部检查。规则已写入 AGENTS，不追加本批例行运行。

## 2026-11-02 第二关地板材质一致性（历史）

- 按用户要求统一第二关可行走地面：入口、中段、补给悬台、主/侧桥和祭坛外圈上表面共享“统一冷灰石铺”材质；世界坐标 UV 使用 3.56 米重复单元，消除旧大岩板与模块石铺的色调/纹理尺度差异。已有石板造型、符文/金属饰条、建筑及深色平台竖侧保留，第一关未改。
- 新源 RiftFloorSetup.cs 与专用 RiftFloor 资产；只复制 20 份显示 Mesh，调整 UV 和上下表面的材质分组。RiftArtPolish、KayKitEntranceSetup、KayKitWatchSetup 结束时都调用同一源，避免完整/局部生成后材质退回旧版。独立菜单“环境 > 统一第二关地板材质”（Ctrl+Alt+Shift+U），正常启动无需执行。
- 重复应用与导航检查通过（Logs/floor-material-verification.txt）。结构比对确认原 407 个 Collider、全部原 Transform/MonoBehaviour 一致，20 份显示 Mesh 的顶点位置和三角形拓扑完全不变；1239 个既有资源/包/设置文件哈希未变，包含第一关、第二关 NavMesh、旧美术、模型导入和 Prefab（Logs/floor-material-structure.txt；改造前场景/图在 Logs/floor-material-baseline）。
- 本轮 Unity 6000.3.23f1 编译通过，Editor 实际输入夹具 68/0（Logs/account-flow-verification.txt）；已查看补给悬台、第二桥/祭坛方向 Game 图，统一纹理在桥与平台间连续。原环境专项本轮 23/0，固定第一关，未保存第一关，不作为第二关画面依据。夹具身份合成、战斗受控，不代替人工完整战斗、真实认证或 Player GPU 性能验收。
- 17:44 Addressables 与开发 Player 构建 Succeeded，0 错误/警告（Logs/v030-development-build.txt）。本轮第二关有图形 Player 37/0、第一关无图形 23/0，退出码均 0（Logs/inventory-floor-graphical.txt、inventory-first-after-floor.txt）；沿用已有入口/中段/侧桥/封印发光及真实资源包掉落至撤离回归，没有新增产品或测试运行时代码。实际图 Logs/floor-player-review/02-well.png 已查看，显示补给平台、主战区与桥面一致；其余图及入口回归存同目录与 Logs/floor-player-entry。
- 同次双包任务的正式包 Addressables 曾因 ProjectSettings/ScriptableBuildPipeline.json 被占用报 Win32 IO 1224，失败报告保留为 Logs/floor-release-first-attempt.txt。随后复用同一 Editor，单独重试正式构建，不修改包依赖/构建顺序；**17:51 Addressables 与正式 Windows v0.3.0 包 Succeeded，0 错误/警告**，实际成功报告另存 Logs/v030-release-build.txt。未提交、推送或发布，未覆盖 v0.2.0；尚待人工完整战斗、暗部/接缝和 Player 性能评审。

## 2026-11-02 左侧方井压迫感与纵深（历史）

- 用户追加要求后，本轮只强化中段左侧原方井，暂不推进祭坛。新增 KayKitWatchSetup.Well.cs，把原低矮井衬改为“裂隙封印井”：高低残拱/断柱、暗色深井壁、两层下沉拱券及三道断续紫色封印。使用已有授权模型，不额外堆碎石，不新增灯、粒子或碰撞；新增井专用材质/铭文网格，原中段铺石和东缘残拱资产不变。
- 与原中段生成器联动并重复应用；所有新增井体显示边界检查限制在 x[-33,-28]、z[27.5,32.5] 的原受护开口，不封盖井口，南北补给桥保持开敞。中段现有 23 个启用 Renderer、15 个遮挡组件（Logs/watch-art-verification.txt）。
- Logs/well-art-structure.txt 确认原 407 个 Collider 及 Transform、原玩法组件序列化不变（只排除可重建的显示淡化组件）；1231 个既有资源/包/设置文件哈希不变，涵盖入口、中段旧资产、祭坛、第一关 Scene、第二关 NavMesh、Prefab 与模型导入。主/补给导航检查通过。改造前场景及 Player 图在 Logs/well-art-baseline/。
- 本轮 Unity 6000.3.23f1 编译通过；Editor 操作回归 68/0，增加实际 WASD 绕行南北补给桥、北桥遮挡淡化和南桥恢复及两张 Game 图（Logs/account-flow-verification.txt、030-depths-well-north.png、030-depths-well-south.png）。Editor 为合成身份、受控战斗；最终仅再调整紫色铭文强度，其打包画面单独检查。原环境专项本轮 23/0，固定第一关，未保存第一关，不能代替第二关画面验收。
- 首轮 Game 检查发现新铭文双面三角形共享顶点造成法线抵消、画面异常白光；已改为正反面分离顶点并增加法线有效性检查，重跑实际 Game 确认异常消失。未改全局 Bloom 或 URP 设置。固定中央/井口/东缘 Editor 采样较上轮增加 48/43/32 批次及 14336/14228/12468 三角形（507/472/482 批次，168760/152947/157743 三角形）；有动画/掉落时序差异，不代表 Player GPU/帧率。
- Player 首轮检查还发现 URP 在构建材质校验时清除了手动设置的发光关键字；已按本地包 BaseShaderGUI.SetMaterialKeywords 的 GI 标记要求修复生成源，并新增实际 Player 铭文 _EMISSION 检查。未改 URP 包或全局配置。
- 最终 17:11 重建 Addressables、开发及正式 Windows v0.3.0 包，均 Succeeded、0 错误、0 警告（Logs/v030-development-build.txt、v030-release-build.txt）；仍保持每次 Player 只构建一次 Addressables。第二关有图形独立 Player **37/0**、第一关无图形 **23/0**，退出码均 0（Logs/inventory-well-graphical.txt、inventory-first-after-well.txt）。含真实包掉落、9→投入6→剩3 / 6→投入4→剩2、取核撤离、死亡/成功重开、暂停与入口淡化恢复。
- 独立包回归新增北桥淡化、南桥恢复、铭文发光及对应视线/截图检查，沿用 --watch-capture 参数；最终实际图 Logs/well-player-review/02-well.png、04-well-north.png、05-well-south.png 已查看，另保留中央、东缘及入口回归图。正式非开发包不包含驱动；位置受控/玩家无敌/受控击杀不代替人工战斗。完整人工战斗、连续移动观感、目标硬件 GPU/帧率和听音仍需验收。未提交、推送或发布；整体美术阶段未完成，下一候选仍是独立祭坛升级。

## 2026-11-02 中段守望台模块化建筑（历史）

- 仅更新 AbyssDepths 中段显示：新 KayKitWatchSetup.cs 复用已有入口模型/断面 Prefab，新增独立 KayKitWatch 材质与两个错缝铺石网格；东侧外缘为高低残柱和悬空门拱，西侧原受护裂隙井内增加石质支撑/衬墙。保留原中央符文、晶体、护栏和战斗空间，无新增灯/粒子/碰撞。入口与祭坛未重建，第一关未修改。
- 独立重建菜单“环境 > 精修第二关守望战区”（Ctrl+Alt+Shift+J）；完整 Rift 精修源也在入口生成后调用同一中段源，避免后续生成覆盖。重复应用检查只有一个“KayKit守望遗迹”根，14 个启用 Renderer、10 个 AbyssOccluder（Logs/watch-art-verification.txt）。不要为正常启动运行环境应用或 Configure 菜单。
- 结构核对：原 407 个 Collider 及 Transform、所有原 MonoBehaviour 序列化一致；本轮开始前 1218 个既有美术/第三方/Prefab/包/设置文件、第一关 Scene、第二关 NavMesh 哈希均不变（Logs/watch-art-structure.txt、Logs/watch-art-baseline/protected.json）。入口/祭坛显示快照和主/侧路导航检查通过，未改玩家、AI、任务或产品 UI。
- 复用 Unity 6000.3.23f1，编译与 Console 无错误；账号/第二关 Editor 夹具 57/0（Logs/account-flow-verification.txt），含原两桥/侧路实际 WASD、取核归途、暂停重开，并增加中段三处实际移动、视线遮挡和显示层无碰撞检查。身份为合成身份、战斗受控，不代表真实认证。原环境专项 23/0（Logs/atmosphere-verification.txt），该工具固定第一关，未保存第一关，也不能代替第二关环境验收。
- 三处固定 Game 镜头已做前后对照（中央/井口/东缘）。Editor 单帧采样由 402/382/397 批次、120068/106885/113739 三角形，变为 459/429/450 批次、154424/138719/145275 三角形；新增建筑带来约 3.2–3.4 万三角形与 47–57 批次。铺石合为两网格，高建筑独立保留淡化；该数据有动画/掉落时序波动，不是 Player GPU/帧率结论。记录 Logs/rift-render-samples.txt；改造前场景/截图存 Logs/watch-art-baseline/。
- 16:38 重建 Addressables、开发与正式 Windows v0.3.0 包，均 Succeeded、0 错误、0 警告（Logs/v030-development-build.txt、v030-release-build.txt），保留每次 Player 只构建一次 Addressables 的顺序。第二关有图形 Player 32/0、第一关无图形 23/0，退出码均 0（Logs/inventory-watch-graphical.txt、inventory-first-after-watch.txt）。包含真实资源包掉落与 9→投入6→剩3 / 6→投入4→剩2、取核撤离、死亡/成功重开和暂停；入口淡化/恢复回归通过。
- 新增开发检查参数 --watch-capture=<绝对目录>，在第二遭遇存活时记录三处 Player 画面并检查 Shader、无新增碰撞及建筑视线；Logs/watch-player-review/01-arena.png、02-well.png、03-arcade.png 已查看。入口回归图存 Logs/watch-player-entry-regression/。位置受控、玩家无敌与受控击杀不等于人工完整战斗；正式非开发包不包含此驱动，也不提供游客入口。
- 尚待人工确认：连续战斗/闪避时建筑与敌人的遮挡、暗部与石材重复感、全程画面和听音、目标硬件 Player GPU/帧率。整体阶段一尚未完成；下一次只处理第二关祭坛模块化建筑，保留入口和中段。当前改动未提交、推送或发布，v0.2.0 未覆盖。下方为历史记录，旧报告不作为本轮证据。

## 已实现的边界

- 每次启动须联网登录；本次进程登录后可断网单机游玩。无游客入口，缺配置会明确报错，没有假登录兜底。
- 本机档案按 Title ID 与 PlayFabId 隔离，仅记录通关和最佳成功用时。没有云存档或远征中途存档；死亡和仅清场不解锁。
- 第一关保持 6 枚碎片投入 4、剩 2；第二关 9 名敌人、投入 6、剩 3。每次远征重置生命和背包，重开使用当前关。
- 第二关已按用户选择从零重制为浮空裂隙：起航浮岛、1.5 米守望台、3 米中央祭坛，八米宽主桥与水平转台，西侧补给悬台和两条五米宽侧桥。固定一瓶生命药水；新地形/材质/晶体位于 `Assets/_Game/Art/FloatingRift/`，不引用旧庭院网格。任务文案、环境声位置、镜头区域和 14 处归途顺序已重配。
- 构建列表包括 MainMenu、SampleScene、AbyssDepths，保留 Addressables 只构建一次的链路。

## 2026-11-02 第二关入口融合与镜头可读性（最新）

- 按 PROJECT_VISION 的任务 1 执行，未扩展到中段/祭坛、第一关或战斗系统。KayKitEntranceSetup 与新增 Polish partial 同步实现入口错缝铺石、石缝底层、低色差材质、拱门/柱体斜断面及遮挡淡化；保留原模型、许可与 .meta。71 块铺石合为两个网格，拱门/柱/侧墙七个 Renderer 使用既有 AbyssOccluder，保持独立以便淡化。原 FBX 和导入设置不变。
- 最终候选已重复生成；结构报告 Logs/entrance-refinement-structure.txt 确认原 407 个碰撞体和 Transform、原始玩法组件完全一致，第一关 Scene、第二关 NavMesh、ThirdParty 源文件及 .meta 哈希未变。主/补给导航 2/0；生成入口保持单根、无新增碰撞。新增淡化组件属于显示层，不放宽其他原组件的检查。
- 打开的 Unity 6000.3.23f1 编译无错误；最终 Editor 账号/第二关操作 46/0，包含新增实际 WASD 过门/返回、遮挡淡化/恢复及对应 Game 截图。原 40 项中的两桥、护栏闪避、取核、归途、暂停、第二关重开和账号隔离继续通过。合成身份与受控击杀，不代表真实认证或完整人工战斗。截图 Logs/030-depths-entry.png、030-depths-entry-gate.png、030-depths-entry-return.png 已查看。
- 入口固定镜头对照：模型接入版历史采样（13:14）471 批次 / 95957 三角形；本轮最终（15:53）351 / 97350。保留更多石铺细节同时合批降低绘制数量；这是单帧 Editor 对照，存在动画/掉落时序差异，不是成品 GPU 或帧率验收。原值及场景备份在 Logs/entrance-refinement-baseline/。
- 开发包回归驱动新增显式 `--entrance-capture=<目录>`：仅在已启动的碎片开发回归中启用，要求非 batch、有图形设备；记录入口、淡化、恢复三张 Player 图并检查淡化和材质。仍只在 Editor/Development 编译，不是正式认证入口或正式包的游客模式。
- 本轮原环境专项 23/0，Console 无错误；该入口固定第一关，未保存第一关，不代替第二关环境验收。16:01 已重建 Addressables、开发与正式 Windows 包，均 Succeeded / 0 错误 / 0 警告；保留每次 Player 只构建一次 Addressables 的链路，未覆盖 v0.2.0。报告 Logs/v030-development-build.txt、Logs/v030-release-build.txt。
- 本轮实际有图形 Windows 开发 Player 第二关 27/0、退出码 0（Logs/inventory-entrance-graphical.txt）；1600×900 的入口、过门淡化、返回恢复三张图在 Logs/entrance-player-review/，已实际查看，材质与透明效果有效。另运行第一关无图形 Player 23/0、退出码 0（Logs/inventory-first-after-entrance.txt）。两者含真实资源包掉落、碎片扣除、取核撤离、暂停与重开；图形夹具仍使用受控位置/无敌/击杀，不代替人工连续战斗、听音或真实性能验收。
- 仍需人工评审完整战斗中的遮挡观感、不同显示器暗部、完整 Player 性能与听音。此次不代表整个阶段一结束；下一迭代是第二关中段单一区域，暂不同时改祭坛和第一关。未提交、推送或发布。

## 2026-11-02 长期目标与开发准则整理（本次仅文档）

- 保留 AGENTS 中原有版本、架构、GUID、动画事件、池、物品身份和验证/发布约束，增加长期品质方向及“继续优化”执行流程；新增 PROJECT_VISION，记录两关美术特色、战斗/关卡目标、四阶段路线、完成标准和当前进度。
- 实际核对工程版本/依赖、LevelCatalog、既有命中反馈和重锤参数、KayKit 生成源/七模型清单及历史验证报告。当前为阶段一进行中；已有战斗与关卡流程不重做，新入口只算已实现待完整验收，历史无图形回归不代替新模型 Player/人工视觉验收。
- 下一阶段三项任务依次为：第二关入口融合与镜头可读性；第二关中段/祭坛分区建筑升级；第一关模块化遗迹与两关美术统一。每次只处理一个区域/主题，具体验收及边界见 PROJECT_VISION。
- 修正 PROJECT_CONTEXT 仍称“六模型子集”的过时表述，实际 Models 清单和目录均为七个 FBX。检查文档链接、事实和差异格式；本次不运行 Unity、不改源码/场景/资源、不提交或推送。保留用户已有工作区变更。

## 2026-11-02 引入模型资源前备份

- 本次为源码与资产备份，包含账号/本机档案、多关卡、浮空裂隙及两关美术、生成源和必要 `.meta`，保留先前 README/文档改动。工程内部版本仍是尚未发布的 0.3.0，不代表第三版正式发布；main 和 v0.2.0 发布基线不改。
- 分支 `codex/backup-before-model-import-20261102` 用于恢复引入新模型前的状态。`Library/`、`Temp/`、`Logs/`、`Builds/` 及 Unity `_Recovery` 临时副本不纳入提交；恢复副本仍保留在本机。原始验证报告为本机忽略产物，关键结论已写入下方记录。
- 本次备份仅核对 Git、文件范围、资产 `.meta`、凭据特征和差异格式；没有重新运行 Unity。游戏验证沿用下方第二轮精修已实际完成的结果，真实 PlayFab 认证、人工画面/战斗/听音和 Player GPU 性能仍待验收。
- 源码和文档的差异空白检查通过；新增 Unity 序列化资产有空字段后的生成尾随空格，备份保留原样，没有为了格式检查重写 Scene、Mesh、材质或 GUID。

## 2026-11-02 KayKit 第二关入口示范（本轮）

- 在现有本机 Unity 6000.3.23f1 中直接导入官方免费 Dungeon Remastered 1.0 子集（固定提交 b0ca9bd96a8072ab36a3a5464f00ed1e06a16d07），六个 FBX 和一张 atlas；不是 itch 当前 Free 1.1 下载。原始 LICENSE、SOURCE、SHA256SUMS 在 Assets/ThirdParty/KayKitDungeon；CC0，未导入付费 EXTRA/SOURCE，未提交/推送/上传。
- KayKitEntranceSetup.cs 生成独立 URP/Lit 材质和 VisualRoot Prefabs（Assets/_Game/Art/KayKitEntrance），并已保存 AbyssDepths。入口小范围替换为浅铺石、蓝晶模型基座、两根不同高度柱和侧边矮墙。RiftArtPolish / Architecture 同步接入；原地图生成器调用原精修流程时也保留本示范。重新运行精修重写了部分 RiftPolish 网格与合批编号。无需为启动游戏重新生成。
- 本轮实际导入/编译、编辑态/运行时入口画面检查无错误；结构快照比较全部现有 Collider、Transform、MonoBehaviour 序列化内容通过，新增模型无碰撞；重复显示生成只有一个根节点。主/补给路线导航 2/0（Logs/kaykit-entrance-verification.txt、floating-rift-navigation.txt）。
- 本轮账号与第二关操作回归 40/0（Logs/account-flow-verification.txt），含实际 WASD 路线、护栏与闪避、取核、归途、暂停、重开；使用合成账号和受控战斗。入口截图 Logs/030-depths-entry.png。另运行仓库要求的原环境专项，通过且无运行错误；该工具固定第一关，不能作为第二关环境验收证据，未保存第一关。
- 第一关 Scene、第二关 NavMesh 的 SHA-256 与本轮初始一致；玩家/敌人/战斗/交互运行时代码、Addressables 配置未改。未重建独立 Player，未发布，尚需人工确认入口完整战斗遮挡、铺石边缘、Player 画面/性能/听音。
- 修改前场景 Logs/AbyssDepths-before-kaykit.unity；工具菜单 Tools > 深渊猎手 > 环境 > 接入 KayKit 入口示范（Ctrl+Shift+K）。工具只用于重建。后续从 MainMenu 正常 Play 即可。

## 2026-11-02 第二关第二轮造型精修

- 实际修改并保存 AbyssDepths 与 RiftPolish 资产；新增 RiftArtPolish.Architecture.cs 并接入原精修流程/地图生成器。入口为斜切分层石环与带八角底座、抱箍、弧肋的蓝晶设施；中段增加断续放射铺石及原开口内衬；祭坛为三层环座、刻纹、后半悬冠与肩切橙晶，紫晶形态/大小错落。背景增加扁平沉积岩和六处不同明度的断拱遗迹。地纹改为周期不规则石铺；自发光略收敛，无新增灯或粒子预算。
- 保留三段布局及可行走区域：Logs/rift-round2-structure.txt 确认原 407 个碰撞体及其 Transform 完全一致，无新碰撞；第一关 Scene 与第二关 NavMesh SHA-256 与本轮开始前相同。主/侧路导航 2/0；重复应用精修工具后再次检查通过。未改玩家、AI、遭遇、任务、交互和 UI 产品代码；AccountFlowVerification 仅在原有编辑器检查时追加绘制采样。
- 复用打开的 Unity 6000.3.23f1，编译及 Console 无错误。最终场景账号/第二关夹具 40/0，已检查入口、侧廊、第二桥、祭坛 Game 截图（Logs/030-depths-*.png、Logs/030-second-bridge.png）。含实际 WASD 往返、护栏连续移动和闪避、取核、暂停及第二关重开；使用合成身份与受控击杀，不能代表真实认证或完整人工战斗验收。
- 本轮另执行原有环境专项 23/0，运行无错误。该工具固定加载第一关，不能作为第二关环境或性能通过的证据，也未将结果保存进第一关 Scene。
- 修改前四处固定镜头为 371/350/450/321 批次、62036/68134/111393/72082 三角形；最终采样为 345/317/432/321 批次、82688/88571/134736/95875 三角形（顺序：入口、侧廊、第二桥、祭坛；Logs/rift-render-samples.txt，旧值另存 Logs/rift-round2-baseline/）。新增建筑令三角形增加约 2.0–2.4 万，批次未增加；这是单帧 Editor UnityStats，掉落/动画时序有波动，不是 Player GPU/帧率验收。
- Addressables 已重建；12:19 开发与正式 Windows 构建均 Succeeded、0 错误、0 警告（Logs/v030-development-build.txt、Logs/v030-release-build.txt）。最新独立 Player 第二关 24/0、第一关 23/0，退出码均 0（Logs/inventory-rift-round2.txt、Logs/inventory-first-after-round2.txt），包含真实资源包掉落、9→投入6→剩3 / 6→投入4→剩2、取核撤离、暂停与重开。无图形受控逻辑回归，不替代成品画面验收。输出 Builds/Windows-v0.3.0/，未覆盖 v0.2.0；构建链仍为每次 Player 只构建一次 Addressables。
- 仍需人工确认：正式 Player 完整战斗时的角色/敌人遮挡、不同显示器暗部和辉光、沿全程移动的远景层次与材质重复感、GPU/帧率及听音。未提交、推送或发布。

## 2026-11-02 第二关第一轮美术精修

- 已实际应用并保存 AbyssDepths：独立 RiftPolish 资产、区域石材、缺角护墙、断裂岩瓣/悬片、三层远景残岛与薄雾、低量浮尘、蓝/紫晶簇、起航罗盘及橙金核心仪式环。RiftArtPolish.cs 为显示层生成源，FloatingRiftMapBuilder 在原有地形/导航生成结束后调用同一流程。
- 原始布局、407 个碰撞体及对应 Transform 完全一致（Logs/depths-art-structure.txt）；第一关场景与第二关 NavMesh 的 SHA-256 与本轮开始前一致。未修改玩家/战斗/AI/任务/UI 运行时代码及旧共享材质。
- 第二关独立 RiftVolume：ACES、+0.45 EV、轻量 Bloom/Vignette、关闭 Motion Blur；相机 SMAA。全局 URP 不改，动态核心/归途/晶体仍沿用现有组件。
- 最终美术下，复用打开的 Unity 6000.3.23f1 执行账号/第二关夹具 40/0，入口至祭坛/侧廊导航 2/0；已查看本轮入口、补给悬台、桥与祭坛实际 Game 截图（Logs/030-depths-*.png、Logs/030-second-bridge.png）。包含实际 WASD 往返、护栏连续移动与闪避、取核、归途苏醒、暂停和重开。使用编辑器合成身份与受控击杀，不代表真实认证或完整人工战斗验收。
- 额外执行原有环境专项 23/0；该入口固定运行第一关，不能代替第二关环境证据。第二关依据为上述实际 Game 截图、40 项夹具与结构/导航检查。
- Addressables 已重建，开发/正式 Windows 构建均成功、0 错误、0 警告；实际独立 Player 第二关 24/0、第一关 23/0，退出码均 0（Logs/inventory-rift-art-polish.txt、Logs/inventory-first-after-rift-art.txt）。Player 为无图形受控逻辑回归。新包 Builds/Windows-v0.3.0/，未覆盖 v0.2.0；未提交、推送或发布。
- 已重复执行精修工具检查可重建性，未增加碰撞或修改导航。静态显示按材质/区域合批；新增粒子两处各最多 18 粒。尚未做第二关固定路线的 Player GPU/帧率性能验收，不以第一关环境采样代替。

## 2026-11-02 第一关美术优化

- 已实际修改并保存 SampleScene；生成源 AbyssArtSetup.cs / AbyssArtSetup.Portfolio.cs 同步更新。新增显示资产在 Art/SunkenPolish：八棱断柱、七种缺角石板、冷灰石/浅灰边/青铜差异、外围暖火盆、前厅破损拱券、桥侧细节、祭坛仪式断环、分层不规则深渊岩与较低透明度雾。
- 第一关使用独立 SunkenVolume（ACES、+0.65 EV、对比/饱和调整、轻量 Bloom/Vignette、关闭 Motion Blur）与相机 SMAA；全局 URP 配置和旧共享材质不改。调色组件作为子资产保存；修正重复生成时合批 Mesh 名称为空的警告。
- 结构比对报告 Logs/first-art-structure.txt：326 个原碰撞体和对应 Transform 完全相同，无新增碰撞。第一关 NavMesh 与第二关 Scene 的 SHA-256 均与本轮开始前相同。未改玩家、战斗、AI、任务、交互和 UI 运行时代码。
- 最终美术下，复用打开的 Unity 6000.3.23f1 完整回归：重击 41/0、远征 57/0、中文展示 50/0、环境 23/0、本机档案 8/0、账号/多关卡 40/0；Logs/v030-suite.txt 为 PASSED。后两项仍是合成身份/本机夹具，不是真实联网认证。
- 已检查本轮实际 Game 截图：入口、两处桥梁、祭坛战斗、核心传递、点灯与熄灭；报告及图片在 Logs/atmosphere-review/、Logs/expedition-review/、Logs/heavy-combat/。Player 人工画面/完整战斗、目标显示器暗部与听音仍待人工验收。
- Addressables 已重建，开发与正式 Windows 构建均 Succeeded、0 错误、0 警告；实际 Player 第一关 23/0、第二关 24/0，退出码均为 0（Logs/inventory-first-art-polish.txt、Logs/inventory-depths-after-first-art.txt）。均为无图形受控逻辑回归，不能替代人工画面验收。新包位于 Builds/Windows-v0.3.0/，没有覆盖 v0.2.0；未提交、推送或发布。
- 本轮五处编辑器采样 450 帧，主线程中位 3.02 ms、P95 3.66 ms；峰值 986 批次、271534 三角形、381 MB 已分配内存。此数据包含编辑器开销，不等同成品性能。新增造型、遮挡构件和 SMAA 带来额外绘制成本；没有宣称性能优于旧版。

## 验证记录

下列早期地图显示/第二桥修复记录属于旧布局历史，不代表当前浮空地图验证；最新重制结果单独记录。

### 2026-11-01 浮空裂隙重制（当前地图）

- 全新地图由 `FloatingRiftMapBuilder` 生成并保存，保留第二关 Scene/NavMesh GUID 与关卡 ID。旧场景备份在 Logs；当前 Scene 对 `Art/Abyss/Depths/` 的网格引用为 0。第一关 Scene、共享 Prefab、物品资产与依赖未改。
- 升坡段只连接水平转台的边缘，不跨过转角，避免重叠桥面高度差大于玩家台阶高度。护栏有实体碰撞并标为导航不可行走；晶体/盘面等装饰无碰撞。
- 已在打开的 Unity 6000.3.23f1 中编译并执行完整回归：本机档案 **8/0**、重击 **41/0**、远征 **57/0**、中文展示 **50/0**、第一关环境 **23/0**；账号/新第二关 **40/0**。`Logs/v030-suite.txt` 于 23:02 更新为 PASSED。上述数字是集成检查项数，非 Test Runner 单元测试数量。
- 新图实际 WASD 走到补给悬台、两座宽桥所有转台、祭坛、入口，连续向外移动与闪避时护栏停止点约 **(11.27, 4.08, 43.88)**，没有掉落。材质、随身灯、晶体浮动/暂停、取核、14 处归途苏醒、两处风声音源播放，以及第二关重开、退出换账号均通过。身份仍为编辑器合成身份，战斗使用无敌与受控击杀。
- 主路/补给导航 **2/0**，报告 `Logs/floating-rift-navigation.txt`；已查看本轮入口、补给悬台、上层桥和中央祭坛截图，位于 `Logs/030-depths-*.png`、`Logs/030-second-bridge.png`。声音检查只确认音源播放，未完成听音验收。
- 新图资源已重建 Addressables；开发包与正式包于 23:03–23:04 构建成功，均 **0 错误、0 警告**（`Logs/v030-development-build.txt`、`Logs/v030-release-build.txt`、`Logs/portfolio-build.txt`）。保留每次 Player 构建只生成一次 Addressables 的顺序。
- 本轮实际 Windows Player：新第二关 **24/0**、第一关 **23/0**，退出码均 0；报告 `Logs/inventory-floating-rift.txt`、`Logs/inventory-floating-first.txt` 均 PASSED。验证真实 AssetBundle 掉落、9→投入6→剩3 / 6→投入4→剩2、取核撤离、背包暂停、胜利/死亡重开与菜单返回。Player 为无图形受控逻辑回归；桥上输入与本轮画面检查来自 Editor，未替代完整 Player 人工操作、画面和听音验收。
- 可运行新包 `Builds/Windows-v0.3.0/AbyssHunter.exe`；未覆盖 v0.2.0，未提交、推送或发布。真实账号/恢复邮件待验收的边界不变。

### 较早记录（旧地图与账号接入历史）

2026-10-30 已实际执行：Unity 编译、场景创建；独立主路与侧廊 NavMesh 连通检查；本机档案 8/0；重击 41/0、远征 56/0、展示 50/0、环境 23/0；账号/解锁编辑器夹具 17/0。后者使用独立 Logs 档案与合成身份，**不代表真实联网认证**。

2026-10-31 最终原有四项回归：重击 41/0、远征 56/0、展示 50/0、环境 23/0，本机档案 8/0。早期一次模拟点击未触发攻击，恢复正常 Editor 窗口后重跑通过，未改战斗代码。

2026-11-01：第二关补全导航边界后，账号/多关卡操作夹具 **26/0**，汇总 `Logs/v030-suite.txt` 为 PASSED。包括实际 WASD 走到侧廊、庭院、祭坛并返回入口，死亡不解锁、清场未撤离不记通关、第一关撤离解锁、第二关重开、退出和换账号隔离。身份仍为编辑器合成身份，不代表联网认证。

导航修复仅在第二关：侧廊药水明确绑定治疗物品；低柱脚/平台边缘标为不可行走；斜桥护栏保留碰撞并增加导航排除体积，避免路径贴边而玩家胶囊受阻。全局代理设置与第一关未改。`Logs/depths-navigation.txt` 补给及主路/侧廊连通检查 3/0。

2026-11-01 构建和独立 Player 已实际执行：

- 在已打开的 Unity 6000.3.23f1 中重建 Addressables，开发与正式构建均 Succeeded，0 错误、0 警告；报告 `Logs/v030-development-build.txt`、`Logs/v030-release-build.txt`。每次 Player 构建仍只生成一次 Addressables。
- 开发包 `Builds/InventoryVerification/AbyssHunter.exe`：第一关 **23/0**、第二关 **24/0**，两次退出码均 0，报告 `Logs/inventory-v030-first.txt`、`Logs/inventory-v030-depths.txt` 均 PASSED。真实资源包掉落、稳定 ID、三段遭遇、取核、6→4→2 / 9→6→3、撤离、暂停、背包、胜利/死亡后重开和菜单返回通过。
- 正式构建位于 `Builds/Windows-v0.3.0/AbyssHunter.exe`，没有覆盖 v0.2.0。首次构建 Title ID 为空；用户提供编号后重新构建，结果见下方配置记录。不能作为真实账号验收通过或已发布版本。
- Editor 的中文表单与第二关画面截图已检查；Player 自动回归为无图形、受控击杀，不能代表 Player 人工画面/听音/战斗平衡验收。所有回归项为集成检查，非 Unity Test Runner 单元测试数量。

批处理执行原有 Play 输入/截图测试会受 WaitForEndOfFrame 限制；改用普通 Editor 后原有四项通过。沙箱许可证连接失败时使用已授权的正常桌面 Editor 权限。2026-10-30 最后一次新增操作回归曾因自动审批额度失败而未启动；不是操作被判断为不安全。

## 待验收

2026-11-01 第二桥通行修复：用户明确指出庭院至祭坛的第二座桥卡住/掉落。原桥短纵深、大横移，斜护栏侵入桥头，改成六米宽的三段折返通道（庭院向前 → 中段向右 → 向前进祭坛），桥面与平台重叠并保留外侧实体护栏，独立导航已重烘焙。Editor 33/0：新增四个桥上转折点实际 WASD、向外连续移动+闪避撞护栏、前往祭坛与返回入口均通过；撞栏停止点约 (10.45,1.08,35.91)，没有掉落。截图 `Logs/030-second-bridge.png` 已检查。未改第一关或共享 Prefab。Addressables 重建及开发/正式构建均成功，0 错误、0 警告；第二关实际 Player 24/0、退出码 0（`Logs/inventory-second-bridge-fix.txt`），含 9→6→3 与撤离/重开。桥上实际输入验证来自 Editor，Player 回归仍为无图形受控逻辑验证。

2026-11-01 地图显示修复：用户反馈第二关地图异常，查明复制后的静态环境合并网格仍保留第一关顶点位置，而碰撞体/逻辑物体已经平移，造成可见地图错位。此前行走回归通过并不能证明显示正确，原画面检查漏检此项。现从场景中保留的 662 个源网格按第二关实际 Transform 重建为 108 个独立批次（`Assets/_Game/Art/Abyss/Depths/`），停用复制来的 81 个旧批次；第一关和共享网格资产未修改。Editor 操作回归再次 26/0，已检查修复后祭坛截图；重建 Addressables 后开发/正式包均 0 错误、0 警告。修复后的第二关实际 Player 回归 24/0、退出码 0（Logs/inventory-depths-visual-fix.txt）；该 Player 回归无图形，画面依据来自 Editor 截图。

2026-11-01 Title ID 配置记录：用户提供 `1302F5`，已写入 Resources 配置；通过真实 HTTPS 登录端点，以 `.invalid` 域名的不存在账号进行连通性探测，返回 HTTP 401 / AccountNotFound。仅证明服务可达，未创建账号、未发送邮件、未验证真实登录。已停止加载旧配置的 Editor Play 会话。开发与正式包随后重新构建，均 0 错误、0 警告；已检查正式包 resources.assets 包含 `1302F5`。用户现在可重新 Play 或运行新包进行真实注册登录测试。

- 真实 PlayFab 注册、重复邮箱、密码错误、网络超时、恢复邮件投递与新旧密码：Title ID 已配置，仍待使用实际测试账号验收。
- 完整人工战斗平衡、听音与视频评审仍未完成；受控击杀/无敌的自动逻辑回归不能替代。
- 新版本未发布。2026-11-02 模型导入前备份的提交/推送已完成，该历史授权不代表后续美术任务已提交或可自动推送；不创建 Release，忽略 Logs/Builds/Library 产物。

## 2026-11-02 入口视觉可见性修正（后续）

用户确认查看的是 Unity 第二关，反馈上一版几乎看不到效果。原版本只有小块铺石、低矮柱和晶体底座，视觉体量不足。本轮只在入口显示层新增 12.5 米宽、5.3 米高的开放石拱门，柱子增加至四根并高低错落（最高 5.8 米），铺石扩至约 12.6×9 米缺角布局（31 块）；原蓝晶与玩法保留。门洞原带独立 wooden door 节点，生成 Prefab 时移除，保证视觉开放。

补充的 wall_doorway.fbx 来自同一官方 CC0 固定版本；SOURCE/SHA256SUMS 已更新，总计七个 FBX。原碰撞、Transform 与 MonoBehaviour 序列化比对、重复生成和主/补给导航均通过；第一关 Scene、第二关 NavMesh SHA-256 未变。新增实际入口截图为本轮 Logs/030-depths-entry.png；没有重建独立 Player 或上传/提交模型。具体操作回归以本节后续验证记录为准。
本轮后续验证：第二关操作夹具重新执行 40/0（合成账号、受控战斗），原环境专项重新执行 23/0、无运行错误。环境专项固定第一关，未保存第一关，不能替代第二关环境验收。实际 Game 截图已查看；仍未构建独立 Player。
