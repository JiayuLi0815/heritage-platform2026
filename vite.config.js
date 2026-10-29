import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  // 同济人文 AI 应用站的静态站点前缀；其他部署可用 vite build --base 覆盖。
  base: '/u/wangyifei/heritage/',
  plugins: [react()],
  // GitHub Pages 那份走 scripts/build-gh-pages.mjs 拼出来的精简素材目录
  publicDir: process.env.VITE_GH_PAGES ? 'public-gh' : 'public',
  server: { port: 5175, host: true },
})
