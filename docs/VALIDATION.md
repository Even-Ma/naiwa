# 本地验证记录

日期：2026-10-26；版本：1.3.1；平台：Windows x64。

使用系统 .NET Framework C# 编译器构建成功，无编译警告。执行 `scripts/build.ps1 -SelfTest`：**103 项检查通过，RESULT: PASS**。

覆盖原版 422 帧素材、透明窗口、转眼/眨眼、腹部变形、拖动/落地、双击/躺倒/起身、多屏坐标、全屏检测、音频开关、帧缓存和 GDI 句柄稳定性；新增专注时钟的暂停/恢复/重置/到期与非法时间输入、真实面板按钮、关闭隐藏/重开及计时器释放检查。

已查看实际窗体渲染图，确认当前 125% DPI 下文字和控件没有裁切。截图见 [陪伴面板](companion-panel.png)。

真实运行检查：

- `--panel` 能启动主程序并打开陪伴面板。
- `--probe` 返回 `Found=True`、`Visible=True`，窗口在工作区内。
- 再次 `--panel` 后第二进程退出，原进程继续运行。
- `--quit` 后原进程正常退出。
- `%LOCALAPPDATA%\NaiwaCompanion\settings.ini` 已保存位置、85% 大小、默认静音、置顶、全屏隐藏及欢迎标记；未出现错误日志。

本次构建 EXE 的 SHA-256：

```text
91542E65A0AAFC3B2B23935741AFF73EA49B2B055567F87CFD24A87DAEB69FDD
```

后续重新编译时 EXE 校验值可能因编译时间等发生变化。GitHub Actions 的远端结果应以仓库构建页显示为准。本记录不表示所有 Windows 设备、DPI 或多显示器组合都已经实机验证。
