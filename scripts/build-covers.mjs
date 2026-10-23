// 从 T7 素材盘为每处遗产生成一张封面图。
// 用法: node scripts/build-covers.mjs <sites.json> <素材根目录> <输出目录>
// 只读传入的路径，不在素材目录里执行任何东西。
import fs from 'node:fs'
import path from 'node:path'
import sharp from 'sharp'

const [, , sitesPath, root, outDir] = process.argv
const sites = JSON.parse(fs.readFileSync(sitesPath, 'utf8'))
fs.mkdirSync(outDir, { recursive: true })

const IMG = /\.(jpe?g|png|webp|avif)$/i

function pickCover(folder) {
  for (const dirName of [folder, path.basename(folder).replace(/_/g, '·')]) {
    const dir = path.join(root, dirName, '图片')
    if (!fs.existsSync(dir)) continue
    const files = fs.readdirSync(dir).filter((f) => IMG.test(f)).sort()
    if (!files.length) continue
    // 优先横图：头像式的竖图在卡片里会被裁掉大半
    const jpgs = files.filter((f) => /\.(jpe?g)$/i.test(f))
    return path.join(dir, (jpgs.length ? jpgs : files)[0])
  }
  return null
}

const missing = []
let done = 0

for (const s of sites) {
  const out = path.join(outDir, `${s.id}.webp`)
  if (fs.existsSync(out)) { done++; continue }
  const src = pickCover(s.folder)
  if (!src) { missing.push([s.id, s.name]); continue }
  try {
    await sharp(src, { failOn: 'none' })
      .rotate()
      .resize({ width: 1200, height: 1200, fit: 'inside', withoutEnlargement: true })
      .webp({ quality: 70 })
      .toFile(out)
    done++
  } catch (e) {
    missing.push([s.id, s.name, String(e.message).slice(0, 60)])
  }
  if (done % 50 === 0) process.stdout.write(`${done}/${sites.length}\n`)
}

console.log(`done ${done}/${sites.length}, missing ${missing.length}`)
if (missing.length) console.log(JSON.stringify(missing.slice(0, 20), null, 1))
