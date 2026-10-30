// Architectural silhouettes share the map's geographic projection.
const LANDMARKS = [
  { name: '自由女神像', lng: -74.04, lat: 40.69, icon: 'liberty', id: '307' },
  { name: '奇琴伊察', lng: -88.57, lat: 20.68, icon: 'pyramid', id: '483' },
  { name: '里约热内卢', lng: -43.21, lat: -22.95, icon: 'christ', id: '1100' },
  { name: '巴黎塞纳河畔', lng: 2.29, lat: 48.86, icon: 'tower', id: '600' },
  { name: '罗马历史中心', lng: 12.49, lat: 41.89, icon: 'colosseum', id: '91' },
  { name: '吉萨金字塔', lng: 31.13, lat: 29.98, icon: 'pyramid', id: '86' },
  { name: '泰姬陵', lng: 78.04, lat: 27.17, icon: 'taj', id: '252' },
  { name: '北京故宫', lng: 116.4, lat: 39.92, icon: 'pagoda', id: '439' },
  { name: '悉尼歌剧院', lng: 151.21, lat: -33.86, icon: 'opera', id: '166' },
]

function Building({ type }) {
  switch (type) {
    case 'pyramid': return <><path d="M3 38 24 7l24 31Z" fill="url(#landmarkGold)"/><path d="m24 7 6 31M3 38h45M11 32h31M15 26h22M19 20h13"/></>
    case 'tower': return <><path d="m24 2-3 17-5 14-8 10h11l5-12 5 12h11l-8-10-5-14Z" fill="url(#landmarkGold)"/><path d="M16 33h16M19 24h10M20 19h8M22 11h4M15 37h18M24 0v5"/></>
    case 'colosseum': return <><path d="M3 17Q24 6 47 17v24Q24 33 3 41Z" fill="url(#landmarkGold)"/><path d="M3 17q21 10 44 0M3 25q21 10 44 0M3 33q21 10 44 0"/>{[8,16,24,32,40].map(x=><path key={x} d={`M${x} 24v-4q2-3 4 0v4m-4 9v-4q2-3 4 0v4m-4 8v-4q2-3 4 0v4`}/>)}</>
    case 'taj': return <><path d="M12 40V25h26v15M17 25c-7-10 8-11 8-20 0 9 15 10 8 20M25 3V0M2 40h46M5 40V17m-2 0h4M45 40V17m-2 0h4M21 40V30q4-6 8 0v10" fill="url(#landmarkGold)"/><path d="M12 25h26M16 32v5M34 32v5"/></>
    case 'pagoda': return <><path d="M7 40h36M13 40V29h24v11M15 27v-9h20v9M20 15V7h10v8M25 7V0M3 29q13-2 22-10 9 8 22 10ZM9 18q10-2 16-9 6 7 16 9ZM15 7q7-2 10-6 3 4 10 6Z" fill="url(#landmarkGold)"/><path d="M22 40v-9h6v9M21 25v-5h8v5"/></>
    case 'opera': return <><path d="M2 40h47v4H2ZM7 37 3 18q13 2 20 19ZM20 37 10 6q22 7 24 31ZM32 37 27 11q18 8 18 26Z" fill="url(#landmarkGold)"/><path d="m10 6 17 31M27 11l12 26M3 18l14 19"/></>
    case 'liberty': return <><path d="M17 43h17v-6H17ZM19 37l3-24h7l4 24ZM23 13 16 4l-3 1 7 16M29 15l7 13-5 2M12 5V0h5v5M21 8l-2-4 4 1 2-4 2 4 4-1-2 4" fill="url(#landmarkGold)"/><circle cx="25" cy="9" r="3"/><path d="m25 16-2 19m5-17 2 17"/></>
    default: return <><circle cx="25" cy="7" r="4" fill="url(#landmarkGold)"/><path d="M5 15h40v4L29 18l3 24H18l3-24-16 1Z" fill="url(#landmarkGold)"/><path d="M25 18v24M14 44h22"/></>
  }
}

export default function MapLandmarks({ projectX, projectY, onOpen, sites, category, zoom }) {
  if (zoom > 2.5) return null
  return <g className="map-landmarks" opacity={Math.min(1, (2.5 - zoom) / 0.6)}>
    <defs><linearGradient id="landmarkGold" x1="0" y1="0" x2="1" y2="1"><stop stopColor="#fff4d6"/><stop offset="0.5" stopColor="#ba9965"/><stop offset="1" stopColor="#5b4934"/></linearGradient></defs>
    {LANDMARKS.map(l => {
      const site = sites.find(s => s.id === l.id)
      if (!site || (category !== '全部' && site.category !== category)) return null
      return <g key={l.id} transform={`translate(${projectX(l.lng) - 15} ${projectY(l.lat) - 27}) scale(.6)`} className="map-landmark" role="button" tabIndex="0" aria-label={`查看${l.name}`} onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onOpen(site) } }} onPointerDown={e => e.stopPropagation()} onClick={() => onOpen(site)}><title>{l.name}</title><rect width="50" height="48" fill="transparent" stroke="none"/><Building type={l.icon}/></g>
    })}
  </g>
}
