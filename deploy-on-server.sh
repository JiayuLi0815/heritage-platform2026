#!/usr/bin/env bash
# Run after extracting heritage-release.tar in /data/home/wangyifei/<release>.
set -euo pipefail
cd "$(dirname "$0")"
[[ "$(id -un)" == wangyifei ]] || { echo '必须以 wangyifei 执行' >&2; exit 1; }
case "$(pwd -P)/" in /data/home/wangyifei/*/) ;; *) echo '发布包必须解压在 /data/home/wangyifei 下的独立目录' >&2; exit 1;; esac
[[ -f dist/index.html && -f SHA256SUMS ]] || { echo '发布包不完整' >&2; exit 1; }
grep -q '/u/wangyifei/heritage/assets/' dist/index.html
echo '校验发布包 SHA-256…'
sha256sum --quiet -c SHA256SUMS
target=/data/web/u/wangyifei/heritage
mkdir -p "$target"
[[ "$(realpath "$target")" == "$target" ]] || { echo '目标目录包含符号链接，停止' >&2; exit 1; }
backup="/data/home/wangyifei/heritage-backup-$(date +%Y%m%d-%H%M%S)-$$"
mkdir "$backup"
cp -a "$target/." "$backup/"
echo "旧版本已备份：$backup"
# Preserve remote-only files. Publish index after every asset is uploaded.
rsync -a --delay-updates --chmod=Du=rwx,Dgo=rx,Fu=rw,Fgo=r \
  --exclude='/index.html' --exclude='/CNAME' --exclude='/404.html' dist/ "$target/"
cp dist/index.html "$target/.index-release-$$"
chmod 644 "$target/.index-release-$$"
mv "$target/.index-release-$$" "$target/index.html"
release_dir=$(pwd -P)
(cd "$target"; sed 's#  dist/#  #' "$release_dir/SHA256SUMS" | sha256sum --quiet -c -)
du -sh "$target"
echo '发布完成：http://192.168.180.14/u/wangyifei/heritage/'
