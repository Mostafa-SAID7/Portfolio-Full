/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsRangeDto. */
export interface AnalyticsRange {
  startDate: string;
  endDate: string;
  days: number;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsOverviewDto. */
export interface AnalyticsOverview {
  range: AnalyticsRange;
  activeUsers: number;
  newUsers: number;
  totalUsers: number;
  sessions: number;
  pageViews: number;
  engagedSessions: number;
  engagementRate: number;
  averageEngagementSeconds: number;
  contactConversions: number;
  previous: AnalyticsOverview;
  generatedAt: string;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsTrendPointDto. */
export interface AnalyticsTrendPoint {
  date: string;
  users: number;
  sessions: number;
  pageViews: number;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsGeoRowDto. */
export interface AnalyticsGeoRow {
  country: string;
  users: number;
  share: number;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsSourceRowDto. */
export interface AnalyticsSourceRow {
  source: string;
  medium: string;
  sessions: number;
  users: number;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsPageRowDto. */
export interface AnalyticsPageRow {
  path: string;
  title: string;
  pageViews: number;
  users: number;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsDeviceRowDto. */
export interface AnalyticsDeviceRow {
  category: string;
  users: number;
  share: number;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsEventRowDto. */
export interface AnalyticsEventRow {
  name: string;
  count: number;
  users: number;
}

/** Mirrors PortfolioAPI.Core.DTOs.AnalyticsReportDto. */
export interface AnalyticsReport {
  overview: AnalyticsOverview;
  trend: AnalyticsTrendPoint[];
  countries: AnalyticsGeoRow[];
  sources: AnalyticsSourceRow[];
  pages: AnalyticsPageRow[];
  devices: AnalyticsDeviceRow[];
  events: AnalyticsEventRow[];
  generatedAt: string;
  notConfigured: boolean;
}
