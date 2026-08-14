export type ReviewModerationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface ProductRatingSummary {
  averageRating: number;
  ratingCount: number;
  distribution: Record<number, number>;
}

export interface ProductReview {
  id: number;
  productId: number;
  customerId: number;
  rating: number;
  title: string;
  content: string;
  moderationStatus: ReviewModerationStatus;
  isVerifiedPurchase: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ProductReviewsPage {
  reviews: ProductReview[];
  summary: ProductRatingSummary;
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SubmitProductReviewRequest {
  rating: number;
  title: string;
  content: string;
}

export interface UpdateProductReviewRequest {
  rating: number;
  title: string;
  content: string;
}

export interface WishlistItem {
  productId: number;
  productName: string;
  slug: string | null;
  isAvailable: boolean;
  addedAtUtc: string;
}

export interface Wishlist {
  id: number;
  customerId: number;
  storeId: number;
  items: WishlistItem[];
}

export interface AddWishlistItemRequest {
  productId: number;
}

export interface AdminProductReview {
  id: number;
  productId: number;
  productName: string | null;
  customerId: number;
  customerDisplayName: string | null;
  storeId: number;
  rating: number;
  title: string;
  content: string;
  moderationStatus: ReviewModerationStatus;
  isVerifiedPurchase: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AdminReviewList {
  items: AdminProductReview[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminWishlistSummary {
  id: number;
  customerId: number;
  customerDisplayName: string | null;
  storeId: number;
  itemCount: number;
  lastAddedAtUtc: string | null;
}

export interface AdminWishlistList {
  items: AdminWishlistSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminWishlistItem {
  productId: number;
  productName: string | null;
  addedAtUtc: string;
}

export interface AdminWishlistDetail {
  id: number;
  customerId: number;
  customerDisplayName: string | null;
  storeId: number;
  items: AdminWishlistItem[];
}
