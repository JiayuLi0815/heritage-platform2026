import { SITES } from '../data/sites.js'
import { IcCube, IcDatabase, IcGlobe, IcShield } from '../components/icons.jsx'

const SOURCES = [
  {
    n: '《世界文化遗产数字化研究报告》',
    d: '本团队（TJ 数智文化大模型虚拟实验室）编著，臧志彭、解学芳主编，2025.11。含 139 案例编码矩阵（30 国 × 8 维度）、五梯队评估结果与 17 处深度案例文本。本平台全部指标数据与数字人知识库的来源。',
    lic: '课题组内部',
    warn: true,
  },
  {
    n: 'Sketchfab',
    d: '全球最大的可下载三维模型社区，10 万余件文化遗产模型。本平台 19 处遗产的三维资产均通过其官方嵌入接口调用：3 件核到了具体许可（秘鲁文化部、CyArk，以及一件由上传者声明 CC0 的模型），其余 16 件许可随上传者而异，详情页只标注「许可见模型页」并给出直达链接。',
    lic: '站点内逐件标注',
    warn: false,
  },
  {
    n: 'CyArk / Open Heritage 3D',
    d: '约 591 个数据集、覆盖 47 处世界遗产，多为激光扫描与摄影测量成果。许可绝大多数为 CC BY-NC(-SA/-ND)，需填表申请下载，给的是 E57 / LAS 点云，须自行转换与自托管。',
    lic: '多数 CC BY-NC',
    warn: true,
  },
  {
    n: 'UNESCO 世界遗产中心',
    d: '各遗产的列入年份、编号、保护范围与官方描述，作为每处案例的基础档案信息。',
    lic: '公开档案',
    warn: false,
  },
  {
    n: 'OpenStreetMap / Natural Earth',
    d: '地图页的矢量国界底图，用于经纬度落点展示。',
    lic: 'ODbL / 公有领域',
    warn: false,
  },
]

export default function DataPage() {
  const withAsset = SITES.filter((s) => s.sketchfab).length
  /* 许可已核到具体条款的只有机构官方号那几处；其余在详情页只标注「许可见模型页」，
     许可随上传者而异，平台不代其声明。这个数字要如实露出来。 */
  const perPage = SITES.filter((s) => s.license === '许可见模型页').length

  return (
    <div>
      <div className="wrap page-head">
        <span className="kicker">数据源</span>
        <h1>数据与资产来源</h1>
        <p>
          平台的每一条指标都可以追溯到课题的编码矩阵，每一件三维资产都标注了来源与许可。
          这一页把所有来源、许可类型与使用限制集中说明。
        </p>
      </div>

      <div className="wrap">
        <div className="stats" style={{ marginBottom: 40 }}>
          <div className="stat">
            <div className="stat-n">139</div>
            <div className="stat-l">编码案例总数</div>
            <div className="stat-d">本平台选取其中 20 处</div>
          </div>
          <div className="stat">
            <div className="stat-n">{withAsset}</div>
            <div className="stat-l">已接入三维资产</div>
            <div className="stat-d">余 1 处走叙事地图模式</div>
          </div>
          <div className="stat">
            <div className="stat-n">
              {perPage}
              <small>/{withAsset}</small>
            </div>
            <div className="stat-l">许可须回模型页核对</div>
            <div className="stat-d">其余 {withAsset - perPage} 处已核到 CC0 / CC BY / CC BY-NC</div>
          </div>
          <div className="stat">
            <div className="stat-n">5</div>
            <div className="stat-l">类数据来源</div>
            <div className="stat-d">见下方逐条说明</div>
          </div>
        </div>

        <div className="section-head" style={{ marginBottom: 20 }}>
          <div>
            <span className="kicker">
              <IcDatabase w={13} /> 来源清单
            </span>
            <h2>五类数据来源</h2>
          </div>
        </div>

        {SOURCES.map((s) => (
          <div className="src-row" key={s.n}>
            <span className="src-ico">
              <IcDatabase />
            </span>
            <span className="src-b">
              <span className="src-n">{s.n}</span>
              <span className="src-d">{s.d}</span>
            </span>
            <span className={`src-lic${s.warn ? ' warn' : ''}`}>{s.lic}</span>
          </div>
        ))}

        {/* 逐件资产 */}
        <div className="section tight" style={{ paddingTop: 60 }}>
          <div className="section-head" style={{ marginBottom: 20 }}>
            <div>
              <span className="kicker">
                <IcCube w={13} /> 资产清单
              </span>
              <h2>二十处遗产的三维资产与许可</h2>
            </div>
          </div>

          <div className="src-row" style={{ background: 'var(--bg-2)', border: 'none', padding: '12px 18px', fontSize: 11.5, color: 'var(--muted)', letterSpacing: '0.06em' }}>
            <span style={{ flex: 1 }}>遗产</span>
            <span style={{ width: 220 }}>模型 / 作者</span>
            <span style={{ width: 150 }}>交互形式</span>
          </div>

          {SITES.map((s) => (
            <div className="src-row" key={s.id}>
              <span className="src-ico" style={{ background: 'rgba(31,26,21,0.05)', color: 'var(--ink-2)' }}>
                <IcGlobe />
              </span>
              <span className="src-b">
                <span className="src-n">
                  {s.name}
                  <span style={{ fontWeight: 400, color: 'var(--muted)', marginLeft: 8, fontSize: 12.5 }}>{s.country}</span>
                </span>
                <span className="src-d">
                  {s.assetTitle ? (
                    <>
                      {s.assetTitle} · {s.assetAuthor}
                    </>
                  ) : (
                    '暂无整体三维资产，采用叙事地图模式'
                  )}
                </span>
              </span>
              <span style={{ width: 150, textAlign: 'right' }}>
                <span className={`src-lic${s.mode === 'C' ? ' warn' : ''}`}>
                  {s.mode === 'A' ? '三维漫游' : s.mode === 'B' ? '破坏—重建' : '叙事地图'}
                </span>
              </span>
            </div>
          ))}
        </div>

        {/* 使用声明 */}
        <div className="section tight" style={{ paddingBottom: 80 }}>
          <div className="disclaimer">
            <h4>
              <IcShield w={14} style={{ verticalAlign: '-2px', marginRight: 6 }} />
              使用与版权声明
            </h4>
            <p>
              <b>一、三维资产。</b>平台嵌入的 {withAsset} 件三维模型全部来自公开渠道，经 Sketchfab 官方嵌入接口调用。
              其中<b>只有 4 件核到了具体许可条款</b>：马丘比丘（Ministerio de Cultura Perú，CC BY 4.0）、
              特奥蒂瓦坎（上传者声明 CC0）、敦煌莫高窟（s10255129，CC0）、悉尼歌剧院（CyArk，CC BY-NC）。
              CC0 与 CC BY 类可自由使用与二次加工，CC BY-NC 类仅限非商业用途，本平台为学术研究用途，合规使用。
              其余 <b>{perPage} 件的许可随上传者而异</b>，平台在详情页只标注
              <b>「许可见模型页」</b>并给出直达链接，不代其声明许可；正式版本会逐件核到具体条款。
              另需注意：详情页「数字档案来源」里列的机构档案（如韩国国家遗产厅、奈良文化财研究所）
              是<b>该处遗产的参考资源</b>，与本平台嵌入的那件模型不一定是同一份资产。
            </p>
            <p>
              <b>二、官方平台的引用边界。</b>平台对官方数字资源一律
              <b>不作框架嵌入，只作来源标注与超链接跳转</b>。这不是保守起见——实测（2026-10 检测）
              数字敦煌返回 <code>X-Frame-Options: SAMEORIGIN</code>，
              全景故宫返回 <code>CSP frame-ancestors 'self' *.dpm.org.cn</code>，技术上就嵌不进去；
              Google Arts &amp; Culture 等其余平台的响应头在本环境无法直连验证，平台同样不去尝试。
              法律上也不该绕：依据欧盟法院 <i>VG Bild-Kunst v. SPK</i>（C-392/19）判决，
              绕过防嵌入措施构成独立的「向公众传播」行为，需另行取得授权。
            </p>
            <p>
              <b>三、指标数据的口径。</b>详情页展示的八项指标取值（已实施 / 部分已实施 / 未发现可验证证据）逐条
              摘自课题 139 案例编码矩阵的「评价」列，与主报告的五梯队分数（满分 14 分）分属不同口径，
              本平台不做换算，避免误读。马丘比丘与吉萨金字塔不在课题 139 案例的 30 国范围内，指标栏留空。
            </p>
            <p>
              <b>四、一处必须讲清楚的数据缺口。</b>编码矩阵八个维度中，前七项逐条给出了三级判定，
              而第八项<b>「数字技术应用效果」在全部 141 个案例里都没有填写判定值</b>——
              「评价」列填的都是各自的案例文档名（如「庆州历史区·数字化应用报告.docx」）。
              本平台因此把这一项记为<b>「矩阵未判定」</b>，既不算作「已实施」，也不归入「未发现可验证证据」：
              没判不等于判为「无」。平台的图表与数字人问答都保留了这一空缺，不替课题组补判。
            </p>
            <p>
              <b>五、卡片配图。</b>首页与列表页每处遗产的配图为<b>平台自制的示意插画</b>，
              用于统一视觉风格，<b>不是实景影像，也不作为遗产形制、材质与环境的实证依据</b>。
              插画由生成式模型产出，个别画面中可能残留不可辨识的装饰性笔触或小尺寸图形，
              均不指代任何真实的文字、铭文与人物。正式版本将逐步替换为已获授权的开放许可摄影
              （如 Wikimedia Commons 的 CC 授权影像）或三维模型的官方渲染图，并逐张标注作者与许可。
            </p>
            <p>
              <b>六、现场影像。</b>详情页画廊里的 <b>7,374 张</b>照片来自课题素材盘（按每处遗产搜集整理），
              平台只把原图压成 1400px 的 webp，内容与构图未做改动。这批图片<b>尚未逐张回溯到原始站点与授权条款</b>，
              因此页面上一律标注「课题素材盘搜集」，<b>不声明其为开放许可或官方影像</b>；
              当前仅作研究用途展示，正式版本会逐张补齐作者与许可，核实不了的一律撤下。
              三维模型同理：详情页标注的「Integra Tecnologia」出自模型文件自带的版权元数据，
              与模型一起展示，不另作转述。
            </p>
            <p>
              <b>七、覆盖范围。</b><b>991 处</b>是平台的收录规模；其中只有 <b>18 处</b>进入课题的编码矩阵、
              <b>34 处</b>接入了三维模型——20 处外链公开资产、16 处本机扫描，其中 2 处两套都有；
              <b>3 处</b>进入沉浸体验试点，另有 17 处附录案例有完整分析文本。
              这四个分母互不相同，平台不把它们合并成一个「991 全覆盖」的说法——
              收录、解读、体验是三件事，覆盖多少就说多少。
              名录取自课题原始表（998 条），剔除重复与无法核校坐标的条目后，991 处进入平台并在图上落点；
              经纬度依 UNESCO 官方描述与 Wikidata 交叉核定。
            </p>
            <p>
              <b>八、署名。</b>本平台由同济大学人文学院 TJ 数智文化大模型虚拟实验室开发，
              数据基础为《世界文化遗产数字化研究报告》（2025.11）。请勿将本平台的编码数据用于商业用途。
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
