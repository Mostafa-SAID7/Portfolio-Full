/**
 * UI label maps for Arabic (categories, types, statuses, skill group titles).
 * Per-project Arabic copy (`projectAr`) is retired — API bilingual fields own content (#39).
 */

/** @deprecated Empty; use API title/description. Kept for localize.tsx compat. */
export const projectAr: Record<string, { title: string; description: string }> = {};

export const categoryAr: Record<string, string> = {
  All: "الكل",
  Clothing: "ملابس",
  Beauty: "تجميل",
  Jewelry: "مجوهرات",
  Furniture: "أثاث",
  Dashboard: "لوحات تحكم",
  Web3: "ويب 3",
  Sports: "رياضة",
  "Tech Accessories": "إكسسوارات تقنية",
  Backend: "خدمات خلفية",
  Other: "أخرى",
};

export const projectTypeAr: Record<string, string> = {
  Team: "عمل جماعي",
  Freelance: "عمل حر",
  Self: "مشروع شخصي",
};

export const projectStatusAr: Record<string, string> = {
  "In Progress": "قيد التنفيذ",
  Completed: "مكتمل",
  Deployed: "منشور",
  Done: "منجز",
};

export const skillGroupAr: Record<string, string> = {
  Frontend: "الواجهات الأمامية",
  Backend: "الخدمات الخلفية",
  Databases: "قواعد البيانات",
  "Architecture & Patterns": "المعمارية وأنماط التصميم",
  "Real-Time & Communication": "الاتصال اللحظي",
  "DevOps & CI/CD": "DevOps والنشر المستمر",
  "Security & Testing": "الأمان والاختبارات",
  "Tools & Platforms": "الأدوات والمنصات",
};
