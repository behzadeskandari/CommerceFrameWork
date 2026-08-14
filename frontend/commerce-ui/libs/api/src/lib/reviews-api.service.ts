import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  AddWishlistItemRequest,
  AdminReviewList,
  AdminWishlistDetail,
  AdminWishlistList,
  ProductRatingSummary,
  ProductReview,
  ProductReviewsPage,
  SubmitProductReviewRequest,
  UpdateProductReviewRequest,
  Wishlist,
  WishlistItem
} from './models/reviews.models';

@Injectable({ providedIn: 'root' })
export class ReviewsApi {
  private readonly http = inject(HttpClient);

  listProductReviews(productId: number, page = 1, pageSize = 10): Observable<ProductReviewsPage> {
    return this.http
      .get<ApiResponse<ProductReviewsPage>>(`/api/reviews/products/${productId}`, {
        params: { page: String(page), pageSize: String(pageSize) }
      })
      .pipe(map(r => r.data!));
  }

  getProductRatingSummary(productId: number): Observable<ProductRatingSummary> {
    return this.http
      .get<ApiResponse<ProductRatingSummary>>(`/api/reviews/products/${productId}/summary`)
      .pipe(map(r => r.data!));
  }

  getOwnReview(productId: number): Observable<ProductReview> {
    return this.http
      .get<ApiResponse<ProductReview>>(`/api/reviews/me/products/${productId}`)
      .pipe(map(r => r.data!));
  }

  submitReview(productId: number, body: SubmitProductReviewRequest): Observable<ProductReview> {
    return this.http
      .post<ApiResponse<ProductReview>>(`/api/reviews/products/${productId}`, body)
      .pipe(map(r => r.data!));
  }

  updateReview(reviewId: number, body: UpdateProductReviewRequest): Observable<ProductReview> {
    return this.http
      .put<ApiResponse<ProductReview>>(`/api/reviews/${reviewId}`, body)
      .pipe(map(r => r.data!));
  }

  listAdminReviews(params?: {
    storeId?: number;
    productId?: number;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<AdminReviewList> {
    const query: Record<string, string> = {};
    if (params?.storeId != null) query['storeId'] = String(params.storeId);
    if (params?.productId != null) query['productId'] = String(params.productId);
    if (params?.status) query['status'] = params.status;
    if (params?.page != null) query['page'] = String(params.page);
    if (params?.pageSize != null) query['pageSize'] = String(params.pageSize);
    return this.http
      .get<ApiResponse<AdminReviewList>>('/api/admin/reviews', { params: query })
      .pipe(map(r => r.data!));
  }

  approveReview(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`/api/admin/reviews/${id}/approve`, {}).pipe(map(() => undefined));
  }

  rejectReview(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`/api/admin/reviews/${id}/reject`, {}).pipe(map(() => undefined));
  }

  deleteReview(id: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`/api/admin/reviews/${id}`).pipe(map(() => undefined));
  }

  listAdminWishlists(params?: {
    storeId?: number;
    customerId?: number;
    page?: number;
    pageSize?: number;
  }): Observable<AdminWishlistList> {
    const query: Record<string, string> = {};
    if (params?.storeId != null) query['storeId'] = String(params.storeId);
    if (params?.customerId != null) query['customerId'] = String(params.customerId);
    if (params?.page != null) query['page'] = String(params.page);
    if (params?.pageSize != null) query['pageSize'] = String(params.pageSize);
    return this.http
      .get<ApiResponse<AdminWishlistList>>('/api/admin/wishlists', { params: query })
      .pipe(map(r => r.data!));
  }

  getAdminWishlist(id: number): Observable<AdminWishlistDetail> {
    return this.http
      .get<ApiResponse<AdminWishlistDetail>>(`/api/admin/wishlists/${id}`)
      .pipe(map(r => r.data!));
  }
}

@Injectable({ providedIn: 'root' })
export class WishlistApi {
  private readonly http = inject(HttpClient);

  get(): Observable<Wishlist> {
    return this.http.get<ApiResponse<Wishlist>>('/api/wishlist').pipe(map(r => r.data!));
  }

  addItem(body: AddWishlistItemRequest): Observable<WishlistItem> {
    return this.http.post<ApiResponse<WishlistItem>>('/api/wishlist/items', body).pipe(map(r => r.data!));
  }

  removeItem(productId: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`/api/wishlist/items/${productId}`).pipe(map(() => undefined));
  }
}
