import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  // 相对基址：同一份产物可挂在 /、/heritage/ 或反向代理到学院域名下
  base: './',
  plugins: [react()],
  server: { port: 5175, host: true },
})
