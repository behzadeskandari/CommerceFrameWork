export interface ContentPageLocalization {
  id?: number;
  languageId: number;
  title: string;
  slug: string;
  body: string;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  canonicalUrl?: string | null;
}

export interface ContentPageSummary {
  id: number;
  storeId: number;
  systemName?: string | null;
  isPublished: boolean;
  publishedFromUtc?: string | null;
  publishedToUtc?: string | null;
  defaultTitle?: string | null;
  defaultSlug?: string | null;
  updatedAtUtc: string;
}

export interface ContentPageDetail {
  id: number;
  storeId: number;
  systemName?: string | null;
  isPublished: boolean;
  publishedFromUtc?: string | null;
  publishedToUtc?: string | null;
  localizations: ContentPageLocalization[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateContentPageRequest {
  storeId: number;
  systemName?: string | null;
  isPublished: boolean;
  publishedFromUtc?: string | null;
  publishedToUtc?: string | null;
  localizations: Omit<ContentPageLocalization, 'id'>[];
}

export type UpdateContentPageRequest = Omit<CreateContentPageRequest, 'storeId'>;

export interface TopicLocalization {
  id?: number;
  languageId: number;
  title: string;
  body: string;
  metaTitle?: string | null;
  metaDescription?: string | null;
}

export interface TopicSummary {
  id: number;
  storeId: number;
  systemName: string;
  isPublished: boolean;
  defaultTitle?: string | null;
  updatedAtUtc: string;
}

export interface TopicDetail {
  id: number;
  storeId: number;
  systemName: string;
  isPublished: boolean;
  publishedFromUtc?: string | null;
  publishedToUtc?: string | null;
  localizations: TopicLocalization[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateTopicRequest {
  storeId: number;
  systemName: string;
  isPublished: boolean;
  publishedFromUtc?: string | null;
  publishedToUtc?: string | null;
  localizations: Omit<TopicLocalization, 'id'>[];
}

export type UpdateTopicRequest = Omit<CreateTopicRequest, 'storeId'>;

export interface WidgetZone {
  id: number;
  systemName: string;
  name: string;
  description?: string | null;
  displayOrder: number;
}

export type WidgetType = 'HtmlBlock' | 'TopicEmbed' | 'MenuEmbed';

export interface WidgetInstance {
  id: number;
  storeId: number;
  widgetZoneId: number;
  zoneSystemName: string;
  widgetType: WidgetType;
  configurationJson: string;
  languageId?: number | null;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateWidgetInstanceRequest {
  storeId: number;
  widgetZoneId: number;
  widgetType: WidgetType;
  configurationJson: string;
  languageId?: number | null;
  displayOrder: number;
  isActive?: boolean;
}

export type UpdateWidgetInstanceRequest = Omit<CreateWidgetInstanceRequest, 'storeId' | 'widgetZoneId'>;

export interface MenuItemLocalization {
  id?: number;
  languageId: number;
  title: string;
}

export type MenuItemLinkType = 'Url' | 'Page' | 'Topic' | 'Category' | 'Product';

export interface MenuItem {
  id?: number;
  parentMenuItemId?: number | null;
  linkType: MenuItemLinkType;
  url?: string | null;
  contentPageId?: number | null;
  topicId?: number | null;
  externalSlug?: string | null;
  displayOrder: number;
  openInNewTab: boolean;
  localizations: MenuItemLocalization[];
}

export interface MenuSummary {
  id: number;
  storeId: number;
  systemName: string;
  name: string;
  isPublished: boolean;
}

export interface MenuDetail extends MenuSummary {
  items: MenuItem[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateMenuRequest {
  storeId: number;
  systemName: string;
  name: string;
  isPublished: boolean;
  items: MenuItem[];
}

export type UpdateMenuRequest = Omit<CreateMenuRequest, 'storeId'>;

export interface StorefrontPage {
  id: number;
  title: string;
  slug: string;
  body: string;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  canonicalUrl?: string | null;
  languageId: number;
}

export interface StorefrontMenuItem {
  id: number;
  title: string;
  url: string;
  openInNewTab: boolean;
  children: StorefrontMenuItem[];
}

export interface StorefrontMenu {
  systemName: string;
  name: string;
  items: StorefrontMenuItem[];
}

export interface StorefrontWidget {
  id: number;
  zoneSystemName: string;
  widgetType: string;
  renderedHtml: string;
}
