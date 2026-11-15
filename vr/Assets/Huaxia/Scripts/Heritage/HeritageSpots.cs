using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 场景里各点位（良渚 ①②③、秦陵 ①②③、泉州 ①②）的图文与语音。
    /// 这些不是 UNESCO 名录里的整处遗产，而是「走到现场看到的这一处」，
    /// 所以单独放一份数据，不挤进 HeritageArchive.All 的三卷正文里。
    /// 面板、语音、图注都复用同一套 HeritageSite，只是没有三维展台（model 留空）。
    /// 图片来自服务器遗产库 public/img/gallery 与项目自带的公开影像。
    /// </summary>
    public static class HeritageSpots
    {
        public static readonly HeritageSite[] All =
        {
            // ------------------------------------------------------------ 良渚
            new HeritageSite
            {
                id = "spot_lz_water",
                name = "① 城墙与水门",
                en = "City Wall, Water Gates and the Dam System",
                volume = "卷一 · 水与玉　　第一站",
                code = "良渚古城遗址 · 现场点位 ①",
                year = "距今约 5300—4300 年",
                place = "浙江 · 杭州 · 余杭",
                era = "内城约 300 公顷，城墙周长约 6 公里",
                scale = "城墙底部铺石、其上夯土，城外有十一条水坝",
                tagline = "城墙不用石　水门能行船",
                tierLine = "● 已证实　城墙、水门、水坝的位置与尺寸均有考古勘探与试掘依据",
                paragraphs = new[]
                {
                    "良渚的城墙是土筑的，但底下先铺了一层石块。石块垫底，上面一层层夯土，两侧再培护坡——这样在江南的软土上才站得住。城墙围出的内城约三百公顷，比明清故宫还大。",
                    "墙上开了八座水门。水门既是城门，也是码头：城里的河道与城外的河网通着，船可以直接摇进城。这是一座以水为街的城。",
                    "城址西北的山间，十一条坝体连成高坝、低坝两级水面，拦洪、蓄水、行船都用得上。这是中国目前发现最早的大型水利系统，比传说里的大禹治水还早约一千年。修它要长期、成规模地调人。"
                },
                keys = new[]
                {
                    "看墙基：石头垫底、夯土在上，这是南方软土上筑城的办法",
                    "看水门：八座水门把城内的河与城外的河接通，船能进城"
                },
                images = new[]
                {
                    "Heritage/Images/spot_lz_water_1",
                    "Heritage/Images/spot_lz_water_2"
                },
                captions = new[]
                {
                    "城墙与稻田（来源：项目遗产库）",
                    "塘山坝体位置示意（来源：项目遗产库）"
                },
                audio = "Heritage/Audio/spots/spot_lz_water",
                model = null
            },

            new HeritageSite
            {
                id = "spot_lz_mojiao",
                name = "② 莫角山台地",
                en = "Mojiaoshan Platform",
                volume = "卷一 · 水与玉　　第二站",
                code = "良渚古城遗址 · 现场点位 ②",
                year = "距今约 5300—4300 年",
                place = "浙江 · 杭州 · 余杭",
                era = "台地高出平地十余米，顶面约三万平方米",
                scale = "宫殿区；南侧池中寺遗址出土炭化稻谷近二十万斤",
                tagline = "人工堆起来的宫城",
                tierLine = "● 已证实　台地范围、堆筑工艺、建筑基址与炭化稻谷均有发掘报告",
                paragraphs = new[]
                {
                    "莫角山不是天然的山，是人工一担一担挑土堆起来的台地。高出平地十余米，顶面三万平方米，是宫殿区。",
                    "台地上找到过大型建筑的柱洞，还有成片的红烧土——那是房子烧塌以后留下的。柱洞的排列说明这里的建筑有成排的柱网，不是草棚。",
                    "南边的池中寺遗址，坑里堆着近二十万斤炭化稻谷，是当时的粮仓。有这样的储备，才养得起修城墙、筑水坝的人。"
                },
                keys = new[]
                {
                    "看台地：整座台地是人工堆筑的，剖面能看出层层夯土",
                    "看红烧土：房子烧塌后留下的红烧土块，是建筑存在的直接证据"
                },
                images = new[]
                {
                    "Heritage/Images/spot_lz_mojiao_1",
                    "Heritage/Images/spot_lz_mojiao_2"
                },
                captions = new[]
                {
                    "莫角山台地发掘面（来源：项目遗产库）",
                    "建筑基址与红烧土堆积（来源：项目遗产库）"
                },
                audio = "Heritage/Audio/spots/spot_lz_mojiao",
                model = null
            },

            new HeritageSite
            {
                id = "spot_lz_jade",
                name = "③ 反山与玉器",
                en = "Fanshan Cemetery and Liangzhu Jades",
                volume = "卷一 · 水与玉　　第三站",
                code = "良渚古城遗址 · 现场点位 ③",
                year = "距今约 5300—4300 年",
                place = "浙江 · 杭州 · 余杭",
                era = "反山、瑶山为高等级墓地",
                scale = "随葬玉琮、玉璧、玉钺；玉料多自外地输入",
                tagline = "玉不是好看　玉是分人的",
                tierLine = "● 已证实　墓葬等级与玉器种类有发掘实证；玉料长途输入的产地分析仍在进行",
                paragraphs = new[]
                {
                    "反山、瑶山的贵族墓里，随葬的是玉琮、玉璧、玉钺。玉琮外方内圆，中孔上下贯通，四角刻着神人兽面纹：上面一顶羽冠，下面一对大眼。",
                    "同样的纹样反复出现，却从不出现在普通人的墓里。做一件玉琮，要先从几百公里外把玉料运回来，再花上成百上千个工去磨。所以玉不只是好看，它是用来区分人的。",
                    "良渚的玉器上没有文字，可它把社会分层写得比文字还清楚：谁能用玉、用几件、用什么样的纹样，都有规矩。"
                },
                keys = new[]
                {
                    "看纹样：神人兽面纹是良渚的「标准像」，只出现在高等级墓里",
                    "看器形：玉琮外方内圆、中孔贯通，玉钺是权力的标志"
                },
                images = new[]
                {
                    "Heritage/Images/spot_lz_jade_1",
                    "Heritage/Images/spot_lz_jade_2"
                },
                captions = new[]
                {
                    "反山、瑶山所在的遗址区域（来源：项目遗产库）",
                    "古城与北缘山地形势（来源：项目遗产库）"
                },
                audio = "Heritage/Audio/spots/spot_lz_jade",
                model = null
            },

            // ------------------------------------------------------------ 秦陵
            new HeritageSite
            {
                id = "spot_qn_pit",
                name = "① 兵马俑一号坑",
                en = "Pit No.1 of the Terracotta Army",
                volume = "卷二 · 帝与陵　　第一站",
                code = "秦始皇陵 · 现场点位 ①",
                year = "公元前 246—前 208 年",
                place = "陕西 · 西安 · 临潼",
                era = "一号坑东西长 230 米、南北宽 62 米",
                scale = "面积一万四千余平方米，陶俑近六千件",
                tagline = "地下的一支军队",
                tierLine = "● 已证实　坑的尺寸、隔墙、棚木与陶俑编队均有发掘实证；编队解释仍有多说",
                paragraphs = new[]
                {
                    "一号坑东西长两百三十米、南北宽六十二米、深约五米，面积一万四千多平方米。坑里十道夯土隔墙隔出十一条过洞：隔墙宽两到两点五米、原来高三点二米，过洞宽约三点五米。",
                    "坑底铺青砖，隔墙顶上架地栿、立柱、枋木、棚木，再铺苇席、抹胶泥、填黄土，把整个坑封成地下空间。一、二、三号坑共用木料约八千多立方米。",
                    "坑里近六千件陶俑面朝东列阵：前锋是轻装步兵，主体车兵和步兵相间，两翼与后卫是重装。武士俑身高一米七五到一米八六。坑顶后来塌了、烧了，土灌进去——今天看到的是塌下来以后的样子，不是当年的样子。",
                    "你现在走的这道斜坡，就是当年把陶俑运进坑里的门道，不是给游客修的入口。"
                },
                keys = new[]
                {
                    "看军阵：前锋、主体、两翼、后卫各有编制，不是随手摆的",
                    "看细节：陶俑身高、发式、甲衣各不相同，是按真人做的"
                },
                images = new[]
                {
                    "Heritage/Images/spot_qn_pit_1",
                    "Heritage/Images/spot_qn_pit_2",
                    "Heritage/Images/spot_qn_pit_3"
                },
                captions = new[]
                {
                    "一号坑全景（来源：Wikimedia Commons）",
                    "一号坑军阵侧看（来源：Wikimedia Commons）",
                    "二号坑棚木遗迹剖面（来源：Wikimedia Commons）"
                },
                audio = "Heritage/Audio/spots/spot_qn_pit",
                model = null
            },

            new HeritageSite
            {
                id = "spot_qn_mound",
                name = "② 封土",
                en = "The Tumulus",
                volume = "卷二 · 帝与陵　　第二站",
                code = "秦始皇陵 · 现场点位 ②",
                year = "公元前 246—前 208 年，修了三十九年",
                place = "陕西 · 西安 · 临潼",
                era = "现存高约 51 米",
                scale = "覆斗形夯土台，一级一级往里收",
                tagline = "一级一级收上去的土山",
                tierLine = "○ 学界推断　封土现存高度有实测；原高与台阶数有争议，地宫未发掘",
                paragraphs = new[]
                {
                    "封土是陵园的中心，夯土一层层堆成覆斗形——上小下大、四面收坡。现在还剩五十多米高，当年更高。",
                    "注意它的外轮廓：一级一级往里收。那是每夯一层就缩小一圈留下的台阶，不是自然长成的山。",
                    "地宫里的事，文献里有「以水银为百川江河大海」这样的说法，但到今天没有发掘，谁也没进去过。所以凡是复原地宫内部的，都是推测。这个场景就停在封土外面，不往下编。"
                },
                keys = new[]
                {
                    "看轮廓：一级一级的台阶是夯筑时逐层收分留下的",
                    "看取舍：地宫未发掘，任何内部复原都只是推测"
                },
                images = new[]
                {
                    "Heritage/Images/spot_qn_mound_1",
                    "Heritage/Images/spot_qn_mound_2"
                },
                captions = new[]
                {
                    "封土近景（来源：Wikimedia Commons）",
                    "封土远望（来源：Wikimedia Commons）"
                },
                audio = "Heritage/Audio/spots/spot_qn_mound",
                model = null
            },

            new HeritageSite
            {
                id = "spot_qn_hall",
                name = "③ 铜车马坑",
                en = "Bronze Chariots and Horses",
                volume = "卷二 · 帝与陵　　第三站",
                code = "秦始皇陵 · 现场点位 ③",
                year = "公元前 246—前 208 年",
                place = "陕西 · 西安 · 临潼",
                era = "出土两乘铜车马，按真车真马约一半大小铸造",
                scale = "一乘由数千个零件组成",
                tagline = "几千个零件拼成一辆车",
                tierLine = "● 已证实　两乘铜车马已修复展出，工艺与尺寸有实测数据",
                paragraphs = new[]
                {
                    "这处房屋基址下面出过两乘铜车马，按真车真马大约一半的大小铸的。一件车由几千个零件拼起来，铸、嵌、焊、销，好几种工艺都用上了。",
                    "看这辆车：车厢、车轮、辕、衡、马具，都能拆开看。伞盖又薄又大，做到这个尺寸不变形，说明当时对铸件的控制已经很稳。",
                    "车马是高等级随葬品，形制可以拿来和文献里的车制对照，是研究秦代工艺和等级的一手材料。从这里往回走，走过界门就回序章。"
                },
                keys = new[]
                {
                    "看工艺：铸、嵌、焊、销并用，一件车几千个零件",
                    "看伞盖：又薄又大而不变形，说明铸件控制很稳"
                },
                images = new[]
                {
                    "Heritage/Images/spot_qn_hall_1",
                    "Heritage/Images/spot_qn_hall_2"
                },
                captions = new[]
                {
                    "铜车马·安车（来源：Wikimedia Commons）",
                    "铜车马展厅（来源：Wikimedia Commons）"
                },
                audio = "Heritage/Audio/spots/spot_qn_hall",
                model = null
            },

            // ------------------------------------------------------------ 泉州
            new HeritageSite
            {
                id = "spot_qz_bridge",
                name = "① 洛阳桥",
                en = "Luoyang Bridge",
                volume = "卷三 · 海与市　　第一站",
                code = "泉州：宋元中国的世界海洋商贸中心 · 现场点位 ①",
                year = "北宋皇祐五年（1053）始建，嘉祐四年（1059）成",
                place = "福建 · 泉州 · 洛阳江入海口",
                era = "中国现存最早的跨海梁式石桥",
                scale = "全长约 730 米，现存桥墩 40 余座",
                tagline = "在烂泥上养蛎固基",
                tierLine = "● 已证实　桥基、桥墩、蛎房固基均有考古与文献双重依据",
                paragraphs = new[]
                {
                    "这里是洛阳江的入海口，宋元时期泉州港的北面门户。洛阳桥就架在这段江上，北宋皇祐五年开始修，花了六年。",
                    "江底是烂泥，桥基站不住。工匠往江里抛填石块，养成一条矮石堤，再在堤上砌墩——这叫筏形基础。为了让石块粘成一体，又在桥基上养牡蛎，让蛎房把石头箍住。这是世界上最早的生物固基。",
                    "桥修好以后，泉州往北的陆路就通了。海船和内陆的货在这里换手，桥上走人，桥下行船。一座桥，把港口和腹地接在了一起。"
                },
                keys = new[]
                {
                    "看桥基：抛石成堤、堤上砌墩，是筏形基础",
                    "看固基：桥基上养牡蛎，蛎房把石块箍成一体"
                },
                images = new[]
                {
                    "Heritage/Images/spot_qz_bridge_1",
                    "Heritage/Images/spot_qz_bridge_2"
                },
                captions = new[]
                {
                    "洛阳桥（来源：项目遗产库）",
                    "洛阳桥桥面与石墩（来源：项目遗产库）"
                },
                audio = "Heritage/Audio/spots/spot_qz_bridge",
                model = null
            },

            new HeritageSite
            {
                id = "spot_qz_pagoda",
                name = "② 开元寺双塔",
                en = "Kaiyuan Temple Twin Pagodas",
                volume = "卷三 · 海与市　　第二站",
                code = "泉州：宋元中国的世界海洋商贸中心 · 现场点位 ②",
                year = "南宋绍定元年（1228）起改石构",
                place = "福建 · 泉州 · 开元寺",
                era = "镇国塔高 48.24 米，仁寿塔高 44.06 米",
                scale = "五层八角仿木构石塔，塔身有浮雕",
                tagline = "石头做的木塔",
                tierLine = "● 已证实　双塔的年代、高度、形制均有实测与文献依据",
                paragraphs = new[]
                {
                    "远处那两座石塔，是开元寺的镇国塔和仁寿塔，一东一西，都是南宋时候改建成石头的。镇国塔高四十八米多，五层八角，每一层收分，塔身外面浮雕着佛、菩萨、罗汉。",
                    "石塔能立七百多年不倒，靠的是每层上下叠压、重心层层收进去。它模仿的是木构的斗拱与出檐，可材料换成了花岗岩——这是把木塔的做法搬进石头里。",
                    "宋元时候，开元寺一带是泉州城的中心，往南出城就是码头。商船出海前来进香，回来了也来还愿——一座城的信仰和生意，是长在一起的。看完这一处，这一卷就读完了。"
                },
                keys = new[]
                {
                    "看收分：五层八角逐层收进去，重心才稳",
                    "看仿木：斗拱、出檐都用石头做出来，是石仿木的典型"
                },
                images = new[]
                {
                    "Heritage/Images/spot_qz_pagoda_1",
                    "Heritage/Images/spot_qz_pagoda_2"
                },
                captions = new[]
                {
                    "开元寺大殿（来源：项目遗产库）",
                    "开元寺镇国塔（来源：项目遗产库）"
                },
                audio = "Heritage/Audio/spots/spot_qz_pagoda",
                model = null
            }
        };

        public static HeritageSite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
                if (All[i].id == id) return All[i];
            return null;
        }

        /// <summary>某个场景里的点位，按 id 前缀取（lz / qn / qz），保持数组顺序。</summary>
        public static HeritageSite[] OfScene(string key)
        {
            var list = new System.Collections.Generic.List<HeritageSite>();
            string prefix = "spot_" + key + "_";
            for (int i = 0; i < All.Length; i++)
                if (All[i].id.StartsWith(prefix)) list.Add(All[i]);
            return list.ToArray();
        }
    }
}
