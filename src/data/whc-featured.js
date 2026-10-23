/* 精选的 20 处与 UNESCO 编号的对应关系。
   两套数据的 id 不是一套（精选用 slug，名录用 UNESCO 编号），
   这张表是它们唯一的桥：地图上点到这 20 处，仍然进原来那份带三维模型的详情页，
   点到其余遗产，进名录详情页。 */
export const FEATURED_BY_WHC = {
  274: 'machu-picchu',
  976: 'gyeongju',
  1293: 'hegra',
  174: 'florence',
  483: 'chichen-itza',
  91: 'rome',
  414: 'teotihuacan',
  27: 'mesa-verde',
  829: 'pompeii',
  373: 'stonehenge',
  870: 'nara',
  292: 'cologne',
  314: 'alhambra',
  440: 'mogao',
  438: 'great-wall',
  439: 'forbidden-city',
  86: 'giza',
  540: 'saint-petersburg',
  148: 'jerusalem',
  166: 'sydney-opera',
}

/* 反查：精选用 slug，本地模型清单用 UNESCO 编号，详情页要靠这张表把两边接上 */
export const WHC_BY_FEATURED = Object.fromEntries(
  Object.entries(FEATURED_BY_WHC).map(([id, slug]) => [slug, Number(id)])
)
