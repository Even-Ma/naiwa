# GitHub 源码与构建

本项目对应的个人源码仓库为 [Even-Ma/naiwa](https://github.com/Even-Ma/naiwa)。仓库中保存源码、素材来源、构建脚本和自动构建工作流；约 25 MB 的上游素材由脚本在需要时获取。

请保留根目录 `LICENSE`、`THIRD_PARTY_NOTICES.md`、README 中的上游署名，以及 `docs/upstream/` 中的来源记录。不要把仓库改成 MIT 等其他许可证，也不要将奶蛙形象、动画、笑声或上游代码声称为原创。上传源码不代表获得角色和视频等第三方内容的额外授权。

## 下载并构建

在 GitHub 仓库选择 **Code → Download ZIP**，完整解压后双击 `build.cmd`。也可以使用 Git 下载：

```powershell
git clone https://github.com/Even-Ma/naiwa.git
cd naiwa
```

构建脚本会先检查 `assets/` 中的三个必需素材。缺失或校验不符时，从 `yugh6/Naiwa` 的固定提交 `72c763fe1349fb08a06f976e1709f4f5f80e09c7` 下载，并使用预置 SHA-256 校验。下载失败或校验不符，不会使用该下载内容，也不会覆盖现有有效文件。网络受限时，可以从完整本地源码包恢复 `assets/` 文件夹。

程序输出到 `dist/Naiwa/奶蛙桌宠.exe`；便携压缩包为 `dist/Naiwa-Windows-x64.zip`。运行桌宠无需联网。

## 本地验证与后续提交

改动源码后，建议执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 -SelfTest
```

确认 `work/self-test/self-test.txt` 最后为 `RESULT: PASS`，并手动确认奶蛙正常显示、右键能打开陪伴面板、双击可以大笑。然后检查并提交本次修改：

```powershell
git status
git add .
git commit -m "Update Naiwa companion"
git push
```

`.gitignore` 已排除构建产物、本机日志、临时测试文件和可从固定上游恢复的素材。无需将生成的 `dist/` 或 `work/` 上传到源码仓库。新增素材时，应补充来源、许可与校验记录。

## GitHub 自动构建

在仓库打开 **Actions**，选择 **Build Windows desktop pet**。工作流会在 Windows 上恢复素材、编译、运行自测并生成便携压缩包。

- 成功后下载 `Naiwa-Windows-x64` 构建产物，解开外层文件后可得到 `Naiwa-Windows-x64.zip`。
- `Naiwa-verification` 中保存自测报告和渲染预览；失败时可先检查这里的报告。
- 每次推送、Pull Request 或手动运行都会执行构建。工作流也可构建标签，但不会自动创建 Release 或对外发布。

如需发布版本，可自行在 Releases 中创建 `v1.3.0`，附上构建得到的 `Naiwa-Windows-x64.zip`。分享时一并保留压缩包中的 `LICENSE`、`THIRD_PARTY_NOTICES.md` 和使用说明，并注明上游作者及原项目地址。