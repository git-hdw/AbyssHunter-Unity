# 《深渊猎手》展示说明

《深渊猎手》的菜单、HUD、开场与结算采用统一展示风格，当前展示关卡为“沉没遗迹追猎”。

## 界面

- 深海蓝、青色与暖金色配色；中文主标题和统一的字号层级。
- 菜单、操作说明、HUD、开场、波次、重锤兵提示、背包及结算统一使用简体中文，按键名称保留 WASD、I、Esc 等键帽标识。
- 主菜单：动态矢量剑徽、开始游戏、操作说明、退出、渐隐转场。
- HUD：数值血量与平滑血条、当前追猎目标、区域威胁、目标方向距离、交互提示、计时、碎片/药水数量、闪避冷却。
- 开场：固定相机初始位置、目标说明、三秒倒计时；战斗在倒计时后开始。
- 暂停和背包：统一模态面板，暂停世界时间并阻断玩家战斗输入；点击药水使用。
- 胜利/失败：独立文案、用时、击败敌人数、碎片数量、承受伤害和剩余血量，支持重开及返回菜单。
- 背景音为项目内合成的原创环境音；保留实际战斗音效。

## 使用

Unity 打开 `MainMenu` 或 `SampleScene`，进入 Play 即可。UI 由 `GamePresentation` 在场景加载后创建。旧版屏幕 Canvas 和旧流程控制器在运行时停用，世界空间敌人血条保留。主题集中在 `PresentationTheme`，徽记由 `AbyssSigil` 绘制。

输入：WASD 移动，左键攻击，空格闪避，E 交互，I 背包，Esc 暂停。结算页 R 重开，M 菜单。远征停用 F5/F9 玩家数据回档，避免核心、敌人与背包进度不一致。

## 录制与导出

1. Unity 菜单 `Tools > 深渊猎手 > Record input-driven demo`。
2. 工具以 1600×900、30 fps 捕获 Game View 和游戏混音。等待退出 Play；原始帧、WAV 和报告在 `Recordings/Presentation/raw`。
3. 在 PowerShell 运行 `./Tools/Encode-Portfolio.ps1`；需要本机 FFmpeg。生成 H.264/AAC、适合直接播放的 MP4。
4. `Tools > 深渊猎手 > Build portfolio player` 导出 Windows 可玩版本到 `Builds/Portfolio`。

录制工具只在 Editor 编译，通过 Input System 虚拟键鼠操作实际游戏；不修改生命、伤害、AI 或波次结果。它是输入回放演示，不是人工操作录像，也不是性能基准。视频中的结算应与该次实际运行结果一致。测试用的受控胜负验证与对外演示分开。

`Preview presentation` 同样走实际游戏流程，截图与报告写入 `Logs/presentation-review`。录制和预览工具均不进入玩家构建。

美术模型仍为项目已有 KayKit 素材；新增界面图形为程序绘制。中文字体为随包内置的 Noto Sans CJK SC，不依赖玩家电脑安装字体。字体源文件与 SIL Open Font License 位于 `Assets/_Game/Resources/Fonts/`；Windows 包根目录保留原文《字体许可.txt》。来源：https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/SimplifiedChinese 。

`Tools > 深渊猎手 > Configure Chinese presentation` 同步场景与预制体中的旧版提示和字体；`Verify Chinese presentation` 验证各页面中文字符、英文残留、文字裁切和交互，报告与截图在 `Logs/presentation-verification.txt`、`Logs/presentation-review/chinese-*.png`。
