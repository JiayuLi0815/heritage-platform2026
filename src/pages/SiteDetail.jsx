import { useEffect, useState } from 'react'
import { INDICATORS, MODE_LABEL, SITES, codedOf, scoreOf } from '../data/sites.js'
import { WHC_BY_FEATURED } from '../data/whc-featured.js'
import { localModels } from '../data/models.js'
import { galleryOf } from '../data/gallery.js'
import ModelStage from '../components/ModelStage.jsx'
import ImageGallery from '../components/ImageGallery.jsx'
import { plateBg } from '../components/SiteCard.jsx'
import { asset } from '../asset.js'
import {
  IcArrowLeft, IcChevronL, IcCube, IcExternal, IcGlobe, IcImage, IcInfo, IcRotate, IcScan,
} from '../components/icons.jsx'

const VAL = ['未发现可验证证据', '部分已实施', '已实施']

export default function SiteDetail({ site, onClose, onOpenSite, openHuman, setHumanCtx }) {
  const isC = site.mode === 'C'
  const tabs = [
    { k: 'about', l: '简介' },
    { k: 'archive', l: '数字档案' },
    ...(isC ? [{ k: 'story', l: '时空叙事' }] : []),
    ...(site.mode === 'B' ? [{ k: 'layers', l: '时期分层' }] : []),
    { k: 'score', l: '数字化评估' },
  ]
  const [tab, setTab] = useState(tabs[0].k)
  const [open, setOpen] = useState(true)
  const [hint, setHint] = useState(true)

  const embed = site.sketchfab
    ? `https://sketchfab.com/models/${site.sketchfab}/embed?autospin=0.25&autostart=1&ui_theme=dark&ui_infos=0&dnt=1`
    : null
  // 精选页的 site.id 是 slug，素材盘按 UNESCO 编号存
  const gid = WHC_BY_FEATURED[site.id] ?? site.id
  // 本地压出来的模型优先于 Sketchfab：不依赖外网、可以换构件，画质也比嵌入好
  const local = localModels(gid)
  const photos = galleryOf(gid)
  const [media, setMedia] = useState(local || embed ? '3d' : 'photos')

  useEffect(() => {
    setTab(isC ? 'story' : 'about')
    setOpen(true)
    setHint(true)
    setMedia(local || embed ? '3d' : 'photos')
    const t = setTimeout(() => setHint(false), 5200)
    return () => clearTimeout(t)
  }, [site.id]) // eslint-disable-line react-hooks/exhaustive-deps

  const n = scoreOf(site)
  const codedN = codedOf(site)
  const showModel = media === '3d' && (local || embed)
  const showPhotos = media === 'photos' && photos > 0

  const related = SITES.filter((s) => s.id !== site.id && s.region === site.region).slice(0, 3)

  return (
    <div className="detail">
      {/* 三维 / 影像舞台 */}
      <div className="stage">
        {showModel ? (
          local ? (
            <ModelStage site={site} models={local} id={gid} />
          ) : (
            <iframe
              src={embed}
              title={site.name}
              allow="autoplay; fullscreen; xr-spatial-tracking; gyroscope; accelerometer"
              allowFullScreen
            />
          )
        ) : showPhotos ? (
          <ImageGallery site={site} id={gid} count={photos} />
        ) : (
          <div className="stage-empty">
            {/* 用该处遗产的示意插画当背幕；图缺失时只剩底色，不影响阅读 */}
            <img
              className="stage-empty-bg"
              src={site.image || asset(`img/${site.id}.webp`)}
              alt=""
              onError={(e) => { e.currentTarget.style.display = 'none' }}
            />
            <div className="stage-empty-veil" />
            <div className="stage-empty-inner">
              <div className="t">{site.name}</div>
              <div className="d">
                此处暂无可嵌入的整体三维资产，平台改用「叙事地图」呈现。
                <br />
                右侧面板按时间轴串联这处遗产的空间演变。
              </div>
            </div>
          </div>
        )}
      </div>

      {/* 顶部控件 */}
      <div className="detail-top">
        <div className="detail-top-l">
          <button className="back-btn" onClick={onClose} aria-label="返回">
            <IcArrowLeft w={16} />
            <span className="bt-l">返回</span>
          </button>
          {(local || embed) && photos ? (
            <div className="stage-tabs" role="tablist" aria-label="查看方式">
              <button
                className={media === '3d' ? 'on' : ''}
                onClick={() => setMedia('3d')}
                role="tab"
                aria-selected={media === '3d'}
              >
                <IcCube w={14} /> 三维
              </button>
              <button
                className={media === 'photos' ? 'on' : ''}
                onClick={() => setMedia('photos')}
                role="tab"
                aria-selected={media === 'photos'}
              >
                <IcImage w={14} /> 影像 {photos}
              </button>
            </div>
          ) : null}
        </div>
        {site.license || local || isC ? (
          <span className="lic-badge">
            <IcScan w={13} style={{ color: 'var(--accent-l)' }} />
            <span className="k">{local ? '本地模型' : site.license || '无三维资产'}</span>
            <span className="v">
              {local ? `Integra Tecnologia · ${local.length} 个构件` : site.assetAuthor || MODE_LABEL[site.mode].label}
            </span>
          </span>
        ) : null}
      </div>

      {showModel && (embed || local) && hint ? (
        <div className="stage-hint" style={{ opacity: hint ? 1 : 0 }}>
          <IcRotate w={14} /> 按住拖动可旋转 · 滚轮缩放 · 右上角可全屏
        </div>
      ) : null}

      {/* 侧栏 */}
      <aside className={`panel${open ? '' : ' closed'}`}>
        <button className="panel-handle" onClick={() => setOpen((o) => !o)} aria-label="展开/收起面板">
          <IcChevronL w={15} />
        </button>

        <div className="panel-head">
          <div className="panel-eyebrow">
            <IcGlobe w={12} />
            {site.country} · {site.region} · {site.year} 年列入
            {site.visitors ? ` · 年 ${site.visitors} 万人次` : ''}
          </div>
          <h2 className="panel-title">{site.name}</h2>
          <div className="panel-title-en">{site.nameEn}</div>
          <div className="panel-tags">
            <span className={`tag n`}>{MODE_LABEL[site.mode].label}</span>
            {site.tags.map((t) => (
              <span className="tag" key={t}>
                {t}
              </span>
            ))}
          </div>
        </div>

        <div className="tabs">
          {tabs.map((t) => (
            <button key={t.k} className={`tab${tab === t.k ? ' on' : ''}`} onClick={() => setTab(t.k)}>
              {t.l}
            </button>
          ))}
        </div>

        <div className="panel-body">
          {/* 简介 */}
          {tab === 'about' && (
            <>
              <p className="prose">{site.blurb}</p>
              <div className="block-title">看点</div>
              {site.highlights.map((h, i) => (
                <div className="h-row" key={i}>
                  <span className="h-dot" />
                  <span className="h-text">{h}</span>
                </div>
              ))}
              <div className="block-title">同地区其他遗产</div>
              {related.map((r) => (
                <button key={r.id} className="map-item" onClick={() => onOpenSite(r.id)}>
                  <span className="n">{r.country.slice(0, 1)}</span>
                  <span className="nm">{r.name}</span>
                  <span className="ar">{MODE_LABEL[r.mode].label}</span>
                </button>
              ))}
            </>
          )}

          {/* 数字档案 */}
          {tab === 'archive' && (
            <>
              <div className="block-title">本处资产</div>
              {local ? (
                <div className="mv-assets">
                  {local.map((x) => (
                    <div className="asset" key={x.file}>
                      <span className="asset-ico">
                        <IcCube />
                      </span>
                      <span className="asset-b">
                        <span className="asset-t">{x.label}</span>
                        <span className="asset-m">
                          <span>本地 glTF · meshopt</span>
                          <span className="asset-lic">Integra Tecnologia</span>
                        </span>
                      </span>
                    </div>
                  ))}
                </div>
              ) : site.sketchfab ? (
                <div className="asset">
                  <span className="asset-ico">
                    <IcCube />
                  </span>
                  <span className="asset-b">
                    <span className="asset-t">{site.assetTitle}</span>
                    <span className="asset-m">
                      <span>{site.assetAuthor}</span>
                      <span className="asset-lic">{site.license}</span>
                    </span>
                  </span>
                </div>
              ) : (
                <div className="note-box" style={{ marginTop: 0 }}>
                  未发现可嵌入的整体三维资产。{site.licenseNote}
                </div>
              )}

              {photos ? (
                <div className="asset" style={{ marginTop: 10 }}>
                  <span className="asset-ico">
                    <IcImage />
                  </span>
                  <span className="asset-b">
                    <span className="asset-t">现场影像 {photos} 张</span>
                    <span className="asset-m">
                      <span>webp · 长边 1400px</span>
                      <span className="asset-lic">课题素材盘搜集</span>
                    </span>
                  </span>
                </div>
              ) : null}

              <div className="block-title">数字档案来源</div>
              {site.sources.map((s, i) => (
                <a
                  className="asset"
                  key={i}
                  href={s.url || undefined}
                  target={s.url ? '_blank' : undefined}
                  rel="noreferrer"
                  style={{ cursor: s.url ? 'pointer' : 'default' }}
                >
                  <span className="asset-ico">
                    <IcScan />
                  </span>
                  <span className="asset-b">
                    <span className="asset-t">{s.label}</span>
                    <span className="asset-m">
                      <span className="asset-lic">{s.license}</span>
                      {s.url ? <span>在新窗口打开</span> : null}
                    </span>
                  </span>
                  {s.url ? <IcExternal w={14} style={{ color: 'var(--muted-2)', flexShrink: 0, marginTop: 3 }} /> : null}
                </a>
              ))}

              <div className="note-box">
                <b>许可说明</b>：{site.licenseNote}。
                平台仅嵌入开放许可资产；对禁止框架嵌入的官方平台，只作来源标注与跳转。
              </div>
            </>
          )}

          {/* 时空叙事（C 模式） */}
          {tab === 'story' && (
            <>
              <div className="block-title">空间演变</div>
              <div className="tl">
                {site.narrative?.map((t) => (
                  <div className="tl-item" key={t.year}>
                    <div className="tl-year">{t.year}</div>
                    <div className="tl-text">{t.text}</div>
                  </div>
                ))}
              </div>
              <div className="note-box">
                这处遗产以<b>城市整体</b>列入世界遗产，单体三维扫描无法承载其价值。
                平台因此改用时间轴串联空间，后续将接入历史地图与街景漫游。
              </div>
            </>
          )}

          {/* 时期分层（B 模式） */}
          {tab === 'layers' && (
            <>
              <div className="block-title">同一空间的时间层</div>
              <div className="tl">
                {site.layers?.map((l) => (
                  <div className="tl-item" key={l.era}>
                    <div className="tl-year">{l.era}</div>
                    <div className="tl-text">
                      <b style={{ color: 'var(--ink)' }}>{l.label}</b>
                      <br />
                      {l.note}
                    </div>
                  </div>
                ))}
              </div>
              <div className="note-box">
                三维模型可切换到不同时期的复原状态，对照同一空间的归属变迁。
                分层数据来自公开历史档案，复原部分会明确标注为推断内容。
              </div>
            </>
          )}

          {/* 数字化评估 */}
          {tab === 'score' && (
            <>
              {site.ind ? (
                <>
                  <div className="score-line">
                    <span className="score-n">
                      {n}
                      <span style={{ fontSize: 16, color: 'var(--muted)' }}>/{codedN}</span>
                    </span>
                    <span className="score-d">
                      已给出判定值的 {codedN} 项中「已实施」的项数
                      <br />
                      逐条摘自课题 139 案例编码矩阵，未作换算
                    </span>
                  </div>

                  <div className="ind-grid">
                    {INDICATORS.map((d, i) => (
                      <div className={`ind v${site.ind[i] === -1 ? 'u' : site.ind[i]}`} key={d.key}>
                        <span className="st" />
                        <span className="lb">{d.label}</span>
                        <span className="vv">
                          {site.ind[i] === -1
                            ? '矩阵未判定'
                            : ['未发现', '部分', '已实施'][site.ind[i]]}
                        </span>
                      </div>
                    ))}
                  </div>

                  <div className="ind-legend">
                    <span>
                      <i style={{ background: '#2f9e6f' }} /> 已实施
                    </span>
                    <span>
                      <i style={{ background: '#d9a03c' }} /> 部分已实施
                    </span>
                    <span>
                      <i style={{ background: 'var(--muted-2)' }} /> 未发现可验证证据
                    </span>
                    <span>
                      <i style={{ background: 'transparent', border: '1.5px solid var(--muted-2)' }} /> 矩阵未填写判定值
                    </span>
                  </div>

                  <div className="note-box">
                    指标口径为课题编码矩阵的<b>原始判定值</b>，与主报告的五梯队分数（满分 14 分）口径不同，不做换算。
                    其中「数字技术应用效果」一栏，编码表全部 141 个案例都只填了案例文档名，
                    没有给出判定值，所以本平台记为<b>未判定</b>而不是「未发现可验证证据」——
                    没判不等于判为「无」。
                  </div>
                </>
              ) : (
                <div className="note-box" style={{ marginTop: 0 }}>
                  <b>暂无编码数据</b>：这处遗产不在课题 139 案例的 30 国范围内，八项指标留空。
                  目前以公开三维资产与官方档案作为主要信息源。
                </div>
              )}

              <div className="block-title">交互形式</div>
              <div className="pane" style={{ padding: 18, boxShadow: 'none' }}>
                <h3 style={{ fontSize: 15 }}>{MODE_LABEL[site.mode].label}</h3>
                <p style={{ fontSize: 13, marginTop: 6 }}>{MODE_LABEL[site.mode].desc}</p>
              </div>

              <button className="btn btn-dark" style={{ width: '100%', justifyContent: 'center', marginTop: 18 }} onClick={() => { setHumanCtx(site); openHuman() }}>
                <IcInfo w={16} /> 让数字人讲讲这处遗产
              </button>
            </>
          )}
        </div>
      </aside>
    </div>
  )
}
