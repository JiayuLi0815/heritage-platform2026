#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
《华夏文明探源 · 河山册》环境音乐生成器
=========================================

全部音频由本脚本用纯数学方式合成，**不含任何采样、不引用任何现有乐曲**，
因此不存在版权问题（原创，可随项目任意分发）。

设计原则
--------
* 五声音阶（宫商角徵羽），只用五度相生，不出现半音与功能和声 → 东方气质
* 乐器用"加性合成 + 指数衰减"模拟：古琴散音、箫/埙长音、编钟泛音、木梆、鼓
* 环境层用滤波噪声：山风、流水、海浪、松涛
* 混响用周期化 FFT 卷积，**循环点无缝**（气息不断）
* 输出 22050 Hz / 单声道 / 16 bit PCM WAV，Unity 直接读

用法
----
    python3 Tools/Audio/generate_ambient.py

输出到 Assets/Huaxia/Resources/Audio/amb_*.wav（Resources 下用路径名加载，
不带扩展名）。修改乐句后重跑即可，产物可直接提交。
"""

import os
import wave

import numpy as np

SR = 22050
OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                       "..", "..", "Assets", "Huaxia", "Resources", "Audio")

# 五声音阶（以 D 为宫）：D E F# A B —— 只用这五个音，怎么叠都不会"西洋"
PENTA = {
    "D2": 73.42, "A2": 110.00, "D3": 146.83, "E3": 164.81, "F#3": 185.00,
    "A3": 220.00, "B3": 246.94, "D4": 293.66, "E4": 329.63, "F#4": 369.99,
    "A4": 440.00, "B4": 493.88, "D5": 587.33, "E5": 659.26, "F#5": 739.99,
    "A5": 880.00,
}

_NOTE_NAMES = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]
_PC = {n: i for i, n in enumerate(_NOTE_NAMES)}


def penta_scale(root):
    """以 root 为宫的五声音阶，覆盖 2~5 八度。

    每处遗址换一个宫音（序章 D、良渚 G、秦陵 A、泉州 C、终章 F）：
    同样的合成器，不同调门，五段轮着听才不会像同一首的五个版本。
    """
    out = {}
    r = _PC[root]
    for octave in (2, 3, 4, 5):
        for semi in (0, 2, 4, 7, 9):
            midi = 12 * (octave + 1) + r + semi
            out[_NOTE_NAMES[midi % 12] + str(octave)] = 440.0 * 2 ** ((midi - 69) / 12)
    return out


# 良渚用 G 宫（水，明亮）；秦陵用 A 宫（土，低而稳）；泉州用 C 宫（海，开阔）
S_G = penta_scale("G")
S_A = penta_scale("A")
S_C = penta_scale("C")
S_F = penta_scale("F")


def loop_length(seconds):
    """把时长取整到采样点，返回 (N, 频率量化函数)。"""
    n = int(round(seconds * SR))
    # 只有频率是 1/时长 的整数倍，波形在循环点才严格闭合
    quantum = SR / n

    def q(f):
        return max(quantum, round(f / quantum) * quantum)

    return n, q


def place(buf, start, seg):
    """把 seg 叠加进循环缓冲区，越界部分回绕到开头 —— 这是无缝循环的关键。"""
    n = buf.size
    idx = (np.arange(seg.size) + int(start)) % n
    np.add.at(buf, idx, seg)


def env_exp(n, attack, decay, sr=SR):
    """快起慢落的指数包络（拨弦、敲击）。"""
    t = np.arange(n) / sr
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    d = np.exp(-t / max(decay, 1e-4))
    return a * d


def pluck(freq, dur, decay=1.6, bright=0.55, sr=SR, detune=0.0):
    """拨弦：奇偶谐波都在，高次谐波衰减更快 → 古琴/古筝的木质颗粒感。"""
    n = int(dur * sr)
    t = np.arange(n) / sr
    out = np.zeros(n)
    for k in range(1, 13):
        fk = freq * k * (1 + 0.0004 * k * k)  # 轻微非谐性，避免电子味
        amp = (bright ** (k - 1)) / k
        out += amp * np.sin(2 * np.pi * fk * t + detune * k) * np.exp(
            -t / (decay / (1 + 0.35 * (k - 1))))
    return out * env_exp(n, 0.004, decay)


def flute(freq, dur, sr=SR, vib=4.6, breath=0.10):
    """箫/埙：基音 + 少量二次谐波，慢起音，气声噪声做"人味"。"""
    n = int(dur * sr)
    t = np.arange(n) / sr
    v = 1 + 0.006 * np.sin(2 * np.pi * vib * t) * np.clip(t / 1.4, 0, 1)
    sig = (np.sin(2 * np.pi * freq * v * t)
           + 0.16 * np.sin(4 * np.pi * freq * v * t)
           + 0.05 * np.sin(6 * np.pi * freq * v * t))
    e = np.clip(t / 1.1, 0, 1) * np.clip((dur - t) / 1.6, 0, 1)
    noise = np.random.default_rng(int(freq)).standard_normal(n)
    noise = lowpass(noise, 2200)
    return (sig + breath * noise * 3.0) * e


def bell(freq, dur, sr=SR):
    """编钟：非整数倍分音，余韵长，金属的凉。"""
    n = int(dur * sr)
    t = np.arange(n) / sr
    partials = [(1.0, 1.00, 1.0), (2.76, 0.42, 0.55), (5.40, 0.22, 0.35),
                (8.93, 0.12, 0.22), (13.3, 0.06, 0.15)]
    out = np.zeros(n)
    for ratio, amp, dk in partials:
        out += amp * np.sin(2 * np.pi * freq * ratio * t) * np.exp(-t / (dur * dk))
    return out * env_exp(n, 0.002, dur * 0.8)


def drum(freq, dur, sr=SR, punch=1.0):
    """鼓/心跳：低频正弦下滑 + 噪声瞬态。"""
    n = int(dur * sr)
    t = np.arange(n) / sr
    f = freq * (1 + 0.55 * np.exp(-t / 0.05))
    body = np.sin(2 * np.pi * np.cumsum(f) / sr) * np.exp(-t / (dur * 0.35))
    noise = lowpass(np.random.default_rng(int(freq * 7)).standard_normal(n), 900)
    return (body * punch + 0.5 * noise * np.exp(-t / 0.04)) * env_exp(n, 0.002, dur * 0.5)


def woodblock(freq, sr=SR):
    """木梆：短促的带通噪声 + 一个木头音高的正弦。"""
    n = int(0.16 * sr)
    t = np.arange(n) / sr
    noise = bandpass(np.random.default_rng(3).standard_normal(n), 1800, 3800)
    return (0.6 * noise + np.sin(2 * np.pi * freq * t)) * np.exp(-t / 0.035)


def bird(freq=3200, dur=0.22, sr=SR):
    """鸟叫：短促的上滑-下滑，两声一组。只用正弦扫频，不用采样。"""
    n = int(dur * sr)
    t = np.arange(n) / sr
    f = freq * (1.0 + 0.38 * np.sin(np.pi * t / dur) ** 0.7)
    sig = np.sin(2 * np.pi * np.cumsum(f) / sr)
    e = np.clip(t / 0.012, 0, 1) * np.exp(-t / 0.055)
    return sig * e


def pad_layer(n, seconds, notes, q, gain=0.045, scale=None):
    """和弦垫：每个音三条轻微失谐的正弦，慢呼吸，垫在旋律底下。"""
    scale = scale or PENTA
    t = np.arange(n) / SR
    out = np.zeros(n)
    for i, note in enumerate(notes):
        f = q(scale[note])
        for d in (-0.55, 0.0, 0.55):
            out += np.sin(2 * np.pi * (f + d) * t + i * 0.9)
    breath = 0.78 + 0.22 * np.sin(2 * np.pi * t / max(1, seconds) * 2 + 0.8)
    return gain * out / max(1, len(notes)) * breath


def _fft_filter(x, gain_fn):
    X = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(x.size, 1 / SR)
    return np.fft.irfft(X * gain_fn(freqs), x.size)


def lowpass(x, cutoff, order=1):
    return _fft_filter(x, lambda f: 1.0 / np.sqrt(1.0 + (f / cutoff) ** (2 * order)))


def bandpass(x, lo, hi):
    return _fft_filter(
        x,
        lambda f: (1.0 / np.sqrt(1.0 + (f / hi) ** 4))
        * (1.0 / np.sqrt(1.0 + (lo / np.maximum(f, 1e-3)) ** 4)))


def noise_loop(n, rng, cutoff, mod=None):
    """循环噪声床：取 FFT 后随机相位 → 天然周期化，首尾自动相接。"""
    X = np.fft.rfft(rng.standard_normal(n))
    freqs = np.fft.rfftfreq(n, 1 / SR)
    X *= 1.0 / np.sqrt(1.0 + (freqs / cutoff) ** 3)
    x = np.fft.irfft(X, n)
    x /= np.max(np.abs(x)) + 1e-9
    if mod is not None:
        t = np.arange(n) / SR
        x *= mod(t)
    return x


def reverb(x, seconds=3.2, mix=0.34, damp=3200, sr=SR):
    """周期化 FFT 卷积混响：把脉冲响应绕成环，循环点不会有"咔"的一声。"""
    n = x.size
    ir_n = int(seconds * sr)
    rng = np.random.default_rng(11)
    ir = rng.standard_normal(ir_n) * np.exp(-np.arange(ir_n) / (ir_n / 5.5))
    ir = lowpass(ir, damp)
    ir /= np.max(np.abs(ir)) + 1e-9
    ir = np.concatenate([ir, np.zeros(n - ir_n)])
    wet = np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(np.roll(ir, 0)), n)
    wet /= np.max(np.abs(wet)) + 1e-9
    wet *= np.max(np.abs(x)) + 1e-9
    return x * (1 - mix) + wet * mix


def normalize(x, rms_target=0.118, ceiling=0.72):
    """统一响度而不是统一峰值 —— 五段音乐轮流播放时不能忽大忽小。"""
    x = x - np.mean(x)
    rms = np.sqrt(np.mean(x ** 2)) + 1e-9
    x *= rms_target / rms
    return x * min(1.0, ceiling / (np.max(np.abs(x)) + 1e-9))


def write_wav(path, x, sr=SR):
    x = normalize(x)
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data.tobytes())
    print("  %-34s %5.1f s  %5.2f MB"
          % (os.path.basename(path), x.size / sr, os.path.getsize(path) / 1048576))


# ---------------------------------------------------------------- 各场景乐段

def amb_archive(seconds=100):
    """序章 · 暮色古道（D 宫，无和弦垫）：古琴的短句、四句箫、远处的鸟。

    注意：**不要加风噪声**。玩家把低频滤波噪声听成了"奇怪的风声"，
    尤其在 10 秒后箫声进来时更明显；序章只留琴、箫、鸟。
    """
    n, q = loop_length(seconds)
    buf = np.zeros(n)

    # 古琴：短句，四五个音一组，句间留白（比原来密，但不吵）
    phrases = [
        [("D3", 2.0, 0.26), ("E3", 4.2, 0.22), ("A3", 6.6, 0.24)],
        [("D4", 12.0, 0.24), ("B3", 14.4, 0.20), ("A3", 16.8, 0.24), ("D3", 19.2, 0.26)],
        [("E4", 25.0, 0.22), ("D4", 27.6, 0.24), ("A3", 30.2, 0.22)],
        [("D3", 37.0, 0.26), ("A3", 39.6, 0.22), ("B3", 42.0, 0.20), ("D4", 44.8, 0.22)],
        [("A3", 52.0, 0.24), ("F#3", 54.6, 0.20), ("E3", 57.4, 0.22)],
        [("D3", 64.0, 0.26), ("E3", 66.4, 0.22), ("A3", 69.0, 0.24), ("D4", 71.8, 0.20)],
        [("B3", 79.0, 0.22), ("A3", 81.6, 0.24), ("D3", 84.4, 0.26)],
        [("E4", 91.0, 0.20), ("D4", 93.4, 0.24), ("A3", 96.0, 0.22)],
    ]
    for phrase in phrases:
        for note, at, g in phrase:
            place(buf, at * SR, g * pluck(q(PENTA[note]), 5.0, decay=2.2, bright=0.42))

    # 箫：四句长音，从山门那头飘过来，和琴的颗粒对位
    for note, at, dur in [("D3", 8.0, 4.6), ("A3", 32.0, 4.2),
                          ("B3", 58.0, 4.8), ("A3", 86.0, 4.4)]:
        place(buf, at * SR, 0.060 * flute(q(PENTA[note]), dur, vib=4.8, breath=0.13))

    # 鸟：远一点、稀一点
    for i, at in enumerate([8.0, 8.5, 21.0, 34.0, 34.4, 48.0, 62.0, 62.3, 76.0, 88.0]):
        f = 2400 + (i * 137) % 1400
        place(buf, at * SR, 0.030 * bird(f, 0.18))

    return reverb(buf, seconds=3.4, mix=0.30)


def amb_liangzhu(seconds=100):
    """良渚 · 水与土（G 宫，高而亮）：流水、鸟、骨哨的高音短句、轻拨弦。

    和序章（D 宫、低音古琴）与秦陵（A 宫、低音埙）刻意分开：
    这一首的主角是高处的水鸟与短促的骨哨，不靠和弦垫铺底。
    """
    n, q = loop_length(seconds)
    rng = np.random.default_rng(2)
    buf = np.zeros(n)
    t = np.arange(n) / SR

    # 水面（坝体溢流）：中高频噪声，慢速起伏
    water = bandpass(noise_loop(n, rng, 3000), 180, 6000)
    water *= 0.55 + 0.45 * np.sin(2 * np.pi * 3 * t / seconds)
    buf += 0.105 * water / (np.max(np.abs(water)) + 1e-9)

    # 只留极轻的五度，不要把五个场景都垫成同一种"和弦墙"
    buf += pad_layer(n, seconds, ["G2", "D3", "G3"], q, 0.028, scale=S_G)

    # 骨哨：比埙高、气声更重，短句；音区落在 G4~E5
    for note, at, dur in [("G4", 2.0, 2.4), ("A4", 6.4, 2.0), ("B4", 10.4, 2.6),
                          ("A4", 16.0, 1.9), ("G4", 20.0, 2.8), ("D4", 25.0, 2.0),
                          ("E4", 30.0, 2.2), ("G4", 35.0, 2.3), ("A4", 40.0, 1.9),
                          ("G4", 45.0, 2.6), ("D4", 52.0, 2.2), ("E4", 57.0, 2.0),
                          ("G4", 62.0, 2.5), ("B4", 68.0, 2.0), ("A4", 73.0, 2.3),
                          ("G4", 79.0, 2.8), ("D4", 86.0, 2.0), ("G4", 92.0, 2.6)]:
        place(buf, at * SR, 0.095 * flute(q(S_G[note]), dur, vib=6.4, breath=0.17))

    # 拨弦：把骨哨的句子接住，用 G 宫的高音区
    for note, at in [("D4", 5.0), ("G4", 8.8), ("E4", 13.6), ("D4", 18.4),
                     ("G3", 23.4), ("A4", 28.2), ("G4", 33.0), ("D4", 38.0),
                     ("E4", 43.4), ("G4", 48.6), ("A4", 55.0), ("G4", 60.4),
                     ("D4", 66.0), ("E4", 71.2), ("G4", 76.8), ("D4", 83.0),
                     ("G3", 89.0), ("G4", 96.0)]:
        place(buf, at * SR, 0.085 * pluck(q(S_G[note]), 3.2, decay=1.5, bright=0.55))

    # 鸟：水边成群
    for i, at in enumerate([3.2, 3.7, 12.0, 12.4, 21.5, 32.0, 32.5, 44.0,
                            49.0, 49.4, 58.0, 69.0, 69.4, 81.0, 87.0]):
        place(buf, at * SR, 0.028 * bird(3000 + (i * 211) % 1500, 0.16))

    # 木梆：很少，做远处劳作的一点提示
    for at in [9.5, 24.0, 48.0, 66.0, 88.0]:
        place(buf, at * SR, 0.040 * woodblock(q(S_G["D4"])))
    return reverb(buf, seconds=2.8, mix=0.24)


def amb_qinling(seconds=100):
    """秦陵 · 塬上的午后（A 大调五声）：暖风、鸟、竹笛的短句、轻拨弦。

    之前那版低音埙 + 木梆 + 开五度长垫，被说「太可怕了」。
    这一首彻底换掉：不用低音、不用木梆、不用鼓，和弦换成大三度，
    旋律往高处走——是晴天站在塬上，不是走进地宫。
    """
    n, q = loop_length(seconds)
    rng = np.random.default_rng(3)
    buf = np.zeros(n)
    t = np.arange(n) / SR

    # 暖风：比序章亮，像午后掠过草坡
    wind = noise_loop(n, rng, 1300,
                      mod=lambda tt: 0.72 + 0.28 * np.sin(2 * np.pi * tt / seconds * 2 + 0.4))
    buf += 0.048 * wind

    # 大调三和弦，且整个抬到中音区：没有低音持续音，就不会有"地宫"感
    buf += pad_layer(n, seconds, ["A3", "C#4", "E4"], q, 0.026, scale=S_A)

    # 竹笛：高音区、短句、轻快，不拖长音
    for note, at, dur in [("E4", 3.0, 2.2), ("A4", 7.4, 1.8), ("B4", 11.2, 2.0),
                          ("A4", 16.0, 1.8), ("E4", 20.4, 2.2), ("F#4", 25.0, 1.9),
                          ("A4", 29.6, 2.0), ("B4", 34.2, 1.8), ("A4", 39.0, 2.2),
                          ("E4", 44.0, 1.9), ("F#4", 49.0, 2.1), ("A4", 54.0, 1.8),
                          ("B4", 59.0, 2.0), ("A4", 64.0, 2.2), ("E4", 69.0, 1.9),
                          ("F#4", 74.0, 2.0), ("A4", 79.0, 1.8), ("B4", 84.0, 2.1),
                          ("A4", 89.0, 2.2), ("E4", 94.0, 2.0)]:
        place(buf, at * SR, 0.075 * flute(q(S_A[note]), dur, vib=5.6, breath=0.07))

    # 拨弦：像有人坐在田埂上拨两下，句子短、间隔松
    for note, at in [("A3", 5.0), ("E4", 6.2), ("A4", 13.4), ("B4", 14.6),
                     ("A4", 22.6), ("E4", 23.8), ("F#4", 31.8), ("A4", 33.0),
                     ("B4", 41.4), ("A4", 42.6), ("E4", 51.0), ("A4", 52.2),
                     ("B4", 61.0), ("A4", 62.4), ("F#4", 71.4), ("E4", 72.6),
                     ("A4", 81.4), ("B4", 82.6), ("A4", 91.4), ("E4", 92.6)]:
        place(buf, at * SR, 0.085 * pluck(q(S_A[note]), 2.4, decay=1.2, bright=0.55))

    # 鸟：午后的塬上，比别处多一些
    for i, at in enumerate([1.6, 2.1, 9.0, 9.4, 18.0, 18.4, 27.0, 36.0, 36.4,
                            45.0, 45.5, 55.0, 66.0, 66.4, 76.0, 86.0, 86.4, 96.0]):
        place(buf, at * SR, 0.028 * bird(2800 + (i * 173) % 1600, 0.15))

    return reverb(buf, seconds=2.2, mix=0.20)


def amb_quanzhou(seconds=100):
    """泉州 · 海与舟（C 宫，开阔）：海浪、南音式拨弦、船索的吱声。

    这一首不用箫——箫留给序章和秦陵。海面上是断续的拨弦句子，
    像码头边有人在调弦，和海浪互不遮挡。
    """
    n, q = loop_length(seconds)
    rng = np.random.default_rng(4)
    buf = np.zeros(n)
    t = np.arange(n) / SR

    # 海浪：两组不同周期的起伏叠加，避免"电子呼吸"
    swell = (0.5 + 0.5 * np.sin(2 * np.pi * 4 * t / seconds - 0.7)
             ) * (0.6 + 0.4 * np.sin(2 * np.pi * 7 * t / seconds + 2.0))
    sea = bandpass(noise_loop(n, rng, 2600), 120, 5000)
    buf += 0.15 * sea / (np.max(np.abs(sea)) + 1e-9) * swell

    # 极轻的 C 宫五度，只做海雾后的底
    buf += pad_layer(n, seconds, ["C3", "G3", "C4"], q, 0.030, scale=S_C)

    # 拨弦（南音琵琶的颗粒感）：短句、留白，音区在 C4~A4
    phrases = [
        [("C4", 3.0), ("D4", 4.2), ("E4", 5.4)],
        [("G4", 11.0), ("E4", 12.4), ("D4", 13.8), ("C4", 15.2)],
        [("A4", 22.0), ("G4", 23.6), ("E4", 25.2)],
        [("D4", 31.0), ("E4", 32.4), ("G4", 33.8), ("A4", 35.2)],
        [("C5", 42.0), ("A4", 43.4), ("G4", 44.8)],
        [("E4", 51.0), ("D4", 52.4), ("C4", 53.8)],
        [("G4", 61.0), ("A4", 62.6), ("G4", 64.2), ("E4", 65.6)],
        [("D4", 72.0), ("C4", 73.6), ("D4", 75.2)],
        [("A4", 82.0), ("G4", 83.6), ("E4", 85.0)],
        [("C4", 92.0), ("D4", 93.4), ("C4", 95.0)],
    ]
    for phrase in phrases:
        for note, at in phrase:
            place(buf, at * SR, 0.115 * pluck(q(S_C[note]), 3.8, decay=1.9, bright=0.58))

    # 船索/桅杆：短促的木与麻绳的摩擦
    for at in [12.0, 12.35, 30.0, 55.0, 55.4, 72.0, 84.0, 84.3]:
        place(buf, at * SR, 0.048 * woodblock(q(S_C["G4"])))

    return reverb(buf, seconds=3.0, mix=0.30)


def amb_epilogue(seconds=110):
    """终章 · 合卷（F 宫）：古琴为主，一记很远的钟，几乎不用和弦。收束，不是结束。"""
    n, q = loop_length(seconds)
    rng = np.random.default_rng(5)
    buf = np.zeros(n)

    for note, at in [("F3", 1.0), ("C4", 7.0), ("D4", 12.5), ("F4", 18.0),
                     ("G4", 24.0), ("F4", 30.0), ("C4", 36.0), ("A3", 42.0),
                     ("G3", 48.0), ("F3", 54.0), ("C4", 60.0), ("D4", 66.0),
                     ("F4", 72.0), ("G4", 78.0), ("F4", 84.0), ("C4", 90.0),
                     ("F3", 96.0), ("D4", 102.0), ("F3", 107.0)]:
        place(buf, at * SR, 0.24 * pluck(q(S_F[note]), 5.0, decay=2.6, bright=0.38))

    for note, at in [("C5", 40.0), ("F5", 86.0)]:
        place(buf, at * SR, 0.040 * bell(q(S_F[note]), 5.0))

    buf += 0.030 * noise_loop(n, rng, 240,
                              mod=lambda tt: 0.5 + 0.5 * np.sin(2 * np.pi * tt / seconds))
    return reverb(buf, seconds=5.2, mix=0.42)


TRACKS = [
    # 序章、良渚、第二章都改用授权音乐（Kevin MacLeod：《Meditation Impromptu 01》
    # 《Ripples》《Eastern Thought》，CC BY 4.0），不再由本脚本合成。
    # 这三个都不在本表里，重跑脚本不会覆盖它们。
    ("amb_quanzhou", amb_quanzhou),
    ("amb_epilogue", amb_epilogue),
]


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    print("合成环境音乐（%d Hz / 单声道 / 无缝循环）" % SR)
    for name, fn in TRACKS:
        write_wav(os.path.join(OUT_DIR, name + ".wav"), fn())
    print("完成 →", os.path.normpath(OUT_DIR))


if __name__ == "__main__":
    main()
