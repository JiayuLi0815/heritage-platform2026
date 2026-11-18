import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// 同济人文 AI 应用站的静态站点前缀。构建默认用它；本地 dev 走根路径，
// 免得每个人都要记住访问 http://localhost:5175/u/wangyifei/heritage/。
// 要覆盖就设 VITE_BASE，例如 VITE_BASE=/ npm run build。
const DEPLOY_BASE = '/u/wangyifei/heritage/'

export default defineConfig(({ command }) => ({
  base: process.env.VITE_BASE ?? (command === 'build' ? DEPLOY_BASE : '/'),
  plugins: [react()],
  // GitHub Pages 那份走 scripts/build-gh-pages.mjs 拼出来的精简素材目录
  publicDir: process.env.VITE_GH_PAGES ? 'public-gh' : 'public',
  server: { port: 5175, host: true },
}))
