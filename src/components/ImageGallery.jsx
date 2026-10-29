import { useEffect, useRef, useState } from 'react'
import { asset } from '../asset.js'
import { galleryFilesOf } from '../data/gallery.js'
import { IcChevronL, IcChevronR } from './icons.jsx'

/* ============================================================
   影像画廊
   ------------------------------------------------------------
   素材盘里每处遗产有 1–8 张原图（多为 5K 以上），压成 1400px 的 webp 放在
   public/img/gallery/<id>/。这里给的是「翻看」而不是「轮播」：有缩略图、
   有计数、能按方向键、能在触屏上左右划——像看一本档案，不像看广告。
   ============================================================ */

const pad = (n) => String(n).padStart(2, '0')
const srcOf = (id, i) => asset(`img/gallery/${id}/${pad(i)}.webp`)

export default function ImageGallery({ site, id = site.id }) {
  const imageFiles = galleryFilesOf(id)
  const n = Math.max(1, imageFiles.length)
  const srcAt = (index) => srcOf(id, imageFiles[index])
  const [i, setI] = useState(0)
  const [on, setOn] = useState(false)
  const thumbs = useRef(null)
  const touch = useRef(null)

  /* 从一处遗产跳到另一处（同国其他遗产是同一页里换数据，不重新挂载），
     张数可能从 8 掉到 3。k 先夹住越界，等下面的 reset 生效再回到第一张 */
  const k = Math.min(i, n - 1)
  const go = (d) => setI((v) => (Math.min(v, n - 1) + d + n) % n)

  useEffect(() => setI(0), [id])

  /* 主图换了一张，先撤掉淡入，等它加载完再浮上来，中间不会闪白 */
  useEffect(() => setOn(false), [k, id])

  useEffect(() => {
    const img = new Image()
    img.onload = () => setOn(true)
    img.onerror = () => setOn(true)
    img.src = srcAt(k)
    /* 顺手把下一张塞进缓存——翻页时就不用等 */
    const next = new Image()
    next.src = srcAt((k + 1) % n)
  }, [k, id, n])

  useEffect(() => {
    const onKey = (e) => {
      /* 数字人面板开着时，方向键属于输入框，不属于画廊 */
      const t = e.target
      if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.isContentEditable)) return
      if (e.key === 'ArrowRight') go(1)
      else if (e.key === 'ArrowLeft') go(-1)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }) // 每次渲染重绑，闭包里才是最新的 n

  /* 缩略图一排，选中的那枚可能滚出可视区，按容器宽度居中 */
  useEffect(() => {
    const box = thumbs.current
    const btn = box?.querySelector('button.on')
    if (box && btn) box.scrollLeft = btn.offsetLeft - (box.clientWidth - btn.offsetWidth) / 2
  }, [k, id])

  const swipe = {
    onPointerDown: (e) => { touch.current = e.clientX },
    onPointerUp: (e) => {
      if (touch.current == null) return
      const dx = e.clientX - touch.current
      touch.current = null
      if (Math.abs(dx) > 45) go(dx < 0 ? 1 : -1)
    },
  }

  return (
    <div className="gal">
      <div className="gal-view" {...swipe}>
        <img
          key={`${id}-${k}`}
          className={`gal-img${on ? ' on' : ''}`}
          src={srcAt(k)}
          alt={`${site.name} 影像 ${k + 1}`}
          draggable="false"
        />
      </div>

      {n > 1 ? (
        <>
          <button className="gal-arrow l" onClick={() => go(-1)} aria-label="上一张">
            <IcChevronL w={20} />
          </button>
          <button className="gal-arrow r" onClick={() => go(1)} aria-label="下一张">
            <IcChevronR w={20} />
          </button>
        </>
      ) : null}

      <div className="gal-foot">
        {n > 1 ? (
          <div className="gal-thumbs" ref={thumbs} role="tablist" aria-label="影像">
            {Array.from({ length: n }, (_, j) => (
              <button
                key={j}
                className={j === k ? 'on' : ''}
                onClick={() => setI(j)}
                role="tab"
                aria-selected={j === k}
                aria-label={`第 ${j + 1} 张`}
              >
                <img src={asset(`img/gallery/${id}/t${imageFiles[j]}.webp`)} alt="" loading="lazy" />
              </button>
            ))}
          </div>
        ) : (
          <span className="mv-one">{site.name}</span>
        )}
        <span className="mv-credit">{n > 1 ? `${k + 1} / ${n}` : ''}</span>
      </div>
    </div>
  )
}
