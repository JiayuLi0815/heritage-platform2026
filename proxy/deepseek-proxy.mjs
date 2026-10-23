#!/usr/bin/env node
/* ============================================================
   弘文 · DeepSeek 代理
   ------------------------------------------------------------
   为什么需要它：放在浏览器里的 Key，任何人都能在 DevTools 里看到并拿走。
   只要这个平台是公开可访问的，前端就不能持有真 Key。这个代理把 Key 留在
   服务器上，前端只拿到一个地址；限额、限流、每日预算都在这里执行。

   启动（Key 只走环境变量，不写进任何文件）：
     DEEPSEEK_API_KEY=sk-xxx node proxy/deepseek-proxy.mjs
   可选环境变量：
     PORT=8787                 端口
     ALLOW_ORIGINS=https://你的域名,http://localhost:5175
                               允许的浏览器来源（逗号分隔）；默认只放行本地开发
     DAILY_CNY=2               每日总预算（元），按偏高的费率估算
     PER_IP_MIN=6              单 IP 每分钟请求数
     PER_IP_DAY=60             单 IP 每日请求数
     MAX_TOKENS=320            单次回答的 token 上限（硬顶，前端传多大都没用）
     STORE=./proxy/usage.json  账本落盘位置（重启不丢当日用量）

   前端接入：构建时设 VITE_DEEPSEEK_PROXY=http://127.0.0.1:8787
   没有这个变量时，前端仍走「本地存 Key」或「本地知识库」。
   ============================================================ */

import http from 'node:http'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = path.dirname(fileURLToPath(import.meta.url))

const KEY = process.env.DEEPSEEK_API_KEY || ''
const PORT = Number(process.env.PORT || 8787)
const DAILY_CNY = Number(process.env.DAILY_CNY || 2)
const PER_IP_MIN = Number(process.env.PER_IP_MIN || 6)
const PER_IP_DAY = Number(process.env.PER_IP_DAY || 60)
const MAX_TOKENS = Number(process.env.MAX_TOKENS || 320)
const MAX_BODY = 64 * 1024
const UPSTREAM = (process.env.DEEPSEEK_BASE || 'https://api.deepseek.com').replace(/\/+$/, '')
const STORE = process.env.STORE || path.join(HERE, 'usage.json')

const ALLOW = (process.env.ALLOW_ORIGINS || 'http://localhost:5175,http://localhost:4173,http://127.0.0.1:5175')
  .split(',')
  .map((s) => s.trim())
  .filter(Boolean)

if (!KEY) {
  console.error('缺少 DEEPSEEK_API_KEY 环境变量，代理不启动。')
  process.exit(1)
}

/* 与前端一致的保守费率（¥/百万 tokens）：命中 0.5 / 未命中 2 / 输出 8。
   宁可比实际多算，不让预算悄悄被跑穿。 */
const RATE = { hit: 0.5, miss: 2, out: 8 }

const dayKey = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function blank() {
  return { d: dayKey(), cny: 0, calls: 0, ips: {}, minute: {} }
}

function load() {
  try {
    const raw = JSON.parse(fs.readFileSync(STORE, 'utf8'))
    if (raw.d === dayKey()) return raw
  } catch {
    /* 第一次运行或文件坏了：重开一本新账 */
  }
  return blank()
}

let ledger = load()
let lastMinute = Math.floor(Date.now() / 60000)

function save() {
  try {
    fs.mkdirSync(path.dirname(STORE), { recursive: true })
    fs.writeFileSync(STORE, JSON.stringify(ledger))
  } catch (e) {
    console.error('账本写盘失败：', e.message)
  }
}

function costOf(u) {
  if (!u) return 0
  const hit = u.prompt_cache_hit_tokens || u.prompt_tokens_details?.cached_tokens || 0
  const miss = u.prompt_cache_miss_tokens ?? Math.max(0, (u.prompt_tokens || 0) - hit)
  return (hit * RATE.hit + miss * RATE.miss + (u.completion_tokens || 0) * RATE.out) / 1e6
}

function resetIfNewDay() {
  if (ledger.d !== dayKey()) ledger = blank()
  const m = Math.floor(Date.now() / 60000)
  if (m !== lastMinute) {
    lastMinute = m
    ledger.minute = {}
  }
}

function cors(origin) {
  if (!origin) return {} // 非浏览器请求（curl / 服务端）不需要 CORS
  if (!ALLOW.includes(origin)) return null
  return {
    'Access-Control-Allow-Origin': origin,
    'Access-Control-Allow-Methods': 'POST, GET, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Access-Control-Max-Age': '86400',
    Vary: 'Origin',
  }
}

function send(res, status, body, headers = {}) {
  const payload = typeof body === 'string' ? body : JSON.stringify(body)
  res.writeHead(status, {
    'Content-Type': 'application/json; charset=utf-8',
    'Cache-Control': 'no-store',
    ...headers,
  })
  res.end(payload)
}

function clientIp(req) {
  const xff = req.headers['x-forwarded-for']
  if (typeof xff === 'string' && xff.length) return xff.split(',')[0].trim()
  return req.socket.remoteAddress || 'unknown'
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let size = 0
    const chunks = []
    req.on('data', (c) => {
      size += c.length
      if (size > MAX_BODY) {
        reject(new Error('too-large'))
        req.destroy()
        return
      }
      chunks.push(c)
    })
    req.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')))
    req.on('error', reject)
  })
}

/* 只放行「一轮对话」需要的东西：模型写死、字数封顶、消息条数封顶。
   前端传任何 model / max_tokens 都以这里为准。 */
function sanitize(raw) {
  let body
  try {
    body = JSON.parse(raw)
  } catch {
    return { error: '请求不是合法 JSON' }
  }
  const msgs = Array.isArray(body?.messages) ? body.messages : null
  if (!msgs || !msgs.length) return { error: '缺少 messages' }
  if (msgs.length > 12) return { error: '消息条数过多' }

  let chars = 0
  const messages = []
  for (const m of msgs) {
    if (!m || (m.role !== 'system' && m.role !== 'user' && m.role !== 'assistant')) {
      return { error: '消息格式不对' }
    }
    const content = typeof m.content === 'string' ? m.content : ''
    chars += content.length
    if (content.length > 8000) return { error: '单条消息过长' }
    messages.push({ role: m.role, content })
  }
  if (chars > 24000) return { error: '上下文过长' }

  return {
    messages,
    model: 'deepseek-chat',
    temperature: Math.min(Number(body.temperature) || 0.6, 0.8),
    max_tokens: Math.min(Number(body.max_tokens) || MAX_TOKENS, MAX_TOKENS),
  }
}

const server = http.createServer(async (req, res) => {
  const origin = req.headers.origin
  const corsHeaders = cors(origin)
  if (corsHeaders === null) return send(res, 403, { error: 'origin', message: '来源不在白名单内' })

  if (req.method === 'OPTIONS') {
    res.writeHead(204, corsHeaders)
    return res.end()
  }

  const url = new URL(req.url || '/', 'http://localhost')

  if (req.method === 'GET' && url.pathname === '/health') {
    resetIfNewDay()
    return send(
      res,
      200,
      {
        ok: true,
        date: ledger.d,
        callsToday: ledger.calls,
        spentToday: Number(ledger.cny.toFixed(4)),
        dailyLimit: DAILY_CNY,
        perIpPerMinute: PER_IP_MIN,
        perIpPerDay: PER_IP_DAY,
      },
      corsHeaders
    )
  }

  if (req.method !== 'POST' || url.pathname !== '/chat') {
    return send(res, 404, { error: 'not-found' }, corsHeaders)
  }

  resetIfNewDay()
  const ip = clientIp(req)

  if (ledger.cny >= DAILY_CNY) {
    return send(res, 429, { error: 'budget', message: '今日模型额度已用完，明天自动重置' }, corsHeaders)
  }

  const ipDay = ledger.ips[ip] || { calls: 0, cny: 0 }
  if (ipDay.calls >= PER_IP_DAY) {
    return send(res, 429, { error: 'ip-day', message: '今天的提问次数到上限了' }, corsHeaders)
  }
  const ipMin = ledger.minute[ip] || 0
  if (ipMin >= PER_IP_MIN) {
    return send(res, 429, { error: 'rate', message: '问得太快了，歇一下再来' }, corsHeaders)
  }
  ledger.minute[ip] = ipMin + 1

  let raw
  try {
    raw = await readBody(req)
  } catch (e) {
    return send(res, 413, { error: e.message }, corsHeaders)
  }

  const reqBody = sanitize(raw)
  if (reqBody.error) return send(res, 400, { error: reqBody.error }, corsHeaders)

  const ctrl = new AbortController()
  const timer = setTimeout(() => ctrl.abort(), 25000)

  try {
    const up = await fetch(`${UPSTREAM}/chat/completions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${KEY}` },
      signal: ctrl.signal,
      body: JSON.stringify(reqBody),
    })

    const text = await up.text()
    if (!up.ok) {
      /* 上游的错误原样透出，但不带 Key、不写日志明细 */
      return send(res, up.status === 401 ? 502 : up.status, { error: 'upstream', status: up.status, detail: text.slice(0, 300) }, corsHeaders)
    }

    let data
    try {
      data = JSON.parse(text)
    } catch {
      return send(res, 502, { error: 'upstream-bad-json' }, corsHeaders)
    }

    const spend = costOf(data.usage)
    ledger.cny += spend
    ledger.calls += 1
    ledger.ips[ip] = { calls: ipDay.calls + 1, cny: ipDay.cny + spend }
    save()

    return send(
      res,
      200,
      {
        choices: [{ message: { role: 'assistant', content: data?.choices?.[0]?.message?.content || '' } }],
        usage: data.usage,
        spentToday: Number(ledger.cny.toFixed(4)),
      },
      corsHeaders
    )
  } catch (e) {
    const aborted = e.name === 'AbortError'
    return send(res, aborted ? 504 : 502, { error: aborted ? 'timeout' : 'network' }, corsHeaders)
  } finally {
    clearTimeout(timer)
  }
})

server.listen(PORT, () => {
  console.log(`DeepSeek 代理已启动：http://127.0.0.1:${PORT}`)
  console.log(`每日预算 ¥${DAILY_CNY} · 单 IP ${PER_IP_MIN} 次/分 · ${PER_IP_DAY} 次/日 · 单次 ≤ ${MAX_TOKENS} tokens`)
  console.log(`允许来源：${ALLOW.join(', ')}`)
  console.log(`账本：${STORE}`)
})
