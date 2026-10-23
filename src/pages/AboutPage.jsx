import { IcLayers, IcSparkle } from '../components/icons.jsx'

const LEVELS = [
  {
    n: '01',
    en: 'Global discovery',
    t: '全球发现',
    d: '通过 991 个世界文化遗产建立全球文化探索入口。',
  },
  {
    n: '02',
    en: 'Intelligent understanding',
    t: '智能理解',
    d: '通过 AI 数字人、知识库与关联信息，帮助用户理解遗产的历史、文化与价值。',
  },
  {
    n: '03',
    en: 'Immersive experience',
    t: '沉浸体验',
    d: '通过 VR 交互与情境叙事，让用户在虚拟空间中主动探索遗产。',
  },
]

/* 成员卡片先留位，等信息确认后只改这里 */
const STUDENTS = ['01', '02', '03', '04'].map((n) => ({
  n,
  name: `同学 ${n}`,
  r: '姓名与分工待补',
}))

const MENTORS = [
  { n: '指', name: '指导教师', r: '姓名待补 · 同济大学人文学院' },
  { n: '企', name: '企业合作单位', r: '单位名称待补' },
]

export default function AboutPage() {
  return (
    <div>
      <div className="wrap" style={{ paddingTop: 'calc(var(--nav-h) + 40px)' }}>
        {/* 一、项目背景 */}
        <div className="section tight">
          <div className="section-head">
            <div>
              <span className="kicker">
                <IcSparkle w={13} /> 项目背景
              </span>
              <h2>从全球发现，到智能理解，再到沉浸体验</h2>
            </div>
          </div>

          <div className="cards-3">
            {LEVELS.map((l) => (
              <div className="pane" key={l.n}>
                <span className="kicker">
                  {l.n} · {l.en}
                </span>
                <h3>{l.t}</h3>
                <p>{l.d}</p>
              </div>
            ))}
          </div>

          <div className="pane intro-pane">
            <span className="kicker">平台简介</span>
            <p>
              平台整合
              991 个世界文化遗产的基础信息与空间分布，通过交互地图、AI 数字人和知识库，为用户提供遗产浏览、
              文化问答与关联探索服务。同时，平台以精选遗产为基础，探索三维场景、互动叙事与 VR
              沉浸体验的融合，让用户通过观察、交互与任务探索，进一步理解遗产的历史背景、空间特征及文化价值。
              平台致力于构建从全球遗产发现、智能文化理解到沉浸式体验的连续探索路径，
              为世界文化遗产的数字阐释、公众传播与交互体验创新提供实践参考。
            </p>
          </div>
        </div>

        {/* 二、项目组成员 */}
        <div className="section tight" style={{ paddingBottom: 80 }}>
          <div className="section-head">
            <div>
              <span className="kicker">
                <IcLayers w={13} /> 团队
              </span>
              <h2>项目组成员</h2>
              <p className="sub">
                平台由同济大学人文学院 TJ 数智文化大模型虚拟实验室组织，学生成员四人，另有指导教师与企业合作单位。
              </p>
            </div>
          </div>

          <div className="cards-4">
            {STUDENTS.map((p) => (
              <div className="pane person todo" key={p.n}>
                <div className="person-av">{p.name.slice(-2)}</div>
                <div className="person-n">{p.name}</div>
                <div className="person-r">{p.r}</div>
              </div>
            ))}
          </div>

          <div className="cards-2" style={{ marginTop: 18 }}>
            {MENTORS.map((p) => (
              <div className="pane person todo" key={p.n}>
                <div className="person-av">{p.n}</div>
                <div className="person-n">{p.name}</div>
                <div className="person-r">{p.r}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
