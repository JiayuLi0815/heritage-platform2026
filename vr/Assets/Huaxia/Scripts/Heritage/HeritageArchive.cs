using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 一处世界遗产的全部展示数据：立牌上刻的字、面板里的正文、图片、语音、微缩模型。
    /// 内容与服务器上的遗产站（192.168.180.14/u/wangyifei/heritage）同源，
    /// 图片取自该站 public/img/sites 与 public/img/gallery。
    /// </summary>
    public class HeritageSite
    {
        public string id;          // UNESCO 编号，同时是资源文件名
        public string name;        // 良渚古城遗址
        public string en;          // Liangzhu Archaeological Site
        public string volume;      // 卷一 · 水与玉
        public string code;        // 世界文化遗产 · 编号 1592
        public string year;        // 2019 年列入
        public string place;       // 浙江 · 杭州 · 余杭
        public string era;         // 距今约 5300—4300 年
        public string scale;       // 内城约 300 公顷
        public string tagline;     // 立牌上那一行小字
        public string tierLine;    // 史实分层（GAME_DESIGN §9.1）
        public string[] paragraphs;
        public string[] keys;
        public string[] images;    // Resources 路径（第 0 张为主图）
        public string[] captions;  // 与 images 一一对应；空则该用默认图注
        public string audio;       // Resources 路径
        public string model;       // 展台上的微缩模型 id；留空表示这一点位没有三维

        /// <summary>有没有三维展台可看。节庆点位与部分现场点位只有影像。</summary>
        public bool HasStage
        {
            get { return !string.IsNullOrEmpty(model); }
        }
    }

    public static class HeritageArchive
    {
        public static readonly HeritageSite[] All =
        {
            new HeritageSite
            {
                id = "1592",
                name = "良渚古城遗址",
                en = "Liangzhu Archaeological Site",
                volume = "卷一 · 水与玉",
                code = "世界文化遗产 · 编号 1592",
                year = "2019 年列入",
                place = "浙江 · 杭州 · 余杭",
                era = "距今约 5300—4300 年",
                scale = "内城约 300 公顷，内外城合计逾 600 公顷",
                tagline = "五千年前的城　水与玉",
                tierLine = "● 已证实　基本信息据《世界遗产名录》申报文本与浙江省文物考古研究所历年发掘报告",
                paragraphs = new[]
                {
                    "良渚古城在浙江杭州余杭，距今约五千三百年到四千三百年。它是一座有城墙、有水门、有宫殿台基，城外还圈着一整套水坝的城。2019 年，它因为「实证中华五千多年文明史」被列入《世界遗产名录》。",
                    "城分三重。中心的莫角山是宫殿区，人工堆起的台地高出地面十余米；往外一圈城墙围出约三百公顷的内城，八座水门开在城墙上，船可以直接摇进城；再往外是外城和周边聚落，合起来超过六百公顷。",
                    "城西北的山间，十一条坝体连成一片水库群，拦出高坝、低坝两级水面。这是中国目前发现最早的大型水利系统，比传说里的大禹治水还早约一千年。修它要长期、成规模地调人——这比任何一件玉器都更能说明当时的社会。",
                    "反山、瑶山的贵族墓里，随葬的是玉琮、玉璧、玉钺。玉琮外方内圆，中孔上下贯通，四角刻神人兽面纹：上面一顶羽冠，下面一对大眼。同样的纹样反复出现，却不出现在普通人的墓里——玉，是那时候用来区分人的。",
                    "稻田、水门、城墙、祭坛，四样东西说的是同一件事：五千年前，这里已经不只是村落，而是一座城。"
                },
                keys = new[]
                {
                    "看水门：八座水门是城门，也是码头，城里的水路与城外河道通着",
                    "看玉琮：外方内圆、中孔贯通，四角的神人兽面纹是良渚的「标准像」",
                    "看水坝：十一条坝体组成的高坝—低坝系统，是中国最早的大型水利工程",
                    "看稻田：莫角山南侧的池中寺遗址，堆着近二十万斤炭化稻谷"
                },
                images = new[]
                {
                    "Heritage/Images/1592_hero",
                    "Heritage/Images/1592_01",
                    "Heritage/Images/1592_02",
                    "Heritage/Images/1592_03",
                    "Heritage/Images/1592_04",
                    "Heritage/Images/1592_05",
                    "Heritage/Images/1592_06"
                },
                audio = "Heritage/Audio/1592",
                // 用户要求：良渚立牌只留影像，不摆三维
                model = null
            },

            new HeritageSite
            {
                id = "441",
                name = "秦始皇陵及兵马俑坑",
                en = "Mausoleum of the First Qin Emperor",
                volume = "卷二 · 帝与陵",
                code = "世界文化遗产 · 编号 441",
                year = "1987 年列入",
                place = "陕西 · 西安 · 临潼",
                era = "公元前 246—前 208 年，修了三十九年",
                scale = "陵园约 56.25 平方公里，封土现存高约 51 米",
                tagline = "两千年前的土　帝与陵",
                tierLine = "○ 学界推断　陵园形制、俑坑规模有考古实证；地宫内部情形仅有《史记》记载，尚未发掘",
                paragraphs = new[]
                {
                    "秦始皇陵在陕西西安临潼的骊山北麓，1987 年列入《世界遗产名录》。陵园从公元前 246 年开始修，到秦二世二年（前 208）被迫停工，前后三十九年。",
                    "陵园内外两重城垣，南面是陵墓区，北面是寝殿、便殿一类的礼制建筑，东西两侧分布着上百座陪葬坑。封土呈覆斗形，现存高度约五十一米，夯层清楚可数。",
                    "1974 年，当地农民打井打出陶片，一号坑由此重见天日。三个俑坑合计约八千件陶俑陶马，每一件的脸都不一样：有的抿嘴，有的皱眉，有的留着八字胡。它们的排列是军阵——前锋、侧翼、后卫，一支军队被原样埋进土里。",
                    "地宫还没有打开。《史记》说「以水银为百川江河大海」，这句话至今只是记载：土壤汞含量异常是间接线索，不能当作实证。这也是这一段被标成「学界推断」的原因。",
                    "兵马俑的意义不在「像不像真人」，而在于它是秦代军工、度量与组织能力的实物切片：兵器上刻着工师与工匠的名字，一批一批可以对上账。"
                },
                keys = new[]
                {
                    "看军阵：一号坑是前锋与主体，二号坑是弩兵、战车与骑兵的混编，三号坑是指挥部",
                    "看脸：八千件陶俑没有两张相同的脸，发式与甲衣区分兵种与等级",
                    "看兵器：青铜铍、戈与弩机上刻着工师名，秦代的问责制度刻在实物上",
                    "看封土：覆斗形夯土台，夯层与排水系统是陵园工程的一部分"
                },
                images = new[]
                {
                    "Heritage/Images/441_hero",
                    "Heritage/Images/441_01",
                    "Heritage/Images/441_02",
                    "Heritage/Images/441_03",
                    "Heritage/Images/441_04",
                    "Heritage/Images/441_05",
                    "Heritage/Images/441_06"
                },
                audio = "Heritage/Audio/441",
                model = "441"
            },

            new HeritageSite
            {
                id = "1561",
                name = "泉州：宋元中国的世界海洋商贸中心",
                en = "Quanzhou: Emporium of the World in Song-Yuan China",
                volume = "卷三 · 港与海",
                code = "世界文化遗产 · 编号 1561",
                year = "2021 年列入",
                place = "福建 · 泉州",
                era = "10—14 世纪（宋元时期）",
                scale = "22 处遗产点，分布在泉州湾沿线与内陆产地",
                tagline = "八百年前的港　海与船",
                tierLine = "● 已证实　22 处遗产点名单据 UNESCO 世界遗产中心档案与泉州市申遗文本",
                paragraphs = new[]
                {
                    "泉州在宋元时期叫刺桐。2021 年，它以「宋元中国的世界海洋商贸中心」为名列入《世界遗产名录》，一共二十二处遗产点。十世纪到十四世纪，这里是世界上最大的港口之一，马可·波罗和伊本·白图泰都写过它。",
                    "二十二处遗产点分成四类：制度保障、多元社群、生产基地、运输网络。九日山的祈风石刻属于第一类——泉州郡守与提举市舶司为商船祈风，把这件事刻在石头上，一刻就是一百多年。",
                    "多元社群留下的是清净寺的阿拉伯式石拱门、灵山的伊斯兰教圣墓、开元寺里的印度教石刻、草庵的摩尼光佛。一座城里同时供着这么多神，是因为海上来的商人就住在这里。",
                    "运输网络是桥、塔、码头：洛阳桥用「筏形基础 + 种蛎固基」在江口淤泥上站住；安平桥跨海五里；六胜塔与万寿塔是进出港的航标；石湖、江口两个码头管着内河与海运的转运。",
                    "生产基地在内陆：磁灶的窑址烧外销瓷，安溪青阳的下草埔冶铁遗址出铁，德化的白瓷从这里出海。码头、桥梁、窑场、寺庙、墓葬连成一条从生产到出海的完整链条——这才是「世界海洋商贸中心」这几个字的解释。"
                },
                keys = new[]
                {
                    "看石刻：九日山十方祈风石刻，是宋代市舶制度留在石头上的记录",
                    "看石桥：洛阳桥的筏形基础与种蛎固基，是中国古代桥梁史上的关键一笔",
                    "看航标：六胜塔、万寿塔一南一北，船进泉州湾先认这两座塔",
                    "看窑与铁：磁灶窑、德化窑、安溪青阳冶铁，是港口背后的生产腹地"
                },
                images = new[]
                {
                    "Heritage/Images/1561_hero",
                    "Heritage/Images/1561_01",
                    "Heritage/Images/1561_02",
                    "Heritage/Images/1561_03",
                    "Heritage/Images/1561_04",
                    "Heritage/Images/1561_05",
                    "Heritage/Images/1561_06"
                },
                audio = "Heritage/Audio/1561",
                model = "1561"
            }
        };

        public static HeritageSite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
                if (All[i].id == id) return All[i];
            // 场景里的点位（良渚 ①②③、秦陵 ①②③、泉州 ①②）走 HeritageSpots，面板是同一套
            return HeritageSpots.Get(id);
        }

        /// <summary>按索引取图，越界或缺失时回落到主图。</summary>
        public static Texture2D Image(HeritageSite site, int index)
        {
            if (site == null || site.images.Length == 0) return null;
            int i = Mathf.Clamp(index, 0, site.images.Length - 1);
            var tex = Resources.Load<Texture2D>(site.images[i]);
            if (tex == null && i != 0) tex = Resources.Load<Texture2D>(site.images[0]);
            return tex;
        }

        public static AudioClip Voice(HeritageSite site)
        {
            return site == null ? null : Resources.Load<AudioClip>(site.audio);
        }
    }
}
