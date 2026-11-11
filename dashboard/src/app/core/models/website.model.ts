export interface Website {
  id: number;
  name: string;
  slug: string;
  description?: string | null;
  baseUrl?: string | null;
  order: number;
  isActive: boolean;
  assignedProjectsCount: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface WebsitePayload {
  name: string;
  slug?: string;
  description?: string | null;
  baseUrl?: string | null;
  order: number;
  isActive: boolean;
}

export interface ProjectWebsiteAssignment {
  websiteId: number;
  websiteSlug: string;
  websiteName: string;
  isPublished: boolean;
  displayOrder: number;
}

export interface ProjectWebsiteAssignmentPayload {
  websiteId: number;
  isPublished: boolean;
  displayOrder: number;
}

