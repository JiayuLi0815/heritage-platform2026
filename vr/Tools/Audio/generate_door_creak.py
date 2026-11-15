#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""秦陵阙门的开门声：木头干涩的吱呀 + 最后一声闷响。纯合成，无采样。

用法：python3 Tools/Audio/generate_door_creak.py
输出：Assets/Huaxia/Resources/Audio/door_creak.wav
"""

import os
import wave

import numpy as np

SR = 22050
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "..", "Assets", "Huaxia", "Resources", "Audio", "door_creak.wav")

DUR = 1.7
n = int(DUR * SR)
t = np.arange(n) / SR
rng = np.random.default_rng(7)

# 门轴的低频摩擦：音高慢慢升，像门被推开时木头在受力
f0 = 72.0 + 34.0 * (t / DUR)
# 粘滑（stick-slip）引起的抖动
slip = 0.55 + 0.45 * np.sin(2 * np.pi * (5.5 + 2.5 * np.sin(2 * np.pi * t * 0.6)) * t)

body = np.sin(2 * np.pi * np.cumsum(f0) / SR) * slip
body += 0.45 * np.sin(2 * np.pi * np.cumsum(f0 * 2.31) / SR) * slip
body += 0.20 * np.sin(2 * np.pi * np.cumsum(f0 * 3.87) / SR) * slip

# 干木头的沙沙声：带通噪声
noise = rng.standard_normal(n)
X = np.fft.rfft(noise)
freqs = np.fft.rfftfreq(n, 1 / SR)
gain = (1.0 / np.sqrt(1.0 + (freqs / 1500.0) ** 4)
        * 1.0 / np.sqrt(1.0 + (260.0 / np.maximum(freqs, 1e-3)) ** 4))
noise = np.fft.irfft(X * gain, n)
noise /= np.max(np.abs(noise)) + 1e-9

sig = 0.55 * body + 0.42 * noise * slip

# 起音、收尾
env = np.clip(t / 0.07, 0, 1) * np.clip((DUR - t) / 0.30, 0, 1)
sig *= env

# 门扇落定的闷响
start = int(1.28 * SR)
m = n - start
tt = np.arange(m) / SR
thud = np.sin(2 * np.pi * 54 * tt) * np.exp(-tt / 0.13)
thud += 0.4 * np.sin(2 * np.pi * 96 * tt) * np.exp(-tt / 0.08)
sig[start:] += 0.65 * thud

sig = sig - np.mean(sig)
sig *= 0.55 / (np.max(np.abs(sig)) + 1e-9)

data = (np.clip(sig, -1, 1) * 32767).astype("<i2")
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with wave.open(OUT, "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(data.tobytes())

print("写出", os.path.normpath(OUT), "%.2f s" % DUR)
