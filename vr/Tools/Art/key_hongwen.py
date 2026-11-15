#!/usr/bin/env python3
"""把弘文的纯白底图集抠成带 alpha 的图集，供 Flipbook / 场景立牌使用。

素材来源：hongwen_atlas.png（6x6、36 帧、2160x2100、白底不透明）。
做法：按「最暗通道」做软阈值得到初始 alpha → 从四条边界泛滥连接的透明区域
（真正的外部背景），内部被围住的亮斑（脸部高光等）回填为不透明 → 输出 RGBA。

用法： python3 Tools/Art/key_hongwen.py
输出： hongwen_atlas_cut.png（图集）、hongwen_bust.png（首帧，场景立牌用）
原图不覆盖，可随时重跑。
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "Assets", "Huaxia", "Resources", "Heritage")
COLS = ROWS = 6

# 白底实测恒为 255，人物最高亮处（白毛衣）在 250~254 之间，
# 因此只在「接近纯白」的一窄带上做软阈值，再靠连通性判断内外。
NEAR_WHITE = 253.0


def flood(passable, seed):
    """从 seed 出发、只在 passable 内四连通扩散。"""
    reach = seed & passable
    while True:
        new = reach.copy()
        new[1:, :] |= reach[:-1, :]
        new[:-1, :] |= reach[1:, :]
        new[:, 1:] |= reach[:, :-1]
        new[:, :-1] |= reach[:, 1:]
        new &= passable
        if np.array_equal(new, reach):
            return reach
        reach = new


def key_frame(rgb):
    dark = rgb.min(axis=2).astype(np.float32)
    h, w = dark.shape
    # 背景是恒定的纯白；人物的白毛衣最亮也能到 254，
    # 所以不靠阈值分割，而是靠「从画面上方边界连通」来判断背景。
    near_white = dark >= NEAR_WHITE
    seed = np.zeros_like(near_white)
    seed[0, :] = True                       # 顶边
    seed[: int(h * 0.55), 0] = True         # 左边上半
    seed[: int(h * 0.55), -1] = True        # 右边上半（右上角常残留方形描边）
    outside = flood(near_white, seed)
    return feather((~outside).astype(np.float32))


def feather(mask, radius=2):
    """盒式羽化，抹掉硬阈值留下的阶梯边。"""
    pad = np.pad(mask, radius, mode="edge")
    acc = np.zeros_like(mask)
    n = 2 * radius + 1
    for dy in range(n):
        for dx in range(n):
            acc += pad[dy:dy + mask.shape[0], dx:dx + mask.shape[1]]
    return acc / float(n * n)


def main():
    src = Image.open(os.path.join(RES, "hongwen_atlas.png")).convert("RGB")
    W, H = src.size
    fw, fh = W // COLS, H // ROWS
    atlas = np.zeros((H, W, 4), np.uint8)
    rgb = np.asarray(src, np.uint8)
    for r in range(ROWS):
        for c in range(COLS):
            y0, x0 = r * fh, c * fw
            tile = rgb[y0:y0 + fh, x0:x0 + fw]
            atlas[y0:y0 + fh, x0:x0 + fw, :3] = tile
            atlas[y0:y0 + fh, x0:x0 + fw, 3] = (key_frame(tile) * 255).astype(np.uint8)

    out = Image.fromarray(atlas)
    out.save(os.path.join(RES, "hongwen_atlas_cut.png"))
    out.crop((0, 0, fw, fh)).save(os.path.join(RES, "hongwen_bust.png"))
    cover = (atlas[:, :, 3] > 8).mean()
    print(f"atlas {W}x{H} frames {COLS*ROWS} ({fw}x{fh})  foreground {cover*100:.1f}%")


if __name__ == "__main__":
    main()
