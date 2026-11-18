import { asset } from '../asset.js'
import './VrPage.css'

/* ============================================================
   《华夏文明探源·河山册》· 沉浸体验
   ------------------------------------------------------------
   只做展示：一段作品介绍 + 一体机内实拍/实时渲染的预览图。
   不放安装包、二维码、规格表、安装引导，也不放视频。
   ============================================================ */

const STILLS = [
  { img: 'still-archive', t: '藏卷阁', s: '序章' },
  { img: 'still-hub', t: '导览', s: '序章' },
  { img: 'still-board', t: '河山册展板', s: '序章' },
  { img: 'still-settlement', t: '良渚 · 聚落', s: '第一章' },
  { img: 'still-liangzhu', t: '良渚 · 水利系统', s: '第一章' },
  { img: 'still-qinling', t: '秦陵 · 陪葬坑', s: '第二章' },
  { img: 'still-port', t: '泉州 · 港口', s: '第三章' },
  { img: 'still-quanzhou', t: '泉州 · 海丝遗迹', s: '第三章' },
  { img: 'still-epilogue', t: '河山册合璧', s: '终章' },
]

export default function VrPage() {
  return (
    <div className="wrap vrp">
      <section className="vrp-hero">
        <div className="vrp-hero-copy">
          <span className="kicker">沉浸体验 · 已完成作品</span>
          <h1>华夏文明探源·河山册</h1>
          <p className="vrp-lead">
            《河山册》是一本可以在头显里翻开的三页之书：良渚的水、秦陵的土、泉州的海，各自压成一册。
            体验者以第一人称走进藏卷阁，逐页寻回散落的文字；合上书的时候，三处世界遗产也连成了一条线。
          </p>
        </div>
        <figure className="vrp-cover">
          <img src={asset('img/vr/cover.webp')} alt="《华夏文明探源·河山册》序章场景" />
          <figcaption>画面来自一体机内的实时渲染</figcaption>
        </figure>
      </section>

      <section className="vrp-sec" id="vrp-stills">
        <div className="vrp-sec-head">
          <span className="kicker">预览图</span>
          <h2>三处遗址，九个瞬间</h2>
        </div>
        <ul className="vrp-stills">
          {STILLS.map((p) => (
            <li key={p.img}>
              <figure>
                <img src={asset(`img/vr/${p.img}.webp`)} alt={`${p.t} 画面`} loading="lazy" />
                <figcaption>
                  <span>{p.s}</span>
                  {p.t}
                </figcaption>
              </figure>
            </li>
          ))}
        </ul>
      </section>

      <section className="vrp-end">
        <p>
          作品以原生应用的形式跑在一体机本地，戴上头显、扣上耳机就能开始，一轮 12 分半。
          网站这边只留一份看得见的记录：这些画面，和这一段介绍。
        </p>
      </section>
    </div>
  )
}
