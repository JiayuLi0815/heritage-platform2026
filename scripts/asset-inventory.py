#!/usr/bin/env python3
"""盘点素材盘：按遗产文件夹统计三维模型 / 视频 / 图片，并与站点表对上号。

Usage:
  python3 -I scripts/asset-inventory.py <素材根目录> <sites-all.json> <out.json>

只读。脚本放在仓库里，素材目录通过参数传入。
"""
import json
import os
import sys

MODEL_EXT = ('.glb', '.gltf', '.obj', '.fbx', '.stl', '.ply', '.usdz', '.dae', '.3ds')
VIDEO_EXT = ('.mkv', '.mp4', '.webm', '.mov', '.m4a', '.avi')
IMAGE_EXT = ('.jpg', '.jpeg', '.png', '.webp', '.tif', '.tiff')


def human(n):
    for unit in ('B', 'KB', 'MB', 'GB'):
        if n < 1024 or unit == 'GB':
            return f'{n:.1f}{unit}'
        n /= 1024


def main():
    root, sites_path, out_path = sys.argv[1:4]
    sites = json.load(open(sites_path, encoding='utf-8'))
    by_folder = {s['folder']: s for s in sites if s.get('folder')}
    by_name = {s['name']: s for s in sites}

    rows = []
    for entry in sorted(os.listdir(root)):
        d = os.path.join(root, entry)
        if not os.path.isdir(d):
            continue
        models, videos, images = [], [], []
        for dirpath, _dirnames, filenames in os.walk(d):
            for f in filenames:
                ext = os.path.splitext(f)[1].lower()
                p = os.path.join(dirpath, f)
                try:
                    size = os.path.getsize(p)
                except OSError:
                    continue
                if ext in MODEL_EXT:
                    models.append((f, size))
                elif ext in VIDEO_EXT:
                    videos.append((f, size))
                elif ext in IMAGE_EXT:
                    images.append((f, size))
        if not (models or videos or images):
            continue
        site = by_folder.get(entry) or by_name.get(entry)
        rows.append({
            'folder': entry,
            'id': site['id'] if site else None,
            'name': site['name'] if site else entry,
            'country': site['country'] if site else None,
            'year': site['year'] if site else None,
            'models': sorted(models, key=lambda x: -x[1]),
            'videos': sorted(videos, key=lambda x: -x[1]),
            'images': len(images),
            'imageBytes': sum(s for _, s in images),
            'modelBytes': sum(s for _, s in models),
            'videoBytes': sum(s for _, s in videos),
        })

    json.dump(rows, open(out_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

    m = [r for r in rows if r['models']]
    v = [r for r in rows if r['videos']]
    i = [r for r in rows if r['images']]
    print(f'folders={len(rows)}  with3d={len(m)}  glb={sum(len(r["models"]) for r in m)}  '
          f'3d={human(sum(r["modelBytes"] for r in m))}')
    print(f'withVideo={len(v)}  files={sum(len(r["videos"]) for r in v)}  '
          f'{human(sum(r["videoBytes"] for r in v))}')
    print(f'withImage={len(i)}  images={sum(r["images"] for r in i)}')
    miss = [r["folder"] for r in m if not r["id"]]
    if miss:
        print('未对上站点表的三维文件夹:', miss)


if __name__ == '__main__':
    main()
