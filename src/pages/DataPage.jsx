import { IcArrowRight, IcShield } from '../components/icons.jsx'

/* 数据源页：三段式——名录、前期研究、平台整理。
   只讲「这份内容从哪来」，不铺逐件清单。 */
export default function DataPage() {
  return (
    <div>
      <div className="wrap page-head">
        <span className="kicker">数据来源</span>
        <h1>数据与资产来源</h1>
        <p className="title-en">DATA SOURCES</p>
        <p>
          平台以 UNESCO 世界遗产名录为基础，结合前期世界文化遗产数字化研究成果，
          整合遗产基础信息与数字化实践资料，为全球文化遗产的查询、探索与沉浸式体验提供内容支持。
        </p>
      </div>

      <div className="wrap" style={{ paddingBottom: 90 }}>
        <section className="ds-sec">
          <div className="ds-side">
            <span className="ds-num">01 / 世界遗产名录</span>
            <span className="ds-en">UNESCO World Heritage Centre</span>
          </div>
          <div className="ds-body">
            <p>
              平台以联合国教科文组织世界遗产中心公布的世界遗产名录为基础，
              整理遗产名称、所在国家或地区、列入年份及遗产价值等信息。
            </p>
            <div className="ds-scope">
              <span className="ds-scope-n">991</span>
              <span className="ds-scope-l">项文化遗产</span>
              <span className="ds-scope-d">
                平台聚焦世界遗产名录中的文化遗产类别，涵盖不同国家和地区的历史建筑、
                考古遗址、历史城市及文化景观等遗产类型。
              </span>
            </div>
            <a className="ds-link" href="https://whc.unesco.org/en/list/" target="_blank" rel="noreferrer">
              访问 UNESCO 世界遗产名录 <IcArrowRight w={14} />
            </a>
          </div>
        </section>

        <section className="ds-sec">
          <div className="ds-side">
            <span className="ds-num">02 / 前期研究成果</span>
            <span className="ds-en">《世界文化遗产数字化研究报告》</span>
          </div>
          <div className="ds-body">
            <p>
              2025 年由实验室发布的研究成果，系统考察全球世界文化遗产的数字化发展现状，
              构建包含三重维度、七大指标的数字化评估体系，为理解不同遗产地的数字化实践提供研究基础。
            </p>
            <p>
              本平台在此基础上，进一步探索世界文化遗产信息的数字化组织、智能化阐释与沉浸式体验设计。
            </p>
          </div>
        </section>

        <section className="ds-sec">
          <div className="ds-side">
            <span className="ds-num">03 / 平台整理与拓展</span>
            <span className="ds-en">Heritage Data &amp; Digital Experience</span>
          </div>
          <div className="ds-body">
            <p>
              在基础名录与前期研究成果的基础上，平台进一步整理遗产相关资料，
              并结合具体遗产的可用资源，拓展图像、三维模型、数字导览及沉浸式交互内容。
            </p>
            <p>
              不同遗产的数字资源与体验内容可能有所差异，具体以平台实际展示为准。
            </p>
          </div>
        </section>

        <div className="note-box ds-note">
          <b>
            <IcShield w={14} style={{ verticalAlign: '-2px', marginRight: 6 }} />
            数据说明
          </b>
          <br />
          遗产基础信息以 UNESCO 官方公开资料为主要依据。研究报告中的统计数据与评估结论对应其发布时的研究范围；
          平台展示内容以实际收录的数据及相应资料来源为准。
        </div>
      </div>
    </div>
  )
}
