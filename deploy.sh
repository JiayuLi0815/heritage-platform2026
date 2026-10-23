#!/usr/bin/env bash
#
# 一条命令把平台发到阿里云服务器：
#   https://www.jiayu98518.work/heritage/
#
# 用法：
#   ./deploy.sh              # 构建 + 上传
#   ./deploy.sh --no-build   # 跳过构建，直接发现有 dist/
#
# 说明：产物用相对基址（vite base: './'）打包，所以同一份 dist
#   既能挂在 /heritage/ 子路径，也能被学院域名反向代理到别的前缀下，
#   或者整包交给学校托管——都不需要重新构建。

set -euo pipefail
cd "$(dirname "$0")"

SERVER="8.133.243.78"
WEBROOT="/var/www/heritage"

if [[ "${1:-}" != "--no-build" ]]; then
  echo "==> 1/2 构建"
  npx vite build
else
  echo "==> 1/2 跳过构建（用现有 dist/）"
fi

if [[ ! -f dist/index.html ]]; then
  echo "错误：dist/index.html 不存在，先跑一次完整构建。" >&2
  exit 1
fi

echo "==> 2/2 上传到 $SERVER:$WEBROOT"
COPYFILE_DISABLE=1 rsync -az --delete \
  --exclude '.DS_Store' \
  -e "ssh -o BatchMode=yes" \
  dist/ "$SERVER:$WEBROOT/"

echo "完成：https://www.jiayu98518.work/heritage/"
