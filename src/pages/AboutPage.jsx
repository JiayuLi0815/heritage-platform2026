import { IcLayers, IcSparkle } from '../components/icons.jsx'
import './AboutPage.css'

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

/* 团队分工：直接罗列，不做头像卡片 */
const TEAM = [
  { role: '总策划', names: '臧志彭、解学芳' },
  { role: '设计总监', names: '李家瑜、王翼菲' },
  { role: '学术总监', names: '周迪晨' },
  { role: '数字人形象设计', names: '王紫琪' },
  { role: '研究团队', names: '魏赛、雷晨、贺雅楠、张思懿' },
  { role: 'AI技术支持', names: '张金玉团队' },
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
            </div>
          </div>

          <ul className="team-list">
            {TEAM.map((t) => (
              <li key={t.role}>
                <span className="team-role">{t.role}</span>
                <span className="team-names">{t.names}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  )
}
