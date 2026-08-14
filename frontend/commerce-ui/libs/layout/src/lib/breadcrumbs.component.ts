import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface BreadcrumbItem {
  label: string;
  link?: string | null;
}

@Component({
  selector: 'cmr-breadcrumbs',
  standalone: true,
  imports: [RouterLink],
  template: `
    <nav aria-label="Breadcrumb">
      <ol class="breadcrumbs">
        @for (item of items; track item.label; let last = $last) {
          <li>
            @if (!last && item.link) {
              <a [routerLink]="item.link">{{ item.label }}</a>
            } @else {
              <span aria-current="page">{{ item.label }}</span>
            }
          </li>
        }
      </ol>
    </nav>
  `,
  styles: [`
    .breadcrumbs { display: flex; flex-wrap: wrap; gap: 0.5rem; list-style: none; padding: 0; margin: 0 0 1rem; color: var(--text-muted); font-size: 0.875rem; }
    .breadcrumbs li:not(:last-child)::after { content: '/'; margin-inline-start: 0.5rem; }
    a { color: inherit; text-decoration: none; }
    a:hover { text-decoration: underline; }
  `]
})
export class BreadcrumbsComponent {
  @Input({ required: true }) items: BreadcrumbItem[] = [];
}
