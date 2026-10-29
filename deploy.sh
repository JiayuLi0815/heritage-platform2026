#!/usr/bin/env bash
# Requires an already authorized tj-apps SSH account and local rsync.
set -euo pipefail
cd "$(dirname "$0")"
SERVER=tj-apps
WEBROOT=/data/web/u/wangyifei/heritage
if [[ "${1:-}" != --no-build ]]; then
  VITE_DEEPSEEK_API_KEY='' npm run build
fi
node scripts/verify-assets.mjs dist
[[ "$(ssh -o BatchMode=yes "$SERVER" 'id -un')" == wangyifei ]] || { echo 'SSH user must be wangyifei' >&2; exit 1; }
ssh "$SERVER" 'mkdir -p /data/web/u/wangyifei/heritage'
rsync -av --delay-updates --exclude='/index.html' --exclude='/CNAME' --exclude='/404.html' dist/ "$SERVER:$WEBROOT/"
rsync -av dist/index.html "$SERVER:$WEBROOT/index.html"
echo 'http://192.168.180.14/u/wangyifei/heritage/'