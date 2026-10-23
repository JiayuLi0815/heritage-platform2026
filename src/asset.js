/* 站点可能挂在根路径，也可能被学院域名反向代理到 /heritage/ 这类子路径下。
   图片一律走相对路径，同一份 dist 换到哪种部署都不用重打包。 */
export const asset = (p) => `${import.meta.env.BASE_URL}${p}`
