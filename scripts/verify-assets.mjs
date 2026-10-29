import fs from 'node:fs'
import path from 'node:path'
import sharp from 'sharp'
import { GALLERY, GALLERY_FILES } from '../src/data/gallery.js'
import { LOCAL_MODELS } from '../src/data/models.js'

const tree = process.argv[2] || 'public'
if (!['public', 'dist'].includes(tree)) throw Error('Usage: node scripts/verify-assets.mjs public|dist')
const sites = JSON.parse(fs.readFileSync('src/data/sites-all.json', 'utf8'))
const errors = []
const images = []
const models = []
for (const s of sites) images.push(`${tree}/img/sites/${s.id}.webp`)
for (const [id, count] of Object.entries(GALLERY)) {
  const list = GALLERY_FILES[id] || []
  if (count !== list.length || new Set(list).size !== count) errors.push(`画廊清单不一致：${id}`)
  for (const n of list) for (const prefix of ['', 't']) images.push(`${tree}/img/gallery/${id}/${prefix}${n}.webp`)
}
for (const [id, list] of Object.entries(LOCAL_MODELS)) {
  for (const m of list) models.push(`${tree}/models/${id}/${m.file}`)
}
for (const p of images) {
  try {
    const meta = await sharp(p).metadata()
    if (meta.format !== 'webp' || !meta.width || !meta.height) throw Error('无效 WebP')
    if (p.includes('/gallery/') && !path.basename(p).startsWith('t') && Math.max(meta.width, meta.height) > 1400) throw Error('主图超过 1400px')
    if (p.includes('/gallery/') && path.basename(p).startsWith('t') && Math.max(meta.width, meta.height) > 360) throw Error('缩略图超过 360px')
  } catch (e) { errors.push(`${p}: ${e.message}`) }
}
for (const p of models) {
  try {
    const data = fs.readFileSync(p)
    if (data.toString('ascii', 0, 4) !== 'glTF' || data.readUInt32LE(4) !== 2 || data.readUInt32LE(8) !== data.length) throw Error('GLB 头或长度不正确')
    const json = JSON.parse(data.subarray(20, 20 + data.readUInt32LE(12)).toString('utf8'))
    for (const item of [...(json.buffers || []), ...(json.images || [])]) if (item.uri && !item.uri.startsWith('data:')) throw Error(`仍依赖外部文件：${item.uri}`)
  } catch (e) { errors.push(`${p}: ${e.message}`) }
}
if (models.length && !fs.existsSync(`${tree}/vendor/meshopt_decoder.js`)) errors.push('缺少 meshopt 解码器')
if (tree === 'dist') {
  const html = fs.readFileSync('dist/index.html', 'utf8')
  if (!html.includes('/u/wangyifei/heritage/assets/')) errors.push('部署前缀不正确')
  for (const f of fs.readdirSync('dist/assets').filter(f => f.endsWith('.js'))) {
    if (/sk-[A-Za-z0-9]{16,}/.test(fs.readFileSync(`dist/assets/${f}`, 'utf8'))) errors.push(`疑似 API Key：${f}`)
  }
}
let bytes = 0, files = 0
function walk(dir) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name)
    if (e.isDirectory()) walk(p)
    else if (e.isFile()) { bytes += fs.statSync(p).size; files++ }
  }
}
walk(tree)
console.log(JSON.stringify({ tree, gallerySites: Object.keys(GALLERY).length, galleryImages: Object.values(GALLERY).reduce((a,b)=>a+b,0), modelSites: Object.keys(LOCAL_MODELS).length, models: models.length, checkedImages: images.length, files, bytes, errors }, null, 2))
process.exitCode = errors.length ? 1 : 0
