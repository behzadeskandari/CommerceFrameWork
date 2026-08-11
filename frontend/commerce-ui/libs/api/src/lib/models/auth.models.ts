export interface SessionResponse {
  isAuthenticated: boolean;
  identityUserId: string | null;
  email: string | null;
  customerId: number | null;
  roles: string[];
  permissions: string[];
}
