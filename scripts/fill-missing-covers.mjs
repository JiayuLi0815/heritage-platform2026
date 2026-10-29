import fs from 'node:fs'
import sharp from 'sharp'
const sites = JSON.parse(fs.readFileSync('src/data/sites-all.json', 'utf8'))
const missing = []
fs.mkdirSync('public/img/sites', { recursive: true })
for (const s of sites) {
  const out = `public/img/sites/${s.id}.webp`
  if (fs.existsSync(out)) continue
  const text = s.name.replace(/[<>&"']/g, '')
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800"><rect width="1200" height="800" fill="#e9eeeb"/><text x="600" y="360" text-anchor="middle" font-family="Microsoft YaHei,sans-serif" font-size="38" fill="#52655a">影像待补充</text><text x="600" y="440" text-anchor="middle" font-family="Microsoft YaHei,sans-serif" font-size="24" fill="#52655a">UNESCO ${s.id}</text></svg>`
  await sharp(Buffer.from(svg)).webp({ quality: 70 }).toFile(out)
  missing.push({ id: s.id, name: text })
}
fs.mkdirSync('work', { recursive: true })
fs.writeFileSync('work/missing-covers.json', JSON.stringify(missing, null, 2))
console.log(JSON.stringify({ placeholders: missing }, null, 2))
