import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  // 相对基址：同一份产物可挂在 /、/heritage/ 或反向代理到学院域名下
  base: './',
  plugins: [react()],
  // GitHub Pages 那份走 scripts/build-gh-pages.mjs 拼出来的精简素材目录
  publicDir: process.env.VITE_GH_PAGES ? 'public-gh' : 'public',
  server: { port: 5175, host: true },
})
