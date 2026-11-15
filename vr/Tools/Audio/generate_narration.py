#!/usr/bin/env python3
"""生成弘文的中文语音包（免费神经网络 TTS）。

首选：edge-tts（微软 Edge 的在线神经网络音色，免注册、免 API Key）
      zh-CN-XiaoxiaoNeural「晓晓」—— 温柔、亲和、有气息感，适合做讲解员。
      产出 mp3 -> afconvert -> 22.05kHz / 16bit / 单声道 WAV
备用：edge-tts 不可用（断网等）时退回 macOS 自带 say -v Tingting。

输出：Assets/Huaxia/Resources/Heritage/Audio/<key>.wav

准备：pip3 install --user edge-tts
用法：python3 Tools/Audio/generate_narration.py [key ...]
      python3 Tools/Audio/generate_narration.py --voice zh-CN-XiaoyiNeural
      python3 Tools/Audio/generate_narration.py --preview   # 只出一段试听，不覆盖语音包
"""
import asyncio
import os
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets/Huaxia/Resources/Heritage/Audio")
PREVIEW = os.path.join(ROOT, "Previews", "voice")

# 温柔亲和的几个候选：晓晓最像讲解员，晓伊更年轻一点，辽宁晓北带口音
VOICE = "zh-CN-XiaoxiaoNeural"
VOICES = {
    "zh-CN-XiaoxiaoNeural": "晓晓（温柔、亲和，默认）",
    "zh-CN-XiaoyiNeural": "晓伊（年轻、轻快）",
    "zh-TW-HsiaoChenNeural": "晓臻（台湾腔，更软）",
    "zh-CN-liaoning-XiaobeiNeural": "晓北（东北口音）",
}
RATE = "-6%"     # 叙事比默认略慢
PITCH = "-2Hz"   # 略低一点，少一点电子味
FALLBACK_VOICE = "Tingting"   # 没有 edge-tts 时的兜底
FALLBACK_RATE = 165

LINES = {
    # 进入序章时弘文的开场白
    "intro": (
        "您好，我是您的智能讲解员弘文，欢迎来到华夏文明探源·沉浸式体验空间。"
        "这是一场关于中华文明源头的行走式展览，展厅就在山谷里。"
        "三条岔路，通向三处世界遗产——浙江余杭的良渚古城，陕西临潼的秦始皇陵，福建泉州的宋元海港。"
        "水、土、海，三种把人群聚在一起的力量。"
        "每一处，都有据可查的年代、面积和出土器物；也有还没说清的地方，我会替你把两者分开讲。"
        "走到立牌跟前按 E，我就给你细说。"
        "请跟随我的指引，翻开《河山册》，开启这段穿越千年的探索旅程吧！"
    ),
    "1592": (
        "良渚古城遗址，在浙江杭州余杭，二零一九年列入世界遗产名录，距今约五千三百年到四千三百年。"
        "内城面积约三百公顷，算上外城超过六百公顷。"
        "城外有一圈水坝，是中国目前发现最早的大型水利系统，"
        "比传说里的大禹治水还早一千年。"
        "城里出土最多的是玉：玉琮、玉璧、玉钺。"
        "玉琮外方内圆，中孔上下贯通，刻着神人兽面纹。"
        "这些东西不出现在普通人的墓里，说明那个时候已经有人能调动成千上万的人力。"
        "稻田、水门、城墙、祭坛，都在说同一件事：五千年前，这里已经是一座城。"
    ),
    "441": (
        "秦始皇陵，在陕西西安临潼的骊山北麓，一九八七年列入世界遗产名录。"
        "陵园从公元前二四六年开始修，前后三十九年，动用了七十多万人。"
        "封土下面至今没有打开，我们今天看到的，是陵园布局、陪葬坑和兵马俑。"
        "一九七四年，当地农民打井打出陶片，一号坑由此重见天日。"
        "三个俑坑合计约八千件陶俑陶马，每一件的脸都不一样。"
        "它们的排列就是军阵：前锋、侧翼、后卫，一支军队被原样埋进土里。"
        "史书说陵里以水银为百川江河，这句话至今还只是记载，没有实证。"
    ),
    "1561": (
        "泉州，宋元时期叫刺桐。"
        "二零二一年，它以「宋元中国的世界海洋商贸中心」为名列入世界遗产名录，一共二十二处遗产点。"
        "十世纪到十四世纪，这里是世界上最大的港口之一，马可波罗和伊本白图泰都写过它。"
        "遗产点里，有九日山的祈风石刻——官员为商船祈风，把这件事刻在石头上；"
        "有清净寺的阿拉伯式石拱门，有开元寺的双塔，有洛阳桥，有德济门遗址，"
        "还有安溪的冶铁遗址和磁灶的窑址。"
        "码头、桥梁、窑场、寺庙、墓葬，连成一条从生产到出海的完整链条。"
    ),
    # 玩家踩进立牌前的引导圈：她走过来，报这一处的基础信息，再把详说交给牌上的 E
    "arrive_1592": (
        "良渚古城遗址，在浙江余杭，距今五千年。"
        "内城三百公顷，城外一圈水坝，是中国最早的大型水利系统。"
        "这块牌上写的是水与玉；按 E，我带你细看。"
    ),
    "arrive_441": (
        "秦始皇陵，在陕西临潼。"
        "陵园修了三十九年，动了七十多万人，封土底下至今没有打开。"
        "这块牌上写的是帝与陵；按 E，我带你细看。"
    ),
    "arrive_1561": (
        "泉州，宋元时候叫刺桐，是当时世界上最大的港口之一，一共二十二处遗产点。"
        "这块牌上写的是港与海；按 E，我带你细看。"
    ),
    # 面板开过之后给小地图指路：她转身朝那条岔路说
    "guide_intro": (
        "顺着这条道往前走，到三岔口就停下。"
        "三块牌子一处一块：左手那条通良渚，中间穿过牌坊通秦陵，右手那条通泉州。"
        "想看哪一处，走到牌子跟前站一下，我再给你细说。"
    ),
    "guide_1592": (
        "良渚在最左手那条路。走到头有一道界门，站到门前按 E，我陪你下去。"
    ),
    "guide_441": (
        "秦陵在正前方。穿过牌坊一直往前，路的尽头就是界门，按 E。"
    ),
    "guide_1561": (
        "泉州在右手那条路。沿路走到界门跟前，按 E 过去。"
    ),
    # 三卷都读完
    "cheer": (
        "三卷都读完了。五千年的城、两千年的军队、一千年的港口，都在你手里这本册子里了。"
    ),
}

# 场景里各点位（①②③④）的短讲解。输出到 Audio/spots/<key>.wav，
# 由 HeritageSpots 里的 HeritageSite.audio 指过去。
SPOTS = {
    # ---------------------------------------------------------------- 良渚
    "spot_lz_water": (
        "先看城墙。良渚的城墙不用石头，是先在底部铺一层石块，再一层层夯土，"
        "外面还包着护坡。城墙围出的内城大约三百公顷，比故宫还大。"
        "城墙上开了八座水门，船能直接摇进城——这是一座以水为街的城。"
        "城址西北的山里，十一条坝体连成高坝、低坝两级水面，拦洪、蓄水、行船都用它。"
        "这是中国目前发现最早的大型水利系统，比传说里的大禹治水还早约一千年。"
    ),
    "spot_lz_mojiao": (
        "这里是莫角山。它不是天然的山，是人工一担一担挑土堆起来的台地，"
        "高出平地十几米，顶上三万平方米，是宫殿区。"
        "台地上找到过大型建筑的柱洞和成片的红烧土——那是房子烧塌以后留下的。"
        "南边的池中寺遗址，坑里堆着近二十万斤炭化稻谷，是当时的粮仓。"
        "站在这个位置往南看，整座内城的格局就清楚了：宫城居中，城墙围一圈，水门通出去。"
    ),
    "spot_lz_jade": (
        "反山、瑶山的贵族墓里，随葬的是玉琮、玉璧、玉钺。"
        "玉琮外方内圆，中孔上下贯通，四角刻着神人兽面纹：上面一顶羽冠，下面一对大眼。"
        "同样的纹样反复出现，却从不出现在普通人的墓里。"
        "做一件玉琮，要从几百公里外运回玉料，再花上成百上千个工。"
        "所以玉不只是好看，它是用来区分人的。"
        "说明：这一带的玉器实物没有收进本项目的影像库，下面两张是墓地所在的遗址区域。"
    ),
    # ---------------------------------------------------------------- 秦陵
    "spot_qn_pit": (
        "一号坑东西长两百三十米，宽六十二米，面积一万四千多平方米。"
        "坑里十道夯土隔墙隔出十一条过洞，隔墙顶上架地栿、立柱、枋木、棚木，再铺苇席、抹泥、填土，"
        "整个坑封成地下空间。坑里近六千件陶俑面朝东列阵："
        "前锋是轻装步兵，主体车兵和步兵相间，两翼与后卫是重装。"
        "武士俑身高一米七五到一米八六，是按真人做的。"
        "你现在站的这道走道，就是当年把陶俑运进坑里的斜坡门道。"
    ),
    "spot_qn_mound": (
        "前面那座土山就是封土，陵园的中心。夯土一层层往上堆，堆成上小下大、四面收坡的覆斗形。"
        "你注意它的外轮廓，一级一级往里收——那是每夯一层就缩小一圈留下的台阶，不是自然长成的山。"
        "地宫里的事，文献里有以水银为百川江河大海的说法，但到今天没有发掘，谁也没进去过。"
        "所以凡是复原地宫内部的，都是推测。这个场景就停在封土外面，不往下编。"
        "说明：封土的高度和台阶数是示意，原高学界有争议。"
    ),
    "spot_qn_hall": (
        "这处房屋基址下面出过两乘铜车马，按真车真马大约一半的大小铸的。"
        "一件车由几千个零件拼起来，铸、嵌、焊、销，好几种工艺都用上了。"
        "伞盖又薄又大，做到这个尺寸不变形，说明当时对铸件的控制已经很稳。"
        "车马是高等级随葬品，形制可以拿来和文献里的车制对照，是研究秦代工艺和等级的一手材料。"
        "从这里往回走，走过界门就回序章。这一卷读完了。"
    ),
    # ---------------------------------------------------------------- 泉州
    "spot_qz_bridge": (
        "这里是洛阳江的入海口，宋元时期泉州港的北面门户。"
        "洛阳桥就架在这段江上，北宋皇祐五年开始修，花了六年。"
        "江底是烂泥，桥基站不住，工匠就往江里抛填石块，养成一条矮石堤，再在堤上砌墩——这叫筏形基础。"
        "为了把石块粘成一体，又在桥基上养牡蛎，让蛎房把石头箍住，这是世界上最早的生物固基。"
        "桥修好以后，泉州往北的陆路就通了，海船和内陆的货在这里换手。"
    ),
    "spot_qz_pagoda": (
        "远处那两座石塔，是开元寺的镇国塔和仁寿塔，一东一西，都是南宋时候改建成石头的。"
        "镇国塔高四十八米多，五层八角，每一层都收分，塔身外面浮雕着佛、菩萨、罗汉。"
        "石塔能立七百多年不倒，靠的是每层上下叠压、重心层层收进去。"
        "宋元时候，开元寺一带是泉州城的中心，往南出城就是码头。"
        "商船出海前来进香，回来了也来还愿——一座城的信仰和生意，是长在一起的。"
        "看完这一处，这一卷就读完了。"
    ),
}


def synth_edge(text, voice, dst):
    import edge_tts

    async def run(mp3):
        com = edge_tts.Communicate(text, voice, rate=RATE, pitch=PITCH)
        await com.save(mp3)

    with tempfile.TemporaryDirectory() as tmp:
        mp3 = os.path.join(tmp, "line.mp3")
        asyncio.run(run(mp3))
        if not os.path.exists(mp3) or os.path.getsize(mp3) == 0:
            raise RuntimeError("edge-tts 没有返回音频")
        subprocess.run(
            ["afconvert", "-f", "WAVE", "-d", "LEI16@22050", "-c", "1", mp3, dst],
            check=True,
        )


def synth_say(text, dst):
    with tempfile.TemporaryDirectory() as tmp:
        aiff = os.path.join(tmp, "line.aiff")
        subprocess.run(["say", "-v", FALLBACK_VOICE, "-r", str(FALLBACK_RATE), "-o", aiff, text], check=True)
        subprocess.run(
            ["afconvert", "-f", "WAVE", "-d", "LEI16@22050", "-c", "1", aiff, dst],
            check=True,
        )


def build(key, text, voice, dst=None):
    dst = dst or os.path.join(OUT, key + ".wav")
    try:
        synth_edge(text, voice, dst)
        engine = voice
    except Exception as e:                     # 断网、限流、没装 edge-tts
        print("  edge-tts 不可用（%s: %s），退回 say" % (type(e).__name__, e))
        synth_say(text, dst)
        engine = "say/" + FALLBACK_VOICE
    size = os.path.getsize(dst) / 1e6
    print("  %s.wav  %.2f MB  <- %s" % (key, size, engine))


def main():
    args = sys.argv[1:]
    voice = VOICE
    preview = False
    if "--voice" in args:
        i = args.index("--voice")
        voice = args[i + 1]
        del args[i:i + 2]
    if "--preview" in args:
        preview = True
        args.remove("--preview")
    os.makedirs(OUT, exist_ok=True)

    if preview:
        # 每个候选念同一段，方便挑音色；挑好改 VOICE 再整包重生成
        os.makedirs(PREVIEW, exist_ok=True)
        sample = "你好，我是弘文。山谷里三条岔路，各通一处世界遗产。走到立牌跟前按 E，我给你细说。"
        for name in VOICES:
            print("试听", name, VOICES[name])
            dst = os.path.join(PREVIEW, name + ".wav")
            try:
                synth_edge(sample, name, dst)
                print("  ->", dst)
            except Exception as e:
                print("  失败", type(e).__name__, e)
        return

    keys = args or (list(LINES) + list(SPOTS))
    for key in keys:
        dst = None
        if key in LINES:
            text = LINES[key]
        elif key in SPOTS:
            text = SPOTS[key]
            dst = os.path.join(OUT, "spots", key + ".wav")
            os.makedirs(os.path.dirname(dst), exist_ok=True)
        else:
            sys.exit("unknown key: " + key)
        print("TTS", key)
        build(key, text, voice, dst)


if __name__ == "__main__":
    main()
