import { useEffect, useRef, useState } from 'react'
import { asset } from '../asset.js'

/* ============================================================
   本地三维舞台
   ------------------------------------------------------------
   素材盘里的 glb 已经压成 meshopt + webp，一处遗产常有十来个构件
   （胡夫金字塔、狮身人面像、卡尔之墓……）。所以这一层要解决两件事：
   一是把模型当成「一组」而不是一个来切换，二是别把 1MB 的
   model-viewer 塞进首屏包——只有真的点进三维页才去加载它。
   ============================================================ */

let viewerReady = false

/* id 是素材目录名（UNESCO 编号）。精选页的 site.id 是 slug，得由调用方换算后传进来 */
export default function ModelStage({ site, models, id = site.id, credit = 'Integra Tecnologia' }) {
  const [i, setI] = useState(0)
  const [ready, setReady] = useState(viewerReady)
  const chips = useRef(null)

  useEffect(() => setI(0), [id])

  /* 十来个构件一排排开，选中的那枚经常在可视区外。
     按容器宽度把它滚到中间，而不是用 scrollIntoView——那个会连页面一起滚。 */
  useEffect(() => {
    const box = chips.current
    const btn = box?.querySelector('button.on')
    if (box && btn) box.scrollLeft = btn.offsetLeft - (box.clientWidth - btn.offsetWidth) / 2
  }, [i, id])

  useEffect(() => {
    if (viewerReady) return
    import('@google/model-viewer').then(({ ModelViewerElement }) => {
      /* 模型是 meshopt 压的，model-viewer 默认不带解码器，得指给它。
         解码器放在 public/vendor/ 自己托管——不挂 Google 的 CDN，断网也能开。 */
      ModelViewerElement.meshoptDecoderLocation = asset('vendor/meshopt_decoder.js')
      viewerReady = true
      setReady(true)
    })
  }, [])

  const m = models[Math.min(i, models.length - 1)]

  return (
    <div className="mv">
      {ready ? (
        <model-viewer
          key={`${id}-${m.file}`}
          src={asset(`models/${id}/${m.file}`)}
          alt={`${site.name} · ${m.label}`}
          camera-controls
          auto-rotate
          auto-rotate-delay="2200"
          rotation-per-second="14deg"
          camera-orbit="30deg 70deg auto"
          min-field-of-view="18deg"
          max-field-of-view="55deg"
          interaction-prompt="none"
          environment-image="neutral"
          shadow-intensity="0.6"
          exposure="1.05"
          touch-action="pan-y"
          loading="eager"
        />
      ) : (
        <div className="mv-wait">正在准备三维视图…</div>
      )}

      <div className="mv-foot">
        {models.length > 1 ? (
          <div className="mv-chips" role="tablist" aria-label="构件" ref={chips}>
            {models.map((x, j) => (
              <button
                key={x.file}
                className={j === i ? 'on' : ''}
                onClick={() => setI(j)}
                role="tab"
                aria-selected={j === i}
              >
                {x.label}
              </button>
            ))}
          </div>
        ) : (
          <span className="mv-one">{m.label}</span>
        )}
        <span className="mv-credit">
          {models.length > 1 ? `${i + 1} / ${models.length} · ` : ''}
          模型：{m.credit ?? credit}
        </span>
      </div>
    </div>
  )
}
