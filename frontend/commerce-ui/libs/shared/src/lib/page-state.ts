export type PageState = 'loading' | 'success' | 'empty' | 'error';

export interface PageError {
  message: string;
  status?: number;
}
