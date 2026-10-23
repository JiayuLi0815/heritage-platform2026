import { useEffect, useState } from 'react'
import { IcCompass, IcSearch } from './icons.jsx'

const TABS = [
  { key: 'home', label: '首页' },
  { key: 'map', label: '地图' },
  { key: 'vr', label: 'VR 体验' },
  { key: 'human', label: '数字人' },
  { key: 'data', label: '数据源' },
  { key: 'about', label: '项目介绍' },
]

export default function Navbar({ view, setView, onSearch }) {
  const [solid, setSolid] = useState(false)

  useEffect(() => {
    const onScroll = () => setSolid(window.scrollY > 40)
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  // 详情页/地图页本身就是深色或独立语境，导航保持实体态
  const forceSolid = view !== 'home'

  return (
    <div className={`nav-outer${solid || forceSolid ? ' solid' : ''}`}>
      <nav className="nav">
        <button className="brand" onClick={() => setView('home')} aria-label="返回首页">
          <span className="brand-mark">
            <IcCompass w={19} style={{ color: '#fff' }} />
          </span>
          <span className="brand-text">
            <span className="brand-name">世界文化遗产数智探索与沉浸式体验平台</span>
            <span className="brand-sub">TJ 数智文化大模型虚拟实验室 · 同济大学人文学院</span>
          </span>
        </button>

        <div className="nav-links">
          {TABS.map((t) => (
            <button
              key={t.key}
              className={`nav-link${view === t.key ? ' on' : ''}`}
              onClick={() => setView(t.key)}
            >
              {t.label}
            </button>
          ))}
        </div>

        <div className="nav-right">
          <button className="icon-btn" onClick={onSearch} aria-label="搜索遗产">
            <IcSearch />
          </button>
          <span className="avatar">TJ</span>
        </div>
      </nav>
    </div>
  )
}

export { TABS }
