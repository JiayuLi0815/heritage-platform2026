#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
《华夏文明探源 · 河山册》翻页声生成器
=====================================

一页纸被掀起、滑过、落下的声音，全部由滤波噪声合成，无采样、无版权问题。

做法
----
* 两次"滑纸"：带通噪声，中心频率随时间先升后落（300Hz → 2.4kHz → 700Hz），
  模拟纸面被手指带起、滑过、落下的过程
* 几下"纤维"：极短的高频噪声脉冲，纸的脆感
* 起音快、尾巴短；峰值压在 -12 dB 左右，不盖过环境音

用法
----
    python3 Tools/Audio/generate_page_turn.py

输出 Assets/Huaxia/Resources/Audio/page_turn.wav（22050 Hz / 单声道 / 16 bit）。
"""

import os
import wave

import numpy as np

SR = 22050
DURATION = 0.52
OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                       "..", "..", "Assets", "Huaxia", "Resources", "Audio")
OUT_PATH = os.path.join(OUT_DIR, "page_turn.wav")


def bandpass_sweep(noise, start, peak, end, q=1.6):
    """时变带通：中心频率按 start->peak->end 走一条拱形曲线。"""
    n = len(noise)
    out = np.zeros(n)
    low = band = 0.0
    qq = 1.0 / q
    for i in range(n):
        t = i / (n - 1)
        # 前 55% 上升，之后回落：手把纸带起来，再滑下去
        if t < 0.55:
            fc = start + (peak - start) * (t / 0.55)
        else:
            fc = peak + (end - peak) * ((t - 0.55) / 0.45)
        f = 2.0 * np.sin(np.pi * min(fc, SR * 0.45) / SR)
        high = noise[i] - low - qq * band
        band += f * high
        low += f * band
        out[i] = band
    return out


def envelope(n, attack, decay, delay=0.0):
    t = np.arange(n) / SR
    t = np.maximum(0.0, t - delay)
    a = 1.0 - np.exp(-t / max(attack, 1e-4))
    d = np.exp(-t / max(decay, 1e-4))
    return a * d


def main():
    rng = np.random.default_rng(7)
    n = int(SR * DURATION)
    noise = rng.normal(0.0, 1.0, n)

    # 第一下：纸被掀起（更亮、更短）
    swish_a = bandpass_sweep(noise, 420.0, 2400.0, 900.0, q=1.7)
    swish_a *= envelope(n, 0.012, 0.085)

    # 第二下：纸滑过去、落平（更闷、略长，晚 0.14 秒进来）
    noise_b = rng.normal(0.0, 1.0, n)
    swish_b = bandpass_sweep(noise_b, 300.0, 1500.0, 520.0, q=2.0)
    swish_b *= envelope(n, 0.020, 0.150, delay=0.14) * 0.85

    mix = swish_a + swish_b

    # 纸纤维的脆响：几声极短的高频脉冲，位置随机但固定种子
    for delay, gain in ((0.06, 0.10), (0.11, 0.07), (0.23, 0.09), (0.31, 0.05)):
        start = int(delay * SR)
        length = int(0.012 * SR)
        if start + length >= n:
            continue
        click = rng.normal(0.0, 1.0, length)
        click *= np.exp(-np.arange(length) / (0.0025 * SR))
        mix[start:start + length] += click * gain

    # 整体包络：不要"啪"的一下开始，也不要拖着尾巴
    shape = envelope(n, 0.006, 0.33)
    mix *= shape

    # 归一化到 -12 dBFS，翻页是配角，不该盖过环境音
    peak = np.max(np.abs(mix))
    if peak > 0:
        mix = mix / peak * 10 ** (-12 / 20)

    pcm = np.clip(mix * 32767.0, -32768, 32767).astype("<i2")

    os.makedirs(OUT_DIR, exist_ok=True)
    with wave.open(OUT_PATH, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())

    print("wrote", os.path.normpath(OUT_PATH), f"{DURATION:.2f}s")


if __name__ == "__main__":
    main()
