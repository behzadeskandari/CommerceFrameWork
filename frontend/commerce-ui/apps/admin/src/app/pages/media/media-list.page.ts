import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MediaApiService, MediaSummary } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { BreadcrumbsComponent } from '@commerce/layout';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [CommonModule, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: 'Media' }
    ]" />

    @if (state === 'loading') { <cmr-loading-state /> } @else {
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }

      <header class="header">
        <h1>Media Library</h1>
        @if (permissions.hasPermission('Media.Upload')) {
          <label class="upload">
            Upload
            <input type="file" multiple accept="image/*,application/pdf" (change)="onUpload($event)" hidden />
          </label>
        }
      </header>

      <div class="grid">
        @for (item of items(); track item.id) {
          <article class="card">
            @if (item.thumbnailUrl || item.url) {
              <img [src]="item.thumbnailUrl || item.url" [alt]="item.altText || item.fileName" />
            } @else {
              <div class="placeholder">{{ item.extension | uppercase }}</div>
            }
            <div class="meta">
              <strong>{{ item.fileName }}</strong>
              <span>{{ item.mediaType }} · {{ formatSize(item.size) }}</span>
              <small>{{ item.createdAtUtc | date:'medium' }}</small>
            </div>
            @if (permissions.hasPermission('Media.Delete')) {
              <button type="button" (click)="remove(item)">Delete</button>
            }
          </article>
        }
      </div>
    }
  `,
  styles: [`
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .upload { background: #2563eb; color: #fff; padding: 0.5rem 0.875rem; border-radius: 0.375rem; cursor: pointer; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 1rem; }
    .card { border: 1px solid #e5e7eb; border-radius: 0.5rem; overflow: hidden; background: #fff; display: grid; gap: 0.5rem; }
    .card img { width: 100%; height: 120px; object-fit: cover; }
    .placeholder { height: 120px; display: grid; place-items: center; background: #f3f4f6; font-weight: 700; }
    .meta { padding: 0 0.75rem; display: grid; gap: 0.25rem; }
    .meta span, .meta small { color: #6b7280; }
    .card button { margin: 0 0.75rem 0.75rem; }
  `]
})
export class MediaListPageComponent implements OnInit {
  private readonly mediaApi = inject(MediaApiService);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  readonly items = signal<MediaSummary[]>([]);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      this.items.set(await firstValueFrom(this.mediaApi.list()));
      this.state = 'success';
    } catch {
      this.errorMessage = 'Failed to load media.';
      this.state = 'error';
    }
  }

  formatSize(size: number): string {
    if (size < 1024) return `${size} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }

  async onUpload(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files?.length) return;

    for (const file of Array.from(files)) {
      try {
        const uploaded = await firstValueFrom(this.mediaApi.upload(file, true));
        this.items.update(list => [uploaded, ...list]);
      } catch {
        this.errorMessage = `Failed to upload ${file.name}.`;
      }
    }
    input.value = '';
  }

  async remove(item: MediaSummary): Promise<void> {
    await firstValueFrom(this.mediaApi.delete(item.id));
    this.items.update(list => list.filter(x => x.id !== item.id));
  }
}
