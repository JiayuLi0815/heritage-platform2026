/* ============================================================
   名录事实层
   ------------------------------------------------------------
   sites-all.json 是 991 处的全量名录，但直接把 991 行铺进 prompt
   又太长（每问一次约两万 token）。这里做两件事：

   1. catalogBrief()——一小段静态统计（数量、年份区间、大洲、国家
      排名），每次对话都带上，模型就不会说「我这儿没有完整清单」；
   2. catalogFacts(q)——只在问题问到年份、国家、大洲时，现算一小段
      相关条目，把准确名单交到模型手里。

   本地知识检索层（DigitalHuman.jsx 的 answer()）也复用这里，
   大模型连不上时同样能答「最早的是哪批」。
   ============================================================ */

import RAW from '../data/sites-all.json'
import { continentOf } from '../data/countries.js'

/* UNESCO 1978 年首批列入 12 处；本名录只收文化与混合遗产，对应其中 8 处。 */
const FIRST_TOTAL = 12

/* 名录里的国名与口语说法不一致的，补几条 */
const C_ALIAS = {
  英国: '大不列颠及北爱尔兰联合王国',
  大不列颠: '大不列颠及北爱尔兰联合王国',
  俄国: '俄罗斯',
  美国: '美国',
}

const firstCountry = (s) => (s.country || '').split(/[,，]/)[0].trim()
const byYearOf = (s) => Number(s.year) || 0

let IDX = null
function index() {
  if (IDX) return IDX
  const byYear = new Map()
  const byCountry = new Map()
  const byCont = {}
  for (const s of RAW) {
    const y = byYearOf(s)
    if (y) {
      if (!byYear.has(y)) byYear.set(y, [])
      byYear.get(y).push(s)
    }
    const c = firstCountry(s)
    if (c) byCountry.set(c, (byCountry.get(c) || 0) + 1)
    const cont = continentOf(c)
    if (cont) byCont[cont] = (byCont[cont] || 0) + 1
  }
  const years = [...byYear.keys()].sort((a, b) => a - b)
  const top = [...byCountry.entries()].sort((a, b) => b[1] - a[1]).slice(0, 6)
  IDX = {
    byYear,
    byCountry,
    byCont,
    years,
    top,
    min: years[0],
    max: years[years.length - 1],
    countries: byCountry.size,
  }
  return IDX
}

const names = (arr, cap = 20) =>
  arr.length <= cap
    ? arr.map((s) => s.name).join('、')
    : arr.slice(0, cap).map((s) => s.name).join('、') + ` 等 ${arr.length} 处`

const firstBatch = () => {
  const i = index()
  return i.byYear.get(i.min) || []
}
const lastBatch = () => {
  const i = index()
  return i.byYear.get(i.max) || []
}

/** 每次对话都带的静态统计 */
export function catalogBrief() {
  const i = index()
  const cont = Object.entries(i.byCont)
    .sort((a, b) => b[1] - a[1])
    .map(([k, v]) => `${k} ${v}`)
    .join('、')
  return (
    `平台名录收录 ${RAW.length} 处文化与混合遗产（自然遗产不在收录范围），` +
    `覆盖 ${i.countries} 个国家/地区，列入年份 ${i.min}–${i.max}。` +
    `大洲分布：${cont}。` +
    `名录最多的国家：${i.top.map(([k, v]) => `${k} ${v}`).join('、')}。`
  )
}

/** 问年份时用的那一段 */
export function yearFact() {
  const i = index()
  const first = firstBatch()
  const last = lastBatch()
  const prev = i.years[i.years.length - 2]
  return (
    `列入最早的是 ${i.min} 年：UNESCO 那一年首批列入 ${FIRST_TOTAL} 处，` +
    `其中文化与混合遗产 ${first.length} 处，即名录里年份最早的这批——${names(first)}。` +
    `另 4 处是自然遗产（加拉帕戈斯群岛、锡门国家公园、纳汉尼国家公园、黄石国家公园），不在本平台名录范围内。` +
    `名录现有数据收录到 ${i.max} 年，那一年列入 ${last.length} 处：${names(last, 6)}` +
    (prev ? `；此前一批是 ${prev} 年。` : '。')
  )
}

/** 点名某个国家时，按国家取条目 */
export function countryOfQuery(q) {
  for (const [alias, full] of Object.entries(C_ALIAS)) {
    if (q.includes(alias)) return full
  }
  const i = index()
  let hit = null
  for (const c of i.byCountry.keys()) {
    if (q.includes(c) && (!hit || c.length > hit.length)) hit = c
  }
  return hit
}

export function countryFact(c) {
  if (!c) return ''
  const list = RAW.filter((s) => firstCountry(s) === c)
  if (!list.length) return ''
  const ys = list.map(byYearOf).filter(Boolean).sort((a, b) => a - b)
  const at = (y) => list.filter((s) => byYearOf(s) === y)
  return (
    `${c}在名录里有 ${list.length} 处；最早 ${ys[0]} 年列入（${names(at(ys[0]), 6)}），` +
    `最近 ${ys[ys.length - 1]} 年列入（${names(at(ys[ys.length - 1]), 6)}）。`
  )
}

const CONTINENTS = ['亚洲', '欧洲', '非洲', '北美洲', '南美洲', '大洋洲', '美洲']

export function continentFact(q) {
  const hit = CONTINENTS.find((k) => q.includes(k))
  if (!hit) return ''
  const i = index()
  const grp = hit === '美洲' ? ['北美洲', '南美洲'] : [hit]
  const n = grp.reduce((a, k) => a + (i.byCont[k] || 0), 0)
  if (!n) return ''
  const tops = [...i.byCountry.entries()]
    .filter(([c]) => grp.includes(continentOf(c)))
    .sort((a, b) => b[1] - a[1])
    .slice(0, 5)
  return `${hit}共 ${n} 处；其中最多的国家：${tops.map(([k, v]) => `${k} ${v}`).join('、')}。`
}

export const YEAR_RE = /最早|首批|第一批|最晚|最新|最迟|哪一年|哪年|什么时候列入|什么时候进|列入年份/

/** 问题问到年份/国家/大洲时，现算的那一段；没有命中的返回空串 */
export function catalogFacts(q) {
  const parts = []
  if (YEAR_RE.test(q)) parts.push(yearFact())
  const c = countryOfQuery(q)
  if (c) parts.push(countryFact(c))
  const cont = continentFact(q)
  if (cont) parts.push(cont)
  return parts.join('\n')
}

/** 本地知识检索层的回答：最早/最新一批 */
export function yearAnswer() {
  const i = index()
  const first = firstBatch()
  const last = lastBatch()
  return (
    `名录里列入最早的是 ${i.min} 年那一批，文化类共 ${first.length} 处：${names(first)}。\n` +
    `UNESCO 1978 年首批实际列入 ${FIRST_TOTAL} 处，另外 4 处是自然遗产（加拉帕戈斯群岛、锡门国家公园、纳汉尼国家公园、黄石国家公园），本平台只收文化与混合遗产，所以不在名录里。\n` +
    `名录数据收录到 ${i.max} 年，那一年列入 ${last.length} 处，例如${names(last, 4)}。`
  )
}
