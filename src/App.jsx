import { useCallback, useEffect, useMemo, useState } from 'react'
import { SITES } from './data/sites.js'
import Navbar from './components/Navbar.jsx'
import { asset } from './asset.js'
import DigitalHuman from './components/DigitalHuman.jsx'
import Home, { AtlasStyles } from './pages/Home.jsx'
import MapPage from './pages/MapPage.jsx'
import HeritageList from './pages/HeritageList.jsx'
import AboutPage from './pages/AboutPage.jsx'
import DataPage from './pages/DataPage.jsx'
import VrPage from './pages/VrPage.jsx'
import SiteDetail from './pages/SiteDetail.jsx'
import CatalogDetail from './pages/CatalogDetail.jsx'
import RAW from './data/sites-all.json'
import { FEATURED_BY_WHC } from './data/whc-featured.js'
import { IcArrowRight, IcCompass, IcSearch, IcClose, IcPin } from './components/icons.jsx'

/* 全部 991 处。精选的 20 处在地图与搜索里优先按精选编号识别，
   剩下的走名录档案页；两边共用一套 id（UNESCO 编号）。 */
const CATALOG = RAW.map((s) => ({ ...s, catalog: true, featured: FEATURED_BY_WHC[s.id] || null }))

export default function App() {
  const [view, setView] = useState('home')
  const [site, setSite] = useState(null)
  const [humanOpen, setHumanOpen] = useState(false)
  const [humanCtx, setHumanCtx] = useState(null)
  const [searchOpen, setSearchOpen] = useState(false)

  const openSite = useCallback((idOrSite) => {
    let s = typeof idOrSite === 'string' ? SITES.find((x) => x.id === idOrSite) : idOrSite
    if (!s) return
    // 名录条目若其实是精选的 20 处之一，要进那份带模型的详情页，而不是降级的档案页
    if (s.catalog && s.featured) s = SITES.find((x) => x.id === s.featured) || s
    setSite(s)
    setSearchOpen(false)
    document.body.classList.add('locked')
  }, [])

  const closeSite = useCallback(() => {
    setSite(null)
    setHumanCtx(null)
    document.body.classList.remove('locked')
  }, [])

  const goto = useCallback((v) => {
    setView(v)
    closeSite()
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }, [closeSite])

  // Esc 逐层关闭：搜索 → 数字人 → 详情
  useEffect(() => {
    const onKey = (e) => {
      if (e.key !== 'Escape') return
      if (searchOpen) return setSearchOpen(false)
      if (humanOpen) return setHumanOpen(false)
      if (site) return closeSite()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [searchOpen, humanOpen, site, closeSite])

  useEffect(() => {
    document.body.classList.toggle('locked', !!site || searchOpen)
  }, [site, searchOpen])

  return (
    <>
      <AtlasStyles />
      {!site && view !== 'map' && view !== 'globe' ? (
        <Navbar view={view} setView={goto} onSearch={() => setSearchOpen(true)} />
      ) : null}

      {view === 'home' && !site ? (
        <Home
          onOpenSite={openSite}
          setView={goto}
          openHuman={() => setHumanOpen(true)}
        />
      ) : null}
      {/* 修改：接通首页「遗产列表」入口，浏览全部名录并复用现有详情页。 */}
      {view === 'catalog' && !site ? <HeritageList onOpenSite={openSite} setView={goto} /> : null}
      {/* 修改：两种地图都进入统一地图工作区，模式切换由地图页内部管理。 */}
      {(view === 'map' || view === 'globe') && !site ? <MapPage onOpenSite={openSite} setView={goto} initialMode={view === 'globe' ? 'globe' : 'flat'} /> : null}
      {view === 'about' && !site ? <AboutPage /> : null}
      {view === 'data' && !site ? <DataPage /> : null}
      {view === 'vr' && !site ? <VrPage /> : null}
      {view === 'human' && !site ? (
        <HumanStandalone onOpenSite={openSite} openHuman={() => setHumanOpen(true)} />
      ) : null}

      {view !== 'home' && view !== 'map' && view !== 'globe' && !site ? <Footer setView={goto} /> : null}

      {site ? (
        site.catalog ? (
          <CatalogDetail site={site} onClose={closeSite} onOpenSite={openSite} />
        ) : (
          <SiteDetail
            site={site}
            onClose={closeSite}
            onOpenSite={openSite}
            openHuman={() => setHumanOpen(true)}
            setHumanCtx={setHumanCtx}
          />
        )
      ) : null}

      <DigitalHuman
        open={humanOpen}
        setOpen={setHumanOpen}
        onOpenSite={openSite}
        contextSite={humanCtx}
        pageSite={site}
      />

      {searchOpen ? <SearchOverlay onClose={() => setSearchOpen(false)} onPick={openSite} /> : null}
    </>
  )
}

/* ============================================================
   数字人独立页（tab 里的「数字人」）
   ============================================================ */
function HumanStandalone({ onOpenSite, openHuman }) {
  const feats = [
    { t: '报告级知识底座', d: '背后是 DeepSeek 大模型与我们团队编著的《世界文化遗产数字化研究报告》，不是单处讲解词。' },
    { t: '证据分级', d: '回答中区分「已实施 / 部分已实施 / 未发现可验证证据」，不把推测说成结论。' },
    { t: '即席比较', d: '可以要求它把两处遗产放在一起比，或列出某一项技术做得最好的几处。' },
    { t: '行为回流', d: '对话与浏览记录会汇总为需求侧研究数据。' },
  ]
  return (
    <div>
      <div className="wrap page-head">
        <span className="kicker">数字人 LLM</span>
        <h1>一个知道整个体系的导览者</h1>
        <p>
          平台的数字人由 DeepSeek 大模型驱动，以我们团队编著的《世界文化遗产数字化研究报告》为知识底座。
          它不只会介绍单处遗产，还能回答跨案例的问题——
          哪些遗产用了区块链、亚洲有哪几处、数字化程度最高的是哪些。
        </p>
      </div>
      <div className="wrap" style={{ paddingBottom: 90 }}>
        <div className="cards-2" style={{ marginBottom: 24 }}>
          {feats.map((f) => (
            <div className="pane" key={f.t}>
              <h3>{f.t}</h3>
              <p>{f.d}</p>
            </div>
          ))}
        </div>

        <div className="dh-band" style={{ marginTop: 12 }}>
          <div>
            <span className="kicker">开始对话</span>
            <h2>左下角点开，直接问它</h2>
            <p className="sub">
              试试：「哪些遗产用了区块链？」「数字化程度最高的是哪几处？」「这些资产能商用吗？」
            </p>
            <div className="hero-actions" style={{ justifyContent: 'flex-start', marginTop: 28 }}>
              <button className="btn btn-fill" onClick={openHuman}>
                打开对话面板 <IcArrowRight w={16} />
              </button>
            </div>
          </div>
          <div className="dh-orb-wrap">
            <div className="dh-orb" onClick={openHuman}>
              <img src={asset('img/human-avatar.webp')} alt="数字人弘文" />
            </div>
          </div>
        </div>

        <div className="note-box" style={{ marginTop: 22 }}>
          <b>当前实现</b>：已接入 DeepSeek。提问时把当前页面档案、平台名录与研究报告的结论作为资料一并送去，
          再按弘文的口语语气作答——短、直接、不分点。没有配置 API Key 或接口不通时，
          自动回落到本地知识检索层，同样给得出可核查的答案。
        </div>
      </div>
    </div>
  )
}

/* ============================================================
   搜索浮层
   ============================================================ */
function SearchOverlay({ onClose, onPick }) {
  const [q, setQ] = useState('')
  const key = q.trim().toLowerCase()

  // 搜全量 991 处；精选的 20 处排前面（它们点开有模型）。
  const res = useMemo(() => {
    const list = key
      ? CATALOG.filter((s) =>
          [s.name, s.nameEn, s.country, s.year, s.category, ...(s.countries || [])]
            .join(' ')
            .toLowerCase()
            .includes(key)
        )
      : CATALOG
    return [...list].sort((a, b) => Number(!!b.featured) - Number(!!a.featured))
  }, [key])

  return (
    <div
      onClick={onClose}
      style={{
        position: 'fixed', inset: 0, zIndex: 900, background: 'rgba(20,13,8,0.6)',
        backdropFilter: 'blur(8px)', display: 'flex', justifyContent: 'center',
        paddingTop: '12vh', animation: 'fade .25s var(--ease) both',
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          width: 'min(620px, calc(100vw - 40px))', height: 'fit-content',
          maxHeight: '70vh', background: '#fff', borderRadius: 'var(--r-lg)',
          boxShadow: 'var(--sh-xl)', overflow: 'hidden', display: 'flex', flexDirection: 'column',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '16px 20px', borderBottom: '1px solid var(--line-2)' }}>
          <IcSearch w={18} style={{ color: 'var(--muted)' }} />
          <input
            autoFocus
            value={q}
            onChange={(e) => setQ(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && res[0]) onPick(res[0])
            }}
            placeholder="搜索遗产、国家、地区或标签…"
            style={{ flex: 1, border: 0, outline: 'none', font: 'inherit', fontSize: 15, background: 'transparent' }}
          />
          <button className="icon-btn" onClick={onClose} aria-label="关闭">
            <IcClose w={16} />
          </button>
        </div>

        <div style={{ overflowY: 'auto', padding: 10 }}>
          {res.length ? (
            res.slice(0, 12).map((s) => (
              <button
                key={s.id}
                onClick={() => onPick(s)}
                style={{
                  width: '100%', display: 'flex', alignItems: 'center', gap: 12,
                  padding: '11px 14px', borderRadius: 'var(--r-sm)', textAlign: 'left',
                }}
                onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg)')}
                onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
              >
                <span
                  style={{
                    width: 34, height: 34, borderRadius: 10, flexShrink: 0,
                    background: s.featured ? 'hsl(150 44% 52% / 0.16)' : 'var(--bg)',
                    color: s.featured ? 'hsl(150 50% 30%)' : 'var(--muted)',
                    display: 'grid', placeItems: 'center',
                  }}
                >
                  <IcPin w={15} />
                </span>
                <span style={{ flex: 1, minWidth: 0 }}>
                  <span style={{ display: 'block', fontSize: 14.5, fontWeight: 500 }}>{s.name}</span>
                  <span style={{ fontSize: 11.5, color: 'var(--muted)' }}>
                    {s.country} · {s.year} · {s.nameEn}
                  </span>
                </span>
                <span style={{ fontSize: 11, color: s.featured ? 'var(--accent-d)' : 'var(--muted-2)', fontWeight: 600, whiteSpace: 'nowrap' }}>
                  {s.featured ? '三维 · 编码' : '名录'}
                </span>
              </button>
            ))
          ) : (
            <div style={{ padding: '36px 20px', textAlign: 'center', color: 'var(--muted)', fontSize: 13.5 }}>
              没有找到匹配的遗产
            </div>
          )}
        </div>

        <div style={{ padding: '10px 18px', borderTop: '1px solid var(--line-2)', fontSize: 11, color: 'var(--muted-2)', display: 'flex', gap: 8 }}>
          <span>
            {key ? `${res.length} 处命中` : `共 ${CATALOG.length} 处收录`}
          </span>
          <span>·</span>
          <span>{SITES.length} 处有三维资产</span>
        </div>
      </div>
    </div>
  )
}

/* ============================================================
   页脚
   ============================================================ */
function Footer({ setView }) {
  return (
    <footer className="footer">
      <div className="wrap">
        <div className="footer-grid">
          <div>
            <div className="footer-brand">
              <span className="brand-mark">
                <IcCompass w={19} style={{ color: '#fff' }} />
              </span>
              <span className="brand-text">
                <span className="brand-name">世界文化遗产数智探索与沉浸式体验平台</span>
              </span>
            </div>
            <p className="footer-desc">
              World Cultural Heritage Digital Exploration &amp; Immersive Experience Platform
              <br />
              同济大学人文学院 TJ 数智文化大模型虚拟实验室
              <br />
              数据基础：《世界文化遗产数字化研究报告》（2025.11）
            </p>
          </div>
          <div>
            <h4>平台</h4>
            <ul>
              <li><a onClick={() => setView('home')}>首页</a></li>
              <li><a onClick={() => setView('map')}>全球地图</a></li>
              <li><a onClick={() => setView('human')}>数字人</a></li>
              <li><a onClick={() => setView('vr')}>VR 体验</a></li>
            </ul>
          </div>
          <div>
            <h4>关于</h4>
            <ul>
              <li><a onClick={() => setView('about')}>项目介绍</a></li>
              <li><a onClick={() => setView('about')}>项目成员</a></li>
              <li><a onClick={() => setView('data')}>数据源与许可</a></li>
            </ul>
          </div>
          <div>
            <h4>数据</h4>
            <ul>
              <li>《世界文化遗产数字化研究报告》</li>
              <li>139 案例编码矩阵</li>
              <li>五梯队评估结果</li>
              <li>34 处三维模型</li>
            </ul>
          </div>
        </div>
        <div className="footer-btm">
          <span className="note">Copyright 同济大学 TJ 数智文化大模型虚拟实验室</span>
        </div>
      </div>
    </footer>
  )
}
