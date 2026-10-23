import { useEffect, useRef, useState } from 'react'
import { INDICATORS, MODE_LABEL, SITES, codedOf, scoreOf } from '../data/sites.js'
import { asset } from '../asset.js'
import { ask, llmMode, llmReady } from '../lib/deepseek.js'
import { YEAR_RE, yearAnswer } from '../lib/catalog-facts.js'
import { siteIntro } from '../data/intros.js'
import { IcChat, IcClose, IcPin, IcSend } from './icons.jsx'

/* 数字人形象：由课题组提供的数字人视频转成动图（WebP），
   同一张头像用在悬浮球、面板标题与消息气泡上；
   打开面板时用较大的半身像，发出第一条提问后收起，把空间让给对话。 */
const AVATAR = asset('img/human-avatar.webp')
const AVATAR_STILL = asset('img/human-avatar-still.webp')
const PORTRAIT = asset('img/human-full.webp')

/* ============================================================
   本地知识检索层
   ------------------------------------------------------------
   在没有接入大模型的情况下，先用研究报告里的 139 案例编码做规则检索，
   保证演示时"问什么都有真答案"。接上 LLM 后，这段作为
   RAG 的检索结果直接塞进 prompt 即可（见文件末尾 TODO）。
   ============================================================ */

const TECH = [
  { i: 4, label: '区块链技术', re: /区块链|链上|存证|确权|数字藏品/ },
  { i: 3, label: 'XR 沉浸式交互', re: /xr|vr|ar|沉浸|虚拟现实|增强现实|混合现实/ },
  { i: 2, label: '三维数字扫描', re: /三维|3d|扫描|建模|点云|lidar|摄影测量/ },
  { i: 5, label: '人工智能技术', re: /人工智能|\bai\b|大模型|算法|智能识别|机器学/ },
  { i: 1, label: '数字漫游系统', re: /漫游|虚拟导览|全景|线上游览|数字展厅/ },
  { i: 0, label: '本国数字化政策', re: /政策|战略|规划|法规|政府支持|制度/ },
  { i: 7, label: '数字技术应用效果', re: /效果|评估|绩效|成效|测量/ },
]

const list = (arr) => arr.map((s) => s.name).join('、')

/* 简称 → 平台的正式名称。游客不会照着名录念全称，
   「莫高窟」「故宫」「金字塔」这些说法要能落到具体那一处。 */
const ALIAS = {
  莫高窟: '敦煌莫高窟',
  敦煌: '敦煌莫高窟',
  故宫: '北京及沈阳故宫',
  金字塔: '孟菲斯与吉萨金字塔',
  吉萨: '孟菲斯与吉萨金字塔',
  黑格拉: '玛甸沙勒（黑格拉）',
  玛甸沙勒: '玛甸沙勒（黑格拉）',
  庞贝: '庞贝与赫库兰尼姆',
  罗马: '罗马历史中心',
  佛罗伦萨: '佛罗伦萨历史中心',
  奈良: '古都奈良',
  庆州: '庆州历史区',
  耶路撒冷: '耶路撒冷古城及城墙',
  圣彼得堡: '圣彼得堡历史中心',
  悉尼: '悉尼歌剧院',
}

/* 主题串联：数字人从「讲解员」变成「向导」的那一步。
   不是自由生成——是在已编码案例的 tags 上做检索，再给出一条可辩护的
   「为什么该放在一起看」。推荐理由里的每一点都能对回某处遗产的标签。 */
const THEMES = [
  {
    re: /城市|规划|公共生活|街道|广场|聚落|urban/i,
    label: '古代城市与公共生活',
    tag: /城市|宫廷|城墙|层叠|新罗王都/,
    why:
      '这几处的共同点不是风格，而是它们都把公共空间当成建造的第一件事：' +
      '街道怎么接、广场放在哪、供水和防御的边界怎么画。把它们的平面放在一起，比看任何一处的高清立面都更能说明问题。',
  },
  {
    re: /建筑|结构|木构|石构|工程|构造/i,
    label: '建筑与结构',
    tag: /木构|石构|哥特|巴洛克|现代主义|建筑|双塔/,
    why:
      '木构、石构、壳体三种体系在这里各有一处代表。比较它们的跨度、受力与材料极限，' +
      '能看出同一个问题——「怎么把屋顶撑起来」——被不同文明解出了完全不同的答案。',
  },
  {
    re: /宗教|信仰|仪式|神圣|寺庙|教堂|祭祀/i,
    label: '宗教空间与仪式',
    tag: /佛教艺术|伊斯兰|三大宗教|哥特|金字塔|崖居/,
    why:
      '这组里既有为单一信仰建的，也有几种信仰叠在同一块地上的。' +
      '看后者比看前者有用：同一处空间被不同仪式反复占用，建筑才会留下改写的痕迹。',
  },
  {
    re: /贸易|迁徙|交流|丝路|商路|运河/i,
    label: '贸易、迁徙与文化交流',
    tag: /丝绸之路|纳巴泰|运河|文艺复兴/,
    why:
      '这几处的价值不在单体，在位置上：它们都卡在人或货必须经过的节点上。' +
      '把地图缩小到能同时看见它们，路线本身就是内容。',
  },
  {
    re: /天文|历法|测量|方位|测绘/i,
    label: '天文、测量与方位',
    tag: /天文|测绘|金字塔/,
    why:
      '这组的共同点是「先把天看明白，再决定房子怎么摆」。' +
      '建筑在这里不是审美问题，是一台测量仪器——看它的朝向、轴线和开口就够判断了。',
  },
  {
    re: /防御|边界|军事|要塞|城墙/i,
    label: '防御与边界',
    tag: /军事防御|城墙|线性遗产/,
    why:
      '一处是线性防御，一处是多层城墙，尺度差了两个量级。' +
      '但两处都在回答同一个问题：一条边界要守多厚，才值得为它修一座城。',
  },
]

export function answer(raw, route) {
  const q = raw.trim()
  const t = q.toLowerCase()

  /* 0 · 自我认知 */
  if (/你是谁|你叫什么|介绍你自己|你是什么|谁在说话/.test(q)) {
    return {
      text:
        '我是弘文，这个平台的文化向导。背后是 DeepSeek 大模型，加上我们团队编著的《世界文化遗产数字化研究报告》——' +
        '里面编码了 **30 个国家、139 处世界文化遗产、8 个数字化维度**。\n' +
        '所以我不只介绍某一处。你告诉我你对什么感兴趣，我把散在各洲的几处串起来，说清为什么该放在一起看；' +
        '需要的话，再把它拆成一张能带进现场的探索任务卡。\n' +
        '试试：「我对古代城市感兴趣」「哪些遗产和宗教有关」「数字化程度最高的是哪几处」。',
    }
  }

  /* 0.5 · 打招呼与客套：不往检索分支里掉，否则回一张菜单，像没听懂 */
  if (/^[\s，。！？!?~]*(你好啊?|您好|hi|hello|嗨|哈喽|在吗|在不在)[\s，。！？!?~]*$/i.test(q)) {
    return {
      text:
        '你好。我是弘文，这个平台的文化向导。\n' +
        '报一处遗产的名字，或者说个感兴趣的主题（古代城市、宗教空间、贸易路线都行），我给你一条能看下去的线。',
    }
  }
  if (/^[\s，。！？!?~]*(谢谢|多谢|感谢|好的|明白|明白了|懂了|收到)[\s，。！？!?~]*$/i.test(q)) {
    return { text: '不客气。还想看哪一处，随时说。' }
  }

  /* 1 · 点名某处遗产（先认全称，再认「莫高窟」这类简称） */
  const alias = Object.keys(ALIAS).find((k) => q.includes(k))
  const site =
    SITES.find((s) => q.includes(s.name) || t.includes(s.nameEn.split('(')[0].trim().toLowerCase())) ||
    (alias ? SITES.find((s) => s.name === ALIAS[alias]) : null)
  if (site) {
    const n = scoreOf(site)
    const pick = (v) => (site.ind ? INDICATORS.filter((_, i) => site.ind[i] === v).map((d) => d.short) : [])
    const codedN = site.ind ? site.ind.filter((v) => v >= 0).length : null
    const done = pick(2)
    const gap = pick(0)
    const part = pick(1)
    const undet = pick(-1)
    return {
      text:
        `**${site.name}**（${site.country}，${site.year} 年列入）——\n${site.blurb}\n\n` +
        (n === null
          ? '这处不在研究报告编码的 139 处案例内（那部分只覆盖 30 国），没有指标数据。目前以三维资产与公开档案为主。'
          : `八项指标中已给出判定值的是 ${codedN} 项，其中 **${n}/${codedN} 为「已实施」**` +
            (done.length ? `（${done.join('、')}）` : '') +
            (part.length ? `；**${part.join('、')}** 为「部分已实施」` : '') +
            (gap.length ? `；**${gap.join('、')}** 未发现可验证证据` : '') +
            (undet.length ? `。另有 **${undet.join('、')}** 在编码矩阵里没有填写判定值` : '。')) +
        `\n\n交互模式：${MODE_LABEL[site.mode].label}——${MODE_LABEL[site.mode].desc}。`,
      sites: [{ id: site.id, name: site.name }],
    }
  }

  /* 1.5 · 名录范围的问题：最早、最新、哪一批。按全量 991 处算，不给「精选范围」的托词 */
  if (YEAR_RE.test(q) && /遗产|列入|名录|哪一批|哪一个/.test(q)) {
    return { text: yearAnswer() }
  }

  /* 2 · 问某项技术 */
  const tech = TECH.find((x) => x.re.test(t))
  if (tech) {
    const yes = SITES.filter((s) => s.ind && s.ind[tech.i] === 2)
    const part = SITES.filter((s) => s.ind && s.ind[tech.i] === 1)
    const none = SITES.filter((s) => s.ind && s.ind[tech.i] === 0)
    const undet = SITES.filter((s) => s.ind && s.ind[tech.i] === -1)
    const codedN = SITES.filter((s) => s.ind).length
    if (!yes.length && !part.length && !none.length) {
      return {
        text:
          `本平台收录的 18 处已编码案例中，**「${tech.label}」这一栏在研究报告里没有填写判定值**——` +
          `141 个案例的「评价」列填的都只是案例文档名。所以这一项目前既不能说已实施，也不能说未实施。`,
      }
    }
    return {
      text:
        `在已编码的 ${codedN} 处案例中，**${tech.label}** 已实施的有 **${yes.length} 处**：\n${list(yes) || '—'}` +
        (part.length ? `\n\n部分已实施（${part.length} 处）：${list(part)}` : '') +
        (none.length ? `\n\n未发现可验证证据（${none.length} 处）：${list(none)}` : '') +
        (undet.length ? `\n\n矩阵未填写判定值（${undet.length} 处）：${list(undet)}` : '') +
        `\n\n（取值逐条摘自 139 案例编码矩阵的「评价」列，三级口径：已实施 / 部分已实施 / 未发现可验证证据。）`,
      sites: yes.slice(0, 8).map((s) => ({ id: s.id, name: s.name })),
    }
  }

  /* 3 · 按地区 / 国家 */
  const REGIONS = ['亚洲', '欧洲', '美洲', '非洲', '大洋洲']
  const region = REGIONS.find((r) => q.includes(r))
  if (region) {
    const arr = SITES.filter((s) => s.region === region)
    return {
      text: `**${region}**共收录 ${arr.length} 处：\n${arr.map((s) => `· ${s.name}（${s.country}）`).join('\n')}`,
      sites: arr.map((s) => ({ id: s.id, name: s.name })),
    }
  }
  const byCountry = SITES.filter((s) => q.includes(s.country))
  if (byCountry.length) {
    return {
      text: `**${byCountry[0].country}**在本平台收录 ${byCountry.length} 处：\n${byCountry.map((s) => `· ${s.name}（${s.year} 年列入）`).join('\n')}`,
      sites: byCountry.map((s) => ({ id: s.id, name: s.name })),
    }
  }

  /* 3.5 · 按主题串联（向导行为：不是回答一处，是给一条线） */
  const theme = THEMES.find((x) => x.re.test(q))
  if (theme) {
    const arr = SITES.filter((s) => theme.tag.test(s.tags.join(' ')))
    if (arr.length) {
      /* 优先挑有整体三维资产的：能进现场看，才谈得上「建议从这处开始」 */
      const lead = arr.find((s) => s.mode === 'A') || arr[0]
      return {
        text:
          `围绕「**${theme.label}**」，平台里有 **${arr.length} 处**可以直接放在一起看：\n` +
          arr.map((s) => `· **${s.name}**（${s.country}）— ${s.tags.slice(0, 2).join('、')}`).join('\n') +
          `\n\n${theme.why}\n\n` +
          `如果要进现场，我建议从 **${lead.name}** 开始——` +
          `它的资产是 ${MODE_LABEL[lead.mode].label}，一条任务线能跑完。` +
          `想的话我可以把它拆成四步任务：进现场看什么、找什么、怎么解释、回来和哪一处对。`,
        sites: arr.map((s) => ({ id: s.id, name: s.name })),
      }
    }
  }

  /* 4 · 问数字化程度 / 排名 */
  if (/最高|最好|最强|领先|排名|程度|梯队|哪个好|哪些好|先进/.test(q)) {
    const ranked = SITES.filter((s) => s.ind)
      .map((s) => ({ s, n: scoreOf(s), d: codedOf(s) }))
      .sort((a, b) => b.n - a.n)
    const max = ranked[0].n
    const lead = ranked.filter((r) => r.n === max)
    const second = ranked.filter((r) => r.n === max - 1)
    return {
      text:
        `按「已实施项数」排序，**领先的是这 ${lead.length} 处**（各 ${max}/${lead[0].d} 项）：\n` +
        lead.map((r) => `· ${r.s.name}（${r.s.country}）`).join('\n') +
        (second.length
          ? `\n\n紧随其后是 ${second.length} 处 ${max - 1}/${second[0].d} 的案例，包括` +
            `${second.slice(0, 4).map((r) => r.s.name).join('、')}等。\n\n`
          : '\n\n') +
        `要说清楚的是：分差全部来自**区块链**和**人工智能**两项——` +
        `政策、漫游、三维扫描、XR、「其他技术」这五项，18 处案例里全部是「已实施」。\n\n` +
        `而第八项**「数字技术应用效果」在研究报告里根本没有判定值**：` +
        `141 个案例的这一栏填的都只是各自的案例文档名。也就是说，八项维度中唯一无法给出结论的，` +
        `恰恰是判断这些投入有没有用的那一项。这不是某处遗产的短板，是整批案例的共同缺口。`,
      sites: lead.map((r) => ({ id: r.s.id, name: r.s.name })),
    }
  }

  /* 5 · 问许可 / 版权 / 能不能用 */
  if (/版权|许可|授权|商用|侵权|能用吗|能不能用|法律|版权问题/.test(q)) {
    return {
      text:
        '平台的三维资产**全部来自公开渠道**，许可已核到具体条款的分三档：\n' +
        '· **CC0 / CC BY**——可自由使用与二次加工，如特奥蒂瓦坎（CC0）、敦煌莫高窟（CC0）、马丘比丘（秘鲁文化部，CC BY 4.0）\n' +
        '· **CC BY-NC**——非商业用途合规，如 CyArk 的悉尼歌剧院\n' +
        '· **机构专有许可**——如沙特黑格拉的 CyArk Tapestry，明示教育与非商业可嵌入\n\n' +
        '要说清楚的是：**19 处里只有 4 处核到了上面对应的条款，其余 15 处取自 Sketchfab 的社区与混合来源模型，' +
        '许可随上传者而异**，平台在详情页只标注「许可见模型页」并给出直达链接，不代其声明许可。\n\n' +
        '另外，**官方平台不能嵌入**：实测数字敦煌返回 X-Frame-Options: SAMEORIGIN、' +
        '全景故宫返回 frame-ancestors \'self\' *.dpm.org.cn，技术上无法 iframe，' +
        '法律上绕过也构成独立的「向公众传播」（CJEU C-392/19）。' +
        '所以本平台只嵌入开放许可资产，官方资源仅作来源标注与跳转。',
    }
  }

  /* 6 · 问数据来源 */
  if (/数据|来源|矩阵|139|编码|报告|课题/.test(q)) {
    return {
      text:
        '来自我们团队——同济大学人文学院 TJ 数智文化大模型虚拟实验室——编著的《世界文化遗产数字化研究报告》（2025.11）：\n' +
        '· **139 案例编码矩阵**——30 国 × 8 维度，逐条判定「已实施 / 部分已实施 / 未发现可验证证据」。' +
        '需要说明的是，八个维度里只有前七项填了判定值，「数字技术应用效果」这一栏 141 个案例填的都是案例文档名，本平台按原样记为「未判定」\n' +
        '· **五梯队评估**——991 处遗产按 14 分制分为领先 / 突破 / 进取 / 探索 / 起步\n' +
        '· **附录案例**——17 处深度案例的完整分析文本\n\n' +
        '三维资产则来自 Sketchfab 的机构官方号与开放社区，每处的来源与许可在详情页「数字档案」标签里逐条列出。',
    }
  }

  /* 7 · 兜底：先承认答不了，再给可行的问法——不甩一张菜单装作答完了 */
  return {
    text:
      '这个我这儿没有现成的答案。平台内的事实我能一条条查——哪处遗产、某类技术、某个国家收录了几处、资产能不能用；' +
      '需要判断和展开的问题，得有更大的资料支撑才答得稳。\n' +
      '你可以换个说法再问一次：报一处遗产的名字，说一类技术（区块链、XR、三维扫描），或者说个主题（古代城市、宗教空间、贸易路线）。',
  }
}

/* ============================================================
   遗产页的开场介绍——知识库直出，不走大模型
   ------------------------------------------------------------
   文案在 data/intros.js：精选 20 处逐处手写，其余名录条目
   只写能核实的字段（所在地、列入年份、类别），不替平台编。
   ============================================================ */
export { siteIntro }

/* ============================================================ */

const QUICK = [
  '我对古代城市感兴趣',
  '哪些遗产和宗教有关',
  '数字化程度最高的是哪几处？',
  '这些资产能商用吗？',
]

function Rich({ text }) {
  return text.split(/(\*\*[^*]+\*\*)/g).map((p, i) =>
    p.startsWith('**') && p.endsWith('**') ? (
      <strong key={i}>{p.slice(2, -2)}</strong>
    ) : (
      <span key={i}>{p}</span>
    )
  )
}

export default function DigitalHuman({ open, setOpen, onOpenSite, contextSite, pageSite }) {
  const [msgs, setMsgs] = useState([])
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState(false)
  const [intro, setIntro] = useState(false)
  const mode = llmMode()
  const logRef = useRef(null)
  const siteId = pageSite?.id

  /* 进了某一处遗产页，左下角自己冒出一句话；12 秒后退回悬浮球，
     点它就开面板。换一处遗产（siteId 变）会重新说一遍。 */
  useEffect(() => {
    if (!pageSite || open) {
      setIntro(false)
      return undefined
    }
    setIntro(true)
    const t = setTimeout(() => setIntro(false), 12000)
    return () => clearTimeout(t)
  }, [siteId, open]) // eslint-disable-line react-hooks/exhaustive-deps

  // 首次打开时给一句开场白（若带着具体遗产进入，则介绍该处）
  useEffect(() => {
    if (!open || msgs.length) return
    const s = pageSite || contextSite
    const first = s
      ? siteIntro(s)
      : '你好，我是弘文，这个平台的文化向导。我不只介绍某一处——你说一个感兴趣的主题，我把散在各洲的几处串起来，说明为什么该放在一起看，再给你一条能带进现场的路线。'
    setMsgs([{ role: 'ai', text: first }])
  }, [open]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (logRef.current) logRef.current.scrollTop = logRef.current.scrollHeight
  }, [msgs, busy])

  const send = async (text) => {
    const q = (text ?? input).trim()
    if (!q || busy) return
    setInput('')
    setMsgs((m) => [...m, { role: 'me', text: q }])
    setBusy(true)

    /* 有 Key 就走 DeepSeek，没有或请求失败就回落到本地知识检索层，
       演示时不会因为断网或额度用完而哑掉。 */
    const history = msgs
      .filter((m) => m.role === 'me' || m.role === 'ai')
      .slice(-4)
      .map((m) => ({ role: m.role === 'me' ? 'user' : 'assistant', content: m.text }))

    if (llmReady()) {
      try {
        const reply = await ask(q, { site: pageSite || contextSite, history })
        setMsgs((m) => [...m, { role: 'ai', text: reply }])
        setBusy(false)
        return
      } catch (err) {
        const local = answer(q)
        const note =
          err?.message === 'budget'
            ? '（今天的模型额度用完了，明天自动重置；先用本地知识库答你。）'
            : '（大模型这会儿没连上，先用本地知识库答你。）'
        setMsgs((m) => [...m, { role: 'ai', text: `${local.text}\n\n${note}`, sites: local.sites }])
        setBusy(false)
        return
      }
    }

    // 本地知识库：留一点思考时间，避免答案比问句先到
    const delay = 420 + Math.min(q.length * 26, 700)
    setTimeout(() => {
      setBusy(false)
      setMsgs((m) => [...m, { role: 'ai', ...answer(q) }])
    }, delay)
  }

  if (!open) {
    return (
      <>
        {intro && pageSite ? (
          <button className="dh-bubble" onClick={() => setOpen(true)}>
            <span className="dh-bubble-t">{siteIntro(pageSite)}</span>
            <span className="dh-bubble-go">问弘文 →</span>
          </button>
        ) : null}
        <button className="dh-fab" onClick={() => setOpen(true)}>
          <span className="dh-fab-orb">
            <img src={AVATAR} alt="" />
          </span>
          <span className="dh-fab-t">
            <span className="a">有问题，问问弘文</span>
          </span>
        </button>
      </>
    )
  }

  return (
    <section className="dh-panel" role="dialog" aria-label="数字人对话">
      <header className="dh-head">
        <span className="dh-head-orb">
          <img src={AVATAR} alt="" />
        </span>
        <span className="dh-head-t">
          <span className="a">弘文 · 文化向导</span>
          <span className={`dh-mode${mode !== 'local' ? ' on' : ''}`}>
            <i />
            {mode === 'local' ? '本地知识库 · 研究报告' : 'DeepSeek 已接入 · 研究报告知识库'}
          </span>
        </span>
        <button className="dh-close" onClick={() => setOpen(false)} aria-label="收起">
          <IcClose w={15} />
        </button>
      </header>

      {/* 开场时站在这里；用户一提问就收起来，不占对话区 */}
      <div className={`dh-stage${msgs.length > 1 ? ' off' : ''}`} aria-hidden={msgs.length > 1}>
        <span className="dh-stage-t">
          <span className="k">数字人 · 弘文</span>
          <strong>你好，我是弘文</strong>
          <span className="s">文化向导。说个感兴趣的主题，我把散在各洲的几处串起来，再给你一条进现场的路线。</span>
        </span>
        <img className="dh-stage-img" src={PORTRAIT} alt="数字人弘文的半身像" />
      </div>

      <div className="dh-log" ref={logRef}>
        {msgs.map((m, i) => (
          <div key={i} className={`msg ${m.role === 'me' ? 'me' : 'ai'}`}>
            <span className="msg-av">
              {m.role === 'me' ? '我' : <img src={AVATAR_STILL} alt="" />}
            </span>
            <div className="bubble">
              <span style={{ whiteSpace: 'pre-line' }}>
                <Rich text={m.text} />
              </span>
              {m.sites?.length ? (
                <span className="mini">
                  {m.sites.map((s) => (
                    <button
                      key={s.id}
                      className="qchip"
                      style={{ marginRight: 6, marginTop: 6 }}
                      onClick={() => onOpenSite(s.id)}
                    >
                      <IcPin w={11} style={{ verticalAlign: '-1px', marginRight: 4 }} />
                      {s.name}
                    </button>
                  ))}
                </span>
              ) : null}
            </div>
          </div>
        ))}
        {busy ? (
          <div className="msg ai">
            <span className="msg-av">
              <img src={AVATAR_STILL} alt="" />
            </span>
            <div className="bubble typing">
              <i />
              <i />
              <i />
            </div>
          </div>
        ) : null}
      </div>

      <div className="dh-quick">
        {QUICK.map((k) => (
          <button key={k} className="qchip" onClick={() => send(k)}>
            {k}
          </button>
        ))}
      </div>

      <div className="dh-input">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && send()}
          placeholder="问一处遗产，或跨案例的问题…"
        />
        <button className="dh-send" onClick={() => send()} disabled={!input.trim() || busy} aria-label="发送">
          <IcSend w={17} />
        </button>
      </div>
    </section>
  )
}

export { IcChat }
