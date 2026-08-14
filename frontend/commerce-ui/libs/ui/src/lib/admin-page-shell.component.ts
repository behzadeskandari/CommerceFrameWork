import { Component, Input } from '@angular/core';

@Component({
  selector: 'cmr-admin-page-shell',
  standalone: true,
  imports: [],
  template: `
    <header class="admin-page-shell">
      <div class="admin-page-shell__heading">
        <h1>{{ title }}</h1>
        @if (description) {
          <p class="admin-page-shell__description">{{ description }}</p>
        }
      </div>
      <div class="admin-page-shell__actions">
        <ng-content select="[actions]" />
      </div>
    </header>
    <ng-content select="[toolbar]" />
    <div class="admin-page-shell__content">
      <ng-content />
    </div>
  `,
  styles: [`
    .admin-page-shell {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      flex-wrap: wrap;
      margin-block: 0.75rem 1rem;
    }
    .admin-page-shell__heading h1 {
      margin: 0;
      font-size: 1.75rem;
      line-height: 1.2;
    }
    .admin-page-shell__description {
      margin: 0.35rem 0 0;
      color: var(--text-muted, #6b7280);
    }
    .admin-page-shell__actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
    }
    .admin-page-shell__toolbar {
      margin-block-end: 1rem;
    }
  `]
})
export class AdminPageShellComponent {
  @Input({ required: true }) title = '';
  @Input() description = '';
}
