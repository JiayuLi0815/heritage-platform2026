#!/usr/bin/env node
/* 把素材盘里的 glb 压进 public/models/<站点 id>/，并生成 src/data/models.js。

   Usage:
     node scripts/build-models.mjs <asset-inventory.json> <素材根目录>

   压缩用 gltf-transform 的 meshopt（几何 + 贴图 webp 2K）。原文件不动，
   脚本只读素材盘、只写仓库。已经压过的文件会跳过，可以中断再跑。
*/
import fs from 'node:fs'
import path from 'node:path'
import { execFileSync } from 'node:child_process'

const [inventoryPath, root] = process.argv.slice(2)
if (!inventoryPath || !root) {
  console.error('usage: node scripts/build-models.mjs <inventory.json> <素材根目录>')
  process.exit(1)
}

const REPO = path.resolve(import.meta.dirname, '..')
const OUT_ROOT = path.join(REPO, 'public', 'models')
const rows = JSON.parse(fs.readFileSync(inventoryPath, 'utf8'))

/* 模型原名是拉丁字母的构件名，直接摆到中文界面上会很生硬。
   这里只翻出现过的那些，遇到新名字自动回落到原名（下划线换空格）。 */
const LABELS = {
  // 1563 哈萨绿洲
  QasrIbrahim: '易卜拉欣宫',
  QasrIbrahim_Mosque: '易卜拉欣宫清真寺',
  HajjiFarm: '哈吉农庄',
  // 86 孟菲斯及其墓地金字塔
  AllMemphis: '孟菲斯墓地全景',
  Kheops: '胡夫金字塔',
  Khephren: '哈夫拉金字塔',
  Mykerinos: '孟卡拉金字塔',
  KheopsSphynx: '胡夫金字塔与狮身人面像',
  Sphynx: '狮身人面像',
  MykerinosValley: '孟卡拉谷庙',
  TombOfQar: '卡尔之墓',
  UnasPyramid: '乌纳斯金字塔',
  KhafreBurialChamber: '哈夫拉葬室',
  // 294 巴勒贝克
  Baalbek: '巴勒贝克遗址全景',
  // 1192 巴林贸易港
  QalatAlBahrain: '巴林堡',
  // 433 巴赫莱要塞
  BahlaFort: '巴赫莱要塞',
  BahlaFortAerial: '巴赫莱要塞鸟瞰',
  Door: '城门',
  Room: '室内',
  // 194 提姆加德
  ArcOfTrajan: '图拉真凯旋门',
  Capitol: '卡皮托利神庙',
  Bathhouse: '浴场',
  Library: '图书馆',
  Context: '遗址环境',
  Theatre: '剧场',
  // 190 昔兰尼
  CyreneGeneral: '昔兰尼全景',
  Gymnasium: '体育场',
  'Temple-Amphitheatre': '神庙与圆形剧场',
  TempleOfZeus: '宙斯神庙',
  // 295 比布鲁斯
  Byblos: '比布鲁斯遗址全景',
  // 1293 黑格拉
  Diwan: '迪万',
  Jablithleb: '贾巴尔·伊斯莱卜',
  'QasrAl-Farid': '独立石墓',
  Khuraymat: '胡赖马特',
  OldCity: '老城',
  Landscape: '墓地景观',
  // 183 莱波蒂斯
  LeptisMagna: '大莱波蒂斯全景',
  Amphitheater: '圆形剧场',
  Forum: '广场',
  Basilica: '巴西利卡',
  Marketplace: '市场',
  Lighthouse: '灯塔',
  TriumphalArc: '凯旋门',
  OldForum: '旧广场',
  Nympheum: '水神庙',
  Baths: '浴场',
  AmphiteatreCircus: '竞技场',
  // 1542 迪尔穆恩墓葬群
  Aali: '阿里墓群',
  '7-21_Aali': '阿里墓群',
  JanabiyahGeneral: '贾纳比亚全景',
  JanabiyahDetail: '贾纳比亚局部',
  '4_JanabiyahGeneral': '贾纳比亚全景',
  '4_JanabiyahDetail': '贾纳比亚局部',
  MadinatHamadGeneral: '哈马德城全景',
  MadinatHamadDetail: '哈马德城局部',
  '2_MadinatHamadGeneral': '哈马德城全景',
  '2_MadinatHamadDetail': '哈马德城局部',
  // 1361 吉达古城
  Windows: '罗尚窗饰',
  BabJadid: '新门',
  S23: '街屋 S23',
  S25: '街屋 S25',
  // 444 阿伊特·本·哈杜
  KsarGeneral: '筑垒村全景',
  KsarDetail: '筑垒村局部',
  // 1336 麦罗埃
  MeroePlate: '麦罗埃全景',
  NorthCemetery: '北墓群',
  N9: '金字塔 N9',
  'N19-32': '金字塔 N19–N32',
}

const mIndex = (f) => {
  const m = f.match(/_M(\d+)_/)
  return m ? Number(m[1]) : 999
}

/* glb 的版权声明在 JSON chunk 的 asset.copyright 里。74 个文件里有 1 个没写，
   界面上不能替它署名，得单独标出来。 */
const DEFAULT_CREDIT = 'Integra Tecnologia'

function copyrightOf(p) {
  try {
    const b = fs.readFileSync(p)
    const len = b.readUInt32LE(12)
    const j = JSON.parse(b.subarray(20, 20 + len).toString('utf8'))
    return j.asset?.copyright || ''
  } catch {
    return ''
  }
}

/* 文件名是哈希的三批（38 杰姆、750 瓦丹等）看不出构件，只能按遗址给个统一前缀 */
const OPAQUE_PREFIX = { 38: '圆形竞技场', 750: '古镇' }

function labelOf(file, i, siteId) {
  const m = file.match(/_M\d+_(.+)\.glb$/i)
  const key = m ? m[1] : file.replace(/\.glb$/i, '')
  if (LABELS[key]) return LABELS[key]
  if (/^dih_/i.test(key)) {
    return `${OPAQUE_PREFIX[siteId] || '模型'} ${String(i + 1).padStart(2, '0')}`
  }
  return key.replace(/_/g, ' ')
}

/* 素材盘里模型不一定都在 3D模型/ 目录下，先按文件名建一张索引，免得靠路径猜 */
const index = new Map()
for (const entry of fs.readdirSync(root)) {
  const dir = path.join(root, entry)
  if (!fs.statSync(dir).isDirectory()) continue
  const seen = (function walk(d) {
    const out = []
    for (const e of fs.readdirSync(d, { withFileTypes: true })) {
      if (e.name.startsWith('.')) continue
      const p = path.join(d, e.name)
      if (e.isDirectory()) out.push(...walk(p))
      else if (e.name.toLowerCase().endsWith('.glb')) out.push(p)
    }
    return out
  })(dir)
  for (const p of seen) {
    const key = `${entry}/${path.basename(p)}`
    if (index.has(key)) throw new Error(`模型文件名重复，需人工确认：${key}`)
    index.set(key, p)
  }
}

const manifest = {}
let done = 0
let skipped = 0
const t0 = Date.now()

for (const row of rows) {
  if (!row.models?.length) continue
  if (!row.id) {
    console.warn(`跳过未对上的文件夹：${row.folder}`)
    continue
  }
  const outDir = path.join(OUT_ROOT, String(row.id))
  fs.mkdirSync(outDir, { recursive: true })

  const sorted = row.models.filter(([file]) => /\.glb$/i.test(file)).sort((a, b) => mIndex(a[0]) - mIndex(b[0]) || a[0].localeCompare(b[0]))
  const list = []

  sorted.forEach(([file], i) => {
    const name = `${String(i + 1).padStart(2, '0')}.glb`
    const src = index.get(`${row.folder}/${file}`)
    const dst = path.join(outDir, name)
    const exists = fs.existsSync(dst) && fs.statSync(dst).size > 0
    if (exists) {
      skipped++
    } else {
      const realSrc = src
      if (!realSrc || !fs.existsSync(realSrc)) {
        console.warn(`找不到源文件：${realSrc}`)
        return
      }
      execFileSync(
        process.execPath,
        [path.join(REPO, 'node_modules/@gltf-transform/cli/bin/cli.js'), 'optimize', realSrc, dst,
          '--compress', 'meshopt', '--texture-compress', 'webp', '--texture-size', '2048', '--simplify', 'false'],
        { stdio: ['ignore', 'ignore', 'inherit'] },
      )
      done++
      const before = fs.statSync(realSrc).size / 1048576
      const after = fs.statSync(dst).size / 1048576
      console.log(`${row.id} ${name} ${labelOf(file, i, row.id)}  ${before.toFixed(1)} → ${after.toFixed(1)} MB`)
    }
    const item = { file: name, label: labelOf(file, i, row.id), source: file }
    const cr = copyrightOf(dst)
    if (!cr.includes(DEFAULT_CREDIT)) item.credit = cr || '来源待核'
    list.push(item)
  })

  if (list.length) manifest[row.id] = list
}

/* 生成的清单要被前端 import，写成普通的 ES 模块 */
const body = `/* 本机素材盘压出来的三维模型。由 scripts/build-models.mjs 生成，不要手改。
   键是站点 id（对应 sites-all.json），值是 public/models/<id>/ 下的文件与中文构件名。 */
export const LOCAL_MODELS = ${JSON.stringify(manifest, null, 2)}

export const localModels = (id) => LOCAL_MODELS[id] || null
`
fs.writeFileSync(path.join(REPO, 'src', 'data', 'models.js'), body)

const sites = Object.keys(manifest).length
const files = Object.values(manifest).reduce((n, a) => n + a.length, 0)
console.log(`\n完成：${sites} 处 / ${files} 个模型（新压 ${done}，已存在 ${skipped}），用时 ${((Date.now() - t0) / 60000).toFixed(1)} 分钟`)
