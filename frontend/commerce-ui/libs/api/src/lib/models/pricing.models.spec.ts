import { DiscountSummary, CouponSummary } from './pricing.models';

describe('pricing.models', () => {
  it('accepts discount summary shape', () => {
    const discount: DiscountSummary = {
      id: 1,
      name: 'Summer sale',
      systemName: 'summer-sale',
      discountType: 'Percentage',
      value: 10,
      currencyCode: null,
      priority: 0,
      isActive: true,
      startsAtUtc: null,
      endsAtUtc: null,
      storeId: null,
      applicationScope: 'Cart'
    };
    expect(discount.discountType).toBe('Percentage');
  });

  it('accepts coupon summary shape', () => {
    const coupon: CouponSummary = {
      id: 1,
      code: 'SAVE10',
      discountId: 1,
      discountName: 'Summer sale',
      isActive: true,
      usageCount: 0,
      globalUsageLimit: null,
      perCustomerUsageLimit: null,
      startsAtUtc: null,
      endsAtUtc: null,
      storeId: null
    };
    expect(coupon.code).toBe('SAVE10');
  });
});
