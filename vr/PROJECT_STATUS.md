# 华夏文明探源 · 河山册 — 项目状态与交接说明

更新日期：2026-11-05（第十五轮 · 引导 / 开门 / 听感 / 清晰度 / 手柄）
工程路径：`/Users/jiayuspace2.0/huaxia-vr`
Unity 版本：`2022.3.62f3c1`　·　Built-in Render Pipeline　·　目标设备 **PICO 4 企业版**

---

## 0. 一句话定位

**一部以中国世界文化遗产为叙事载体的沉浸式 VR 体验作品**——不是数字展厅，不是开放世界游戏，是「可以走进去的遗址短篇」。

体验者以「守册人」身份进入一本被水泡坏的《河山册》，在三处遗址中让三页纸从湿到干：

```
序章 暮色古道 → 良渚（水之卷）→ 秦陵（土之卷）→ 泉州（海之卷）→ 终章 合书
```

**本版只做三处遗址：良渚、秦陵、泉州。** 敦煌与北京中轴线已从策划案中删除，不再排期。

叙事设定见 [GAME_DESIGN.md](./GAME_DESIGN.md)（v6 · 12 分半故事线版），美术规范见 [ART_DIRECTION.md](./ART_DIRECTION.md)。

---

## 1. 当前已完成

### 1.1 工程与管线

- Unity 工程可命令行（batchmode）编译、生成场景、渲染预览图。
- XR 基础包已接入：`com.unity.xr.interaction.toolkit` 2.6.5、`com.unity.xr.openxr` 1.14.0、`com.unity.inputsystem` 1.7.0、`com.unity.xr.management` 4.4.0。
- XR 加载器配置（第十五轮补）：`Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset`，Standalone / Android 各挂 `OpenXRLoader`，自动加载与自动运行已开。菜单 `Huaxia/XR/启用 OpenXR 加载器（PICO 4）` 可重建。
- 色彩空间 Linear；Active Input Handling = Both。
- 中文字体（**只用两家，均 SIL OFL 1.1，可商用可再分发**，归属见 [ASSETS_LICENSES.md](./ASSETS_LICENSES.md)）：
  - `LXGWWenKai-Regular/Medium.ttf`（霞鹜文楷）→ UI 正文、提示、幡名、碑文、书页手记。楷书的温润，替代了原来的黑体。
  - `SourceHanSerifCN-Bold.ttf`（思源宋体 Bold）→ 匾额、书题、路引牌、章节标题、石木刻字。榜书的端正。
  - 代码里对应 `uiText / uiTitle / plate / display / bookPage` 五个字段，`CreateTextPlate(..., title: true)` 走宋体，其余走楷体。
- 原 `SourceHanSansCN-*.otf`（黑体，约 25 MB）已删除，场景与预制体中无任何引用。
- 环境音乐：五段，**三段外部授权 + 两段程序合成**。
  - 序章 `amb_archive.wav` = Kevin MacLeod《Meditation Impromptu 01》，CC BY 4.0，署名必须保留。
  - 良渚 `amb_liangzhu_music.wav` = Kevin MacLeod《Ripples》（独奏古筝，60 Hz 以下能量≈0），CC BY 4.0，署名必须保留。
  - 第二章 `amb_qinling_music.wav` = Kevin MacLeod《Eastern Thought》，CC BY 4.0，署名必须保留。
  - 泉州 `amb_quanzhou.wav`、终章 `amb_epilogue.wav` 仍由 `Tools/Audio/generate_ambient.py` 程序化合成（加性合成 + 滤波噪声 + 周期化 FFT 卷积混响），无采样、无第三方版权。
  - 全部 22050 Hz 单声道、循环点闭合。授权与逐曲说明见 [ASSETS_LICENSES.md](./ASSETS_LICENSES.md)。
- 音效：开门声 `door_creak.wav` 与翻页声 `page_turn.wav` 已按反馈删除（合成声不像真声）。`AutoDoor.creakResource` 与 `HandBook.pageTurnResource` 默认为空字符串，留空即静音，以后换真实录音把 Resources 路径填回即可，代码不用改。
- 音频运行时：`Scripts/Systems/AmbientDirector.cs`（`DontDestroyOnLoad` 常驻，双 AudioSource 交叉淡入淡出，无播放器场景自动补 2D `AudioListener`）+ `Scripts/Systems/SceneAmbient.cs`（每场景一个锚点，`Start()` 调 `AmbientDirector.Play`；**空名字会调 `AmbientDirector.Stop` 淡出上一场**，否则常驻播放器会把上一场的曲子带过来）。导入设置为 Streaming + 未压缩 PCM（Vorbis 会在循环点留填充）。
- 音频运行时已验证（编辑器播放模式脚本化跑通，非仅编辑器静态检查）：交叉淡入淡出期间新旧两源音量互补、无音量塌陷；各场景 `LoadScene` 后切到正确曲目与音量（序章 0.62 / 良渚 0.45 / 秦陵 0.38 / 泉州 0.48 / 终章 0.55）；常驻播放器跨场景存活；终章无 `AudioListener` 时备用监听器自动接管。`Stop()` 的渐变淡出曾是死代码（两处 `MoveTowards` 互相抵消），已修。

### 1.2 美术系统（东方风格化，已落地）

| 系统 | 文件 | 说明 |
| --- | --- | --- |
| 主体着色器 | `Assets/Huaxia/Shaders/StylizedLit.shader` | 半兰伯特三阶光 + 假 GI（`_BounceColor`，仅朝下法线）+ 三平面纹理 + 柔和边缘光 + `_ShadowTint` 冷影 |
| 水面着色器 | `Assets/Huaxia/Shaders/StylizedWater.shader` | 半透明 + Fresnel 天空反射 + 波纹 + 岸线渐隐，`ZWrite Off` |
| 文字着色器 | `Assets/Huaxia/Shaders/TextSingleSided.shader` | `Cull Back`，背面另放一片镜像副本 |
| 色板 | `Assets/Huaxia/Editor/HuaxiaPalette.cs` | 夯土、木、灰瓦、青绿、玉、朱砂、雾等统一 sRGB 常量 |
| 程序化纹理 | `Assets/Huaxia/Editor/StylizedTextureFactory.cs` | 18 种纹理（夯土、木纹、瓦垄、茅草、石、纸、水波…），全部代码生成，零外部贴图依赖 |
| 几何工厂 | `Assets/Huaxia/Editor/MeshFactory.cs` | `Terrain` / `GableRoof` / `CurvedRoof` / `MountainRidge` / `Canopy` |
| 场景构建器 | `Assets/Huaxia/Editor/HuaxiaBuilder.cs` | 材质工厂与四张参数表、几何原语、世界空间文字牌、`RenderPreviews` 21 机位出图 |

### 1.3 场景

| 场景 | 文件 | 内容 | 状态 |
| --- | --- | --- | --- |
| 序章 · 暮色古道 | `Archive.unity` | 开放山谷：石牌坊、暮色远山、枫与竹、灯笼暖光、石径、桥与池塘、重檐藏卷阁、东西配殿、甬道石栏 | ✅ 已建 |
| 第一章 · 良渚古城 | `Liangzhu.unity` | 湿地古城：水稻田 8 块、干栏长屋聚落 8 栋、木桥、水门、城墙与剖面、莫角山宫殿前庭、祭坛、码头、玉琮/玉璧地标、反山墓地、塘山长堤、远山 | ✅ 已建 |
| 第二章 · 秦陵 | `Qinling.unity` | 神道、外城阙门、K1 兵马俑坑（48×18m，深 1.75m）、棚木与枋木、夯土城垣与剖面（含楷书碑）、封土、陪葬坑、工棚、2850 棵草 + 320 棵松柏 | ✅ 已建 |
| 第三章 · 泉州 | `Quanzhou.unity` | 后渚港石铺码头、栈桥、干坞与敞甲板隔舱船体、块石场院、缆桩、九日山祈风石刻、三教碑、海雾、外海福船剪影 | ✅ 已建 |
| 终章 · 合书 | `Epilogue.unity` | 藏卷阁内景：月洞窗与窗外三层远山、隔扇门虚掩、门额楷书「藏卷閣」、四座卷柜、挂轴与三叠远山挂屏、香几铜炉、案上《河山册》与三片飞页、末页、拜垫、梁下灯笼与光尘 | ✅ 已建 |

> 序章原有的「圆形科技展厅」已彻底删除，全工程不再存在圆形展台 / 科技蓝光 / 章节节点柱结构。

### 1.3.1 泉州视觉返修（本轮）

- **铺装全部块石化解**：码头面、港区场院（`QzYardPave`）、干坞坞底三层地面都由「一整块大板」改成「深色底层 + 1.2~1.9 m 块石面层 + 0.05 分缝 + 逐块高差 + 错缝」。一整块平面无论怎么调色都会读成沙丘或水泥板，这是本工程反复验证的结论。
- **配色收窄**：`QzPave` 0.55 / `QzPaveWorn` 0.61 / `QzPaveDark` 0.49，三色随机阈值 0.86 / 0.88 —— 早期版本深浅差过大，铺装读成黑白棋盘。
- **干坞船体**：半剖（`half=true`）会让近侧船壳完全消失、只剩一排立板，读成看台长凳。改为**整壳 + 敞甲板 + 骨架探出舷口 0.5 m**：平视是一条正在修的福船，俯看能看到 12 道隔舱板。隔舱板由亮橙色（`woodPale`）改回船木色，只保留榫头浅色。
- **船帆**：`Cyl(name,parent,pos,radius,height)` 的参数顺序曾写反，2.4 m 半径 × 0.42 m 高把卷帆渲染成竖椭圆。外海剪影帆宽从 `len*0.17` 提到 `len*0.40`，不再是细竖条。
- **地表压暗**：`QuanzhouColor` 全域降明度（land 0.25/0.22、gravel 0.30/0.25…）；太阳改 33° 仰角、1.46 强度、近中性色 (1,0.97,0.92)，去掉偏黄。
- **窗光收敛**：`QzLantern` 自发光降到 (0.36,0.24,0.12)，白天不再像夜景城市。
- **祈风石刻（`_stele`）重构**：原来碑面被整块岩石包住（`Boulder_1` 5.1 m 压在 faceZ 1.28 上），机位后退后只剩三块灰石。改为「台基 + 上山石阶 + 岩壁开凿碑面」：`PrayerTerrace` 挡土台把坡地削平，香炉、供石、石灯改按 `deckTop` 相对摆放（此前按山顶高度 `gy` 摆放，离中心越远越悬空）；石阶 22 级由坡脚插到台沿；岩体改「一大两小」三盒复合，yaw 收到 ±11°。
- **碑面朝向叠层 bug**：`QzPrayerStele` 整组 `Euler(0,180,0)`，世界坐标 = 组原点 − local，所以「朝观察者」= local **+Z 增大**。碑面/文字必须 local z 递增叠层，写反就全部被碑面挡住。
- **竖排文字参数 bug**：`QzVerticalText(..., plateMeters, glyphMeters, ...)` 的第 6 参是**米制字高**，调用处误传像素值 128 → `fontSize ≈ 103225`，只剩一个巨大字的碎片。改回 0.52~1.00 m 后「祈風」两个楷书字才完整。
- **挡土墙面加粗石**：整块平台读成水泥基座，改为三皮错缝粗石（`TerraceStone_*`），颜色只在 `stone` / `stonePale` 之间跳，用 `stoneDark` 会变黑白格子。
- **三教碑去掉「条形码」**：中、右格原来只刻 7×2 条横短痕，读起来像贴图损坏。改为三格各一大字楷书「儒 / 釋 / 道」。
- **货棚屋顶**：`GableRoof` 是零厚度壳，`overhang 1.30` 让 15 m 宽的瓦面悬在 9.2 m 柱列外，读成飘着的一片板。缩到 `10.4×14.2, h 1.50, overhang 0.55`，并加檐口封板（`CargoFascia_*`）与联系梁（`CargoBeam_*`）。
- **场院铺装**：`YardSlab_*` 增加 ±1.1° 随机 yaw，配合逐块高差与三色随机，避免读成计算机贴砖。

### 1.3.2 终章 · 藏卷阁返修（本轮，共 11 轮出图）

v6 策划案把结局改成「回藏卷阁、三页归位、末页空白」，所以终章不再是复用序章，而是**独立房间** `Epilogue.unity`（`EpilogueScene.cs`）。反复出图踩出来的规律：

- **月洞窗必须是一块整墙上开洞，不是石块拼的环**。早期用一圈石块拼接，读成「齿轮」。正确做法：`MeshFactory.PlateWithHole(halfW, halfH, r, 64)` 做整面墙（把洞口自然留在中间），再用 `PlateRing(inner, outer, 96)` 压一圈窗套，两道石台做窗台。窗洞内径 `EpiWinR=0.98`、窗心高 `EpiWinY=2.18`。
- **窗外的天光必须是纯色自发光板**。`EmissiveMat` 内部走 `Mat()`，名字推不出纹理关键词就**回落到 stone 纹理**——天光板满屏噪点就是这么来的。凡纯色自发光面（天光 / 墨枝 / 灯纸 / 火苗）一律 `Calm(name, 0, 0, 0)` 把 detail / macro / bump 三个强度清零。
- **材质名带不出关键词就会拿到错的纹理**。`EpiLacquerRed` 里没有 lacquer 关键词 → stone → 柱子全是花斑。改用 `MatAs(..., "plaster")` + `Calm`。终章所有材质统一 `Epi` 前缀（材质按名全局缓存，名字必须全工程唯一）。
- **卷柜里的卷轴要竖插，不能横放**。横放（`Euler(0,0,90)`）从正面看到的只有一根长条，像面包棍；竖插（`Euler(90,0,0)`、5 列 × 2 行、口径朝前）正面读到的是一个一个圆头，再配合轴头小球才是卷柜。
- **卷轴配色**：`paper` / `paperOld` / `silkRed`（6 个里 1 个红）。深色 silk 圆头在柜内会读成「黑药丸」。
- **匾额字号决定会不会换行**：`createTextPlate` 的字高（米）= `fontSize × size.x / 500`。门额「藏卷閣」三个字从 168 降到 132 才一行放得下（约束：字数 × 字高 ≤ 牌宽 × 0.78）。
- **终章机位**：案在 `z=2.10`（前脸 ≈1.55），要在画面里同时容下 `y=3.47` 的门额匾和案面，相机必须退到 `z≈−0.1~0.3`、`fov 58~60`、视线轻微上抬。`z=0.92` 会顶到案。
- **挂屏的水墨要分层**：三道等长短横线会读成「收据」。改为三叠远山（`Hill_0..2`，宽度 0.19 / 0.275 / 0.36，只占下半幅）+ 朱印。
- **香几/铜炉的「家具感」全靠细节**：束腰、牙板、外撇腿（±3.2°）+ 腿足，缺一样就读成现代边桌；铜炉要扁圆盖 + 三细足 + 细立耳，全圆球会读成绿球。
- **门扇只留一线缝**（9°）。大角度开门会让门外天光板形成一条刺眼亮带。
- **墨枝宁细勿粗**：0.022 / 0.014 / 0.013 / 0.010 四段，不要交叉杂枝；梅花用同墨色小点，不要白色花点。

### 1.3.3 秦陵阙门返修（本轮）

- **阙身保留一整块收分台体，夯层靠细缝不靠分层几何**。试过两种错法：① 一整个四棱台 + 三道浅色横线 → 大木桶；② 9 层夯土块逐层内收、明暗交替 → 一摞木箱（层级太粗，每层 0.87 m）。最终：`QnFrustum` 一体 + **12 道 0.03 m 深色细缝**（`QueLayer_*`，沿收分面等比缩），夯层才像夯出来的，轮廓也不会碎。
- **收分收窄**：`baseW 7.0 → 6.4`，顶部收 1.92（原 2.48）。收分过大 + 侧光，轮廓会读成木盆。
- **阙顶必须是四坡**：原来一块 1.70 m 出挑的平瓦板 = 一块黑板。现用 `QnHipRoof(halfW, halfD, h, ridgeHalf, lift)`（本文件内的庑殿网格：四坡汇短正脊、四角起翘）出挑 0.88 m，配夯土压顶、封檐板、正脊。
- **柏树冠改成连续毛边柱体**：三层锥（每层 `h*0.46` 高、半径收 60%）是「圣诞树」；改成五层锥仍是「摞起来的塔」。最终 `ConiferCrown(radius, height, seed)`：6 环 × 10 段，半径按 `(1-t)^0.86` 收、逐顶点抖 0.80~1.24 倍，轮廓连续、边缘毛糙。**同一个轮廓问题，加层数不管用，要换拓扑。**

### 1.3.4 序章三岔口引导改造（本轮）

起因：站在山门外看不见「往哪走」——三根望柱立在 `dir*19.5`、体量小、没有光柱，被古亭和树挡掉两根。

- **望柱改成幡柱**（`ArchiveScene.ChapterNode` 重写）：石座 1.36×0.48 + 木竿 `r=0.095 h=6.22` + 石护脚 + 6.0 m 处横臂铜头；幡面 `0.94×2.86` 是**发光纸**（三卷配色：水之卷 InkTeal / 土之卷 Terracotta / 海之卷 Azurite 混 PaperWarm），竖写三字卷名 `fontSize 400`；竿头铜球 + `Pulse`；柱脚小石板刻站名。点光 range 11 / intensity 2.4。
- **位置改显式坐标**，yaw 统一朝向南山道：`(-8.20,0,9.00) / (0,0,10.60) / (8.20,0,9.00)`。原来的 `dir*19.5` 太远，且西北/东北两根落在视野角外。
- **新增路引牌**（`BuildRouteSign`）：石座 + 铜压条 + **发光纸面灯箱**（`EmissiveMat("SignPaper", PaperWarm, AmberPale*0.45)`），三行「左 卷一·水之卷 良渚 / 中 卷二·土之卷 秦陵 / 右 卷三·海之卷 泉州」，朱砂标题「三卷分路」，按 E 还有一段走近怎么走的说明。初版位置 `(3.65,0,-17.00)` yaw −20° 在路径东边；**后来按反馈挪到 `(-4.50,0,-20.60)` yaw −20°，即直线进谷的左侧**（见 1.3.8）。
- **古亭西移**到 `(-11.00, 0, 1.20)`：让开「牌坊 → 甬道 → 藏卷阁」的轴线，卷二幡柱才立得住。
- **北向甬道**从 z=12.2 起铺 14 块到 33.3，`PathBase_N` 中心 23.2 / 长 23.2，牙石 x=±2.05、中心 23.4 / 长 24.0。
- **三条支路各加 4 盏引路石灯**（`BuildStoneLamp(..., withLight:false)` 只做几何不点灯，避免灯数爆掉）；甬道那条的横向偏移改成 3.05 m 让开牙石。
- **树挪位**：`Maple_Hero (-6.4,-1.2) → (-15.0,-8.0)`、`Maple_D (-11.2,10.2) → (-15.6,12.4)`——原来这两棵正压在水之卷幡柱的视线上，右侧幡怎么摆都看不见。
- **HUD/文案同步**：objectives 增「◇ 西侧「问道亭」的残卷可以翻一翻」，「望柱」→「幡柱」。
- **`GameState.Chapters` 修 bug**：原写 `"qin"`，而 `Chapter2Scene.chapterId = "qinling"` → 卷二页永不完成、`AllChaptersComplete` 永远 false。

> **同时修掉的渲染坑**：`ProjectSettings.asset` 里给 Standalone 关掉 `m_DynamicBatching`。Unity 2022 batchmode 在 `RenderForwardShadowMaps → RenderDynamicBatch → DrawSharedGeometryJobs` 会 segfault，几何/点光一多必崩。代价：VR 端不再动态合批，进真机前要么重开、要么改静态合批/图集。

> **另一个必须记住的坑**：`RenderPreviews` **不能带 `-nographics`**。带了会 `Forcing GfxDevice: Null`，命令照样 `rc=0`，但 32 张全是纯灰 `(205,205,205)`。只有编译检查脚本（`-nographics -quit` + 仅 `Assets.Refresh`）可以带。

### 1.3.5 序章四项返修（本轮：走不到遗址 / 空屋子 / 脚下那本书 / 文案）

用户原始四条反馈：① 进不了后面三个场景；② 有几个房子能进去是空的、左边房子里还有棵树；③ 屏幕下方一直有本书；④ 文案要「人话」。

**① 进不了遗址 —— 根因是「路尽头没有任何触发体」。**
- 新增 `Assets/Huaxia/Scripts/Interaction/ScenePortal.cs`：`sceneToLoad / spawnId / line / lockedLine / requireAllChapters / radius`。进圈只显示提示（**sticky**，不会一转头就消失），按 E 才淡入下一场。
- 新增 `Assets/Huaxia/Scripts/Core/SpawnPoint.cs`：`id` + `static Find(id)` + `Place(player)`；`Place()` 里**先禁用 `CharacterController` 再设位置**，否则 CC 的内部位置缓存会把玩家拉回原处（本轮最耗时的一坑）。
- `RouteEnd()` 三道界门：门前石坪 + 两根石柱 + 木楣 + 匾额（卷一/卷二/卷三，匾朝来路）+ 界石（「路，到这儿为止」）+ 门后山石/松/竹收住视线。半径 21.5 / 21.5 / 21.0，秦陵那道横向 −6.6 让开甬道。
- `ArchiveScene` 5 个落点：`start / after_epilogue / back_liangzhu / back_qinling / back_quanzhou`；三章各有 `from_hub`。
- 回程不再被扔回山门外：`TaskManager` 返回时 `LoadScene(returnScene, "back_" + chapterId)`，落点在门下 3.7 m（感应圈 3.15 m，差 0.5 m 不会被立刻回送）。
- `UIManager` 增 `ShowPrompt(text, sticky)` / `ClearPrompt()` / `LoadScene(scene, spawnId)`；`GameState.pendingSpawn` 跨场景传递；`DesktopPlayerController.Start()` 调 `ApplyPendingSpawn()`。
- 藏卷阁门前接终章（`HallGate`，z 35.2，r 2.6）：`requireAllChapters`，没齐只弹「还差 N 卷」。

**② 空屋子 —— 墙和门全是无碰撞的装饰片。**
- `ArchiveArchitecture.BuildGreatHall / BuildSideHall` 的墙体与门窗补碰撞，两间配殿**门封死**。
- 门楣加木牌写清作用：`藏卷阁` / `抄书处 · 纸还没裁完` / `值房 · 守山人住的地方`。
- 穿进西配殿的 `Pine_E/F` 移到院外。

**③ 脚下那本书 —— `BookHolder` 一直挂在相机下。** 改成开场不自动抬，按 `Tab` 才取。

**④ 文案全部改成口语**：序章 HUD 目标、三条幡柱弹窗、路引牌、界门提示、藏卷阁锁门提示、三章 intro 与目标，都去掉说明腔。

**顺带修的视觉问题**：主楼退远 10 m 后前庭空出一片草地 → 加 `Court_N` 前庭夯土（**首版 y −0.10 / 高 0.34 被地形 ±0.06 噪声吞掉**，现顶面抬到 0.17，甬道石板 0.10、牙石 0.15 同步抬高）；秦陵门前接 `Spur_Qinling` + 石板引路；删掉宽度超过屋顶的 `GateRidge`；门口松竹让开中线。

**验证**：临时播放模式脚本 `GateTest`（已删）实测 4 道 portal、5 个落点、书位姿 `(0,−0.72,0.24)`、门提示文案、锁门提示、`from_hub` 落点误差 0.05 m、`back_liangzhu` 误差 0.00 且距门 3.7 m，全部通过。

### 1.3.6 第十四轮：键位拆分、界石、翻页声、过场字幕、三遗址口语化

**① 键位拆分（原 P0-1：`E` 三重身份）。**
- `E` 现在只做世界交互与过界。弹窗确认改为 `空格 / Enter / 鼠标左键`。
- 册子翻页改为 `→ / 滚轮下`、`← / 滚轮上`；`Q`、`E` 不再翻页。
- `BookHolder.Update` 在 `UIManager.BlocksPlayer`（弹窗 / 过场）期间不吃任何输入，空格不会顺手翻掉一页。
- 所有弹窗脚注的 `[E] 关闭 / 进入 / 合上 / 知道了 / 开始探索` 同步改为 `[空格] …`；HUD 增加「滚轮翻页 / 空格 确认」。
- **顺带**：`CreatePlayer` 里把 `greetOnStart` 置 false。此前场景里序列化的是 1，开场书会自己抬 2.6 秒——和「按 Tab 才取」的设定矛盾，也是开局按 E 误触的来源之一。

**② 遗址界石（原 P1-3：进去看一眼出不来）。**
- `HuaxiaBuilder.CreateReturnStone()`：石座 + 立石 + 木顶 + 浅色刻字（「界石 / 地名 / 顺来路，回山谷」）。
- 三处遗址入口各一块，挂 `ScenePortal`：→ `Archive`，落点 `back_<chapter>`，半径 2.8 m。
- 站位：良渚在夯土大道西缘 `(-1.5, -52.5)`；秦陵在阙门内侧 `(4.0, -76.6)`；泉州在码头面 `(9.2, -54.8)`。三处离各自的 `from_hub` 都 ≥ 3.8 m，落地不会立刻被回送。
- 良渚第一版放在 `x=-3.6`，整块石头立进了稻田水面里；挪到路缘后才落在夯土上（**站位要按路面宽度算，不能只看预览**）。

**③ 翻页声（原 P1-5）。**
- 新脚本 `Tools/Audio/generate_page_turn.py`：两段时变带通噪声（掀纸 / 滑纸）+ 四下极短纤维脆响，22050 Hz 单声道 16 bit，峰值 −12 dBFS。
- `HandBook` 增加 `pageTurnResource = "Audio/page_turn"`、`pageTurnVolume`，`Awake` 建 2D `AudioSource`；`TurnNext/TurnPrev` 起手播放，每次音高 `0.94~1.06` 随机。

**④ 过场字幕（原 P1-4 的一半）。**
- `UIManager.LoadScene(scene, spawn, label)` 新增 `label`：淡黑 0.55 s 后，在黑场上留 0.85 s 地名再加载。
- `ScenePortal.destinationLabel`：三道界门「卷一 · 水之卷　良渚」等，藏卷阁门「终章 · 河山册」，界石「序章 · 暮色古道」。
- 门缝透光 / 雾仍未做，切场是「黑 + 一行字」，比纯黑软，但不是光效。

**⑤ 三遗址考据文案口语化（原 P1-2）。**
- 良渚 7 条、秦陵 6 条、泉州 6 条线索正文全部重写：第二人称、短句、先说看见什么再说为什么，去掉「体现了 / 构成了 / 是……的重要材料」这类论文腔。
- 史实、年代、地名、争议说明、示意声明一律保留；三层标记（● / ○ / △）不变。
- 三章 `introBody` 同步微调（秦陵「一头发不出名字的人」→「一头是没有名字的修陵人」等）。
- **未改**：「文明档案」弹窗正文保持书面体——它在故事里就是一份档案，不是导游词。

**验证**：
- 新增常驻自检 `Assets/Huaxia/Editor/SceneSelfCheck.cs`（菜单 `Huaxia/自检：场景不变量`，可 batchmode 跑）。
  检查 4 道界门、三处界石及其落点、各场景 `from_hub / back_* / start / after_epilogue`、线索点数量（7/6/6）、档案脚注是否已改成空格、HUD 是否写明 Tab、终章合书点、翻页声能否加载。
  **本轮结果：pass 37 / fail 0。**
- `BuildAll` 重建五个场景无 C# 错误；`RenderPreviews` 出图 48 张（含三张新的 `*_returnstone.png`），已目视检查界石站位与刻字可读性。

### 1.3.7 第十五轮：引导、开门、听感、清晰度、手柄（七项反馈）

**① 背景音乐重做（反馈 1）。**
- `Tools/Audio/generate_ambient.py` 新增 `bird()`（三音一组、随机间隔的鸟鸣）与 `pad_layer()`（五声音阶三音铺底），重写五段环境床。
- 秦陵原来那版有心跳鼓和次低频 drone——这是「诡异」的直接来源，已全部删掉；改成风声 + 远处鸟 + 稀疏的木鱼式点音。
- 良渚：水田环境声 + 竹笛短句 + 铺底；序章：山谷风 + 鸟 + 箫；泉州保留海浪，叠了更轻的弦垫；终章最静。
- 五段全部重新合成：`amb_archive / amb_liangzhu / amb_qinling / amb_quanzhou` 各 100 s、`amb_epilogue` 110 s，22,050 Hz 单声道，循环点闭合。
- **没有试听**，只保证生成与资源加载成功；音量/音色要戴耳机过一遍。

**② 目标引导（反馈 2）。**
- 新脚本 `Scripts/UI/ObjectiveGuide.cs` 挂在 HUD 上，每 0.4 s 找「最近一处还没看的线索」，写一行：`下一处　<标题>　<方向> <距离> 米`。
- 方向用 `SignedAngle` 分八档（正前方 / 右前方 / 右侧 / 右后方 / 正后方…），距离按米取整。
- 优先级：没去过的遗址界门 `score -= 1000`（序章里先带你出山谷）；遗址内只在主线索里挑，主线索看完就给界石。
- 全部看完时写「本程的线索都看过了 —— 回界石按 E 回山谷」。
- **终章门槛从「三卷读完」改成「三处都去过」**：`GameState.VisitChapter` 在 `TaskManager.Start()` 记录；`AllChaptersVisited` 取代 `AllChaptersComplete`；没看全线索也能进书房。藏卷阁门的话术同步改成「还差 N 处没去 / 三处遗址都去过了才开门」。

**③ 开场与进场说明（反馈 3）。**
- 新脚本 `Scripts/UI/SceneIntro.cs`：延迟若干秒自动弹一次模态，`onlyIfNoVisits` 控制只在第一次进入时弹。
- 序章加入 `SceneIntro`：正文说明「这半卷书为什么是这三页」——良渚写水、秦陵写土、泉州写海，三样东西各写一页；并说明左上是引导、三处去过就开书房、不必逐条看完。
- 三处遗址的进场说明仍由 `TaskManager.introBody` 承担（0.8 s 后弹出，已有「这一处看什么」的完整段落），没有重复叠加第二个弹窗。
- 序章岔路木牌的正文重写：解释水/土/海的分法，并写明「三处都去过，藏卷阁才开」。

**④ 手柄 / 头显（反馈 4）。**
- 新脚本 `Scripts/Player/XRInputBridge.cs`：只在 `XRSettings.isDeviceActive` 时接管，桌面端行为完全不变。
- 映射：左摇杆走、右摇杆整步转身 30°、右手扳机互动/确认/翻页、左手 grip 取/收册子、A 确认、B 取消。
- 头显姿态读 `InputTracking`（不依赖 Input Actions 资产）；右手挂一条 `LineRenderer` 瞄准线；HUD 在 VR 里改成挂在头前 2.2 m 的世界空间画布。
- `DesktopPlayerController` 增加 `xrActive / externalMove / xrRay` 三个外部入口，以及 `InteractFocused()`、`SnapTurn()`。
- 新增 `Assets/Huaxia/Editor/XRSetup.cs`（菜单 `Huaxia/XR/启用 OpenXR 加载器（PICO 4）`）：创建 `XRGeneralSettingsPerBuildTarget`，给 Standalone 和 Android 各挂 `OpenXRLoader`。**此前工程里没有这个资产，OpenXR 实际上从未被初始化**——这是 VR 不工作的根因之一。
- **未上真机**：本机只装了 WebGL 构建模块（无 Android），且 OpenXR 的 Standalone 支持仅 Windows64，Mac 上无法运行 XR。

**⑤ 秦陵城门自动开（反馈 5）。**
- 新脚本 `Scripts/Interaction/AutoDoor.cs`：按玩家距离用 `MoveTowards` 把两扇门转到 `openAngle = 95°`，进门 8.5 m 触发、退到 11.5 m 外回关，开门瞬间播一次合成的吱呀声。
- 删掉了 `QinGate` 里那块挡住门洞的深色 `Passage` 板。
- 「进去看什么」靠两件事解决：引导行（②）与信息点的视觉强化（⑥）。

**⑥ 信息点视觉强化（反馈 5 的后半）。**
- `CreateInfoPoint` 加高：地面一圈 `GroundRing`（半径 1.05）、3.1 m 高的细光柱、光柱顶 0.17 m 的球（`Pulse` 呼吸），灯的距离/强度提高到 7 m / 1.9。
- 标题牌抬到 2.30 m、层级牌 1.92 m，远处先看到的是柱顶那一颗亮球和一条竖线，走到跟前才读字。

**⑦ 清晰度（反馈 6）。**
- 所有世界空间文字牌的内部画布单位从 500 提到 1000、字号 ×2：同样的物理尺寸，字形栅格分辨率翻倍（`CreateTextPlate`、`CreateWorldLabel`）。
- `RenderPreviews` 改为 2× 超采样（3200×1800 渲染）+ 4× MSAA，再盒式降采样到 1600×900；原尺寸大图另存到 `Previews/hires/`（已加入 `.gitignore`）。
- 这是「预览图」层的修法。**实际运行时牌子上的字仍然受贴图/材质分辨率限制，不是靠这条修好的**；真正的运行时清晰度还取决于头显单眼分辨率与 UI 缩放。

**⑧ R 键（反馈 7）。**
- `R` = 回本场景落点；`Shift+R` = 跨场景回序章山门外（`GameState.pendingSpawn = "start"`）。仅编辑器内生效。

### 1.4 运行逻辑（桌面第一人称 + VR 代码层已接，未上真机）

- WASD 移动、鼠标视角、Shift 奔跑、E 互动 / 过界、Tab 取册、←/→ 或滚轮翻页、V 整体视角、Esc 释放鼠标、空格 / Enter / 左键确认弹窗。
- **完整键位、逐节点模态文案、已知断裂与优化清单见 [`INTERACTION_SPEC.md`](./INTERACTION_SPEC.md)**。
- 键位分裂已修（第十四轮）：`E` 只做世界交互，弹窗确认是空格，翻页是方向键 / 滚轮。
- 中心射线交互：聚焦信息点与调查点，显示提示。
- **主线索 / 次线索分层（v6 20 分钟节奏）**：每章 6~7 个互动点里只有 3 条是主线索，其余标 `optional = true`。
  - 主线索：只数这三条，集齐即「本卷读完」，解锁「文明档案」，触发书自动抬起变干。
  - 次线索：看也行、不看也不挡进度；每看一条，书页边上多一行小字（`BookLeaf.foot`），`BookHolder.Peek()` 顺手把书抬起来 2.8 秒让人看见写了什么。
  - 良渚 主线 `water/mojiao/jade`，次 `field/wall/altar/dock`；秦陵 主线 `wall/pit/mound`，次 `bronze/workshop/weight`；泉州 主线 `quay/dock/stele`，次 `rope/cargo/prayer`。
- 跨场景进度：`PlayerPrefs`；场景淡入淡出。
- 编辑器菜单：`Huaxia/构建全部场景`、`Huaxia/渲染场景预览图`、`Huaxia/清除探索进度`。

> **注意**：v6 策划案要求把 UI 收敛为「《河山册》是唯一 UI」、交互改为近手抓取 + 装置 + 物体旁注。现有线索/资料卡系统属于 v3 遗产，将在 VR 垂直切片（M2）时替换，暂不改。

### 1.5 预览图（`Previews/`，48 张）

- 序章 11 张：`archive`（牌坊望藏卷阁）、`archive_hub`、`archive_pavilion`、`archive_pond`、`archive_hall`、`archive_wide`、`archive_sidehall`、`archive_hallgate`（藏卷阁门前的终章门）、`archive_end_liangzhu` / `archive_end_qinling` / `archive_end_quanzhou`（三条路尽头的界门）
- 良渚 8 张：`liangzhu`（桥头望城）、`liangzhu_settlement`、`liangzhu_hamlet`、`liangzhu_mojiao`、`liangzhu_altar`、`liangzhu_wall`、`liangzhu_dock`、`liangzhu_city`（航拍全景）
- 秦陵 7 张：`qinling`、`qinling_pit`、`qinling_mound`、`qinling_wall`、`qinling_bronze`、`qinling_workshop`、`qinling_wide`
- 泉州 7 张：`quanzhou`、`quanzhou_pier`、`quanzhou_dock`、`quanzhou_cargo`、`quanzhou_wall`、`quanzhou_stele`、`quanzhou_wide`
- 终章 4 张：`epilogue_hall`（厅内全景）、`epilogue`（案前看月洞窗）、`epilogue_book`（俯看《河山册》）、`epilogue_door`（回望虚掩的门）

另附书本体预览 5 张：`book_wet`（卷首湿透）、`book_dry`（书名页 + 卷一正文）、`book_note`（页边注）、`book_last`（末页 + 翻过的三叶）、`book_lab`（四态并排校验）。

再附「手中书」三态 3 张（`Huaxia/仅渲染：手中河山册（进度三态）`）：`book_grip_entry`（刚拿到，只有「卷一 良渚」）、`book_grip_done`（本卷读完，正文「文明始于共同生活 / 水要分。」浮出）、`book_grip_note`（看过次线索，页面左下多两列小字）。机位必须从 **−Z 侧**俯看，纸面才朝上。

### 1.6 《河山册》本体（`BookFactory.cs` + `BookPage.shader`）

书是**唯一 UI**，因此它的视觉和场景同等重要。生成方式：`BookFactory` 烘 13+1 张 PNG（纤维、水渍、潮斑、墨层、朱印）→ 建网格 → 建材质 → 存 `Resources/HandBook.prefab`；`BookLab.unity` 是四态校验场景，只用于出图。

- **纸色**（sRGB 入参，脚本里 `Lin()` 转线性）：正面 `_Color (0.900,0.880,0.818)` / `_WetColor (0.740,0.725,0.668)`，叶背 `_Color (0.870,0.850,0.782)`。
  早期用 0.925 导致终章页面在直射光下裁剪成一片纯白，反向下调过两轮后定为现值：既保留暖米纸感，又不溢出。
- **`LightingHuaxiaBook` 是自定义光照模型**：`diffuse = Albedo * light.color * ramp`，**不含 N·L、不含阴影衰减**。所以页面亮度只对「方向光强度」和「反照率」敏感，调 `_AmbientBoost` / `RenderSettings.ambientIntensity` 几乎没有可见变化（实测调 1.8→0.9 只掉 3 个色阶）。想压暗纸只能改 `_Color` 或调方向光。
- **叶背双面顶点 + 法线（反直觉，务必记住）**：`PageBackQuadMesh()` 是 8 顶点双面片，`triangles {0,2,1, 0,3,2, 4,5,6, 4,6,7}`。叶被 `Euler(0,0,180)` 翻转后**实际可见的是 4–7 组**，其烘焙朝上的法线在世界空间变成朝下，导致翻过去那几叶偏暗约 40%。修法：**8 个顶点法线全部设 `Vector3.down`**（实测 178→224 色阶）。
- **叶背 `receiveShadows = false`**：叶背与正面只隔 0.6 mm，开着会被自己正面那片糊住（关掉后 155→178）。
- **叶背墨层必须为 0**：所有叶共用同一张墨贴图，叶背会透出镜像书名。`Book_Page_Verso` 走独立分支（湿 0.26、污 0.50、无墨）。
- **重建链路**：改 `BookFactory` 里的材质/网格后**必须跑 `BuildBookMenu`**（重建预制体），只跑 `Render*Previews` 会渲染出旧结果；`book_*.png` 还要额外跑 `BuildBookLabMenu`（BookLab 场景里另复制了一份材质）。
- **页边注排版**：`BakeLayer("Book_Foot_" + idx)` 里两列注在 1024×1440 的层上竖排。行高约 1.18 倍字号，34pt × 12 字 ≈ 480px，从 `top 850` 起笔留 110px 余量；**一开始用 40pt 从 978 起笔，12 字直接冲出纸底被截断**——加注时先数字符数。
- **终章真书**：`EpilogueScene` 的 `PlaceRealBook()` 实例化预制体，`DryLeaf()` 给末页 `_Wetness 0.42`（全干的纸在那间屋里会过曝），末态左右页采样约 (224,222,168) / (253,228,169)，亮度比 0.95、同色相。

### 1.7 史实分层（`GAME_DESIGN.md` §9 已落地）

规范要求「每块考据牌都要让玩家知道这条是实证还是推测」，因此 19 个考据点全部带层级：

- 枚举 `Interactable.SourceTier { Confirmed, Inferred, Unverified }`，配 `tier` / `tierNote` 两个字段。
- 视觉三层：`● 已证实`（不额外挂标，靠正文首行）／`○ 学界推断`／`△ 待考证 · 此为艺术处理`。
- 世界空间第二行小字：宽 **1.10**（≈0.24 m 高）、`y = 1.70`、标题上移到 `y = 2.02`。颜色 Inferred `(0.92,0.90,0.84)`、Unverified `(0.96,0.88,0.78)`，都按贴纸白给——**第一版 0.92 宽 + 土黄 + y=1.76 在 5 米外糊进沙色背景完全看不见**，务必保持这个尺寸与明度。
- 层级行同时拼到模态正文首行（`TierLine() + "\n\n" + body`），后面跟一句 `tierNote` 说明为什么算推测。**`tierNote` 对 Confirmed 也生效**（如良渚水门「具体形制为示意」、秦陵封土「原高有争议」），不要以为只有推断层级才带注。
- 分配结果（依据逐条抄自 §9.2~§9.4）：
  - 良渚 5 Confirmed（水稻田 / 城墙与水门 / 城墙剖面 / 瑶山祭坛 / 莫角山台地）+ 2 Inferred（钟家港码头「依据河道木构遗迹复原，上部结构无存」、反山墓地与玉器「神人兽面纹含义有多种解释，不给结论」）。
  - 秦陵 5 Confirmed（夯土城垣剖面 / 陪葬坑 / 铜车马坑 / 封土 / 秦权与秦量）+ 1 Inferred（修陵人工棚「工棚形制为示意复原，戳印文字仅作形式提示」）。
  - 泉州 2 Confirmed（水密隔舱 / 祈风石刻）+ 1 Inferred（货场「货物种类为史实，此处组合为示意」）+ 3 Unverified（石砌码头「后渚港港区为史实，形制按同期港口类比设计」、缆桩「港口活动为史实，缆桩与磨痕为艺术处理」、三教碑「多宗教并存为史实，同一岩壁并置为艺术处理」）。
- **验证盲区**：世界空间文字牌能被 `RenderPreviews` 渲进图里（已裁切放大确认「○ 学界推断」「△ 艺术处理」可读），但模态弹窗是 `ScreenSpaceOverlay`，**渲染预览图拍不到**，`TierLine()` 的实际排版只能靠代码审查 + 真机 Play。

### 1.3.8 第十六轮：换掉所有「诡异」声音 + 序章六项返修

**① 音频全面换血（用户连续三轮反馈「诡异 / 吓人」）。**
- 程序合成的音乐整段弃用，序章与第二章改用 Kevin MacLeod 的真实曲目（CC BY 4.0，必须署名）：
  - 序章 `amb_archive.wav` = *Meditation Impromptu 01*（独奏钢琴，低频 <120 Hz 只占 2.8%，无持续低音层）。
  - 第二章 `amb_qinling_music.wav` = *Eastern Thought*。旧的合成 `amb_qinling.wav` 已删除。
  - 处理方式：`afconvert` 转 22050 Hz 单声道 → numpy 等功率交叉淡化末 4 秒与开头 → 无缝循环。
- 删除 `amb_liangzhu.wav`（旧的合成版）、`door_creak.wav`（开门只动不响）、`page_turn.wav`（翻页只动不响）。
  `AutoDoor.creakResource` / `HandBook.pageTurnResource` 默认值改为空字符串；以后换真实录音填回路径即可。
- 良渚随后补上授权曲目 `amb_liangzhu_music.wav` = *Ripples*（独奏古筝，57 BPM，描述原文「Flowing like water」；
  60 Hz 以下能量≈0、120 Hz 以下 0.2%，没有低频持续层），场景音量 0.45。
- `SceneAmbient` 补上「空名字 = 停止播放」：原来空名字会让 `AmbientDirector.Play` 直接 return，上一场的曲子被常驻播放器带着继续播。
- `generate_ambient.py` 的 `TRACKS` 只留 `amb_quanzhou` / `amb_epilogue`，重跑脚本不会覆盖三首授权曲目。
- 自检里删掉两条「翻页声 / 开门声能否加载」的检查（资源已不存在），pass 从 58 降到 56。

**② 序章六项。**
- 「三卷分路」牌从路径东侧 `(3.65,0,-17.00)` 挪到直线进谷的左侧 `(-4.50,0,-20.60)`，yaw −20°。
- 路引牌正文按用户给的稿子整段替换（三卷各一段、末尾「请自由探索」），不再是一句「怎么走」。
- 三岔口圆坛上方加世界空间标签「请行走到此处」，`Outline` 深色描边，暗背景下也读得清。
- 序章音乐去掉山风层；后整首换成上面那首钢琴曲。
- 「复原说明」碑转向 `yaw 254°`，碑面朝路；`BuildStele` 正文板从 0.90 加宽到 0.98，标点不再单独掉到下一行。
- 删掉「路，到此为止」「路，到这就是尽头」这类空话，牌子与碑只留可用的信息。

**验证**：`BuildAll` 通过；`SceneSelfCheck` **pass 56 / fail 0**；`HandBook.prefab` 序列化 `pageTurnResource:`（空）、`Qinling.unity` 序列化 `creakResource:`（空）、`Liangzhu.unity` 序列化 `clipName: amb_liangzhu_music`。新曲循环接缝差 0.00015（相邻采样典型步长 0.0092），听不出断点。

---

### 1.3.9 第十七轮：良渚走不通的三处 + 「看完怎么回去」

**① 断路是生成 bug，不是史实。** 良渚的路本来该是连续夯土路，问题出在三处几何：
- 主路 26 块板间距 2.55 m、板深 2.45 m，每两块之间 10 cm 空档，远看就是一段段断口；改为首尾相接。
- 南河木桥 4.6 m 宽 × 4.6 m 长，横跨 7 m 宽的河，两头各剩 1.2 m 水面——走到桥头就掉不下去也过不去。改为 4.6 × 8.2 m，两头压上路基。
- 两座桥原本**没有任何碰撞体**（`DeckBridge` 只画板），人到桥头踩的是河底。补了一块隐藏的 `DeckCollider`（渲染器关闭），桥面顶与夯土路齐平。

**② 「良渚古城」门前那道台阶。**
- 城门两扇门扇原来只开 28°，中间净宽 0.70 m，而 `CharacterController` 胶囊直径 0.68 m，走到匾额下就被顶住。改为各开 76°，贴向门墙，净宽约 2.4 m。
- 城内木桥桥面高 0.18 m，像一道门槛；降到与城内地面齐平（`LiCityFloor − 0.02`）。
- 莫角山台地南坡：2.0 m 的过渡等于 50° 陡坎，而 `slopeLimit` 正好 50°。坡带宽到 4.0 m，台沿从 z=20 退到 z=22，让城内运河的岸边（软边到 z≈18.7）先收完，两个起伏不再叠加；最陡处 **42°**。台顶的路与坡道木栈按新坡面重铺（原来第一块浮空 2 m、最后一块埋进台里）。

**③ 看完怎么回山谷、怎么进下一处。**
- 读完三条主线索仍照旧自动触发：书抬起来、文明档案 → 空格 → 回 `Archive` 的 `back_<chapter>` 落点。
- 新增 **Q「合卷回山」**（`ReturnHome`，由 `TaskManager.Awake` 自动挂上）：随时按 Q 弹确认框，「空格回山谷 / Esc 再走走」。进度在 PlayerPrefs 里，下次回来接着看。开场说明与 HUD 都写明了这个出口。`Ask()` 独立成公开方法，走查脚本可直接调用（等价于按 Q）。
- 序章的 `ObjectiveGuide` 之前只认 `Interactable`，而路尽头的界门是 `ScenePortal`，所以回山谷后引导会指向身边的说明牌。现在山谷里优先指界门：**没去过的卷 → 去过但没读完的卷 → 三卷全读完才指藏卷阁（终章）**，并显示「下一程 · 卷名 · 方向 距离」。

**④ 桥头「卡在缝里」（第十八轮补）。**
- 南河木桥长 8.2 m，但河道在 z=−47…−41 之外还有 2.2 m 的软岸（−49.2 就开始下陷）。8.2 m 的桥两头停在 −48.1 / −39.9，脚下已经是 0.58 m 深的岸坡，桥面却还在 0.10 m——走到桥头就被 0.68 m 的立面顶住，这就是「卡在缝里」。桥长加到 10.6 m，两头落在 −49.3 / −38.7 的平地上，高差只剩 0.19 m（在 `stepOffset=0.35` 以内）。
- 城内木桥同样检查过：两端高差约 0.06 m。
- **空格跳跃**：`DesktopPlayerController.jumpHeight = 1.1 m`，起跳速度按 `√(2·|g|·h)` 算；模态弹窗打开时 Update 已提前 return，不会和「空格确认」冲突；书抬在眼前时不跳。三个遗址的 HUD 都加了「空格 跳跃」。

**⑤ 进门后「卡在缝里」的真正主因：主轴上的纵向水道（第十九轮）。**
- `LiCanalMask` 里有一条 c（x −10…10，z 6…16.5），配一条沿 x=0 从 z=−20 铺到 40 的 `Canal_Gate` 水带。结果进城主轴被压低 1.15 m：地面在 z≈13 掉到 −0.05 m，水面却在 0.78 m，路板悬在头顶 1 m；走到城内木桥前，桥面 1.13 m 对地面 −0.05 m，是一道 **1.18 m 的立面**，人卡在桥头进不去，回头也爬不出坑。
- 删掉 c 和 `Canal_Gate`：陆门到木桥是唯一的入城通道，不能再兼作水道。城内东西向水道（b）与木栈桥保留。
- 城门土垄的环形开口只在门口中心挖得开，往城里 4–7 m 还压着 0.4 m 的土埂；`LiGateOpen` 增加一条 x ±4.2、z 1.5…14 的直走廊，门内地面拉平到 1.05–1.09 m。
- 城内路板从板顶 1.20 m 降到 1.10 m，与地面齐平；运河软岸范围内不再铺路板（原来会悬空 0.6 m）。
- 自检新增 `CheckWalkLine`：沿 x=0 从南岸到莫角山坡脚每 0.25 m 打一条向下射线，取「地形 / 桥板」里最高的可站立面，相邻高差 > 0.30 m 直接 FAIL。这条把桥面、路板、台地坡一起算进去，比只看地形高度更接近实际脚下。

**顺手修的**：反山墓地三件玉琮的四节都带 collider，从北侧看线索时准星先打到玉琮、打不到感应球。玉琮只是摆放的器物，撤掉碰撞（`false`）。

**验证**：`BuildAll` 通过；`SceneSelfCheck` **pass 68 / fail 0**（第十九轮）。良渚相关：两座桥的碰撞桥面与齐平、四个桥头端点高差 ≤0.30 m、主轴全程无台阶（`CheckWalkLine`）、两扇门扇开角 ≥70°、主轴最大坡度 41.8°。临时诊断脚本已删。
`PlayModeProbe` **pass 27 / fail 0**：新增「按 Q 弹框 → 回序章 back_liangzhu 落点（误差 0.00 m）→ 引导改指秦陵」。同时修正了走查脚本自己的两处误判（编辑器里 Game 视图没焦点时 `Scan` 不跑，提示永远不显示；秦陵落点本来就在触发圈外却expect门立刻开），并给线索点走查补了俯仰对准。
**注意**：PlayModeProbe 是图形界面里的播放模式走查，这一轮按用户要求没有再跑真机；68 条自检是 headless 场景/物理层面的验证，实际手感仍以用户实走为准。

### 1.3.10 第二十轮：跳跃键、回山谷的落点、HUD 收条、线索勾

**① 跳跃从空格改到「、」键。** `DesktopPlayerController.Move()` 里 `KeyCode.Space` → `KeyCode.Backslash`（中文输入法下就是「、」）。空格只留给弹窗确认，跳跃与确认不再共用一只手。三个遗址的 HUD 文案同步改成「「、」跳跃」。

**② Q 合卷回山落回三岔口正中。** 原来回的是被走那条路的尽头（`back_<chapter>`），脸还朝着山谷，看着像被退了半路。
- 序章新增落点 `hub_center`：`(0, 0.1, −2.2)`，朝向 +Z，正对三根望柱与三条岔路。
- `ReturnHome.Ask()` 改为 `LoadScene("Archive", "hub_center")`。`back_*` 落点保留（自检与其它入口仍在用）。

**③ 底部提示条平时不显示。** 之前 `PromptPanel`（1100×50、alpha 0.55 的黑条）一直挂在屏幕下方，只是里面的字被 `SetActive(false)` 藏起来——所以没话说的时候也有一块半透明黑。
- `UIManager` 在 `Awake` 里给 `PromptPanel` 挂 `CanvasGroup`，初始 alpha 0；`ShowPrompt / HidePrompt / ClearPrompt` 改为写目标值，`Update` 每帧用 `MoveTowards` 淡入淡出（约 0.17 s），淡到 0.01 再把文字物体关掉。

**④ 左上角线索表：开过的装置画勾。**
- `TaskManager.Refresh()` 原来只列三条主线索，用 ◆／◇；现在列出全部装置（主线索 + 「其余可看可不看」），完成打金色 **✓**，未完成是空心 ○，抬头改成「本卷线索 n / 6」。
- 全部装置看完 → `ShowToast("这一卷看完了　·　合卷回山谷，往下一卷去", 7 s)`，主动告诉玩家可以往下走。
- `ShowToast` 增加可选时长参数（`seconds = 0` 仍是常驻），到点自己收回。

**⑤ 序章进度板。** 新增 `Scripts/UI/HubBoard.cs`，构建时挂在序章的 UI 物体上；`Start` 里按 PlayerPrefs 现算三卷的 ✓／○ 覆盖静态文案。三卷全完成时提示「三卷已合　·　藏卷阁的门开了，往山谷尽头走」。

**⑥ 秦陵美术（本轮前半段）。** 山体从远处的切纸板换成地形里的真实山体：800 m 见方地形、`QnMassif` 叠出前后山脊、6 m 级碎褶、林冠色阶按线性空间重取（原来 sRGB 0.10 的固有色在线性下只有 0.01 反射率，背光北坡糊成黑布）；平原从「沙漠黄」改成草地与秸秆色田块；树冠提亮并加环境光增益，林下补灌丛；夯土纹理去掉所有等高横线、bump 0.30→0.10；`QnTurf` 改名 `QnTurfGrass`（原名字识别不出草，落回石纹）。用户确认**秦岭到此为止，不再改**。

**验证**：`BuildAll` 通过（四场景全部保存）；`SceneSelfCheck` **pass 68 / fail 0**。本轮按用户要求没有再跑播放模式 / 真机。

---

### 1.3.11 第二十一轮：藏卷阁的正门

**① 正面墙让出门洞。** 原来 `Wall_F` 是整面砌死的一堵墙，只在墙外贴三扇纸格扇——走到跟前没有入口。现在拆成 `Wall_F_L` / `Wall_F_R` / `Wall_F_Head` 三段，正中让出 2.9 m 宽、3.6 m 高的门洞；左右两间的纸格扇保留，中间那间让给真门。

**② 门构件（`ArchiveArchitecture.BuildHallGate`）。** 门槛（石）、两根门柱、门楣、两扇 1.44 m 宽的门板；门板挂在 `Hinge_L` / `Hinge_R` 两个空物体下，绕 Y 轴转向外开——绕轴转才像门，整块平移像帘子。门板用浅一号的 `wood`，压条与门环压深，远看是两扇分明的大门而不是一个黑洞。

**③ 运行时开关（`Scripts/Interaction/HallDoor.cs`）。** `Start` 读 `GameState.AllChaptersVisited`：三处遗址**都进去过**就转开（60°/s，约 1.4 s），没走齐就保持关闭、门闩横着。判据是「去过」不是「读完」，与 `ScenePortal.requireAllChapters` 同一套。

**④ 门里的字。** 后墙加「河 山 册」竖排字牌 + 室内地面（`Hall_Floor`）。门关着看不见，门一开，从院子里正对着它——给人往里走的理由。
（**已被 1.3.13 取代**：字牌删掉，改成一扇亮着的隔扇，并且跨过门槛直接进终章。）

**⑤ 界门挪到门口。** `HallGate` 的 `ScenePortal` 从 `z=35.2`（台阶脚下）挪到 `z=41.2`（门扇前 0.4 m），半径 2.2；文案改「[E] 推门进去　·　终章 · 河山册」，锁着时「门还闩着　—— 三处遗址都进去过才开」。这扇门后面接的就是 `Epilogue`（终章「河山册」），进门前一直是这样，只是触发点原先在三米开外、看着不像在开门。

**⑥ 序章进度板同步。** `HubBoard` 的开门口令从 `AllChaptersComplete` 改成 `AllChaptersVisited`，提示语改成「三处都去过了　·　藏卷阁的门开着，往山谷尽头推门进去」；面板里多一行门的状态（「藏卷阁的门开着」／「还差 n 处没去」）。

**⑦ 预览工具。** 新增 `HuaxiaBuilder.RenderHallDoorPreviews`（菜单「Huaxia/渲染藏卷阁正门预览」），出 `archive_door_closed.png` / `archive_door_open.png` / `archive_door_inside.png` 三张；`RenderPreview` 加了一个可选的 `afterOpen` 回调，用来在拍照前把只有运行时才动的门轴摆好。

**⑧ 自检。** `SceneSelfCheck` 增加 4 条：正面墙让出门洞（`Wall_F` 不存在、左右两段存在）、挂了 `HallDoor`、两扇门扇都在、开角 ≥ 60°。

**验证**：`BuildAll` 通过；`SceneSelfCheck` **pass 72 / fail 0**；三张正门预览图已渲染并肉眼确认（关着是两扇分明的木门，开着能看见里面的「河山册」）。

---

### 1.3.12 第二十二轮：藏卷阁踏跺能走上去了

**问题**：门是能开了，但人走不上去。原因是台基是三级 0.44 m 的石阶，而 `CharacterController.stepOffset = 0.35`——正面那五级踏跺只爬到 1.32，再往上正好压着两级 0.44 的台基，人卡在院子中间上不去。之前没人发现，是因为界门设在台阶脚下（z=35.2），根本不用走上去。

**改法**：正面踏跺重做（`ArchiveArchitecture.BuildGreatHall` 的 `Step_F`）。
- 七级，每级 0.22 m、踏面 0.55 m，从 `z=-halfD-4.95` 一路接到最上一层台面（1.54 → 台基 1.56）。
- 每级是从地面砌到顶面的实心块（高度 = 该级顶面），侧看成一道三角石梯，不会有悬空的板。
- 恰好落在台基的三道棱上：z=38.25 处 0.88 与 Terrace_1 齐平，z=39.35 处 1.32 与 Terrace_2 齐平——三级台基于是变成踏跺中间的休息平台，不再是三道翻不过去的坎。
- 御路石（`ImperialWay`）往前挪到第一级之前，不然整块被埋进踏跺里。

**自检**：`SceneSelfCheck` 新增 `CheckHallApproach()`——沿 x=0 从 z=32 走到 z=41，每 0.25 m 向下打一条射线取最高碰撞面，相邻高差 > 0.30 m 即判失败（这正是「走不上去」的量化定义）；另加一条护栏，先断言院子（≈0）和台面（1.56）都取得到，防止射线打空导致假通过。

**验证**：`BuildAll` 无错；`SceneSelfCheck` **pass 74 / fail 0**；新渲染 `Previews/archive_door_approach.png`，从院子里能看见七级踏跺一路接到门前。

---

### 1.3.13 第二十三轮：门后面就是终章

**问题**：门开了、台阶也上得去了，推门进去却是一间空屋子。`Hall` 里原先只有一块地板和一排字，走进去和外面断了片——而玩家真正该到的地方是那间书房（`Epilogue`，Unity 里直接打开就能看见的那间），终章本来就在那儿。

**改法：门后面直接就是书房，中间不再隔着一段空屋。**

- **跨门槛即转场。** `ScenePortal` 新增 `autoEnter` / `innerRadius`：勾上后不用按 E，走进 `innerRadius` 就直接进下一场。`HallGate` 的界门挪到 `z=43.8`（门扇平面约在 42.0），`innerRadius=1.70`——人得真的越过门，但不必摸到里面的隔扇。门口到触发点约 1.4 m，走两步就过去，读起来是「推门进去」而不是「站在门口按 E」。
- **拆掉空屋的字牌，换成亮着的隔扇。** 后墙那排「河 山 册」删掉；门内 2.35 m 处立三扇纸格扇（`Hall_Screen`，每扇 1.18 × 3.55 m，用终章那套 `BuildLatticePanel`），屏后一盏暖光（`ScreenFill`）。纸面透光——从院子里隔着门看进来，是「里头点着灯、有人住」，不是一道黑洞或者一间空房。高度按门洞顶（3.6 m）做满，上方不露空。
- **门板配色。** 门板用 `wood`、压条和门环用 `woodDark`：原先整扇门都是一块深色，远看只剩一个门洞。
- **序章文案。** `HubBoard` 里那行「还差 n 处没去／藏卷阁的门开着」整句删掉——那是状态播报，不是人话。开门的提示只留一句 toast「三处都去过了，藏卷阁的门开着。」；「眼下」那一栏在有门之后改成一行「· 去山谷尽头，进藏卷阁」。
- **怎么回到都没去过的状态。** 新增 `Editor/DevTools.cs`，菜单 **Huaxia/清空探索进度（三卷回到都没去过）**：调 `GameState.ResetAll()` + `PlayerPrefs.Save()`。清完三卷回到未访问、门重新闩上、开场说明会再弹一次。（进度存在 PlayerPrefs，不清就得手动删 `Huaxia.*` 的键。）

**验证**：`BuildAll` 无错；`SceneSelfCheck` **pass 74 / fail 0**（门洞、`HallDoor`、两扇门扇、开角、踏跺高差五条都在）；四张正门预览重渲并逐张确认——关着是两扇木门带门环，开着能看见里面亮着的隔扇，院子里能看见七级踏跺一路接到门前。

---

## 2. 验证状态

已验证：

- Unity 命令行编译无 C# 错误、无 exception、无 abnormal mesh bounds 警告。
- 三个场景可生成并保存（`BUILD_EXIT=0`）。
- 48 张预览图正常出图（Metal / Apple M5），含三张新的 `*_returnstone.png`。
- **`SceneSelfCheck` 常驻自检：pass 68 / fail 0**（2026-11-05，第十九轮）。检查：序章开场说明（且含三处地名）、每个 HUD 都有 `ObjectiveGuide`、每个遗址都有进场说明、秦陵城门挂了 `AutoDoor` 且两扇门扇 / 开角 ≥60° / 触发 ≥5 m；良渚两座桥有可走碰撞面且与路齐平、四个桥头端点高差 ≤0.30 m、主轴全程无台阶（`CheckWalkLine`）、两扇门扇开角 ≥70°、主轴最大坡度 ≤45°。原先「翻页声 / 开门声可加载」两条随资源删除一并移除。
- **XR 配置资产已生成并校验**：`Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset`，Standalone 与 Android 各挂 1 个 `OpenXRLoader`，`automaticLoading / automaticRunning` 均为 1。此前该资产不存在，OpenXR 从未初始化。
- 史实分层标记：`liangzhu_dock.png` 已裁切放大确认「○ 学界推断」、`quanzhou.png` 确认「△ 艺术处理」，两行小字在取景距离内可读。

- **Play Mode 已可自动化**：`[InitializeOnLoadMethod]` + `SessionState` 重挂 `EditorApplication.update` 的播放模式脚本在本机跑通。`PlayModeProbe` **pass 27 / fail 0**（第十七轮）：界门引导、良渚线索可拾取、Q 合卷回山落在 `back_liangzhu`（误差 0.00 m）、回山谷引导改指秦陵、秦陵门自动开合、泉州/终章。headless 下仍进不了播放模式，要图形界面跑。
- 界门与落点：`from_hub` 落点误差 0.05 m；`back_liangzhu` 误差 0.00、距门 3.7 m（感应半径 3.15 m，不会立刻回送）。

未验证：
- 模态弹窗里的 `TierLine()` 首行（`ScreenSpaceOverlay`，渲不进预览图）目前只做了代码审查。
- **序章 / 良渚 / 秦陵三首授权曲只验证了格式、响度、频段分布与循环点**，没有主观试听；选曲是否合适要戴上耳机判断。翻页声与开门声已按反馈删除，暂无声。
- **键位拆分只做了代码路径与场景数据自检，没有真人按一遍**：`E` 不再进确认分支是读代码确认的，建议进编辑器实跑一次（站界门前按空格 → 不该被送走；抬书按空格 → 不该翻页）。
- **VR 真机未接**（PICO 4 企业版尚未到手接入）。`XRInputBridge` 的映射、头显姿态、HUD 世界空间转换全部只做过代码审查；`XRSetup` 能生成正确的 loader 配置，但**本机只装了 WebGL 构建模块，没有 Android 支持**，无法出 APK；OpenXR 的 Standalone 支持只覆盖 Windows64，Mac 编辑器里也不会激活 XR。以上都要等真机和 Android 模块到位后才算数。
- **泉州 / 终章两段合成音乐只验证了「能生成、能加载、循环点闭合」**，没有主观试听；这两段没被点名，序章与秦陵两首已换成授权曲目。
- **秦陵城门自动开、信息点光柱高度、HUD 引导行的实际观感**没有进 Play Mode 手动走一遍；只有场景数据自检。
- **运行时文字清晰度**没有真机验证。本轮提的是世界空间文字牌的栅格分辨率（画布 500→1000 单位、字号 ×2）和预览图的 2× 超采样；头显里 HUD 字号是否够大（`XRInputBridge.MoveHudToWorldSpace` 的 1.8 m / 0.0011）必须戴上看过再调。

---

## 3. 已知坑（很重要，避免重复踩）

- **Unity 编辑器开着时 batchmode 会失败**（工程锁 `Temp/UnityLockfile`），构建前必须退出 Unity。Unity Hub 进程存在不代表编辑器打开，要看 lockfile。
- **颜色必须 `Lin()`**（sRGB→线性），否则整体发灰。
- **surface shader 里绝不要写 `o.Normal`**。写了会强制生成 `WorldNormalVector(IN, o.Normal)`，要求 Input 有 `INTERNAL_DATA`；两者凑齐会导致**整屏纯白过曝**。正确做法：只写 `o.WorldNormal`。
- **品红 = shader 编译失败；纯白 = `o.Normal` + `INTERNAL_DATA` 组合。**
- **`Mathf.SmoothStep(from, to, t)` 的 `t` 是归一化参数**，不能把「位置」直接传进去；本工程用自写 `Ramp(t, e)` 替代。
- **自定义屋顶网格**：屋脊默认沿 **Z 轴**、坡面沿 ±X 下降。要让脊线沿长轴，必须放进绕 Y 90° 的容器（干栏屋、大殿均已如此）。
- **地形解析高度与网格有 ±0.27 米级差异**，贴地构件一律要留余量或抬到起伏之上。
- **良渚低地掩膜曾经是 −0.55**，导致所有构件悬空 1 米；现为 −0.08。
- **水色**：半透明水 + 暖阳光相乘会变橄榄绿。靠提高色饱和度 + `_SkyTint` 天空底光（`0.10 + fres*0.85`）解决。
- **`_BounceColor` 对水平地面无效**，不能靠它提亮地面。
- **金属度 ≤0.1、smoothness ≤0.45**，没有反射探针，过高会发黑。
- LDR RenderTexture 下 **emission ≥ 1 会过曝成白块**，灯笼/窗纸光要压在 1 以下。
- **世界空间文字牌**必须用 `Huaxia/TextSingleSided`（`Cull Back`），牌面背后另放一片绕 Y 转 180° 的同文；两片间距要按**米**换算成 Plate 局部单位（`0.006 * 500 / sizeMeters.x`）。间距给反或给太小，从背后读到的仍是镜像字。
- **纹理管线**：albedo 灰阶均值 0.78，shader 内 `*1.28` 还原为 1.0；法线由同一高度场 Sobel 而来（`sRGBTexture=false`），高度场必须先 2×3×3 模糊，否则 256px 白噪声会盖住 20px 尺度结构。贴图 meta 被删后 GUID 变化，**必须重跑 BuildAll**。

---

## 4. 下一步

> **第十五轮已落地**：环境音乐重做（去掉秦陵心跳鼓/低频 drone）、目标引导行、终章门槛改为「三处都去过」、序章开场说明、秦陵城门自动开 + 开门声、信息点视觉强化、文字牌 2× 栅格、预览 2× 超采样、手柄映射与 XR 配置资产、`R / Shift+R` 对调。
>
> **第十四轮已落地**：`E` 键拆分、三处界石、翻页声、过场黑场字幕、三遗址文案口语化。上一轮遗留的「E 三重身份」已清。

三处遗址 + 序章 + 终章的场景骨架已全部建完（`BuildAll` 一条命令可重建）。接下来是「从场景变成作品」的部分：

1. **真人完整通跑一遍并掐时间**（最优先，别再靠估算）：从山门到终章合书跑三次，记录每章用时、在哪一段卡住、哪句提示没读懂。键位刚改，必须在真机前先手跑。
2. **《河山册》本体**（v6 §M2，主体已完成）：纸张湿润 / 褶皱 / 纤维、从湿到干的材质过渡、墨迹随干燥显现、翻页、章节完成自动抬起、次线索落页边注都已通。**还剩**：拆掉 v3 遗留的线索卡模态（`TaskManager` 完成章节时仍会弹「文明档案」），把终章的「此页留给后来者。」写进末页。
3. **终章做厚**（原 P1-1）：三卷内容在案上摊开、月洞窗外天光走一遍、末页落字。目前终章只有一张案、一本册子、一扇窗，情绪落点太轻。
4. **近手抓取交互层**：VR 交互从「中心射线点选」改为近手抓取 + 装置交互（吸附 + 阻尼 + 触觉模拟重量），三处遗址各一个可操作的装置（良渚的木桩 / 秦陵的夯具 / 泉州的缆绳）。
5. **叙事节奏**：按 v6 的 12:30 主线跑一遍，检查三句手记（「水要分。」「土要夯。」「风要等。」）的落点、终章末页「此页留给后来者。」的出现时机。
6. **VR 接入（M2）**：PICO 4 + OpenXR 真机联调，性能预算（单眼 2K / 72fps）。前置：在 Unity Hub 给 2022.3.62f3c1 装 **Android Build Support**（含 OpenJDK / SDK / NDK）；`BuildAll` 之后再跑一次 `Huaxia/XR/启用 OpenXR 加载器（PICO 4）`；真机上先验证右手瞄准线是否对准可交互物、HUD 字号是否够读。
7. 视觉残项（可选，非阻塞）：门缝透光 / 界门雾、终章门缝的一线亮白、挂屏水墨在小尺寸下的可读性、良渚地表偏白、`quanzhou_wide` 水面亮矩形。

---

## 5. 命令行

```bash
UNITY="/Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents/MacOS/Unity"

# 构建全部场景（约 3–4 分钟）
"$UNITY" -batchmode -nographics -projectPath "$PWD" \
  -executeMethod Huaxia.EditorTools.HuaxiaBuilder.BuildAll -quit -logFile /tmp/unity-build.log

# 渲染全部预览图（约 4–5 分钟，不可只渲染单张）
# 每张按 2× 超采样 + 4×MSAA 渲染：Previews/*.png 是 1600×900（进仓库），
# Previews/hires/*.png 是 3200×1800 原图（.gitignore，供 1:1 看细节）
"$UNITY" -batchmode -projectPath "$PWD" \
  -executeMethod Huaxia.EditorTools.HuaxiaBuilder.RenderPreviews -quit -logFile /tmp/unity-render.log

# 只重建程序化纹理（约 40 秒）
"$UNITY" -batchmode -nographics -projectPath "$PWD" \
  -executeMethod Huaxia.EditorTools.StylizedTextureFactory.RebuildAll -quit -logFile /tmp/unity-tex.log

# 场景不变量自检（约 30 秒；改完场景/键位先跑这个，再看预览图）
"$UNITY" -batchmode -nographics -projectPath "$PWD" \
  -executeMethod Huaxia.EditorTools.SceneSelfCheck.Run -logFile /tmp/unity-selfcheck.log
grep -E "FAIL|pass=" /tmp/unity-selfcheck.log
```

> `grep -c "error"` 会误报 Licensing / LogAssemblyErrors，需要过滤后再判断构建是否成功。

---

## 6. 版本管理（GitHub）

- 远程：`git@github.com:JiayuLi0815/huaxia-vr.git`（SSH，走 `~/.ssh/id_ed25519_github`），主分支 `main`。
- `.gitignore` 已排除 `Library/ Temp/ Logs/ obj/ Build/ Builds/ UserSettings/ .DS_Store *.csproj *.sln`。
  这些都可由 Unity 重建，入库只会让仓库膨胀数百 MB。
- **推送时机**：`BuildAll` + `RenderPreviews` 确认画面通过后立刻提交一次 —— 那是一个可回滚的视觉稳定点。
  一天 3–5 次提交即可；不要定时推，也不要对已推送的提交做 amend / force push，否则历史失去回滚价值。
- **不要在 Unity 开着时提交场景文件**，编辑器会持续写 `.unity`，提交到的是半成品。
- **场景文件已不入库（2026-11-06 起）**。`Assets/Huaxia/Scenes/` 整目录写进 `.gitignore`，
  从索引里移出（本地文件保留）。原因：`Qinling.unity` 生成出来 173 MB，超过 GitHub 单文件 100 MB 的硬上限，
  推不上去；而四个场景本来就是各 `*Scene.cs` 里 `NewScene` 从空场景重建、`SaveScene` 落盘的，
  入库只是把可重建的网格数据存一遍。
  **clone 后要做的第一件事**：打开工程跑一次 `Huaxia/HuaxiaBuilder/BuildAll`（或命令行
  `-executeMethod Huaxia.EditorTools.HuaxiaBuilder.BuildAll`），场景会重新生成；
  场景重建后 GUID 是新的，`BuildAll` 末尾会按路径重写 `EditorBuildSettings.scenes`，
  所以构建列表也会自己恢复，不用手动拖。
  走 LFS 不划算：免费额度 1 GB 存储，一次推送就吃掉两百多 MB。
