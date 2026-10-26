# 第三方来源与许可说明

## 采用的上游项目

本项目是 [yugh6/Naiwa](https://github.com/yugh6/Naiwa) 的非商业衍生版本，基于以下固定提交收集源码及素材：

`72c763fe1349fb08a06f976e1709f4f5f80e09c7`

上游桌宠的制作与发布作者为 **yugh6**，抖音名称为 **握石竹**（抖音号：45357131546）。本项目保留其署名，不将上游代码、形象、动画或音频声称为本项目原创。

上游 `LICENCE` 采用 **PolyForm Noncommercial License 1.0.0**。其原文保留于本项目根目录 [LICENSE](LICENSE)，不改为 MIT 或其他许可证。相关许可覆盖的使用、修改及分发应满足其非商业目的和通知义务。分发本项目或其中任何上游部分时，应一并保留许可证及适用的上游通知。

- [固定版本的上游许可](https://github.com/yugh6/Naiwa/blob/72c763fe1349fb08a06f976e1709f4f5f80e09c7/LICENCE)
- [PolyForm Noncommercial 1.0.0 正文](https://polyformproject.org/licenses/noncommercial/1.0.0/)
- [固定版本的上游说明](https://github.com/yugh6/Naiwa/blob/72c763fe1349fb08a06f976e1709f4f5f80e09c7/README.md)

## 图像、动画、图标与笑声

已收集的第三方素材为：`assets/素材.nwp`、`assets/原版笑声.wav`、`assets/naiwa.ico`、`docs/upstream/形象预览.png` 和 `docs/upstream/新增互动预览.png`。各文件的固定来源、用途、大小与 SHA-256 见 [素材来源清单](docs/ASSET_SOURCES.md)。

这些素材由上游仓库提供。本项目没有取得或额外授予原角色、原始图片、原始视频、声音表演及其他第三方内容的独立版权许可；仓库许可证和本项目说明不应被理解为对这些权利的额外授权。上游 README 声明其使用原始图片与视频，但没有逐项提供原角色及原视频的权利链证明。

1.4.1 在渲染时对原始待机、转眼、眨眼及大笑 PNG 的脚部透明边缘进行局部补缝和去色边处理。原始 `素材.nwp` 文件保持不变；这是本项目的局部显示修整，没有使用其他作者的脚部贴图或替换原动画。

上游要求分享时注明作者及原项目地址，禁止搬运冒充原创；如需用于短视频公开发布，应联系作者取得许可，未经许可不要用于自媒体公域传播。此处保留该要求，完整上游说明另存于 `docs/upstream/README.md`。

## 新增的 AI 辅助二创动作

`assets/actions/sheets/holdlaugh.png`、`headbob.png` 和 `shufflestep.png` 为本项目使用 ImageGen 新制的动作图集，以现有奶蛙渲染图 `native-idle.png` 为角色身份参考，并机械缩放至统一规格。它们属于基于 yugh6 上游角色基础的 AI 辅助二创，不是网络原视频、上游原始动作帧或其他桌宠图集的复制、抠图和拼接。新增内容在本项目有权许可的范围内作为同一 **PolyForm Noncommercial 1.0.0 非商业衍生** 分发；不会因 AI 生成而转移原角色或其他第三方权利。

动作题材参考分别为 [Maple498 的鼓腮憋笑文字说明](https://github.com/Maple498/nai-wa-codex-pet#动作与触发方式)、恶俗硬汉的 [《变异奶龙摇头晃脑舞》](https://www.bilibili.com/video/BV13DjP6kES4) 和暖气杀手aaaa 的 [《奶龙闪身步教学》](https://www.bilibili.com/video/BV1uSe16KEfi/)。憋笑是通用表情方向，其余为已有网络舞蹈题材；不将三者统称为最新热梗，也不声称逐帧复刻原视频或精确编舞。视频源帖本轮只核对了可检索元数据，页面直接抓取遇到 HTTP 412，未完成逐帧观看核验。

没有下载或拼接上述源视频，没有使用其音乐、声音，亦未使用 **HanYang、Maple498 或 andybi0403** 的图片、精灵图或视频帧。引用这些来源不表示取得其素材授权；Maple498 对自身像素资产的 CC BY 4.0 不构成这三张 PNG 的素材许可。图集规格、SHA-256、制作说明与提示记录入口见 [MEME_ASSETS.md](docs/MEME_ASSETS.md) 和 [素材清单](docs/ASSET_SOURCES.md)。

## 仅作调研、未采用的参考

以下项目只用于了解同类桌宠的表现和交互。本项目未从这些仓库采用代码、精灵图、视频、音频或安装脚本，也不主张取得其素材授权。

| 项目 | 调研用途 |
| --- | --- |
| [timerring/codex-pet-naiwa](https://github.com/timerring/codex-pet-naiwa) | Codex 奶蛙动画和状态参考；其说明亦区分原创内容与第三方素材。 |
| [Maple498/nai-wa-codex-pet](https://github.com/Maple498/nai-wa-codex-pet) | 像素风奶蛙及状态动画调研；鼓腮憋笑仅作文字说明参考，未采用图像。 |
| [andybi0403/naiwa](https://github.com/andybi0403/naiwa) | 同人动作表与素材权利边界调研，未采用图像。 |
| [LynnShaw/naiwa-pet](https://github.com/LynnShaw/naiwa-pet) | 原帧大笑动画与轻量桌宠方案参考。 |
| [Diyeego/naiwa-pet](https://github.com/Diyeego/naiwa-pet) | 拖拽、甩动与右键交互参考。 |
| [woainiunai/milk-frog-desktoppet](https://github.com/woainiunai/milk-frog-desktoppet) | 输入互动、视线跟随与菜单参考。 |

原始资料收集日期：2026-10-24；新增动作参考与素材记录日期：2026-10-26。
