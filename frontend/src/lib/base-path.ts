// When served through shophub-app's "/shop-proxy/{id}" reverse proxy, this app doesn't own its
// origin root — the prefix is a different shop id per request and isn't known at build time, so
// it has to be read back out of the URL the page actually loaded at, once, at startup. Reached
// directly at a shop pod's own root instead, there's no such prefix and this is just ''.
const shopProxyMatch = /^\/shop-proxy\/[0-9a-fA-F-]{36}/.exec(window.location.pathname)

export const basePath = shopProxyMatch?.[0] ?? ''
