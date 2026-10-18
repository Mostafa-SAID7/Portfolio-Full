import { createFileRoute } from "@tanstack/react-router";
import type {} from "@tanstack/react-start";
import { projects } from "@/data";
import { SITE } from "@/lib/site";

/** Tiny non-cryptographic hash (FNV-1a) used only for ETag generation. */
function hash(input: string): number {
  let h = 0x811c9dc5;
  for (let i = 0; i < input.length; i++) {
    h ^= input.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  return h >>> 0;
}

interface SitemapEntry {
  path: string;
  changefreq?: "always" | "hourly" | "daily" | "weekly" | "monthly" | "yearly" | "never";
  priority?: string;
}

export const Route = createFileRoute("/sitemap.xml")({
  server: {
    handlers: {
      GET: async ({ request }) => {
        const baseUrl = SITE.url || new URL(request.url).origin;
        const entries: SitemapEntry[] = [
          { path: "/", changefreq: "weekly", priority: "1.0" },
          { path: "/about", changefreq: "monthly", priority: "0.8" },
          { path: "/skills", changefreq: "monthly", priority: "0.8" },
          { path: "/experience", changefreq: "monthly", priority: "0.8" },
          { path: "/projects", changefreq: "weekly", priority: "0.9" },
          { path: "/contact", changefreq: "yearly", priority: "0.7" },
          ...projects.map((p) => ({
            path: `/projects/${p.id}`,
            changefreq: "monthly" as const,
            priority: "0.6",
          })),
        ];

        const urls = entries.map((e) =>
          [
            `  <url>`,
            `    <loc>${baseUrl}${e.path}</loc>`,
            e.changefreq ? `    <changefreq>${e.changefreq}</changefreq>` : null,
            e.priority ? `    <priority>${e.priority}</priority>` : null,
            `  </url>`,
          ]
            .filter(Boolean)
            .join("\n"),
        );

        const xml = [
          `<?xml version="1.0" encoding="UTF-8"?>`,
          `<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">`,
          ...urls,
          `</urlset>`,
        ].join("\n");

        // Weak ETag over the generated body so crawlers revalidating an hour
        // later get a 304 instead of re-downloading the whole document.
        const etag = `W/"${xml.length.toString(16)}-${hash(xml).toString(16)}"`;
        const headers = {
          "Content-Type": "application/xml",
          "Cache-Control": "public, max-age=3600, s-maxage=86400, stale-while-revalidate=604800",
          ETag: etag,
        };

        if (request.headers.get("if-none-match") === etag) {
          return new Response(null, { status: 304, headers });
        }

        return new Response(xml, { headers });
      },
    },
  },
});
