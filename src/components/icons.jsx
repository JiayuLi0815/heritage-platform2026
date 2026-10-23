const S = ({ d, children, w = 16, fill = 'none' }) => (
  <svg
    viewBox="0 0 24 24"
    width={w}
    height={w}
    fill={fill}
    stroke="currentColor"
    strokeWidth="1.9"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    {d ? <path d={d} /> : children}
  </svg>
)

export const IcCompass = (p) => (
  <S {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M15.6 8.4l-2 5.2-5.2 2 2-5.2z" />
  </S>
)
export const IcSearch = (p) => (
  <S {...p}>
    <circle cx="11" cy="11" r="7" />
    <path d="M20 20l-3.5-3.5" />
  </S>
)
export const IcArrowRight = (p) => <S d="M5 12h13M13 6l6 6-6 6" {...p} />
export const IcArrowLeft = (p) => <S d="M19 12H6M11 6l-6 6 6 6" {...p} />
export const IcChevronL = (p) => <S d="M15 5l-7 7 7 7" {...p} />
export const IcChevronR = (p) => <S d="M9 5l7 7-7 7" {...p} />
export const IcBookmark = ({ on, ...p }) => (
  <S d="M6 3.5h12v17l-6-4.2-6 4.2z" fill={on ? 'currentColor' : 'none'} {...p} />
)
export const IcClose = (p) => <S d="M6 6l12 12M18 6L6 18" {...p} />
export const IcSend = (p) => <S d="M4.5 12h15M13 5.5l6.5 6.5-6.5 6.5" {...p} />
export const IcImage = (p) => (
  <S {...p}>
    <rect x="3.5" y="4.5" width="17" height="15" rx="2.5" />
    <circle cx="9" cy="10" r="1.6" />
    <path d="M4.5 17l4.6-4.4 4.2 3.9 2.6-2.4 3.6 3.4" />
  </S>
)
export const IcCube = (p) => (
  <S {...p}>
    <path d="M12 2.8l8 4.6v9.2l-8 4.6-8-4.6V7.4z" />
    <path d="M4 7.4l8 4.6 8-4.6M12 12v9.2" />
  </S>
)
export const IcGlobe = (p) => (
  <S {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M3 12h18M12 3c2.5 2.6 3.8 5.6 3.8 9s-1.3 6.4-3.8 9c-2.5-2.6-3.8-5.6-3.8-9s1.3-6.4 3.8-9z" />
  </S>
)
export const IcChat = (p) => (
  <S {...p}>
    <path d="M20.5 12c0 4.1-3.8 7.4-8.5 7.4-1 0-2-.15-2.9-.42L4.2 20.6l1.3-3.9A7 7 0 013.5 12c0-4.1 3.8-7.4 8.5-7.4s8.5 3.3 8.5 7.4z" />
  </S>
)
export const IcSparkle = (p) => (
  <S {...p}>
    <path d="M12 3l1.9 5.3L19 10l-5.1 1.7L12 17l-1.9-5.3L5 10l5.1-1.7z" />
    <path d="M18.5 16.5l.7 2 2 .7-2 .7-.7 2-.7-2-2-.7 2-.7z" />
  </S>
)
export const IcLayers = (p) => (
  <S {...p}>
    <path d="M12 3l9 5-9 5-9-5z" />
    <path d="M3 13l9 5 9-5" />
  </S>
)
export const IcMap = (p) => (
  <S {...p}>
    <path d="M9 4L3 6.5v13L9 17l6 2.5 6-2.5v-13L15 6.5z" />
    <path d="M9 4v13M15 6.5v13" />
  </S>
)
export const IcPin = (p) => (
  <S {...p}>
    <path d="M12 21s7-5.6 7-11a7 7 0 10-14 0c0 5.4 7 11 7 11z" />
    <circle cx="12" cy="10" r="2.6" />
  </S>
)
export const IcRotate = (p) => (
  <S {...p}>
    <path d="M20 12a8 8 0 11-2.6-5.9" />
    <path d="M20 3.5V9h-5.5" />
  </S>
)
export const IcInfo = (p) => (
  <S {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M12 11v5.5M12 7.8v.2" />
  </S>
)
export const IcDatabase = (p) => (
  <S {...p}>
    <ellipse cx="12" cy="6" rx="8" ry="3.2" />
    <path d="M4 6v12c0 1.8 3.6 3.2 8 3.2s8-1.4 8-3.2V6" />
    <path d="M4 12c0 1.8 3.6 3.2 8 3.2s8-1.4 8-3.2" />
  </S>
)
export const IcScan = (p) => (
  <S {...p}>
    <path d="M4 8V5.5A1.5 1.5 0 015.5 4H8M16 4h2.5A1.5 1.5 0 0120 5.5V8M20 16v2.5a1.5 1.5 0 01-1.5 1.5H16M8 20H5.5A1.5 1.5 0 014 18.5V16" />
    <path d="M4 12h16" />
  </S>
)
export const IcShield = (p) => (
  <S {...p}>
    <path d="M12 3l7 2.6v5.6c0 4.5-2.9 8.4-7 9.8-4.1-1.4-7-5.3-7-9.8V5.6z" />
    <path d="M9.2 12l2 2 3.6-3.8" />
  </S>
)
export const IcExternal = (p) => (
  <S {...p}>
    <path d="M14 4h6v6M20 4l-8.5 8.5" />
    <path d="M19 14.5v4A1.5 1.5 0 0117.5 20h-12A1.5 1.5 0 014 18.5v-12A1.5 1.5 0 015.5 5h4" />
  </S>
)
export const IcMenu = (p) => <S d="M4 7h16M4 12h16M4 17h16" {...p} />
