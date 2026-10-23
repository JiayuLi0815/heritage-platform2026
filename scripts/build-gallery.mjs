#!/usr/bin/env node
/* 把素材盘里每处遗产的图片压成网页画廊，并生成 src/data/gallery.js。

   Usage:
     node scripts/build-gallery.mjs <asset-inventory.json> <素材根目录>

   每处输出 public/img/gallery/<id>/01.webp…（长边 1400，q70）与同目录下的
   t01.webp…（缩略图 360，q62）。原图不动，脚本只读素材盘、只写仓库。
   已经生成的跳过，可以中断再跑。
*/
import fs from 'node:fs'
import path from 'node:path'
import sharp from 'sharp'

const [inventoryPath, root] = process.argv.slice(2)
if (!inventoryPath || !root) {
  console.error('usage: node scripts/build-gallery.mjs <inventory.json> <素材根目录>')
  process.exit(1)
}

const REPO = path.resolve(import.meta.dirname, '..')
const OUT_ROOT = path.join(REPO, 'public', 'img', 'gallery')
fs.mkdirSync(OUT_ROOT, { recursive: true })

const IMG = /\.(jpe?g|png|webp|avif|tiff?)$/i
const FULL = { width: 1400, height: 1400, fit: 'inside', withoutEnlargement: true }
const THUMB = { width: 360, height: 360, fit: 'inside', withoutEnlargement: true }

const rows = JSON.parse(fs.readFileSync(inventoryPath, 'utf8')).filter((r) => r.id && r.images > 0)

/* 图片目录不一定叫「图片」，和封面脚本一样按两种命名各试一次 */
function imageDir(folder) {
  for (const name of [folder, path.basename(folder).replace(/_/g, '·')]) {
    const d = path.join(root, name, '图片')
    if (fs.existsSync(d)) return d
  }
  return null
}

/* 文件名是 图片_<序号>_<哈希>.jpg，按序号排才是拍摄/收录顺序 */
function orderOf(f) {
  const m = f.match(/_(\d+)_/)
  return m ? Number(m[1]) : Number.MAX_SAFE_INTEGER
}

function listImages(dir) {
  return fs
    .readdirSync(dir)
    .filter((f) => IMG.test(f) && !f.startsWith('.'))
    .sort((a, b) => orderOf(a) - orderOf(b) || a.localeCompare(b))
}

const manifest = {}
const failed = []
let done = 0
let skipped = 0
let files = 0
const t0 = Date.now()

async function buildSite(row) {
  const dir = imageDir(row.folder)
  const outDir = path.join(OUT_ROOT, String(row.id))
  if (!dir) {
    failed.push([row.id, row.folder, '找不到图片目录'])
    return
  }
  const list = listImages(dir)
  if (!list.length) {
    failed.push([row.id, row.folder, '目录里没有图片'])
    return
  }
  fs.mkdirSync(outDir, { recursive: true })

  const kept = []
  for (let i = 0; i < list.length; i++) {
    const nn = String(i + 1).padStart(2, '0')
    const main = path.join(outDir, `${nn}.webp`)
    const thumb = path.join(outDir, `t${nn}.webp`)
    if (fs.existsSync(main) && fs.existsSync(thumb)) {
      skipped++
      kept.push(nn)
      continue
    }
    try {
      const src = path.join(dir, list[i])
      await sharp(src, { failOn: 'none', limitInputPixels: false })
        .rotate()
        .resize(FULL)
        .webp({ quality: 70 })
        .toFile(main)
      await sharp(src, { failOn: 'none', limitInputPixels: false })
        .rotate()
        .resize(THUMB)
        .webp({ quality: 62 })
        .toFile(thumb)
      done++
      kept.push(nn)
    } catch (e) {
      /* 单张坏图不该拖垮整处遗产：跳过它，编号不占位，其余照常出 */
      failed.push([row.id, list[i], String(e.message).slice(0, 70)])
    }
  }
  if (kept.length) manifest[row.id] = kept.length
  files += kept.length
}

/* sharp 自己会用多核，这里再开 3 条并发，读盘和编码能叠上 */
const POOL = 3
let cursor = 0
async function worker() {
  while (cursor < rows.length) {
    const row = rows[cursor++]
    await buildSite(row)
    const n = cursor
    if (n % 25 === 0 || n === rows.length) {
      console.log(
        `${n}/${rows.length} 处 · ${files} 张 · 新转 ${done} 跳过 ${skipped} 失败 ${failed.length}` +
          ` · ${((Date.now() - t0) / 60000).toFixed(1)} 分钟`
      )
    }
  }
}
await Promise.all(Array.from({ length: POOL }, worker))

/* 清单给前端 import，只记每处有几张——文件名是规整的 01…NN */
const body = `/* 本机素材盘压出来的画廊图片。由 scripts/build-gallery.mjs 生成，不要手改。
   键是站点 id，值是张数；文件在 public/img/gallery/<id>/NN.webp（缩略图 tNN.webp）。 */
export const GALLERY = ${JSON.stringify(manifest, null, 0)}

export const galleryOf = (id) => GALLERY[id] || 0
`
fs.writeFileSync(path.join(REPO, 'src', 'data', 'gallery.js'), body)

console.log(
  `\n完成：${Object.keys(manifest).length} 处 / ${files} 张（新转 ${done}，已存在 ${skipped}，失败 ${failed.length}），` +
    `用时 ${((Date.now() - t0) / 60000).toFixed(1)} 分钟`
)
if (failed.length) {
  fs.writeFileSync(path.join(REPO, 'scripts', 'gallery-failed.json'), JSON.stringify(failed, null, 1))
  console.log(`失败清单写在 scripts/gallery-failed.json（前 5 条）`)
  console.log(failed.slice(0, 5))
}
