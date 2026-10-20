/** A project category managed from the dashboard (api/v1/Categories). */
export interface Category {
  id: number;
  /** Display name (English) - the canonical name. */
  nameEn: string;
  /** Display name (Arabic). */
  nameAr: string;
  order: number;
  isActive: boolean;
  /** How many projects use this category (admin list only). */
  projectCount: number;
}

/** Payload for create/update (server-managed fields are never sent). */
export interface CategoryPayload {
  nameEn: string;
  nameAr: string;
  order: number;
  isActive: boolean;
}
