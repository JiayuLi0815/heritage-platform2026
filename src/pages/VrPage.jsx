import { useEffect, useState } from 'react'
import { IcArrowRight } from '../components/icons.jsx'

/* ============================================================
   《遗产现场》· WebXR 入口
   ------------------------------------------------------------
   只留入口卡片。除卡片本身的必要说明外，页面不放其它文字——
   展位上这一页就是一次「戴上头显」的动作。
   原有的机制 / 流程 / 方案比较文案存档在 .archive/VrPage-2026-10-23.jsx。
   ============================================================ */
export default function VrPage() {
  const [xr, setXr] = useState('checking')
  const [note, setNote] = useState('')

  useEffect(() => {
    let alive = true
    if (!('xr' in navigator)) {
      setXr('none')
      return
    }
    navigator.xr
      .isSessionSupported('immersive-vr')
      .then((ok) => alive && setXr(ok ? 'ok' : 'none'))
      .catch(() => alive && setXr('none'))
    return () => {
      alive = false
    }
  }, [])

  const launch = () => {
    if (xr === 'ok') {
      setNote('设备已就绪。原型场景还在做，接上之后这里会直接唤起 immersive-vr 会话——待上线。')
    } else if (xr === 'none') {
      setNote('这台设备没有可用的 immersive-vr 会话。用一体机浏览器打开本页，或者等桌面降级版上线。')
    } else {
      setNote('正在检测头显…')
    }
  }

  return (
    <div className="wrap vr-page">
      <div className="vr-launch">
        <div>
          <span className="kicker">入口</span>
          <h2>戴上头显，从这里进</h2>
          <p>
            原型场景开发中。上线后本按钮会直接唤起 WebXR 会话，无需安装应用——
            先在一体机浏览器里打开这个网址即可。
          </p>
          {note ? <p className="vr-note">{note}</p> : null}
        </div>
        <div className="vr-launch-r">
          <button className="btn btn-fill" onClick={launch}>
            {xr === 'ok' ? '进入 VR 场景' : xr === 'checking' ? '检测设备中…' : '进入 VR 场景'}
            <IcArrowRight w={16} />
          </button>
          <span className="vr-state">
            <i className={`vr-dot ${xr}`} />
            {xr === 'ok'
              ? '已检测到 immersive-vr 设备'
              : xr === 'checking'
              ? '正在检测 WebXR'
              : '未检测到头显，将使用桌面降级版'}
          </span>
        </div>
      </div>
    </div>
  )
}
