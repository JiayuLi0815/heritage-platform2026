import { useEffect, useState } from 'react'
import { IcSearch } from './icons.jsx'

const TABS = [
  { key: 'home', label: '首页' },
  // 修改：遗产名录与地图各有独立入口，首页按钮和顶部导航保持一致。
  { key: 'catalog', label: '探索遗产' },
  { key: 'map', label: '数字地图' },
  { key: 'vr', label: '沉浸体验' },
  { key: 'data', label: '数据洞察' },
  { key: 'about', label: '关于我们' },
]

function HeritageMark() {
  return <svg viewBox="0 0 36 44" fill="none" stroke="currentColor" strokeWidth="1.65" aria-hidden="true"><path d="M3 12 18 4l15 8H3ZM5 16h26M3 38h30M1 42h34M7 18v17m6-17v17m10-17v17m6-17v17M18 18v17"/></svg>
}
export default function Navbar({ view, setView, onSearch }) {
  const [solid,setSolid]=useState(false)
  const [menu,setMenu]=useState(false)
  useEffect(()=>{const scroll=()=>setSolid(window.scrollY>30);scroll();window.addEventListener('scroll',scroll,{passive:true});return()=>window.removeEventListener('scroll',scroll)},[])
  useEffect(()=>setMenu(false),[view])
  const go=key=>{setMenu(false);setView(key)}
  return <header className={`nav-outer atlas-nav-outer${solid||view!=='home'?' solid':''}`}><nav className="nav atlas-nav" aria-label="主导航"><button className="brand atlas-brand" onClick={()=>go('home')} aria-label="返回首页"><span className="atlas-brand-mark"><HeritageMark/></span><span><strong>世界文化遗产</strong><small>数智探索与沉浸式体验平台</small></span></button><div className={`nav-links atlas-nav-links${menu?' menu-open':''}`}>{TABS.map(t=><button key={t.key} className={`nav-link${view===t.key||(t.key==='map'&&view==='globe')?' on':''}`} onClick={()=>go(t.key)} aria-current={view===t.key?'page':undefined}>{t.label}</button>)}</div><div className="atlas-nav-tools"><button className="atlas-search-trigger" onClick={onSearch} aria-label="搜索遗产"><span>搜索遗产、国家或关键词</span><IcSearch w={17}/></button><span className="atlas-language" aria-label="当前语言：中文">中 <i>/</i> <span>EN</span></span><button className="atlas-menu-button" aria-label={menu?'关闭导航菜单':'打开导航菜单'} aria-expanded={menu} onClick={()=>setMenu(v=>!v)}>{menu?'✕':'☰'}</button></div></nav></header>
}
export { TABS }
