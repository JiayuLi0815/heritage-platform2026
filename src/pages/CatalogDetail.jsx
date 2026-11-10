import { useEffect, useMemo, useState } from 'react'
import RAW from '../data/sites-all.json'
import { FEATURED_BY_WHC } from '../data/whc-featured.js'
import { localModels } from '../data/models.js'
import { galleryOf } from '../data/gallery.js'
import ModelStage from '../components/ModelStage.jsx'
import ImageGallery from '../components/ImageGallery.jsx'
import { asset } from '../asset.js'
import { IcArrowLeft, IcCube, IcExternal, IcGlobe, IcImage, IcMap, IcPin } from '../components/icons.jsx'

/* ============================================================
   名录档案页
   ------------------------------------------------------------
   991 处里，只有 20 处进了编码矩阵、有三维资产。其余的在平台上
   不是「待建页面」，也不是拿 20 处的页面套一个空壳——它们各有
   自己的封面图、年代、类别、官方编号和位置，只是还没有模型。
   这一页就只讲这些真有的东西，缺口写在明面上。
   ============================================================ */

const ALL_BY_COUNTRY = RAW.reduce((m, s) => {
  ;(m[s.country] ||= []).push(s)
  return m
}, {})

export default function CatalogDetail({ site, onClose, onOpenSite, onLocateSite }) {
  const [imgOk, setImgOk] = useState(true)
  const local = localModels(site.id)
  const photos = galleryOf(site.id)
  /* 有模型的默认看模型，没模型的直接进画廊——这页原本只有一张封面 */
  const [media, setMedia] = useState(local ? '3d' : 'photos')

  useEffect(() => {
    setImgOk(true)
    setMedia(local ? '3d' : 'photos')
    document.body.classList.add('locked')
    return () => document.body.classList.remove('locked')
  }, [site.id]) // eslint-disable-line react-hooks/exhaustive-deps

  const related = useMemo(
    () =>
      (ALL_BY_COUNTRY[site.country] || [])
        .filter((x) => x.id !== site.id && !FEATURED_BY_WHC[x.id])
        .slice(0, 6),
    [site.id, site.country]
  )

  const showModel = local && media === '3d'
  const showPhotos = !showModel && photos > 0
  const lat = Number(site.lat)
  const lng = Number(site.lng)
  const coord = `${Math.abs(lat).toFixed(2)}°${lat >= 0 ? 'N' : 'S'} ${Math.abs(lng).toFixed(2)}°${lng >= 0 ? 'E' : 'W'}`

  return (
    <div className="cat-detail">
      <div className="cat-stage">
        {showModel ? (
          <ModelStage site={site} models={local} />
        ) : showPhotos ? (
          <ImageGallery site={site} id={site.id} count={photos} />
        ) : (
          <>
            {imgOk ? (
              <img
                className="cat-cover"
                src={asset(`img/sites/${site.id}.webp`)}
                alt={site.name}
                onError={() => setImgOk(false)}
              />
            ) : null}
            <div className={`cat-cover-fallback${imgOk ? ' veiled' : ''}`} aria-hidden="true">
              <IcGlobe w={64} />
              <span>{site.country}</span>
            </div>
          </>
        )}

        <div className="detail-top">
          <div className="detail-top-l">
            <button className="back-btn" onClick={onClose} aria-label="返回">
              <IcArrowLeft w={16} />
              <span className="bt-l">返回</span>
            </button>

            {local && photos ? (
            <div className="stage-tabs" role="tablist" aria-label="查看方式">
              <button
                className={media === '3d' ? 'on' : ''}
                onClick={() => setMedia('3d')}
                role="tab"
                aria-selected={media === '3d'}
              >
                <IcCube w={14} /> 三维模型
              </button>
              <button
                className={media === 'photos' ? 'on' : ''}
                onClick={() => setMedia('photos')}
                role="tab"
                aria-selected={media === 'photos'}
              >
                <IcImage w={14} /> 图像 {photos}
              </button>
            </div>
            ) : null}
          </div>
        </div>

        <div className="cat-cover-meta">
          <span className="k">UNESCO World Heritage · No. {site.id}</span>
          <span className="v">{coord}</span>
        </div>
      </div>

      <aside className="cat-panel">
        <div className="panel-head">
          <div className="panel-eyebrow">
            <IcGlobe w={12} />
            {site.country} · {site.year} 年列入 · {site.category}遗产
          </div>
          <h2 className="panel-title">{site.name}</h2>
          <div className="panel-title-en">{site.nameEn}</div>
          <div className="panel-tags">
            <span className="tag n">{local ? '名录档案 · 三维' : '名录档案'}</span>
            <span className="tag">{site.year}</span>
            <span className="tag">{site.category}遗产</span>
            {site.countries?.length > 1 ? <span className="tag">跨国 · {site.countries.length} 国</span> : null}
          </div>
        </div>

        <div className="cat-body">
          <dl className="cat-facts">
            <div>
              <dt>官方编号</dt>
              <dd>
                {site.page ? (
                  <a href={site.page} target="_blank" rel="noreferrer">
                    {site.id} <IcExternal w={12} />
                  </a>
                ) : (
                  site.id
                )}
              </dd>
            </div>
            <div>
              <dt>列入年份</dt>
              <dd>{site.year}</dd>
            </div>
            <div>
              <dt>遗产类别</dt>
              <dd>{site.category}</dd>
            </div>
            <div>
              <dt>所在国家</dt>
              <dd>
                {site.country}
                {site.countries?.length > 1 ? (
                  <span style={{ display: 'block', fontSize: 11.5, color: 'var(--muted)', fontWeight: 400 }}>
                    {site.countries.join(' / ')}
                  </span>
                ) : null}
              </dd>
            </div>
            <div>
              <dt>坐标</dt>
              <dd>{coord}</dd>
            </div>
            {local ? (
              <div>
                <dt>三维资产</dt>
                <dd>
                  {local.length} 个构件
                  <span style={{ display: 'block', fontSize: 11.5, color: 'var(--muted)', fontWeight: 400 }}>
                    扫描与建模：Integra Tecnologia
                  </span>
                </dd>
              </div>
            ) : null}
            {photos ? (
              <div>
                <dt>图像</dt>
                <dd>
                  {photos} 张
                  <span style={{ display: 'block', fontSize: 11.5, color: 'var(--muted)', fontWeight: 400 }}>
                    来源：TJ数智实验室采集
                  </span>
                </dd>
              </div>
            ) : null}
          </dl>

          <div className="cat-actions">
            {site.page ? (
              <a className="btn btn-line" href={site.page} target="_blank" rel="noreferrer">
                在 UNESCO 名录中查看 <IcExternal w={15} />
              </a>
            ) : null}
            {/* 修改：使用站内地图定位，避免外部地图访问失败。 */}
            <button
              type="button"
              className="btn btn-line"
              onClick={() => onLocateSite(site)}
            >
              <IcMap w={15} /> 在地图上定位
            </button>
          </div>

          {related.length ? (
            <div className="cat-related">
              <div className="cat-related-h">
                <span className="kicker">同一国家的其他遗产</span>
                <span className="c">{ALL_BY_COUNTRY[site.country].length} 处</span>
              </div>
              <div className="cat-related-list">
                {related.map((r) => (
                  <button
                    key={r.id}
                    className="cat-related-item"
                    onClick={() => onOpenSite({ ...r, catalog: true })}
                  >
                    <IcPin w={13} />
                    <span className="nm">{r.name}</span>
                    <span className="yr">{r.year}</span>
                  </button>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      </aside>
    </div>
  )
}
