export interface UrlRecordDto {
  id: number;
  entityName: string;
  entityId: number;
  slug: string;
  languageId: number | null;
  storeId: number | null;
  isActive: boolean;
}

export interface SeoMetadataDto {
  id: number;
  entityName: string;
  entityId: number;
  languageId: number | null;
  storeId: number | null;
  metaTitle: string | null;
  metaDescription: string | null;
  metaKeywords: string | null;
  canonicalUrl: string | null;
  structuredDataJson: string | null;
}

export interface SeoSettingsDto {
  storeId: number;
  defaultMetaTitle: string | null;
  defaultMetaDescription: string | null;
  robotsTxt: string | null;
  sitemapEnabled: boolean;
}

export interface UpsertUrlRecordRequest {
  entityName: string;
  entityId: number;
  slug: string;
  languageId?: number | null;
  storeId?: number | null;
  isActive: boolean;
}

export interface UpsertSeoMetadataRequest {
  entityName: string;
  entityId: number;
  languageId?: number | null;
  storeId?: number | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  canonicalUrl?: string | null;
  structuredDataJson?: string | null;
}

export interface UpdateSeoSettingsRequest {
  defaultMetaTitle?: string | null;
  defaultMetaDescription?: string | null;
  robotsTxt?: string | null;
  sitemapEnabled: boolean;
}
