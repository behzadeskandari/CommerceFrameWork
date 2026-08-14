export type PageState = 'loading' | 'success' | 'ready' | 'empty' | 'error';

export interface PageError {
  message: string;
  status?: number;
}
