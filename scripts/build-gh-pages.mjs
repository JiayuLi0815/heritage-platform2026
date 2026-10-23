/* GitHub Pages 专用的精简构建。
   板块：完整版 1.8 GB，超过 Pages 单站 1 GB 的上限，所以这里只带
   —— 全部 991 处的封面、精选 20 处的画廊、全部三维模型、底图与 vendor。
   被裁掉的是 960 多处的画廊（1.2 GB），它们的详情页照常打开，只是没有相册。

   产物在 dist-gh/，源码仓库的 public/ 原样不动。
   用法：node scripts/build-gh-pages.mjs
*/
import { execFileSync } from 'node:child_process'
import fs from 'node:fs'
import path from 'node:path'

const ROOT = path.resolve(import.meta.dirname, '..')
const PUBLIC = path.join(ROOT, 'public')
const TMP_PUBLIC = path.join(ROOT, 'public-gh')
const OUT = path.join(ROOT, 'dist-gh')
const GALLERY_JS = path.join(ROOT, 'src/data/gallery.js')

const link = (src, dest) => fs.linkSync(src, dest) // 硬链接，不占额外空间
function linkTree(src, dest) {
  fs.mkdirSync(dest, { recursive: true })
  for (const e of fs.readdirSync(src, { withFileTypes: true })) {
    const s = path.join(src, e.name)
    const d = path.join(dest, e.name)
    if (e.isDirectory()) linkTree(s, d)
    else link(s, d)
  }
}

const { SITES } = await import(path.join(ROOT, 'src/data/sites.js'))
const { FEATURED_BY_WHC } = await import(path.join(ROOT, 'src/data/whc-featured.js'))
const slugs = new Set(SITES.map((s) => s.id))
const featured = Object.entries(FEATURED_BY_WHC)
  .filter(([, slug]) => slugs.has(slug))
  .map(([id]) => id)

console.log(`==> 精简素材：封面 + 精选 ${featured.length} 处画廊 + 三维模型`)
fs.rmSync(TMP_PUBLIC, { recursive: true, force: true })
fs.mkdirSync(TMP_PUBLIC, { recursive: true })
for (const dir of ['geo', 'vendor', 'models']) {
  linkTree(path.join(PUBLIC, dir), path.join(TMP_PUBLIC, dir))
}
fs.mkdirSync(path.join(TMP_PUBLIC, 'img'), { recursive: true })
for (const f of fs.readdirSync(path.join(PUBLIC, 'img'))) {
  if (f === 'gallery') continue
  const s = path.join(PUBLIC, 'img', f)
  const d = path.join(TMP_PUBLIC, 'img', f)
  if (fs.statSync(s).isDirectory()) linkTree(s, d)
  else link(s, d)
}
fs.mkdirSync(path.join(TMP_PUBLIC, 'img/gallery'), { recursive: true })
for (const id of featured) {
  const s = path.join(PUBLIC, 'img/gallery', id)
  if (fs.existsSync(s)) linkTree(s, path.join(TMP_PUBLIC, 'img/gallery', id))
}

// 画廊张数表同步裁到精选：否则点开未被收录的条目会去请求不存在的图片
const gsrc = fs.readFileSync(GALLERY_JS, 'utf8')
const backup = `${GALLERY_JS}.build-gh.bak`
fs.writeFileSync(backup, gsrc)
const m = gsrc.match(/export const GALLERY = (\{[\s\S]*?\})\n/)
if (!m) throw new Error('gallery.js 格式变了，没找到 GALLERY 对象')
const full = JSON.parse(m[1])
const slim = Object.fromEntries(featured.filter((id) => full[id]).map((id) => [id, full[id]]))
fs.writeFileSync(
  GALLERY_JS,
  gsrc.replace(m[1], JSON.stringify(slim).replace(/"/g, '"'))
)

/* GitHub 上的产物是公开的，Key 不能进 bundle：构建期间把 .env.local 挪开，
   改走服务器上的代理（VITE_DEEPSEEK_PROXY）。 */
const ENV_LOCAL = path.join(ROOT, '.env.local')
const ENV_BACKUP = path.join(ROOT, '.env.local.build-gh.bak')
const hadEnv = fs.existsSync(ENV_LOCAL)
if (hadEnv) fs.renameSync(ENV_LOCAL, ENV_BACKUP)

try {
  console.log('==> vite build → dist-gh/')
  execFileSync('npx', ['vite', 'build', '--outDir', 'dist-gh', '--emptyOutDir'], {
    cwd: ROOT,
    stdio: 'inherit',
    env: {
      ...process.env,
      VITE_GH_PAGES: '1',
      VITE_DEEPSEEK_PROXY: process.env.VITE_DEEPSEEK_PROXY || 'https://www.jiayu98518.work/dhapi',
      VITE_DEEPSEEK_API_KEY: '',
    },
  })
} finally {
  fs.copyFileSync(backup, GALLERY_JS)
  fs.rmSync(backup, { force: true })
  fs.rmSync(TMP_PUBLIC, { recursive: true, force: true })
  if (hadEnv) fs.renameSync(ENV_BACKUP, ENV_LOCAL)
}

// 出包前把 Key 查一遍：公开产物里不该出现任何 sk- 开头的串
const leaked = fs
  .readdirSync(path.join(OUT, 'assets'))
  .filter((f) => f.endsWith('.js'))
  .filter((f) => /sk-[A-Za-z0-9]{16,}/.test(fs.readFileSync(path.join(OUT, 'assets', f), 'utf8')))
if (leaked.length) throw new Error(`产物里发现疑似 Key：${leaked.join(', ')}`)
console.log('==> 已确认产物中不含 API Key')

const size = (p) => {
  let t = 0
  let n = 0
  const walk = (q) => {
    for (const e of fs.readdirSync(q, { withFileTypes: true })) {
      const f = path.join(q, e.name)
      if (e.isDirectory()) walk(f)
      else { t += fs.statSync(f).size; n++ }
    }
  }
  walk(p)
  return [t, n]
}
const total = size(OUT)
console.log(`完成：${(total[0] / 1e6).toFixed(0)} MB / ${total[1]} 个文件 → ${OUT}`)
