import { INDICATORS, MODE_LABEL, SITES } from '../data/sites.js'
import { HONGWEN_SKILL } from './hongwen-skill.js'
import { catalogBrief, catalogFacts } from './catalog-facts.js'

/* ============================================================
   数字人 · DeepSeek 通道
   ------------------------------------------------------------
   弘文的知识底座 = DeepSeek 大模型 + 本团队编著的
   《世界文化遗产数字化研究报告》（2025.11）。
   没配代理地址时整段自动让位给本地知识检索层（DigitalHuman.jsx 的
   answer()），演示不依赖网络；走代理时把当前页面的事实、平台名录和
   报告里的结论作为【资料】一起发过去，再按弘文的说话方式生成回答。

   接入方式（唯一）：
     VITE_DEEPSEEK_PROXY 指向自建代理（proxy/deepseek-proxy.mjs）。
     Key 只存在服务器（proxy/.env 或 systemd 的 Environment），前端拿不到，
     限流与每日预算也在代理端执行。前端不再接受任何 API Key 变量，
     避免 Key 被打进公开 JS。
     没设代理地址时，整段回落到本地知识检索层。
   ============================================================ */

const PROXY = (import.meta.env.VITE_DEEPSEEK_PROXY || '').replace(/\/+$/, '')

/* 'proxy' | 'local' */
export function llmMode() {
  return PROXY ? 'proxy' : 'local'
}

export function llmReady() {
  return llmMode() !== 'local'
}

const VAL = ['未发现可验证证据', '部分已实施', '已实施', '矩阵未判定']

/* 当前页面那一处：只给模型真正用得上的字段，控制 prompt 长度 */
function siteFact(s) {
  if (!s) return ''
  const country = (s.countries?.length ? s.countries.join('、') : s.country || '').replace(/,/g, '、')
  const lines = [
    `名称：${s.name}${s.nameEn ? `（${s.nameEn}）` : ''}`,
    `国家：${country}`,
    s.year ? `列入年份：${s.year}` : '',
    s.category ? `类别：${s.category}` : '',
    s.region ? `地区：${s.region}` : '',
    s.blurb ? `简介：${s.blurb}` : '',
    s.tags?.length ? `标签：${s.tags.join('、')}` : '',
    s.mode ? `平台交互模式：${MODE_LABEL[s.mode]?.label || ''}——${MODE_LABEL[s.mode]?.desc || ''}` : '',
    s.ind
      ? `数字化八项编码：${s.ind.map((v, i) => `${INDICATORS[i].short}=${VAL[v + 1] || '矩阵未判定'}`).join('；')}`
      : '不在研究报告编码的 139 处案例内（该部分只覆盖 30 国），八项指标没有数据。',
  ]
  return lines.filter(Boolean).join('\n')
}

function roster() {
  return SITES.map((s) => `${s.name}｜${s.country}｜${s.year}`).join('\n')
}

/* 许可数据不在页面事实里，但用户问得最多；不给它模型就会答「没有资料」。 */
function licFact() {
  const known = SITES.filter((s) => s.license && s.license !== '许可见模型页')
  const unknown = SITES.filter((s) => s.license === '许可见模型页').length
  const n = SITES.filter((s) => s.sketchfab).length
  return (
    `平台接入三维资产 ${n} 处，均经 Sketchfab 官方嵌入接口调用。` +
    `核到具体条款的 ${known.length} 处：${known.map((s) => `${s.name}（${s.license}）`).join('、')}。` +
    `其余 ${unknown} 处的许可随上传者而异，详情页只标注「许可见模型页」并给出直达链接，平台不代其声明。` +
    `数字敦煌、全景故宫等官方平台禁止 iframe，平台只做来源标注与超链接跳转，不嵌入。` +
    `详情页的现场影像来自课题素材盘，未逐张回溯原始授权，页面统一标注来源，仅作研究展示。`
  )
}

/* 行为规范在 hongwen-skill.js；这里只补平台特有的口径。 */
const SYSTEM = `${HONGWEN_SKILL}

## 平台口径（优先于上面的通用说法）
- 提到数字化程度时，区分「已实施 / 部分已实施 / 未发现可验证证据」；编码矩阵里没填判定值的，说「未判定」，不要说成「没做」。
- 年份、国家、指标以【资料】为准。资料里的「未判定」就照实说，不替课题组补判。
- 【全量名录】就是平台的完整名录（991 处），不是抽样。【与问题相关的名录条目】里的名单是核对过的，问到最早、最新、某个国家有几处，直接给结论和名单。
- 不要说自己「没有完整清单」「不能给出绝对答案」，也不要在正确回答后面补一句「这只是精选范围」之类的免责话。名录只收文化与混合遗产；确实涉及自然遗产时，说明一次不在收录范围就够了。
- 【精选案例】只有 20 处，是页面重点展示用的，不代表名录的全部。`

/* 返回纯文本；失败时抛错，由调用方回落到本地知识库 */
export async function ask(question, { site, history = [] } = {}) {
  if (!PROXY) throw new Error('no-proxy')

  const ctrl = new AbortController()
  const timer = setTimeout(() => ctrl.abort(), 20000)

  /* 名录事实按问题现算：问年份/国家/大洲时才多带一段，避免整份 991 行进 prompt */
  const facts = catalogFacts(question)

  const context =
    `【知识来源】《世界文化遗产数字化研究报告》，本团队（同济大学人文学院 TJ 数智文化大模型虚拟实验室）编著，臧志彭、解学芳主编，2025 年 11 月。用户问数据、结论、案例时，以此报告为准。\n\n` +
    `【当前页面】\n${siteFact(site) || '（不在某一处遗产页，是平台首页或其它栏目）'}\n\n` +
    `【全量名录】${catalogBrief()}\n` +
    `平台接入三维模型 34 处；3 处进入沉浸体验试点。\n` +
    (facts ? `\n【与问题相关的名录条目】\n${facts}\n` : '\n') +
    `\n【资产与许可】${licFact()}\n\n` +
    `【精选案例（名称｜国家｜列入年份）】\n${roster()}`

  try {
    const res = await fetch(`${PROXY}/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      signal: ctrl.signal,
      body: JSON.stringify({
        model: 'deepseek-chat',
        temperature: 0.6,
        max_tokens: 320,
        messages: [
          { role: 'system', content: SYSTEM },
          ...history.slice(-4),
          { role: 'user', content: `【资料】\n${context}\n\n【用户】\n${question}` },
        ],
      }),
    })
    if (res.status === 429) throw new Error('budget')
    if (!res.ok) {
      /* 代理把上游的 401 包成 502 + {status:401,detail}，这里单独标成密钥问题，
         好让界面说「Key 无效」，而不是含糊的「没连上」。 */
      let body = null
      try {
        body = await res.json()
      } catch {
        /* 不是 JSON 就按普通失败处理 */
      }
      const detail = body ? `${body.detail || ''}${JSON.stringify(body)}` : ''
      if (res.status === 401 || body?.status === 401 || /authentication|invalid.*key|unauthorized/i.test(detail)) {
        throw new Error('auth')
      }
      throw new Error(`deepseek ${res.status}`)
    }
    const data = await res.json()
    const text = data?.choices?.[0]?.message?.content?.trim()
    if (!text) throw new Error('empty')
    return text
  } finally {
    clearTimeout(timer)
  }
}
