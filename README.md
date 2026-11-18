# 世界文化遗产数智探索与沉浸式体验平台

同济大学人文学院 · TJ 数智文化大模型虚拟实验室

一个把「全球名录—智能问答—三维体验」连成一条路径的文化遗产数字交互平台。
收录 991 处文化与混合遗产，覆盖 154 个国家／地区；精选 20 处做三维与沉浸式场景。

##目录
heritage-platform2026/
├─ index.html            唯一 HTML 入口，挂载 #root，引 Google Fonts
├─ vite.config.js        构建 base=/u/wangyifei/heritage/、dev 走根路径、5175 端口；VITE_GH_PAGES 时换素材目录
├─ package.json          仅 4 个运行时依赖：react、react-dom、@google/model-viewer、meshoptimizer
├─ PROJECT.md            项目定位一页纸（做什么/不做什么/验收标准/分工）
├─ README.md             页面结构与构建说明
├─ .env.example          DeepSeek 三种接入方式的环境变量模板
├─ deploy.sh             本地构建 + 校验 + rsync 到 tj-apps 服务器
├─ deploy-on-server.sh   服务器侧解包发布：SHA256 校验 → 备份旧版 → 再上传
├─ SHA256SUMS            发布包校验清单
├─ src/                  应用源码
├─ public/               运行时静态素材（不入 git 的大件除外）
├─ scripts/              素材管线（把素材盘离线转成 public/ + src/data 清单）
├─ proxy/                DeepSeek API Key 代理服务
├─ vr/                   《华夏文明探源·河山册》VR 工程的 Unity 工程（PICO 4）
├─ work/                 管线中间产物（盘点、缺图清单、校验报告）
└─ dist/ dist-media/     构建产物 / 媒体副产物（均被 .gitignore 忽略）
## 页面结构

| 栏目 | 说明 |
| --- | --- |
| 首页 | 四步闭环（发现 / 理解 / 体验 / 关联）、二十处精选遗产、八项指标分布 |
| 全球地图 | 991 处按真实经纬度落点，按缩放聚合成簇；可搜遗产、国家、大洲 |
| 数字人「弘文」 | DeepSeek 驱动，以《世界文化遗产数字化研究报告》为知识底座 |
| VR 体验 | 《遗产现场》的探索闭环 |
| 名录档案 | 每一处的国家、年份、类别、坐标、相册与三维场景 |

## 本地运行

```bash
npm install
npm run dev        # http://localhost:5175/
```

`npm run dev` 用根路径，直接开 `http://localhost:5175/` 即可。只有构建产物才带
`/u/wangyifei/heritage/` 前缀（`vite.config.js` 里的 `DEPLOY_BASE`），所以预览 `dist/`
要开 `http://localhost:5175/u/wangyifei/heritage/`；想看根路径的构建就 `VITE_BASE=/ npm run build`。

### 拉下来以后素材是缺的（重要）

仓库只装代码和可读的工程文件。下面这些体积大的素材被 `.gitignore` 排除，**新克隆里是空的**：

| 目录 | 内容 | 大小 |
| --- | --- | --- |
| `public/img/gallery/` | 991 处遗址的相册（缩略图 `tNN.webp` + 大图 `NN.webp`） | 1.2 GB |
| `public/models/` | 各遗址的三维模型 `.glb` | 338 MB |
| `public/vr/*.apk` | 一体机安装包 | 187 MB |
| `proxy/.env` | DeepSeek 密钥 | — |

这些缺失表现为「文字和版式都在、图是空白」，不是代码坏了。素材由素材盘经
`scripts/build-gallery.mjs`、`scripts/build-models.mjs` 生成，或直接向项目负责人索取 `public/` 整包。

反过来说，`public/img/vr/`（VR 页 11 张 webp，976 KB）和全部封面图是**入库**的，
新克隆一定有；如果这几张也空白，先看是不是停在旧提交（`git log -1` 应为最新）或还在看旧的 `dist/`。

### 图片是怎么被页面取到的

`public/` 下的文件原样搬到网站根目录，代码里统一走 `src/asset.js`：

```js
export const asset = (p) => `${import.meta.env.BASE_URL}${p}`
// asset('img/vr/cover.webp') → /u/wangyifei/heritage/img/vr/cover.webp（构建后）
//                            → /img/vr/cover.webp（dev）
```

所以排查「图不显示」只要看浏览器 Network 里那条请求的完整 URL 与本地文件是否对得上。

### 数字人「弘文」

密钥只在服务端：复制 `proxy/.env.example` 为 `proxy/.env` 填入 `DEEPSEEK_API_KEY`，再启动代理
（见 [proxy/README.md](./proxy/README.md)）。代理没起或没有 `.env`，页面上会表现为「DeepSeek 没接上」。

## 构建

```bash
npx vite build     # 完整版，产物在 dist/（含全部画廊与模型，约 1.8 GB）
node scripts/build-gh-pages.mjs   # GitHub Pages 精简版，产物在 dist-gh/（约 471 MB）
```

精简版只带封面、精选 20 处的画廊与全部三维模型，其余条目的相册留空，
以适配 GitHub Pages 单站 1 GB 的上限。

## 数据与资产

- 名录数据：UNESCO World Heritage List（仅文化与混合遗产），1978–2024
- 数字化编码：课题组《世界文化遗产数字化研究报告》139 案例编码矩阵
- 三维模型：各遗产地官方或开放许可渠道，逐个标注来源与许可

## 许可

代码用于教学与研究展示。第三方图片、三维模型的版权归各来源方，
具体许可见页面内「数据源与许可」。
