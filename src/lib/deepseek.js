import { INDICATORS, MODE_LABEL, SITES } from '../data/sites.js'
import { HONGWEN_SKILL } from './hongwen-skill.js'
import { catalogBrief, catalogFacts } from './catalog-facts.js'

/* ============================================================
   数字人 · DeepSeek 通道
   ------------------------------------------------------------
   弘文的知识底座 = DeepSeek 大模型 + 本团队编著的
   《世界文化遗产数字化研究报告》（2025.11）。
   没有 API Key 时整段自动让位给本地知识检索层（DigitalHuman.jsx 的
   answer()），演示不依赖网络；有 Key 时把当前页面的事实、平台名录和
   报告里的结论作为【资料】一起发过去，再按弘文的说话方式生成回答。

   接入方式：
     VITE_DEEPSEEK_PROXY 优先——指向自建代理（proxy/deepseek-proxy.mjs），
     Key 只在服务器上，前端拿不到，限额也在服务器执行。
     没设代理时用 VITE_DEEPSEEK_API_KEY（.env.local）。**这个 Key 会被打进
     公开 JS**，这是当前阶段的临时做法：等服务器下来，换成代理地址即可，
     前端代码不用改。
   ============================================================ */

const BASE = (import.meta.env.VITE_DEEPSEEK_BASE || 'https://api.deepseek.com').replace(/\/+$/, '')
const PROXY = (import.meta.env.VITE_DEEPSEEK_PROXY || '').replace(/\/+$/, '')

/* 'proxy' | 'key' | 'local' */
export function llmMode() {
  if (PROXY) return 'proxy'
  if (import.meta.env.VITE_DEEPSEEK_API_KEY) return 'key'
  return 'local'
}

export function llmReady() {
  return llmMode() !== 'local'
}

function key() {
  return import.meta.env.VITE_DEEPSEEK_API_KEY || ''
}

/* ---------- 当日用量与预算 ----------
   在浏览器里存 Key 就守不住「别人拿走 Key 直接调接口」，
   所以这里的上限只是演示机的安全带：防止自己把额度跑光。
   真正的限额必须放在代理端（proxy/deepseek-proxy.mjs）。 */
const UKEY = 'dh.deepseek.usage'

/* 官方现价（deepseek-flash，¥/百万 tokens）约为 命中 0.02 / 未命中 1 / 输出 2；
   这里按高价估算（命中 0.5 / 未命中 2 / 输出 8），宁可少算可用余额，也不少算花费。 */
const RATE = { hit: 0.5, miss: 2, out: 8 }

const dayKey = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

export function usageToday() {
  try {
    const u = JSON.parse(localStorage.getItem(UKEY) || 'null')
    if (!u || u.d !== dayKey()) return { d: dayKey(), cny: 0, calls: 0 }
    return u
  } catch {
    return { d: dayKey(), cny: 0, calls: 0 }
  }
}

function costOf(u) {
  if (!u) return 0
  const hit = u.prompt_cache_hit_tokens || u.prompt_tokens_details?.cached_tokens || 0
  const miss = u.prompt_cache_miss_tokens ?? Math.max(0, (u.prompt_tokens || 0) - hit)
  return (hit * RATE.hit + miss * RATE.miss + (u.completion_tokens || 0) * RATE.out) / 1e6
}

function recordUsage(u) {
  const t = usageToday()
  t.cny += costOf(u)
  t.calls += 1
  try {
    localStorage.setItem(UKEY, JSON.stringify(t))
  } catch {
    /* 隐私模式下只是不记账，不拦对话 */
  }
}

export const DAILY_CAP = Number(import.meta.env.VITE_DH_DAILY_CNY || 2)

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
  const mode = llmMode()
  if (mode === 'local') throw new Error('no-key')

  /* 走代理时预算由服务器算，这里不重复拦 */
  if (mode === 'key' && usageToday().cny >= DAILY_CAP) throw new Error('budget')

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

  const headers = { 'Content-Type': 'application/json' }
  if (mode === 'key') headers.Authorization = `Bearer ${key()}`

  try {
    const res = await fetch(mode === 'proxy' ? `${PROXY}/chat` : `${BASE}/chat/completions`, {
      method: 'POST',
      headers,
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
    if (!res.ok) throw new Error(`deepseek ${res.status}`)
    const data = await res.json()
    const text = data?.choices?.[0]?.message?.content?.trim()
    if (!text) throw new Error('empty')
    if (mode === 'key') recordUsage(data.usage)
    return text
  } finally {
    clearTimeout(timer)
  }
}
