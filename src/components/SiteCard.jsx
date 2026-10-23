import { useState } from 'react'
import { MODE_LABEL } from '../data/sites.js'
import { IcArrowRight } from './icons.jsx'
import { asset } from '../asset.js'

/** 无图时按 hue 生成的渐变视觉板 */
export const plateBg = (hue) =>
  `linear-gradient(152deg, hsl(${hue} 44% 54%) 0%, hsl(${hue + 20} 40% 33%) 58%, hsl(${hue + 34} 36% 19%) 100%)`

export default function SiteCard({ site, index = 0, onOpen }) {
  // 卡片配图统一放在 public/img/{id}.webp，缺失时自动退回渐变视觉板
  const [imgOk, setImgOk] = useState(true)
  const src = site.image || asset(`img/${site.id}.webp`)

  return (
    <article
      className="card"
      style={{ animationDelay: `${Math.min(index, 8) * 45}ms` }}
      onClick={() => onOpen(site)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && onOpen(site)}
    >
      <div className="card-media">
        {imgOk ? (
          <img src={src} alt={site.name} loading="lazy" onError={() => setImgOk(false)} />
        ) : (
          <div className="plate" style={{ background: plateBg(site.hue) }}>
            <div className="plate-en">
              <div className="t">{site.nameEn}</div>
              <div className="c">{site.countryEn}</div>
            </div>
          </div>
        )}
        <div className="card-scrim" />

        <span className={`badge mode-${site.mode}`}>
          <i className="d" />
          {MODE_LABEL[site.mode].label}
        </span>

        <div className="card-body">
          <div className="card-meta">
            <span>{site.country}</span>
            <i className="sep" />
            <span>{site.year} 列入</span>
            {site.visitors ? (
              <>
                <i className="sep" />
                <span>年 {site.visitors} 万人次</span>
              </>
            ) : null}
          </div>
          <h3 className="card-title">{site.name}</h3>
          <p className="card-sub">{site.nameEn}</p>
          <span className="card-cta">
            查看详情 <IcArrowRight w={14} />
          </span>
        </div>
      </div>
    </article>
  )
}
