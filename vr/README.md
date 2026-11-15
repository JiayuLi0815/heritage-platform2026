# 华夏文明探源 ·《河山册》VR

世界文化遗产沉浸式 VR 体验的 Unity 工程。体验者以「守册人」身份进入一本被水泡坏的《河山册》，
在三处遗址中让三页纸从湿到干：

```
序章 暮色古道 → 良渚（水之卷）→ 秦陵（土之卷）→ 泉州（海之卷）→ 终章 合书
```

全片约 12 分半三处遗址，非开放世界，也不是数字展厅，是「可以走进去的遗址短篇」。

## 打开工程

| 项 | 值 |
| --- | --- |
| Unity 版本 | 2022.3.62f3c1 |
| 渲染管线 | Built-in Render Pipeline |
| 目标设备 | PICO 4 企业版（OpenXR）；编辑器内有桌面走查模式 |
| XR 依赖 | XR Interaction Toolkit 2.6.5 · OpenXR 1.14.0 · Input System 1.7.0 · XR Management 4.4.0 |

用 Unity Hub 以 2022.3.62f3c1 打开本目录即可。`Packages/com.unity.xr.openxr.picoxr`
是随工程内置的本地包，不要删。

## 场景不在仓库里

四个场景由 Editor 脚本从空场景程序化重建，生成出来的网格会整块序列化进 `.unity`
（秦陵那份已 173 MB，超过 GitHub 单文件 100 MB 的硬上限），因此未入库。

首次打开后，在菜单执行一次：

```
Huaxia ▸ 构建全部场景 (序章古道 + 良渚 + 秦陵 + 泉州)
```

即重建序章与良渚 / 秦陵 / 泉州三处场景（耗时较长）。材质、纹理、几何同样由代码生成
（`Assets/Huaxia/Editor/`）。预览图见 `Previews/`，无需 Unity 即可查看。

## 文档

| 文件 | 内容 |
| --- | --- |
| `PROJECT_STATUS.md` | 项目状态与交接说明：已完成项、已验证项、待办 |
| `GAME_DESIGN.md` | 叙事与关卡设计（v6 · 12 分半故事线） |
| `ART_DIRECTION.md` | 美术规范：东方风格化、色板、着色器 |
| `INTERACTION_SPEC.md` | 交互规范：移动、抓取、引导、开门 |
| `VOICE_SCRIPT.md` / `VOICE_SCRIPT_COMPANION.md` | 旁白与同伴语音稿 |
| `ASSETS_LICENSES.md` | 字体、音乐、模型授权归属（含 CC BY 4.0 署名要求） |

## 第三方资产授权

字体（霞鹜文楷、思源宋体）为 SIL OFL 1.1；三段环境音乐为 Kevin MacLeod，CC BY 4.0，
署名必须保留。逐项说明见 `ASSETS_LICENSES.md`。
