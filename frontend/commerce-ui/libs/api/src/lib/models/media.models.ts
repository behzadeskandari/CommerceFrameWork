export interface MediaSummary {
  id: number;
  storeId: number;
  fileName: string;
  originalFileName: string;
  contentType: string;
  extension: string;
  size: number;
  mediaType: string;
  isPublic: boolean;
  width?: number | null;
  height?: number | null;
  title?: string | null;
  altText?: string | null;
  url: string;
  thumbnailUrl?: string | null;
  createdAtUtc: string;
}

export interface ProductMediaSummary {
  mediaAssetId: number;
  role: string;
  displayOrder: number;
  url: string;
  thumbnailUrl?: string | null;
  altText?: string | null;
}
