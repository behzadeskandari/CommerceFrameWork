import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: number;
  kind: ToastKind;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly messages = signal<ToastMessage[]>([]);

  show(kind: ToastKind, message: string, durationMs = 4000): void {
    const toast: ToastMessage = { id: this.nextId++, kind, message };
    this.messages.update(items => [...items, toast]);
    window.setTimeout(() => this.dismiss(toast.id), durationMs);
  }

  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message, 6000);
  }

  dismiss(id: number): void {
    this.messages.update(items => items.filter(item => item.id !== id));
  }
}
