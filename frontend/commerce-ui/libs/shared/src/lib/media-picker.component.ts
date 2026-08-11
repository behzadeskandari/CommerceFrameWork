import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MediaApiService } from '@commerce/api';
import { MediaSummary } from '@commerce/api';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'cmr-media-picker',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="picker">
      <div class="toolbar">
        <input type="file" accept="image/*" (change)="onFileSelected($event)" />
        @if (uploadError()) {
          <p class="error">{{ uploadError() }}</p>
        }
      </div>
      <div class="grid">
        @for (item of items(); track item.id) {
          <button type="button" class="item" (click)="select(item)">
            @if (item.thumbnailUrl || item.url) {
              <img [src]="item.thumbnailUrl || item.url" [alt]="item.altText || item.fileName" />
            } @else {
              <span>{{ item.fileName }}</span>
            }
          </button>
        }
      </div>
    </div>
  `,
  styles: [`
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(96px, 1fr)); gap: 0.5rem; }
    .item { border: 1px solid #e5e7eb; border-radius: 0.375rem; padding: 0.25rem; background: #fff; cursor: pointer; }
    .item img { width: 100%; height: 72px; object-fit: cover; display: block; }
    .error { color: #b91c1c; }
  `]
})
export class MediaPickerComponent {
  private readonly mediaApi = inject(MediaApiService);

  readonly items = signal<MediaSummary[]>([]);
  readonly uploadError = signal<string | null>(null);

  @Input() role = 'Gallery';
  @Output() readonly picked = new EventEmitter<MediaSummary>();

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.items.set(await firstValueFrom(this.mediaApi.list()));
  }

  select(item: MediaSummary): void {
    this.picked.emit(item);
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadError.set(null);
    try {
      const uploaded = await firstValueFrom(this.mediaApi.upload(file, true));
      this.items.update(list => [uploaded, ...list]);
      this.picked.emit(uploaded);
    } catch {
      this.uploadError.set('Upload failed.');
    } finally {
      input.value = '';
    }
  }
}
