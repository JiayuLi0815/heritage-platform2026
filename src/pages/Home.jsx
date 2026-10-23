import { useMemo, useState } from 'react'
import { INDICATORS, MODE_LABEL, REGIONS, SITES } from '../data/sites.js'
import SiteCard from '../components/SiteCard.jsx'
import { asset } from '../asset.js'
import { IcArrowRight, IcChevronL, IcChevronR, IcGlobe, IcSparkle } from '../components/icons.jsx'

/* 四步闭环。四项能力不是并列的入口，是一条走得到底的路径——
   每一步都以前一步的结果为输入，第四步回到第一步。 */
const JOURNEY = [
  {
    n: '01', en: 'Discover', t: '发现',
    d: '991 处遗产按真实经纬度落点。缩小看各大洲的分布与数量，放大到一处就是一条档案——点开即读，有三维模型的直接进场景。',
    k: '对应能力 · 全球名录 + 交互地图',
  },
  {
    n: '02', en: 'Understand', t: '理解',
    d: '问它为什么。数字人由 DeepSeek 驱动、以我们团队的《世界文化遗产数字化研究报告》为底座，能回答跨案例的问题，也会把报告里没判定的地方直说。',
    k: '对应能力 · 知识库 + 智能问答',
  },
  {
    n: '03', en: 'Experience', t: '体验',
    d: '走进一处现场。精选的遗产做成可交互的三维场景——站着不动，靠看、指、转，把空间读一遍。',
    k: '对应能力 · 三维资产 + WebXR',
  },
  {
    n: '04', en: 'Connect', t: '关联',
    d: '带走一份档案：你发现了什么、它和哪几处有关。平台据此推荐下一处，让理解连成链，而不是停在孤点上。',
    k: '对应能力 · 知识网络 + 探索档案',
  },
]

const SLIDES = [
  { id: 'mogao', hue: 25, cap: '敦煌莫高窟 · 735 个洞窟、4.5 万平方米壁画' },
  { id: 'machu-picchu', hue: 150, cap: '马丘比丘 · 秘鲁文化部的官方三维扫描已开放下载' },
  { id: 'teotihuacan', hue: 42, cap: '特奥蒂瓦坎 · 十二万人的「众神之城」，模型完全开放' },
]

export default function Home({ onOpenSite, setView, openHuman }) {
  const [region, setRegion] = useState('全部')
  const [mode, setMode] = useState('全部')
  const [slide, setSlide] = useState(0)

  const s = SLIDES[slide]

  const filtered = useMemo(
    () =>
      SITES.filter(
        (x) => (region === '全部' || x.region === region) && (mode === '全部' || x.mode === mode)
      ),
    [region, mode]
  )

  /* 八项指标在已编码案例中的分布（从课题编码矩阵逐条读出，真实计算） */
  const coded = SITES.filter((x) => x.ind)
  const indStats = INDICATORS.map((d, i) => ({
    ...d,
    v2: coded.filter((x) => x.ind[i] === 2).length,
    v1: coded.filter((x) => x.ind[i] === 1).length,
    v0: coded.filter((x) => x.ind[i] === 0).length,
    vU: coded.filter((x) => x.ind[i] === -1).length,
    total: coded.length,
  }))

  return (
    <>
      {/* ============ HERO ============ */}
      <header className="hero">
        <div className="hero-fallback" />
        {SLIDES.map((sl, i) => (
          <img
            key={sl.id}
            className={`hero-bg${i === slide ? ' on' : ''}`}
            src={asset(`img/hero-${sl.id}.webp`)}
            alt=""
            onError={(e) => { e.currentTarget.style.display = 'none' }}
          />
        ))}
        <div className="hero-scrim" />

        <div className="hero-inner">
          <span className="hero-kicker">发现 · 理解 · 体验 · 关联</span>
          <h1>
            世界文化遗产
            <br />
            数智探索与沉浸式体验平台
          </h1>
          <p className="hero-title-en">
            World Cultural Heritage Digital Exploration &amp; Immersive Experience Platform
          </p>
          <p className="hero-org">同济大学人文学院 · TJ 数智文化大模型虚拟实验室</p>
          <p className="hero-lead">
            从全球发现到沉浸探索，
            <br />
            构建连接文化遗产、智能知识与数字体验的交互平台。
          </p>
          <div className="hero-actions">
            <button className="btn btn-fill" onClick={() => setView('map')}>
              开始探索 <IcArrowRight w={16} />
            </button>
            <button className="btn btn-line" onClick={() => setView('about')}>
              了解项目
            </button>
          </div>
        </div>

        <div className="hero-carousel">
          <span className="car-cap">{s.cap}</span>
          <button className="car-arrow" onClick={() => setSlide((p) => (p - 1 + SLIDES.length) % SLIDES.length)} aria-label="上一处">
            <IcChevronL w={15} />
          </button>
          <div className="car-dots">
            {SLIDES.map((_, i) => (
              <button key={i} className={`car-dot${i === slide ? ' on' : ''}`} onClick={() => setSlide(i)} aria-label={`第 ${i + 1} 处`} />
            ))}
          </div>
          <button className="car-arrow" onClick={() => setSlide((p) => (p + 1) % SLIDES.length)} aria-label="下一处">
            <IcChevronR w={15} />
          </button>
        </div>
      </header>

      {/* ============ 统计 ============ */}
      <section className="section tight">
        <div className="wrap">
          <div className="stats">
            <div className="stat">
              <div className="stat-n">139</div>
              <div className="stat-l">编码案例</div>
              <div className="stat-d">30 个国家 · 逐条人工编码</div>
            </div>
            <div className="stat">
              <div className="stat-n">8</div>
              <div className="stat-l">数字化维度</div>
              <div className="stat-d">政策 / 漫游 / 扫描 / XR / 链 / AI / 其他 / 效果</div>
            </div>
            <div className="stat">
              <div className="stat-n">20</div>
              <div className="stat-l">本期上线案例</div>
              <div className="stat-d">19 处已接入三维资产</div>
            </div>
            <div className="stat">
              <div className="stat-n">
                6<small>/18</small>
              </div>
              <div className="stat-l">区块链技术「已实施」</div>
              <div className="stat-d">八项维度里差距最大的一项</div>
            </div>
          </div>
        </div>
      </section>

      {/* ============ 四步闭环 ============ */}
      <section className="section">
        <div className="wrap">
          <div className="section-head">
            <div>
              <span className="kicker">体验逻辑</span>
              <h2>一个闭环，四步走到底</h2>
              <p className="sub">
                地图、数字人、三维场景不是三个并列的功能入口，是同一件事的四步。
                走完一步，下一步才有意义；第四步把人送回来，落到另一处遗产上。
              </p>
            </div>
          </div>

          <div className="journey">
            {JOURNEY.map((j) => (
              <div className="jstep" key={j.n}>
                <div className="jstep-top">
                  <span className="jstep-n">{j.n}</span>
                  <span className="jstep-en">{j.en}</span>
                </div>
                <div className="jstep-t">{j.t}</div>
                <p className="jstep-d">{j.d}</p>
                <div className="jstep-k">{j.k}</div>
              </div>
            ))}
          </div>

          <div className="cover-strip">
            <span className="cover-strip-l">覆盖范围</span>
            <span><b>991</b> 处收录</span>
            <span><b>18</b> 处进入编码矩阵</span>
            <span><b>34</b> 处有三维模型</span>
            <span><b>3</b> 处做沉浸体验（试点）</span>
          </div>
          <p className="sub" style={{ marginTop: 14, maxWidth: 760 }}>
            广泛收录解决覆盖面，智能关联解决理解深度，精选体验解决沉浸感——三件事不在一个量级上，
            平台不把它们混成一个数字来说。
          </p>
        </div>
      </section>

      {/* ============ 遗产名录 ============ */}
      <section className="section" id="sites">
        <div className="wrap">
          <div className="section-head">
            <div>
              <span className="kicker">精选遗产</span>
              <h2>二十处世界文化遗产</h2>
              <p className="sub">
                每处遗产按可获得的公开数字资产，走三种不同的交互形式——三维漫游、破坏与重建、叙事地图。
              </p>
            </div>
            <button className="btn btn-line" onClick={() => setView('map')}>
              <IcGlobe w={16} /> 在地图上查看
            </button>
          </div>

          <div className="filters">
            <div className="filter-group">
              <span className="filter-label">地区</span>
              {REGIONS.map((r) => (
                <button key={r} className={`chip${region === r ? ' on' : ''}`} onClick={() => setRegion(r)}>
                  {r}
                </button>
              ))}
            </div>
            <div className="filter-group">
              <span className="filter-label">交互形式</span>
              {['全部', ...Object.keys(MODE_LABEL)].map((m) => (
                <button key={m} className={`chip${mode === m ? ' on' : ''}`} onClick={() => setMode(m)}>
                  {m === '全部' ? '全部' : MODE_LABEL[m].label}
                </button>
              ))}
            </div>
          </div>

          <div className="grid">
            {filtered.length ? (
              filtered.map((site, i) => (
                <SiteCard key={site.id} site={site} index={i} onOpen={onOpenSite} />
              ))
            ) : (
              <div className="empty">
                <strong>没有符合条件的遗产</strong>
                换一个地区或交互形式试试
              </div>
            )}
          </div>
        </div>
      </section>

      {/* ============ 指标分布 ============ */}
      <section className="section" style={{ background: 'var(--bg-2)' }}>
        <div className="wrap">
          <div className="section-head">
            <div>
              <span className="kicker">数据洞察</span>
              <h2>八项指标的分布</h2>
              <p className="sub">
                把本平台收录案例的编码值逐条汇总，会看到两条裂缝：一是<b>技术栈严重偏科</b>——
                政策、漫游、三维扫描、XR 与「其他技术」五项全部 18/18，而区块链只有 6 处、人工智能 14 处；
                二是<b>第八项「数字技术应用效果」在编码矩阵里根本没有判定值</b>，
                141 个案例的「评价」列填的都只是案例文档名。没判，不等于判为「无」——
                平台把这个状态原样留出，不代课题组补判。
              </p>
            </div>
          </div>

          <div className="ind-bars">
            {indStats.map((d) => (
              <div className="ind-bar-row" key={d.key}>
                <span className="ind-bar-name">{d.label}</span>
                <span className="ind-bar-track">
                  <i className="s2" style={{ width: `${(d.v2 / d.total) * 100}%` }} />
                  <i className="s1" style={{ width: `${(d.v1 / d.total) * 100}%` }} />
                  <i className="s0" style={{ width: `${(d.v0 / d.total) * 100}%` }} />
                  <i className="su" style={{ width: `${(d.vU / d.total) * 100}%` }} />
                </span>
                <span className="ind-bar-val">
                  {d.vU === d.total ? (
                    <em className="undet">未判定</em>
                  ) : (
                    <>
                      {d.v2}
                      <small>/{d.total}</small>
                    </>
                  )}
                </span>
              </div>
            ))}
          </div>

          <div className="ind-bar-legend">
            <span><i style={{ background: '#2f9e6f' }} /> 已实施</span>
            <span><i style={{ background: '#d9a03c' }} /> 部分已实施</span>
            <span><i className="z" /> 未发现可验证证据</span>
            <span><i className="u" /> 矩阵中未填写判定值</span>
            <span className="note">分母为已纳入课题编码矩阵的 18 处；马丘比丘与吉萨不在 30 国范围内</span>
          </div>

          <div className="ind-callout">
            <b>为什么这一栏空着值得单说。</b>
            课题组为每一处案例都写了「数字技术应用效果」文档，但编码表里这一列始终没有回填判定值。
            结果就是：八项维度中唯一无法给出结论的，恰恰是判断「这些投入有没有用」的那一项。
            平台保留这个空缺，正是为了让缺口本身可见——它比任何一处遗产的分数都更能说明供给侧的问题。
          </div>
        </div>
      </section>

      {/* ============ 数字人 ============ */}
      <section className="section">
        <div className="wrap">
          <div className="dh-band">
            <div>
              <span className="kicker">第二步 · 理解</span>
              <h2>它不只是讲解员，是带你找线索的向导</h2>
              <p className="sub">
                讲解词回答「这是什么」。向导要回答三件事：为什么值得看、和什么有关、接下来去哪。
              </p>
              <div className="dh-feats">
                <div className="dh-feat">
                  <span className="n">01</span>
                  <span>
                    <span className="t">理解眼前这一处</span>
                    <span className="d">
                      你正在看哪处遗产，它就从编码矩阵里调出那一行：七项判定值、缺口在哪、交互形式是什么。不念通用稿。
                    </span>
                  </span>
                </div>
                <div className="dh-feat">
                  <span className="n">02</span>
                  <span>
                    <span className="t">发现遗产之间</span>
                    <span className="d">
                      说一句「我对古代城市感兴趣」，它按主题把散在各洲的几处串起来，并说清为什么该放在一起看——不是给一张推荐列表。
                    </span>
                  </span>
                </div>
                <div className="dh-feat">
                  <span className="n">03</span>
                  <span>
                    <span className="t">把人送进现场</span>
                    <span className="d">
                      聊到某个空间问题时，它把它转成一张探索任务卡：进 VR 之后要找什么、看什么、回来怎么对上。
                    </span>
                  </span>
                </div>
              </div>
              <div className="hero-actions" style={{ justifyContent: 'flex-start', marginTop: 30 }}>
                <button className="btn btn-fill" onClick={openHuman}>
                  <IcSparkle w={16} /> 现在问它一个问题
                </button>
              </div>
            </div>
            <div className="dh-orb-wrap">
              <div className="dh-orb" onClick={openHuman}>
                <img src={asset('img/human-avatar.webp')} alt="数字人弘文" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ============ 进入现场 ============ */}
      <section className="section tight">
        <div className="wrap">
          <div className="vr-band">
            <div>
              <span className="kicker">第三步 · 体验</span>
              <h2>《遗产现场》：不是把模型搬进头显</h2>
              <p>
                三维浏览解决的是「看得到」。我们要的是「读得出来」——在一处现场里观察、发现、解释、关联，
                最后带走一份自己的探索档案，而不是一个看完就关的模型。
              </p>
              <div className="loop">
                <b>观察</b><em>→</em><b>发现</b><em>→</em><b>解释</b><em>→</em><b>关联</b>
              </div>
            </div>
            <div className="vr-band-r">
              <button className="btn btn-fill" onClick={() => setView('vr')}>
                看这个体验怎么做 <IcArrowRight w={16} />
              </button>
              <span className="hint">一处做透，不做 991 个能转一圈的空场景</span>
            </div>
          </div>
        </div>
      </section>
    </>
  )
}
