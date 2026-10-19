/**
 * The Angular dev server proxies `/api` to the Portfolio API
 * (http://localhost:5039) via proxy.conf.json, so in development requests
 * are same-origin and CORS-free.
 *
 * In production, deploy the API behind the same domain (reverse proxy /
 * platform rewrite) and keep this relative; if the API lives on another
 * domain, replace it with the absolute URL at build time.
 */
export const API_URL = '/api';
