import { useMemo, useState } from 'react'
import RAW from '../data/sites-all.json'
import { FEATURED_BY_WHC } from '../data/whc-featured.js'
import { asset } from '../asset.js'
import { IcArrowRight, IcSearch } from '../components/icons.jsx'

const PAGE_SIZE = 24
const COUNTRIES = [...new Set(RAW.map(s => s.country))].sort((a, b) => a.localeCompare(b, 'zh'))

// 修改：首页「遗产列表」的真实目的页，直接使用完整名录，避免只显示首页的 20 处精选。
export default function HeritageList({ onOpenSite, setView }) {
  const [query, setQuery] = useState('')
  const [country, setCountry] = useState('全部')
  const [category, setCategory] = useState('全部')
  const [page, setPage] = useState(1)
  const filtered = useMemo(() => {
    const term = query.trim().toLocaleLowerCase()
    return RAW.filter(s => (country === '全部' || s.country === country)
      && (category === '全部' || s.category === category)
      && `${s.name} ${s.nameEn} ${s.country} ${s.year}`.toLocaleLowerCase().includes(term))
  }, [query, country, category])
  const pages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const current = Math.min(page, pages)
  const rows = filtered.slice((current - 1) * PAGE_SIZE, current * PAGE_SIZE)
  const changeFilter = (setter, value) => { setter(value); setPage(1) }
  const changePage = next => {
    setPage(next)
    document.getElementById('heritage-list-results')?.scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' })
  }
  return <main className="heritage-list-page">
    <header className="heritage-list-heading"><span className="atlas-section-kicker">THE HERITAGE COLLECTION</span><h1>遗产列表</h1><p>从一处遗产出发，读懂世界的文化记忆。浏览收录的 {RAW.length} 处遗址，打开属于它的数字档案。</p><button className="atlas-text-link" onClick={() => setView('map')}>在地图中探索 <IcArrowRight w={17}/></button></header>
    <div className="heritage-list-filters">
      <label className="heritage-list-search"><IcSearch w={17}/><input aria-label="搜索遗产名称、国家或年份" placeholder="搜索遗产、国家或列入年份" value={query} onChange={e => changeFilter(setQuery, e.target.value)}/></label>
      <label>国家和地区<select value={country} onChange={e => changeFilter(setCountry, e.target.value)}><option value="全部">全部国家和地区</option>{COUNTRIES.map(c => <option key={c}>{c}</option>)}</select></label>
      <label>遗产类型<select value={category} onChange={e => changeFilter(setCategory, e.target.value)}><option value="全部">全部类型</option><option value="文化">文化遗产</option><option value="混合">混合遗产</option></select></label>
    </div>
    <div className="heritage-list-result-head" id="heritage-list-results" aria-live="polite"><span>共 {filtered.length} 处遗产</span><span>第 {current} / {pages} 页</span></div>
    <div className="atlas-collection-grid">
      {rows.map(s => <button className="atlas-collection-card" key={s.id} onClick={() => onOpenSite({ ...s, catalog: true, featured: FEATURED_BY_WHC[s.id] || null })}><div className="heritage-list-photo"><span className="heritage-list-photo-fallback" aria-hidden="true">WORLD HERITAGE</span><img src={asset(`img/sites/${s.id}.webp`)} alt={s.name} loading="lazy" onError={e => { e.currentTarget.style.visibility = 'hidden' }}/><span className="atlas-card-arrow">↗</span></div><small>{s.country} · {s.category}遗产 · {s.year}</small><h2>{s.name}</h2><p>{s.nameEn}</p></button>)}
    </div>
    {!filtered.length ? <div className="heritage-list-empty"><p>没有找到符合条件的遗产，请换个关键词或筛选条件。</p><button className="atlas-text-link" onClick={() => { setQuery(''); setCountry('全部'); setCategory('全部'); setPage(1) }}>清除筛选 <IcArrowRight w={16}/></button></div> : null}
    {pages > 1 ? <nav className="heritage-list-pager" aria-label="遗产列表分页"><button disabled={current === 1} onClick={() => changePage(current - 1)}>← 上一页</button><span>{current} / {pages}</span><button disabled={current === pages} onClick={() => changePage(current + 1)}>下一页 →</button></nav> : null}
  </main>
}
