import { useEffect, useMemo, useRef, useState } from 'react'
import RAW from '../data/sites-all.json'
import { FEATURED_BY_WHC } from '../data/whc-featured.js'
import { LOCAL_MODELS } from '../data/models.js'
import { GALLERY } from '../data/gallery.js'
import { continentOf } from '../data/countries.js'
import { asset } from '../asset.js'
import { IcSearch, IcGlobe } from '../components/icons.jsx'
import MapLandmarks from '../components/MapLandmarks.jsx'
import { HeritageGlobe } from './Home.jsx'
import './MapPage.css'

/* 统一地图：平面 / 3D 共用筛选、搜索和详情入口。
   平面地图放大后按屏幕网格聚合点位，页面仅保留地图画布和控件。 */

const W = 1000
const H = 500
const K_MIN = 1
const K_MAX = 22
/* 合并网格的边长。必须大于「两个相邻合并圈半径之和」，
   否则欧洲那种密集区会糊成一团、数字互相压住。 */
const CELL = 52

const projX = (lng) => ((lng + 180) * W) / 360
const projY = (lat) => ((90 - lat) * H) / 180

const ringToPath = (ring) =>
  ring.map(([lng, lat], i) => `${i ? 'L' : 'M'}${projX(lng).toFixed(1)},${projY(lat).toFixed(1)}`).join('') + 'Z'

const geomToPath = (g) => {
  if (!g) return ''
  if (g.type === 'Polygon') return g.coordinates.map(ringToPath).join('')
  if (g.type === 'MultiPolygon') return g.coordinates.map((p) => p.map(ringToPath).join('')).join('')
  return ''
}

const clamp = (v, a, b) => Math.min(b, Math.max(a, v))
const median = (a) => {
  const b = [...a].sort((p, q) => p - q)
  return b[Math.floor(b.length / 2)]
}

/* ------------------------------------------------------------
   地名标注：只做层级，不做详图。
   lv1 = 大洲与大洋，看全球时出现；lv2 = 区域，放大一点才出现。
   都压得很淡，作用是给一堆圈圈一个方位感，不是当底图注记读。
   ------------------------------------------------------------ */
const LABELS = [
  // —— 大洲 ——
  { t: '北美洲', lng: -103.4, lat: 49.5, lv: 1 },
  { t: '南美洲', lng: -60, lat: -9.3, lv: 1 },
  { t: '欧洲', lng: 18, lat: 69.1, lv: 1 },
  { t: '非洲', lng: 22.9, lat: 3.4, lv: 1 },
  { t: '亚洲', lng: 92, lat: 52.8, lv: 1 },
  { t: '大洋洲', lng: 134, lat: -19.2, lv: 1 },
  { t: '南极洲', lng: 10, lat: -76, lv: 1 },
  // —— 大洋 ——
  { t: '太平洋', lng: -148, lat: 2, lv: 1, oc: true },
  { t: '大西洋', lng: -30, lat: 26, lv: 1, oc: true },
  { t: '印度洋', lng: 75, lat: -22, lv: 1, oc: true },
  { t: '北冰洋', lng: 0, lat: 80, lv: 1, oc: true },
  { t: '南大洋', lng: 0, lat: -58, lv: 1, oc: true },
  // —— 区域（第二层）——
  { t: '西欧', lng: 2, lat: 48, lv: 2 },
  { t: '北欧', lng: 17, lat: 64, lv: 2 },
  { t: '东欧', lng: 35, lat: 55, lv: 2 },
  { t: '地中海', lng: 18, lat: 35, lv: 2, oc: true },
  { t: '中东', lng: 45, lat: 27, lv: 2 },
  { t: '中亚', lng: 68, lat: 44, lv: 2 },
  { t: '南亚', lng: 79, lat: 21, lv: 2 },
  { t: '东亚', lng: 112, lat: 37, lv: 2 },
  { t: '东南亚', lng: 104, lat: 12, lv: 2 },
  { t: '西伯利亚', lng: 100, lat: 63, lv: 2 },
  { t: '北非', lng: 12, lat: 24, lv: 2 },
  { t: '西非', lng: -4, lat: 10, lv: 2 },
  { t: '东非', lng: 38, lat: 2, lv: 2 },
  { t: '南部非洲', lng: 25, lat: -24, lv: 2 },
  { t: '北美东部', lng: -80, lat: 39, lv: 2 },
  { t: '北美西部', lng: -116, lat: 41, lv: 2 },
  { t: '中美洲', lng: -88, lat: 15, lv: 2 },
  { t: '加勒比', lng: -73, lat: 18, lv: 2, oc: true },
  { t: '安第斯', lng: -70, lat: -20, lv: 2 },
  { t: '南美南部', lng: -62, lat: -34, lv: 2 },
  { t: '澳新', lng: 146, lat: -31, lv: 2 },
]

/* 两层标注各自有出现和退场的区间：洲名在放大后被区域名接替，
   再放大就全退掉——近看一处遗产时，地名只会碍事。 */
const labelOpacity = (lv, k) =>
  lv === 1
    ? clamp((2.5 - k) / 0.7, 0, 1)
    : clamp((k - 1.7) / 0.6, 0, 1) * clamp((5.4 - k) / 1.1, 0, 1)

/* ============================================================
   可搜索的地名
   ------------------------------------------------------------
   大洲、大洋都做成可以直接搜的「地方」：输入亚洲、太平洋，
   地图飞过去，底下列表换成那一片范围里的遗产。
   box = [南纬, 西经, 北纬, 东经]，fit 到这块框；
   没有 box 的（大洋、区域）给中心点与缩放倍数。
   ============================================================ */
const PLACE_BOX = {
  亚洲: [-9, 26, 78, 180],
  欧洲: [34, -26, 72, 46],
  非洲: [-35.5, -18, 37.5, 52],
  北美洲: [6, -170, 84, -50],
  南美洲: [-56, -82, 13, -34],
  美洲: [-56, -170, 84, -34],
  大洋洲: [-50, 110, -9, 180],
  南极洲: [-90, -180, -58, 180],
}

const PLACE_ALIAS = {
  亚洲: ['亚细亚', 'asia'],
  欧洲: ['欧罗巴', 'europe'],
  非洲: ['阿非利加', 'africa'],
  美洲: ['南北美洲', 'americas'],
  北美洲: ['北美', 'north america'],
  南美洲: ['南美', 'south america'],
  大洋洲: ['澳洲', 'oceania'],
  南极洲: ['南极', 'antarctica'],
  太平洋: ['pacific', 'pacific ocean'],
  大西洋: ['atlantic', 'atlantic ocean'],
  印度洋: ['indian ocean'],
  北冰洋: ['北极洋', 'arctic', 'arctic ocean'],
  南大洋: ['南冰洋', 'southern ocean'],
  地中海: ['mediterranean'],
}

const PLACES = [
  ...Object.entries(PLACE_BOX).map(([t, box]) => ({ t, box })),
  /* 大洋给 2 倍：再小，边缘一夹，画面中心就落到岸上去了；
     北冰洋贴着图顶，得再近一点才看得见冰缘 */
  ...LABELS.filter((l) => !PLACE_BOX[l.t]).map((l) => ({
    t: l.t,
    center: [l.lat, l.lng],
    k: l.lv === 1 ? (l.oc ? (l.t === '北冰洋' ? 3 : 2) : 2.1) : 3.4,
  })),
]

/* 去掉空白与常见标点再比：输入「马 丘」「马丘，」都能对上 */
const norm = (s) => (s || '').toLowerCase().replace(/[\s·・,，.。、'’"“”\-—–_()（）:：;；!！?？]/g, '')

/* 模糊匹配：相等 > 开头 > 包含 > 按顺序命中所有字
   （最后一档让「莫高」也能找到「敦煌莫高窟」）。返回越小越靠前。 */
function matchScore(qn, text) {
  const n = norm(text)
  if (!n || !qn) return null
  if (n === qn) return 0
  if (n.startsWith(qn)) return 1
  const i = n.indexOf(qn)
  if (i >= 0) return 2 + Math.min(i, 8) * 0.01
  /* 跳字匹配只许隔一个字：「莫高」能到敦煌莫高窟，
     但「埃及」不该命中「皮埃蒙特及……」这种两个字恰好挨着的名字 */
  let p = 0
  let first = -1
  let last = -1
  for (let i = 0; i < n.length; i++) {
    if (n[i] !== qn[p]) continue
    if (p === 0) first = i
    last = i
    p++
    if (p === qn.length) break
  }
  return p === qn.length && qn.length >= 2 && last - first <= qn.length ? 3 : null
}

/* 返回 { p, exact }：输入正好是地名本身时 exact 为真。
   「欧洲」既是洲名，也是一处遗产名的开头（欧洲温泉疗养胜地），
   这时候要按洲算，不能被那处遗产抢走。 */
function findPlace(qn) {
  let best = null
  for (const p of PLACES) {
    for (const name of [p.t, ...(PLACE_ALIAS[p.t] || [])]) {
      const nn = norm(name)
      if (nn === qn) return { p, exact: true }
      if (qn.length >= 2 && (nn.startsWith(qn) || qn.startsWith(nn))) {
        if (!best || nn.length < best.len) best = { p, len: nn.length }
      }
    }
  }
  return best ? { p: best.p, exact: false } : null
}

// 有三维的一律标蓝：精选 20 处是外链 Sketchfab，剩下的十几处是本地压出来的 glb。
// 两种来源对用户来说是同一件事——点进去能转，所以图上不给它们分颜色。
// 没有三维、但有素材盘影像的，是常态（982 处）；两样都没有的只有 9 处，画得更淡。
const ALL = RAW.map((s) => {
  const featured = FEATURED_BY_WHC[s.id] || null
  const nImg = GALLERY[s.id] || 0
  return {
    ...s,
    x: projX(s.lng),
    y: projY(s.lat),
    featured,
    nImg,
    has3d: Boolean(featured || LOCAL_MODELS[s.id]),
    hasImg: nImg > 0,
  }
})
const N3D = ALL.filter((s) => s.has3d).length
const CATEGORIES = ['全部', '文化', '自然', '混合']

function CategoryIcon({ category }) {
  if (category === '全部') return <IcGlobe w={29} />
  return <svg viewBox="0 0 32 32" width="29" height="29" fill="none" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    {category === '文化' ? <><path d="M3 11 16 4l13 7ZM4 27h24M3 30h26M6 14v10m6-10v10m8-10v10m6-10v10"/></> : category === '自然' ? <><path d="M6 26C0 9 18 5 28 3c-1 18-9 27-22 23ZM4 30 23 9M10 22l-1-8m7 2 7-1"/></> : <><circle cx="16" cy="10" r="7"/><circle cx="9" cy="22" r="7"/><circle cx="23" cy="22" r="7"/></>}
  </svg>
}

/* ------------------------------------------------------------
   国家
   ------------------------------------------------------------
   搜「中国」「美国」这类国家名，镜头要按这个国家的所有落点取景，
   而不是跳到第一处遗产——否则停在哪儿、放多大全看巧不巧。
   跨境遗产按原表里的第一个国家算（与列表里显示的一致）。
   ------------------------------------------------------------ */
const COUNTRY_ALIAS = {
  英国: '大不列颠及北爱尔兰联合王国',
  俄国: '俄罗斯',
  朝鲜: '朝鲜民主主义人民共和国',
}

const COUNTRIES = (() => {
  const m = new Map()
  for (const s of ALL) {
    const name = (s.country || '').split(/[,，]/)[0].trim()
    if (!name) continue
    if (!m.has(name)) m.set(name, { name, sites: [], en: new Set() })
    const o = m.get(name)
    o.sites.push(s)
    for (const e of s.countries || []) o.en.add(e)
  }
  return [...m.values()]
})()

/* 国家名（含英文名与俗称）对上输入。完全相等才算准，
   其余只做「没搜到遗产时」的兜底。 */
function findCountry(qn) {
  for (const [k, v] of Object.entries(COUNTRY_ALIAS)) {
    if (qn !== norm(k)) continue
    const c = COUNTRIES.find((x) => x.name === v)
    if (c) return { c, exact: true }
  }
  let best = null
  for (const c of COUNTRIES) {
    for (const n of [c.name, ...c.en]) {
      const nn = norm(n)
      if (!nn) continue
      if (nn === qn) return { c, exact: true }
      if (qn.length >= 2 && (nn.startsWith(qn) || qn.startsWith(nn))) {
        if (!best || nn.length < best.len) best = { c, len: nn.length }
      }
    }
  }
  return best ? { c: best.c, exact: false } : null
}

export default function MapPage({ onOpenSite, setView = () => {}, initialMode = 'flat' }) {
  const [geo, setGeo] = useState(null)
  const [failed, setFailed] = useState(false)
  const [hover, setHover] = useState(null)
  const [q, setQ] = useState('')
  const [viewport, setViewport] = useState({ k: 1, x: 0, y: 0 })
  // 修改：平面与 3D 共用页面、搜索和筛选状态，切换时不丢失当前条件。
  const [mode, setMode] = useState(initialMode)
  const [continent, setContinent] = useState('全部')
  const [country, setCountry] = useState('全部')
  const [globeZoom, setGlobeZoom] = useState(1)
  const [globeTarget, setGlobeTarget] = useState(null)
  const [category, setCategory] = useState('全部')
  const [showLandmarks, setShowLandmarks] = useState(true)
  const [textureFailed, setTextureFailed] = useState(false)
  const countryOptions = useMemo(() => COUNTRIES.filter(c => continent === '全部' || continentOf(c.name) === continent).sort((a,b) => a.name.localeCompare(b.name, 'zh')), [continent])
  const filteredSites = useMemo(() => {
    const query = norm(q)
    const place = query ? findPlace(query) : null
    return ALL.filter(s => {
      if (category !== '全部' && s.category !== category) return false
      if (continent !== '全部' && continentOf(s.country) !== continent) return false
      if (country !== '全部' && s.country.split(/[,，]/)[0].trim() !== country) return false
      if (!query) return true
      if (place?.exact && PLACE_BOX[place.p.t]) return place.p.t === '美洲' ? ['北美洲','南美洲'].includes(continentOf(s.country)) : continentOf(s.country) === place.p.t
      if (place) return true
      return [s.name, s.nameEn, s.country, ...(s.countries || [])].some(t => matchScore(query, t) !== null)
        || (norm(s.name).length >= 2 && query.includes(norm(s.name)))
    })
  }, [category, continent, country, q])
  const globeViewportRef = useRef(null)
  const updateGlobeZoom = value => setGlobeZoom(clamp(value, .7, 3.5))
  useEffect(() => {
    const el = globeViewportRef.current
    if (!el || mode !== 'globe') return
    const wheel = e => { e.preventDefault(); setGlobeZoom(v => clamp(v * Math.exp(-e.deltaY * .0015), .7, 3.5)) }
    el.addEventListener('wheel', wheel, { passive: false })
    return () => el.removeEventListener('wheel', wheel)
  }, [mode])

  const svgRef = useRef(null)
  const drag = useRef(null)
  const pointers = useRef(new Map())
  const pinch = useRef(null)
  const hit = useRef(null) // 按下时命中的节点动作；抬起时若没拖动就执行

  /* 地图在窄屏上只有三百多像素宽，13 个用户单位的字落到屏幕上只剩四五个像素。
     量出「每用户单位多少 CSS 像素」，按屏幕尺寸反算字号，地名在任何宽度下都读得出来。 */
  const [ppu, setPpu] = useState(1)
  useEffect(() => {
    const el = svgRef.current
    if (!el || typeof ResizeObserver === 'undefined') return
    const ro = new ResizeObserver(() => {
      const w = el.getBoundingClientRect().width
      if (w > 0) setPpu(w / W)
    })
    ro.observe(el)
    return () => ro.disconnect()
  }, [])

  useEffect(() => {
    fetch(asset('geo/world.geo.json'))
      .then((r) => { if (!r.ok) throw new Error('Map unavailable'); return r.json() })
      .then(setGeo)
      .catch(() => setFailed(true))
  }, [])

  const landPath = useMemo(
    () => (geo?.features ? geo.features.map((f) => geomToPath(f.geometry)).join('') : ''),
    [geo]
  )

  const graticule = useMemo(() => {
    const lines = []
    for (let lng = -180; lng <= 180; lng += 30) {
      const x = projX(lng)
      lines.push(<line key={`v${lng}`} className="map-grat" x1={x} y1="0" x2={x} y2={H} />)
    }
    for (let lat = -60; lat <= 60; lat += 30) {
      const y = projY(lat)
      lines.push(<line key={`h${lat}`} className="map-grat" x1="0" y1={y} x2={W} y2={y} />)
    }
    return lines
  }, [])

  /* ---------- 视图变换 ---------- */
  const clampView = (v) => {
    const k = clamp(v.k, K_MIN, K_MAX)
    return {
      k,
      x: clamp(v.x, W - W * k, 0),
      y: clamp(v.y, H - H * k, 0),
    }
  }

  const zoomAt = (factor, [px, py]) => {
    setViewport((v) => {
      const k2 = clamp(v.k * factor, K_MIN, K_MAX)
      const wx = (px - v.x) / v.k
      const wy = (py - v.y) / v.k
      return clampView({ k: k2, x: px - wx * k2, y: py - wy * k2 })
    })
  }

  const reset = () => setViewport({ k: 1, x: 0, y: 0 })

  /* ---------- 飞行 ----------
     搜索定位不是硬切：中心点平移、缩放按几何级数走，
     拖拽或滚轮一碰就取消，不跟用户抢镜头。 */
  const viewportRef = useRef(viewport)
  viewportRef.current = viewport
  const raf = useRef(0)
  const noMotion =
    typeof window !== 'undefined' &&
    window.matchMedia?.('(prefers-reduced-motion: reduce)').matches

  const flyTo = (to) => {
    if (!to) return
    const from = viewportRef.current
    cancelAnimationFrame(raf.current)
    if (noMotion) {
      setViewport(to)
      return
    }
    const f = { cx: (W / 2 - from.x) / from.k, cy: (H / 2 - from.y) / from.k, k: from.k }
    const t = { cx: (W / 2 - to.x) / to.k, cy: (H / 2 - to.y) / to.k, k: to.k }
    const t0 = performance.now()
    const D = 560
    const step = (now) => {
      const p = Math.min(1, (now - t0) / D)
      const e = 1 - Math.pow(1 - p, 3)
      const k = f.k * Math.pow(t.k / f.k, e)
      const cx = f.cx + (t.cx - f.cx) * e
      const cy = f.cy + (t.cy - f.cy) * e
      setViewport(clampView({ k, x: W / 2 - cx * k, y: H / 2 - cy * k }))
      if (p < 1) raf.current = requestAnimationFrame(step)
    }
    raf.current = requestAnimationFrame(step)
  }

  useEffect(() => () => cancelAnimationFrame(raf.current), [])

  /* 一处遗产：把它放到画面中心；已经放大过就保持当前倍数 */
  const viewForSite = (s, curK) => {
    const k = clamp(Math.max(curK, 8), K_MIN, K_MAX)
    return clampView({ k, x: W / 2 - s.x * k, y: H / 2 - s.y * k })
  }

  /* 一个地方：有框就框进去，没有框就按中心点定倍 */
  const viewForPlace = (p) => {
    if (p.box) {
      const [la1, ln1, la2, ln2] = p.box
      const wpx = ((ln2 - ln1) / 360) * W
      const hpx = ((la2 - la1) / 180) * H
      const k = clamp(Math.min(W / wpx, H / hpx) * 0.92, K_MIN, K_MAX)
      const cx = projX((ln1 + ln2) / 2)
      const cy = projY((la1 + la2) / 2)
      return clampView({ k, x: W / 2 - cx * k, y: H / 2 - cy * k })
    }
    const k = clamp(p.k || 3, K_MIN, K_MAX)
    const cx = projX(p.center[1])
    const cy = projY(p.center[0])
    return clampView({ k, x: W / 2 - cx * k, y: H / 2 - cy * k })
  }

  /* 一个国家：框它的落点。
     直接框全部会出事——英国的百慕大在大西洋那头、法国的塔普塔普阿泰
     在太平洋、智利的复活节岛离本土 3800 公里，一框镜头就跑到两洲之间。
     也不能简单地按分位数剪：澳大利亚从西海岸到弗雷泽岛本来就摊开 40°，
     剪了会漏掉 Shark Bay。
     区分两者的办法是看「断开」：把坐标排好，若存在一道缝，缝的一侧
     点少、且宽度超过全幅一半，那一侧就是飞地，丢掉，再查下一道缝。 */
  const coreRange = (vals) => {
    let a = [...vals].sort((p, q) => p - q)
    for (let t = 0; t < 3 && a.length > 2; t++) {
      let gi = -1
      let g = 0
      for (let i = 1; i < a.length; i++) {
        const d = a[i] - a[i - 1]
        if (d > g) {
          g = d
          gi = i
        }
      }
      const span = a[a.length - 1] - a[0]
      if (!span || g <= span * 0.5) break
      const left = gi
      const right = a.length - gi
      if (left === right) break
      a = left > right ? a.slice(0, gi) : a.slice(gi)
    }
    return [a[0], a[a.length - 1]]
  }

  const viewForCountry = (c) => {
    const pts = c.sites
    if (!pts.length) return null
    const [bx0, bx1] = coreRange(pts.map((p) => p.x))
    const [by0, by1] = coreRange(pts.map((p) => p.y))
    const core = pts.filter((p) => p.x >= bx0 && p.x <= bx1 && p.y >= by0 && p.y <= by1)
    const use = core.length ? core : pts

    let x0 = Infinity
    let x1 = -Infinity
    let y0 = Infinity
    let y1 = -Infinity
    for (const p of use) {
      x0 = Math.min(x0, p.x)
      x1 = Math.max(x1, p.x)
      y0 = Math.min(y0, p.y)
      y1 = Math.max(y1, p.y)
    }
    const PAD = 0.16
    const w = Math.max(x1 - x0, W * 0.02)
    const h = Math.max(y1 - y0, H * 0.04)
    const k = clamp(Math.min((W * (1 - 2 * PAD)) / w, (H * (1 - 2 * PAD)) / h), K_MIN, 12)
    const cx = (x0 + x1) / 2
    const cy = (y0 + y1) / 2
    return clampView({ k, x: W / 2 - cx * k, y: H / 2 - cy * k })
  }

  const toSvg = (e) => {
    const r = svgRef.current.getBoundingClientRect()
    return [((e.clientX - r.left) / r.width) * W, ((e.clientY - r.top) / r.height) * H]
  }

  // 滚轮缩放：必须非 passive 才能挡住页面滚动
  useEffect(() => {
    const el = svgRef.current
    if (!el) return
    const onWheel = (e) => {
      e.preventDefault()
      const r = el.getBoundingClientRect()
      const p = [((e.clientX - r.left) / r.width) * W, ((e.clientY - r.top) / r.height) * H]
      zoomAt(Math.exp(-e.deltaY * 0.0016), p)
    }
    el.addEventListener('wheel', onWheel, { passive: false })
    return () => el.removeEventListener('wheel', onWheel)
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  /* ---------- 拖拽 / 双指 ---------- */
  // 指针被 svg 捕获后，pointerup 只会派发给 svg，节点自身的 onClick 收不到。
  // 所以按下时先把「点中的是谁」记下来，抬起时若几乎没移动就当作点击。
  const onPointerDown = (e) => {
    cancelAnimationFrame(raf.current) // 手一碰就停：不跟用户抢镜头
    // 指针中途失效时 setPointerCapture 会抛异常，不能让它把后面的拖拽状态一起带走
    try {
      svgRef.current.setPointerCapture?.(e.pointerId)
    } catch { /* 没有活动指针，忽略 */ }
    pointers.current.set(e.pointerId, [e.clientX, e.clientY])
    if (pointers.current.size === 1) {
      drag.current = { sx: e.clientX, sy: e.clientY, vx: viewport.x, vy: viewport.y, moved: 0 }
    } else if (pointers.current.size === 2) {
      hit.current = null
      const [a, b] = [...pointers.current.values()]
      pinch.current = { d: Math.hypot(a[0] - b[0], a[1] - b[1]), k: viewport.k }
      drag.current = null
    }
  }

  const onPointerMove = (e) => {
    if (!pointers.current.has(e.pointerId)) return
    pointers.current.set(e.pointerId, [e.clientX, e.clientY])

    if (pinch.current && pointers.current.size >= 2) {
      const [a, b] = [...pointers.current.values()]
      const d = Math.hypot(a[0] - b[0], a[1] - b[1])
      if (pinch.current.d > 0) {
        const mid = toSvg({ clientX: (a[0] + b[0]) / 2, clientY: (a[1] + b[1]) / 2 })
        zoomAt((pinch.current.k * (d / pinch.current.d)) / viewport.k, mid)
      }
      return
    }

    // 拖拽状态先取到局部变量：setViewport 的回调是稍后才跑的，
    // 中间一次 pointerup 就可能把 drag.current 清成 null，回调里再读就崩了
    const d = drag.current
    if (!d) return
    const r = svgRef.current.getBoundingClientRect()
    const dx = ((e.clientX - d.sx) / r.width) * W
    const dy = ((e.clientY - d.sy) / r.height) * H
    d.moved = Math.max(d.moved, Math.hypot(dx, dy))
    setViewport((v) => clampView({ k: v.k, x: d.vx + dx, y: d.vy + dy }))
  }

  const onPointerUp = (e) => {
    pointers.current.delete(e.pointerId)
    if (pointers.current.size < 2) pinch.current = null
    if (pointers.current.size === 0) {
      const d = drag.current
      const fn = hit.current
      drag.current = null
      hit.current = null
      if (d && d.moved <= 4 && fn) fn()
    }
  }

  /* ---------- 合并同一格里的点 ---------- */
  const clusters = useMemo(() => {
    const cells = new Map()
    for (const s of filteredSites) {
      const sx = s.x * viewport.k + viewport.x
      const sy = s.y * viewport.k + viewport.y
      if (sx < -12 || sx > W + 12 || sy < -12 || sy > H + 12) continue
      const key = `${Math.floor(sx / CELL)}:${Math.floor(sy / CELL)}`
      let c = cells.get(key)
      if (!c) cells.set(key, (c = { n: 0, sx: 0, sy: 0, items: [] }))
      c.n++
      c.sx += sx
      c.sy += sy
      c.items.push(s)
    }
    return [...cells.values()]
      .map((c) => ({ n: c.n, x: c.sx / c.n, y: c.sy / c.n, items: c.items }))
      .sort((a, b) => b.n - a.n)
  }, [viewport, filteredSites])

  /* ---------- 视野内列表 ----------
     按国家、名称排序而不是按离图心远近：看全球时图心在大西洋中间，
     按距离排会把非洲的十几处顶到最前面，看起来像随机顺序。 */
  const inView = useMemo(() => {
    const list = filteredSites.filter((s) => {
      const sx = s.x * viewport.k + viewport.x
      const sy = s.y * viewport.k + viewport.y
      return sx >= 0 && sx <= W && sy >= 0 && sy <= H
    })
    return list.sort(
      (a, b) => a.country.localeCompare(b.country, 'zh') || a.name.localeCompare(b.name, 'zh')
    )
  }, [viewport, filteredSites])

  /* ---------- 搜索 ----------
     一个输入框同时认三种东西：遗产名、国家名、地名（大洲 / 大洋 / 区域）。
     1）算出所有能对上的遗产，按匹配程度排；
     2）再看输入是不是一个地名；
     3）遗产「完全同名或开头相同」优先，否则地名优先——
        输入「亚洲」不会被某处名字里带「亚」的遗产抢走。 */
  const res = useMemo(() => {
    const raw = q.trim()
    if (!raw) return null
    const qn = norm(raw)
    if (!qn) return null

    const hits = []
    for (const s of filteredSites) {
      let byName = false
      let sc = matchScore(qn, s.name)
      if (sc !== null) byName = true
      const en = matchScore(qn, s.nameEn)
      if (en !== null) {
        sc = sc === null ? en + 0.5 : Math.min(sc, en + 0.5)
        byName = true
      }
      /* 「中国长城」这种把国家名当前缀的打法：遗产名整个落在输入里 */
      const nm = norm(s.name)
      if (nm.length >= 2 && qn.length > nm.length && qn.length - nm.length <= 4 && qn.includes(nm)) {
        if (sc === null || 2.5 < sc) sc = 2.5
        byName = true
      }
      const co = matchScore(qn, s.country)
      if (co !== null) sc = sc === null ? co + 1 : Math.min(sc, co + 1)
      if (sc !== null) hits.push({ s, sc, byName })
    }
    /* 名字对上的排在「只是国家名对上」的前面 */
    hits.sort(
      (a, b) =>
        b.byName - a.byName ||
        a.sc - b.sc ||
        a.s.name.length - b.s.name.length ||
        a.s.name.localeCompare(b.s.name, 'zh')
    )

    /* 地名正好等于输入时，除非有遗产名与它完全一样，否则按地名走 */
    const place = findPlace(qn)
    const siteExact = hits.length > 0 && hits[0].sc === 0
    if (place && !siteExact && (place.exact || !hits.length || hits[0].sc > 1)) {
      return { kind: 'place', place: place.p, total: hits.length }
    }

    /* 国家：输入正好是国家名，或者没有任何遗产名对上（「中国遗产」这类）
       ——「中国长城」这种点名某处遗产的，仍按遗产走 */
    const country = findCountry(qn)
    if (country && !siteExact && (country.exact || !hits.some((h) => h.byName))) {
      const list = country.c.sites.filter(s => category === '全部' || s.category === category).sort(
        (a, b) => Number(a.year) - Number(b.year) || a.name.localeCompare(b.name, 'zh')
      )
      return { kind: 'country', country: { name: country.c.name, sites: list }, total: list.length }
    }
    if (hits.length) return { kind: 'sites', hits: hits.slice(0, 60) }
    return { kind: 'none', query: raw }
  }, [q, filteredSites, category])

  // 修改：两种地图共用定位目标；输入筛选点位，按回车再移动镜头。
  const focusView = (to, zoom = 1.6) => {
    if (!to) return
    flyTo(to)
    setGlobeTarget({ lng: ((W / 2 - to.x) / to.k / W) * 360 - 180, lat: 90 - ((H / 2 - to.y) / to.k / H) * 180 })
    updateGlobeZoom(zoom)
  }
  const focusSites = sites => {
    if (sites.length) focusView(viewForCountry({ sites }))
  }
  const changeContinent = value => {
    setContinent(value); setCountry('全部'); setHover(null)
    if (value === '全部') { reset(); setGlobeTarget({ lng: 20, lat: 20 }); updateGlobeZoom(1) }
    else focusView(viewForPlace({ t: value, box: PLACE_BOX[value] }), 1.15)
  }
  const flyToResult = () => {
    if (!res) return
    cancelAnimationFrame(raf.current)
    if (res.kind === 'sites') focusView(viewForSite(res.hits[0].s, viewportRef.current.k), 2)
    else if (res.kind === 'country' && filteredSites.length) focusSites(filteredSites)
    else if (res.kind === 'place') focusView(viewForPlace(res.place), 1.15)
  }
  const openEntry = (s) => onOpenSite(s.featured ? s.featured : { ...s, catalog: true })

  return (
    <div className={`map-page heritage-atlas atlas-map-workspace mode-${mode}`}>
      <div className="wrap">
        <div className="map-stage">
          <aside className="atlas-map-sidebar" aria-label="遗产统计与分类">
            <div className="atlas-map-heading"><span>THE WORLD HERITAGE ATLAS</span><h1>世界遗产地图</h1></div>
            <div className="atlas-map-stats">
              <div><strong>{ALL.length.toLocaleString()}</strong><span>世界遗产地</span></div>
              <div><strong>{new Set(ALL.flatMap(s => s.countries || [s.country])).size}</strong><span>国家和地区</span></div>
              <div><strong>{N3D}</strong><span>三维数字档案</span></div>
            </div>
            <div className="atlas-map-categories" role="group" aria-label="按遗产类型筛选">
              {CATEGORIES.map(c => <button key={c} className={category === c ? 'active' : ''} aria-pressed={category === c} onClick={() => { setCategory(c); setHover(null) }}><CategoryIcon category={c}/><span>{c === '全部' ? '全部遗产' : `${c}遗产`}</span></button>)}
            </div>
            <div className="atlas-map-compass" aria-hidden="true"><span>N</span><svg viewBox="0 0 100 100"><circle cx="50" cy="50" r="35"/><circle cx="50" cy="50" r="29"/><path d="M50 6v88M6 50h88M25 25l50 50m0-50L25 75"/><path d="m50 13 7 30 30 7-30 7-7 30-7-30-30-7 30-7Z"/><path d="m50 13 7 30-7 7-7 7 7 30V50L13 50l30-7Z" fill="currentColor"/></svg><span>S</span><i>W</i><i>E</i></div>
            <button className="atlas-map-home-link" onClick={() => setView('home')}>← 返回首页</button>
          </aside>
          <div className="atlas-map-canvas">
          <div className="atlas-map-topbar">
            {/* 修改：左上角统一切换，两种地图保留相同筛选条件。 */}
            <div className="atlas-map-mode" role="group" aria-label="地图显示方式"><button aria-pressed={mode === 'flat'} onClick={() => setMode('flat')}>平面地图</button><button aria-pressed={mode === 'globe'} onClick={() => setMode('globe')}><IcGlobe w={17}/>3D地图</button></div>
          <div className="atlas-map-search-panel">
          <div className="map-search">
            <IcSearch w={15} />
            <input
              value={q}
              onChange={(e) => setQ(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') flyToResult()
                if (e.key === 'Escape') setQ('')
              }}
              placeholder="搜索遗产、国家、大洲或关键词…"
              aria-label="搜索遗产、国家、大洲或大洋，回车定位"
            />
            {q ? (
              <>
                <button className="map-search-go" onClick={flyToResult} aria-label="定位到结果">
                  ↵ 定位
                </button>
                <button className="map-search-x" onClick={() => setQ('')} aria-label="清除搜索">
                  ×
                </button>
              </>
            ) : null}
          </div>
          {/* 修改：检索条件同时作用于平面和 3D；大洲改变后清空不再适用的国家。 */}
          <div className="atlas-map-filters">
            <select aria-label="按大洲筛选" value={continent} onChange={e => changeContinent(e.target.value)}><option value="全部">全部大洲</option>{Object.keys(PLACE_BOX).filter(c => c !== '美洲').map(c => <option key={c}>{c}</option>)}</select>
            <select aria-label="按国家筛选" value={country} onChange={e => { setCountry(e.target.value); if (e.target.value !== '全部') focusSites(COUNTRIES.find(c => c.name === e.target.value)?.sites || []); else if (continent !== '全部') focusView(viewForPlace({ box: PLACE_BOX[continent] }), 1.15); else { reset(); setGlobeTarget({ lng: 20, lat: 20 }); updateGlobeZoom(1) } }}><option value="全部">全部国家</option>{countryOptions.map(c => <option key={c.name}>{c.name}</option>)}</select>
            <select aria-label="按类型筛选" value={category} onChange={e => { setCategory(e.target.value); setHover(null) }}><option value="全部">全部类型</option>{CATEGORIES.filter(c => c !== '全部').map(c => <option key={c} value={c}>{c}遗产</option>)}</select>
          </div>
          <div className="atlas-map-filter-status" role="status">{filteredSites.length ? `找到 ${filteredSites.length} 处遗产${q.trim() ? ' · 回车定位' : ''}` : '没有符合条件的遗产，请调整筛选或关键词'}{(q || continent !== '全部' || country !== '全部' || category !== '全部') ? <button onClick={() => { setQ(''); setCategory('全部'); changeContinent('全部') }}>清除筛选</button> : null}</div>
          </div>
        </div>
          <svg
            ref={svgRef}
            className={`map-svg${drag.current ? ' dragging' : ''}`}
            viewBox={`0 0 ${W} ${H}`}
            preserveAspectRatio="xMidYMid meet"
            style={{ display: mode === 'flat' ? 'block' : 'none' }}
            role="img"
            aria-label="可缩放与拖动的世界遗产地图，金色点为遗产，蓝色点含三维模型"
            onPointerDownCapture={() => { hit.current = null }}
            onPointerDown={onPointerDown}
            onPointerMove={onPointerMove}
            onPointerUp={onPointerUp}
            onPointerCancel={(e) => { hit.current = null; onPointerUp(e) }}
          >
            <defs>
              <radialGradient id="mapGlow" cx="50%" cy="45%" r="70%">
                <stop offset="25%" stopColor="#020d18" stopOpacity="0" />
                <stop offset="100%" stopColor="#010912" stopOpacity="0.9" />
              </radialGradient>
              <filter id="atlasTerrain"><feColorMatrix type="matrix" values=".65 0 0 0 0  0 .72 0 0 .02  0 0 .8 0 .04  0 0 0 1 0"/></filter>
            </defs>
            <rect width={W} height={H} fill="#04111e" />

            <g transform={`translate(${viewport.x} ${viewport.y}) scale(${viewport.k})`}>
              {!textureFailed ? <image href={asset('geo/earth.webp')} width={W} height={H} preserveAspectRatio="none" filter="url(#atlasTerrain)" onError={() => setTextureFailed(true)} /> : null}
              <g>{graticule}</g>
              {landPath ? <path className={`map-land${textureFailed ? ' fallback' : ''}`} d={landPath} vectorEffect="non-scaling-stroke" /> : null}
            </g>
            <rect width={W} height={H} fill="url(#mapGlow)" pointerEvents="none" />
            <g transform={`translate(${viewport.x} ${viewport.y}) scale(${viewport.k})`}>
              {viewport.k < 3 ? filteredSites.map(s => <g key={s.id} className={`atlas-map-light${s.has3d ? ' has-model' : ''}`} transform={`translate(${s.x} ${s.y})`} role="button" tabIndex={-1} aria-label={`查看${s.name}`} onMouseEnter={() => setHover(s.id)} onMouseLeave={() => setHover(null)} onPointerDown={() => { hit.current = () => openEntry(s) }}>
                <circle className="light-glow" r={(s.has3d ? 7 : 4) / viewport.k}/><circle className="light-core" r={(s.has3d ? 2.8 : 1.15) / viewport.k}/>
              </g>) : null}

              {/* 字号除以缩放倍数：字随地图走，但屏幕上的大小不变 */}
              {LABELS.map((l) => {
                const o = labelOpacity(l.lv, viewport.k)
                if (o < 0.02) return null
                // 目标屏幕字号 → 用户单位；除以 k 抵消缩放，字随地图走但不跟着变大
                const px = l.lv === 1 ? (ppu < 0.75 ? 10.5 : 13) : ppu < 0.75 ? 9 : 9.5
                return (
                  <text
                    key={l.t}
                    x={projX(l.lng)}
                    y={projY(l.lat)}
                    className={`map-lbl lv${l.lv}${l.oc ? ' oc' : ''}`}
                    style={{ fontSize: px / (ppu * viewport.k), opacity: o }}
                  >
                    {l.t}
                  </text>
                )
              })}
            </g>

            {textureFailed && !landPath ? (
              <text x={W / 2} y={H / 2} fill="rgba(255,236,214,0.4)" fontSize="13" textAnchor="middle">
                {failed ? '底图暂不可用，仍可搜索和查看遗产档案' : '正在载入底图…'}
              </text>
            ) : null}

            {viewport.k >= 3 ? clusters.map((c, i) => {
              if (c.n === 1) {
                const s = c.items[0]
                const cls = s.has3d ? ' hot' : s.hasImg ? '' : ' bare'
                return (
                  <g
                    key={s.id}
                    className="map-node"
                    role="button"
                    tabIndex="0"
                    aria-label={`查看${s.name}`}
                    onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); openEntry(s) } }}
                    transform={`translate(${c.x.toFixed(1)}, ${c.y.toFixed(1)})`}
                    onMouseEnter={() => setHover(s.id)}
                    onMouseLeave={() => setHover(null)}
                    onPointerDown={() => { hit.current = () => openEntry(s) }}
                  >
                    <circle className="halo" r={s.has3d ? 9 : s.hasImg ? 6 : 5} />
                    <circle className={`core${cls}`} r={s.has3d ? 4 : s.hasImg ? 2.8 : 1.9} />
                    {hover === s.id ? (
                      <text className="tip" y="-10">{s.name}</text>
                    ) : null}
                  </g>
                )
              }
              // 合并圈只报数量，不区分有没有模型——数字统一绿色，颜色留到放大成单点时再说
              const r = clamp(7 + Math.sqrt(c.n) * 1.5, 9, 18)
              return (
                <g
                  key={`c${i}`}
                  className="map-cluster"
                  role="button"
                  tabIndex="0"
                  aria-label={`${c.n} 处遗产，点击放大`}
                  onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); zoomAt(2.6, [c.x, c.y]) } }}
                  transform={`translate(${c.x.toFixed(1)}, ${c.y.toFixed(1)})`}
                  onMouseEnter={() => setHover(`c${i}`)}
                  onMouseLeave={() => setHover(null)}
                  onPointerDown={() => { hit.current = () => zoomAt(2.6, [c.x, c.y]) }}
                >
                  <circle className="halo" r={r + 3} />
                  <circle className="disc" r={r} />
                  <text className="num" y="1">{c.n}</text>
                  {hover === `c${i}` ? (
                    <text className="tip" y={-r - 8}>
                      {c.items.slice(0, 3).map((s) => s.name).join(' · ')}
                      {c.n > 3 ? ` 等 ${c.n} 处` : ''}
                    </text>
                  ) : null}
                </g>
              )
            }) : null}
            {showLandmarks ? <MapLandmarks projectX={lng => projX(lng) * viewport.k + viewport.x} projectY={lat => projY(lat) * viewport.k + viewport.y} sites={filteredSites} onOpen={openEntry} category={category} zoom={viewport.k}/> : null}
            {viewport.k < 3 && hover && filteredSites.some(s => s.id === hover) ? (() => {
              const s = filteredSites.find(s => s.id === hover)
              return <text className="tip" x={clamp(s.x * viewport.k + viewport.x, 85, W - 85)} y={Math.max(18, s.y * viewport.k + viewport.y - 12)}>{s.name}</text>
            })() : null}
          </svg>
          {/* 修改：3D 只显示地球，不再附带模型列表；缩放后点位与地球一起变化。 */}
          <div className="atlas-map-globe" ref={globeViewportRef} hidden={mode !== 'globe'}>
            <div className="atlas-globe-sphere" style={{ transform: `scale(${globeZoom})` }}><HeritageGlobe large sites={filteredSites} target={globeTarget} zoom={globeZoom} onZoom={updateGlobeZoom} onOpenSite={openEntry}/></div>
          </div>

          <div className="map-zoom">
            <button disabled={mode === 'globe' ? globeZoom >= 3.5 : viewport.k >= K_MAX} onClick={() => mode === 'globe' ? updateGlobeZoom(globeZoom * 1.25) : zoomAt(1.6, [W / 2, H / 2])} aria-label="放大">＋</button>
            <button disabled={mode === 'globe' ? globeZoom <= .7 : viewport.k <= K_MIN} onClick={() => mode === 'globe' ? updateGlobeZoom(globeZoom / 1.25) : zoomAt(1 / 1.6, [W / 2, H / 2])} aria-label="缩小">－</button>
            <button className="rst" onClick={() => { reset(); updateGlobeZoom(1); setGlobeTarget({ lng: 20, lat: 20 }) }} aria-label="回到全球">
              <svg viewBox="0 0 24 24" width="23" height="23" fill="none" stroke="currentColor" strokeWidth="1.4"><circle cx="12" cy="12" r="7"/><circle cx="12" cy="12" r="3"/><path d="M12 1v4m0 14v4M1 12h4m14 0h4"/></svg>
            </button>
            {mode === 'flat' ? <button className="layers" onClick={() => setShowLandmarks(v => !v)} aria-label="切换地标图层" aria-pressed={showLandmarks}><svg viewBox="0 0 24 24" width="25" height="25" fill="none" stroke="currentColor" strokeWidth="1.3"><path d="m12 3 10 7-10 7L2 10Zm-9 12 9 6 9-6"/></svg></button> : null}
          </div>

          <div className="map-scale">
            <span className="k">缩放</span>
            <b>{(mode === 'globe' ? globeZoom : viewport.k).toFixed(1)}×</b>
            <span className="n">{mode === 'globe' ? `筛选 ${filteredSites.length} 处` : `视野内 ${inView.length} 处`}</span>
          </div>

          <div className="map-legend">
            <span className="t">图例</span>
            <span className="r">
              <i className="hot" /> 含三维模型 · {filteredSites.filter(s => s.has3d).length}
            </span>
            <span className="r">
              <i /> 遗产点位 · {filteredSites.length}
            </span>
            <span className="r">
              <i className="cl" /> {mode === 'globe' ? '拖动旋转 · 滚轮缩放' : viewport.k < 3 ? '滚轮缩放 · 拖动探索' : '聚合点 · 点击放大'}
            </span>
          </div>
          <div className="atlas-map-credit">地形影像 · 项目内置全球底图</div>
          {!filteredSites.length ? <div className="atlas-map-notice">{category === '自然' ? '当前名录尚未收录自然遗产' : '没有符合条件的遗产'}<button onClick={() => { setQ(''); setCategory('全部'); changeContinent('全部') }}>查看全部遗产 ↗</button></div> : null}
          </div>
        </div>

      </div>
    </div>
  )
}
