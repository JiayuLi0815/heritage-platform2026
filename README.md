# 世界文化遗产数智探索与沉浸式体验平台

同济大学人文学院 · TJ 数智文化大模型虚拟实验室

一个把「全球名录—智能问答—三维体验」连成一条路径的文化遗产数字交互平台。
收录 991 处文化与混合遗产，覆盖 154 个国家／地区；精选 20 处做三维与沉浸式场景。

##目录
heritage-platform2026/
├─ index.html            唯一 HTML 入口，挂载 #root，引 Google Fonts
├─ vite.config.js        base=/u/wangyifei/heritage/、5175 端口；VITE_GH_PAGES 时换素材目录
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
npm run dev        # http://localhost:5175
```

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
