export interface CustomerSummary {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  active: boolean;
  deleted: boolean;
  createdAtUtc: string;
}

export interface CustomerDetail extends CustomerSummary {
  identityUserId: string;
  updatedAtUtc: string;
  addresses: CustomerAddress[];
}

export interface CustomerAddress {
  id: number;
  customerId: number;
  label: string;
  firstName: string;
  lastName: string;
  country: string;
  stateProvince: string | null;
  city: string;
  address1: string;
  address2: string | null;
  postalCode: string;
  phoneNumber: string | null;
  isDefaultBilling: boolean;
  isDefaultShipping: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface RegisterCustomerRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface UpdateCustomerRequest {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
}

export interface AddCustomerAddressRequest {
  label: string;
  firstName: string;
  lastName: string;
  country: string;
  city: string;
  address1: string;
  postalCode: string;
  stateProvince?: string | null;
  address2?: string | null;
  phoneNumber?: string | null;
  isDefaultBilling?: boolean;
  isDefaultShipping?: boolean;
}

export type UpdateCustomerAddressRequest = AddCustomerAddressRequest;

export interface AuthenticationResult {
  identityUserId: string;
  customerId: number;
  email: string;
}
