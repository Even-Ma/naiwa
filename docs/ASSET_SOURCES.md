# 奶蛙素材来源清单

原始素材收集日期：2026-10-24；新增动作记录日期：2026-10-26。上游采用来源：[yugh6/Naiwa](https://github.com/yugh6/Naiwa)。作者：**yugh6／握石竹**（抖音号：45357131546）。

下表的五项上游原始素材均固定到提交 `72c763fe1349fb08a06f976e1709f4f5f80e09c7`，用于本项目的非商业衍生版本。另有三组以既有角色为身份参考新制的 AI 辅助二创动作，单独记录于下文。以下记录的是来源和本地完整性校验，不构成素材权利归属的独立保证。上游许可证原文保留在根目录 [LICENSE](../LICENSE)，其他权利说明见 [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md)。

## 已收集的素材

| 本地路径 | 固定版本来源 | 用途 | 字节数 |
| --- | --- | --- | ---: |
| `assets/素材.nwp` | [素材.nwp](https://github.com/yugh6/Naiwa/blob/72c763fe1349fb08a06f976e1709f4f5f80e09c7/%E7%B4%A0%E6%9D%90.nwp) | 上游原图、转眼、眨眼和大笑动作包。 | 25,060,301 |
| `assets/原版笑声.wav` | [原版笑声.wav](https://github.com/yugh6/Naiwa/blob/72c763fe1349fb08a06f976e1709f4f5f80e09c7/%E5%8E%9F%E7%89%88%E7%AC%91%E5%A3%B0.wav) | 上游从原视频提取的笑声。 | 622,482 |
| `assets/naiwa.ico` | [源码/naiwa.ico](https://github.com/yugh6/Naiwa/blob/72c763fe1349fb08a06f976e1709f4f5f80e09c7/%E6%BA%90%E7%A0%81/naiwa.ico) | 窗口、程序及托盘图标。 | 2,686 |
| `docs/upstream/形象预览.png` | [形象预览.png](https://github.com/yugh6/Naiwa/blob/72c763fe1349fb08a06f976e1709f4f5f80e09c7/%E5%BD%A2%E8%B1%A1%E9%A2%84%E8%A7%88.png) | 上游待机与转眼效果对照。 | 49,865 |
| `docs/upstream/新增互动预览.png` | [新增互动预览.png](https://github.com/yugh6/Naiwa/blob/72c763fe1349fb08a06f976e1709f4f5f80e09c7/%E6%96%B0%E5%A2%9E%E4%BA%92%E5%8A%A8%E9%A2%84%E8%A7%88.png) | 上游互动动作参考。 | 214,756 |

### SHA-256

以下校验值于收集日从本地文件直接计算：

| 本地路径 | SHA-256 |
| --- | --- |
| `assets/素材.nwp` | `a9c7795974d860da0c82b45237456698640ee8d4252b5db37503383d23fa46ae` |
| `assets/原版笑声.wav` | `e808f6f932abfb81e7104eb1c586fe6c828bda5db4ef62e52e0638a64a01fdfa` |
| `assets/naiwa.ico` | `a4f75cefe859f51f39cf84588b9aca701bd10da684ae3bbf88db6d3dfcff8f60` |
| `docs/upstream/形象预览.png` | `741428af6d361b14941bd30894be9576aa0b3c4038b9b79904a046b8c74e77b2` |
| `docs/upstream/新增互动预览.png` | `5981af9fef195194bc8e7961b456a97638fa3d43d2125a1ef83dab7bd58a63ae` |

## 上游素材处理说明

依据上游固定版本 README，待机使用原图，视线变化仅替换眼部，眨眼由原视频的七个眼部帧组成；大笑使用原视频连续帧并保留笑倒收尾。戳肚子、拖动及落地反馈采用局部变形、旋转和缩放。上述描述用于记录来源；本项目没有把原素材重新标注为原创图像，也没有从其他参考项目混入素材。

## 新制的 AI 辅助二创动作

这三张 PNG 由本项目使用 ImageGen，以既有奶蛙渲染图 `native-idle.png` 为角色身份参考生成，再机械缩放至统一规格。每张均为 **1152 × 768、RGBA、4 列 × 2 行、每格 288 × 384、8 个姿态**；本地 alpha 范围均为 **0–255**。它们不是上游新提供的原视频帧，也不是对网络源片的抠图或拼接。具体动作题材、生成过程及参考边界见 [MEME_ASSETS.md](MEME_ASSETS.md)，提示记录见 [meme-prompts.txt](meme-prompts.txt)。

| 本地路径 | 动作 | 字节数 | SHA-256 |
| --- | --- | ---: | --- |
| `assets/actions/sheets/holdlaugh.png` | 憋笑／绷不住，通用表情动作。 | 939,099 | `098ac891bd6cd5b4138701384af296fe92ee53d93ec6a482e818fb6ec55f9ab5` |
| `assets/actions/sheets/headbob.png` | 摇头晃脑题材的趣味改编。 | 889,532 | `cbe2e6aa896ad70b1e3fb6cd0bddadbd2b89b1e9a5fa3f5d2ddf06ca76118240` |
| `assets/actions/sheets/shufflestep.png` | 闪身步题材的趣味改编。 | 830,856 | `4ea58ec4299406eb0b154675a334c8611ef6762008e6f137d0806b34aa43821f` |

`shufflestep.png` 经定点图像修复，清除底行第一格手臂跨格产生的相邻姿态碎片；尺寸仍为 1152 × 768，其他两组动作素材不变，上表记录修复后的字节数与校验值。

仅作题材或文字参考的来源为：[Maple498 的鼓腮憋笑动作说明](https://github.com/Maple498/nai-wa-codex-pet#动作与触发方式)、恶俗硬汉于 2026-06-16 发布的 [《变异奶龙摇头晃脑舞》](https://www.bilibili.com/video/BV13DjP6kES4)、暖气杀手aaaa 于 2026-09-18 发布的 [《奶龙闪身步教学》](https://www.bilibili.com/video/BV1uSe16KEfi/)。憋笑属于通用表情方向，摇头晃脑是已有舞蹈题材，闪身步有当年 9 月的传播记录，不宣称三项均为最新爆梗。视频页直接抓取遇到 HTTP 412，仅核对了可检索元数据，没有逐帧核验源片，不声称精确复刻编舞。

本轮未下载或拼接参考视频，也没有使用 **HanYang、Maple498 或 andybi0403** 的图片、图集、视频帧或音频。新增图集保留 yugh6 角色基础及本项目非商业衍生许可，不额外授予原型、视频或其他第三方权利。

## 许可与传播要求

上游 `LICENCE` 为 **PolyForm Noncommercial License 1.0.0**。本项目作为非商业衍生保留该许可证与作者信息。原始角色、图片、视频、图标、声音及可能涉及的其他第三方权利，均未由本项目额外授予；上游仓库未逐项证明这些原素材的权利来源。

上游要求分享注明 yugh6／握石竹与原仓库，禁止冒充原创；短视频公开发布须联系作者取得许可，未经许可请勿用于自媒体公域传播。请同时保留 [上游 README](upstream/README.md) 和 [第三方通知](../THIRD_PARTY_NOTICES.md)。

## 调研参考，未采用

| 项目 | 所见形态或交互 | 本项目采用情况 |
| --- | --- | --- |
| [timerring/codex-pet-naiwa](https://github.com/timerring/codex-pet-naiwa) | Codex 动画图集、任务状态动作和方向注视。 | 未采用代码或素材。 |
| [Maple498/nai-wa-codex-pet](https://github.com/Maple498/nai-wa-codex-pet) | 像素风奶蛙与 Codex 状态对应动画；“鼓腮憋笑”的文字动作说明。 | 仅文字参考，未采用代码、图片或图集。 |
| [andybi0403/naiwa](https://github.com/andybi0403/naiwa) | 同人动作表与素材权利边界调研。 | 未采用代码、图片或图集。 |
| [LynnShaw/naiwa-pet](https://github.com/LynnShaw/naiwa-pet) | Tauri 桌宠、透明动画帧和原视频音频。 | 未采用代码或素材。 |
| [Diyeego/naiwa-pet](https://github.com/Diyeego/naiwa-pet) | 拖拽甩动、蓄力和多宠物切换。 | 未采用代码或素材。 |
| [woainiunai/milk-frog-desktoppet](https://github.com/woainiunai/milk-frog-desktoppet) | BongoCat 式输入反馈、视线跟随和右键菜单。 | 未采用代码或素材。 |

这些参考项目的许可不适用于本项目已采用的 yugh6 素材；各自的许可声明也不能替代原始角色或原视频的授权。
