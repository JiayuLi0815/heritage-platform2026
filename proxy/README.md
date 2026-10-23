# 弘文 · DeepSeek 代理

目的只有一个：**让 API Key 不出现在浏览器里**。

前端存 Key（localStorage 或 `VITE_` 变量）都无法防人：前者能被同一台机器上的下一个人拿走，
后者会被打进公开 JS，任何人打开 DevTools 就看见。代理把 Key 留在服务器，前端只拿到地址，
并把「限流 + 每日预算」放在服务端执行。

## 本地跑

```bash
DEEPSEEK_API_KEY=sk-xxx node proxy/deepseek-proxy.mjs
curl http://127.0.0.1:8787/health
```

然后另开一个终端起前端：

```bash
VITE_DEEPSEEK_PROXY=http://127.0.0.1:8787 npx vite
```

`.env.local` 里设 `VITE_DEEPSEEK_PROXY` 也可以，但它是**构建时**变量，改了要重新 build。

## 它拦什么

| 限制 | 默认 | 环境变量 |
|---|---|---|
| 每日总花费 | ¥2 | `DAILY_CNY` |
| 单 IP 每分钟 | 6 次 | `PER_IP_MIN` |
| 单 IP 每天 | 60 次 | `PER_IP_DAY` |
| 单次回答 tokens | 320（前端传多大都按这个） | `MAX_TOKENS` |
| 允许的浏览器来源 | 本地开发地址 | `ALLOW_ORIGINS` |
| 模型 | 写死 `deepseek-chat` | — |

账本落在 `proxy/usage.json`（重启不丢当日用量）。**不要提交这个文件，
也不要把它放进公开目录。**

## 部署到服务器

1. 用 systemd 常驻，Key 写进服务的 `Environment=`，不要写进仓库：

   ```ini
   [Unit]
   Description=hongwen deepseek proxy
   After=network.target

   [Service]
   Environment=DEEPSEEK_API_KEY=sk-xxx
   Environment=PORT=8787
   Environment=DAILY_CNY=2
   Environment=ALLOW_ORIGINS=https://你的站点域名
   ExecStart=/usr/bin/node /srv/heritage/proxy/deepseek-proxy.mjs
   Restart=always

   [Install]
   WantedBy=multi-user.target
   ```

2. 站点是 https 时，代理也必须走 https（否则浏览器按混合内容拦掉）。
   用 nginx 反代同一个域名下的路径：

   ```nginx
   location /dhapi/ {
     proxy_pass http://127.0.0.1:8787/;
     proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
     proxy_set_header Host $host;
     proxy_read_timeout 30s;
   }
   ```

   `X-Forwarded-For` 必须带上，否则所有访客在代理眼里是同一个 IP，
   单 IP 限流会误伤（或者失效）。

3. 构建前端时设 `VITE_DEEPSEEK_PROXY=https://你的站点域名/dhapi`。

## 说清楚做不到的事

- **Origin 白名单只拦浏览器**。用 curl 直接打这个地址，CORS 不生效——所以真正兜底的是
  每 IP 限流和每日预算，不是白名单。
- **单 IP 限流挡不住大规模换 IP**。公开站点若被盯上，最终防线是 `DAILY_CNY`：
  预算烧完，代理返回 429，前端自动回落到本地知识库，只是没有大模型回答，站点不会挂。
- DeepSeek 控制台没有「单 Key 每日限额」的开关，额度只能由调用方自己记。
  所以务必给这个平台单独建一个 Key——出事时删掉它，不影响别的项目。
