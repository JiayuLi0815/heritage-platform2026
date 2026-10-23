#!/usr/bin/env python3
"""Build src/data/sites-all.json from the T7 drive index + Wikidata coordinates.

Usage:
  python3 -I scripts/build-sites.py <index.json> <rows.json> <wikidata.json> <out.json>

All paths are passed in; nothing is read from the script's own directory.
"""
import json
import re
import sys

CATEGORY = {'Cultural': '文化', 'Natural': '自然', 'Mixed': '混合'}

# Wikidata 里这三处没有可用的坐标（主条目缺 P625，或条目未挂 P757），
# 按 UNESCO 官方描述取点；1567 为跨法比两国的线性遗产，取中段代表点。
FALLBACK = {
    '624': (49.8917, 10.8883),
    '946': (43.3375, 17.8150),
    '1567': (49.9000, 2.3000),
}


def lead_digits(s):
    m = re.match(r'^(\d+)', s or '')
    return m.group(1) if m else None


def wkt_point(s):
    m = re.match(r'^Point\(\s*(-?[\d.]+)\s+(-?[\d.]+)\s*\)$', s or '')
    if not m:
        return None
    return float(m.group(2)), float(m.group(1))  # lat, lng


def main():
    index_path, rows_path, wd_path, out_path = sys.argv[1:5]
    index = json.load(open(index_path, encoding='utf-8'))
    rows = json.load(open(rows_path, encoding='utf-8'))
    wd = json.load(open(wd_path, encoding='utf-8'))['results']['bindings']

    # 中文国名：998 条原表里带 source_country，同 id 可能有多行（跨境/重复），取第一个非空
    country_zh = {}
    for r in rows:
        cid = r.get('id')
        c = (r.get('source_country') or '').strip()
        if cid and c and cid not in country_zh:
            country_zh[cid] = c

    # 坐标：同一 id 可能有多行（主条目 + 组成部分，跨境站点还有多国），优先主条目
    coords = {}
    for b in wd:
        nid = lead_digits(b['id']['value'])
        if not nid:
            continue
        pt = wkt_point(b.get('coord', {}).get('value'))
        if not pt:
            continue
        exact = b['id']['value'] == nid
        prev = coords.get(nid)
        if prev is None or (exact and not prev[0]):
            coords[nid] = (exact, pt[0], pt[1])

    out, skipped = [], []
    for x in index:
        sid = x['id']
        if sid in coords:
            lat, lng = coords[sid][1], coords[sid][2]
        elif sid in FALLBACK:
            lat, lng = FALLBACK[sid]
        else:
            skipped.append((sid, x.get('name_zh')))
            lat = lng = None

        # 展示名优先用素材文件夹名——它来自 998 条原表的中文名，比索引里的
        # name_zh 更接近课题口径（如「敦煌莫高窟」而非「莫高窟」）。
        folder = x.get('folder_name') or ''
        display = folder.replace('_', '·').strip()
        out.append({
            'id': sid,
            'name': display or x.get('name_zh') or x.get('name_en'),
            'nameEn': x.get('name_en'),
            'country': country_zh.get(sid) or (x.get('countries') or [''])[0],
            'countries': x.get('countries') or [],
            'year': x.get('year'),
            'category': CATEGORY.get(x.get('category'), x.get('category')),
            'lat': lat,
            'lng': lng,
            'page': x.get('page'),
            'folder': x.get('folder_name') or x.get('name_zh'),
        })

    out.sort(key=lambda s: (s['country'] or '', s['name'] or ''))
    json.dump(out, open(out_path, 'w', encoding='utf-8'), ensure_ascii=False, separators=(',', ':'))

    withco = sum(1 for s in out if s['lat'] is not None)
    print(f'wrote {len(out)} sites -> {out_path}')
    print(f'with coords: {withco}, without: {len(out) - withco}')
    if skipped:
        print('skipped:', skipped)


if __name__ == '__main__':
    main()
