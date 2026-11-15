# 第三方素材与授权

本项目的几何、材质、纹理、字体效果全部由代码生成；外部素材是两款中文字体
（**SIL Open Font License 1.1**）和三首授权音乐（**CC BY 4.0**）。
两者都允许免费商用与随项目再分发。

## 字体

| 文件 | 字体 | 版本/字重 | 授权 | 用途 |
| --- | --- | --- | --- | --- |
| `Assets/Huaxia/Fonts/LXGWWenKai-Regular.ttf` | 霞鹜文楷 LXGW WenKai | Regular | SIL OFL 1.1 | UI 正文、模态正文、按键提示 |
| `Assets/Huaxia/Fonts/LXGWWenKai-Medium.ttf` | 霞鹜文楷 LXGW WenKai | Medium | SIL OFL 1.1 | 书页手记、幡名、碑文、站名（世界空间楷体刻字） |
| `Assets/Huaxia/Fonts/SourceHanSerifCN-Bold.ttf` | 思源宋体 Source Han Serif CN | Bold | SIL OFL 1.1 | 匾额、书题、路引牌标题、章节标题、石木榜书 |

- 霞鹜文楷：<https://github.com/lxgw/LxgwWenKai>（基于 Fontworks Klee One，OFL）
- 思源宋体：<https://github.com/adobe-fonts/source-han-serif>（Adobe + Google，OFL）

字体系统的代码入口在 `Assets/Huaxia/Editor/HuaxiaBuilder.cs` 的 `LoadFonts()`。
`CreateTextPlate(..., title: true)` 用宋体，其余世界空间文字用楷体。

### 已移除 / 未采用

- `SourceHanSansCN-*.otf`（思源黑体）：已停用，黑体不属于本作的东方气质。
- 系统自带楷体 / 仿宋 / 宋体（Windows、macOS、Adobe 版）：**授权不允许随项目再分发**，一律不打包。
- 沐瑶软笔手写体：字体文件内没有授权声明，来源不明，不使用。

## 环境音乐

三首外部授权曲目。CC BY 4.0 允许免费商用、修改与再分发，但**必须保留作者署名**。
对外发布（WebGL / VR 包 / 展览）时，请在片尾或说明页保留对应的署名行。

### 序章 · 暮色古道

| 项目 | 内容 |
| --- | --- |
| 文件 | `Assets/Huaxia/Resources/Audio/amb_archive.wav` |
| 曲目 | *Meditation Impromptu 01* |
| 作者 | Kevin MacLeod（Incompetech） |
| 来源 | <https://incompetech.com/music/royalty-free/mp3-royaltyfree/Meditation%20Impromptu%2001.mp3> |
| 授权 | Creative Commons Attribution 4.0 International（CC BY 4.0） |
| 授权全文 | <https://creativecommons.org/licenses/by/4.0/> |
| 改动 | 转 22050 Hz 单声道；末 4 秒与开头交叉淡化，做成无缝循环 |

> "Meditation Impromptu 01" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0.

### 良渚 · 水乡古城

| 项目 | 内容 |
| --- | --- |
| 文件 | `Assets/Huaxia/Resources/Audio/amb_liangzhu_music.wav` |
| 曲目 | *Ripples* |
| 作者 | Kevin MacLeod（Incompetech） |
| 来源 | <https://incompetech.com/music/royalty-free/mp3-royaltyfree/Ripples.mp3> |
| 授权 | Creative Commons Attribution 4.0 International（CC BY 4.0） |
| 授权全文 | <https://creativecommons.org/licenses/by/4.0/> |
| 改动 | 转 22050 Hz 单声道；末 4 秒与开头交叉淡化，做成无缝循环 |

选曲理由：独奏古筝、57 BPM，描述原文就是「Flowing like water」；60 Hz 以下能量接近 0，
没有低频持续层，不会出现上次那种「诡异」的底噪。

> "Ripples" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0.

### 第二章 · 秦陵

| 项目 | 内容 |
| --- | --- |
| 文件 | `Assets/Huaxia/Resources/Audio/amb_qinling_music.wav` |
| 曲目 | *Eastern Thought* |
| 作者 | Kevin MacLeod（Incompetech） |
| 来源 | <https://incompetech.com/music/royalty-free/mp3-royaltyfree/Eastern%20Thought.mp3> |
| 授权 | Creative Commons Attribution 4.0 International（CC BY 4.0） |
| 授权全文 | <https://creativecommons.org/licenses/by/4.0/> |
| 改动 | 转 22050 Hz 单声道；末 4 秒与开头交叉淡化，做成无缝循环 |

> "Eastern Thought" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0.

### 泉州港与终章（程序合成，原创）

`amb_quanzhou.wav` / `amb_epilogue.wav`
由 `Tools/Audio/generate_ambient.py` 用 numpy 从零合成（加性合成 + 滤波噪声 +
FFT 卷积混响），不含任何采样或现有乐曲片段，不涉及第三方版权。修改乐句后重跑脚本即可复现：

```bash
python3 Tools/Audio/generate_ambient.py
```

| 文件 | 场景 | 时长 | 声音构成 |
| --- | --- | --- | --- |
| `amb_quanzhou.wav` | 泉州港 | 100 s | 海浪涌动、拨弦短句、轻和弦垫 |
| `amb_epilogue.wav` | 终章 · 合卷 | 110 s | 单古琴 + 长混响 |

22050 Hz 单声道，循环点严格闭合（频率量化为 1/时长 的整数倍，
混响用周期化卷积），因此可以无缝循环播放。

### 没有音效的地方

- **开门声** `door_creak.wav`：已删除，`AutoDoor.creakResource` 默认为空，门只动不响。
- **翻页声** `page_turn.wav`：已删除，`HandBook.pageTurnResource` 默认为空，翻页只动不响。

以后要换成真实录音，把 Resources 路径填回对应字段即可，代码不用改。
