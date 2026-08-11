import { Component, Input, output } from '@angular/core';

@Component({
  selector: 'cmr-pagination',
  standalone: true,
  template: `
    <nav class="pagination" aria-label="Pagination">
      <button type="button" [disabled]="page <= 1" (click)="pageChange.emit(page - 1)">Previous</button>
      <span>Page {{ page }} of {{ totalPages }}</span>
      <button type="button" [disabled]="page >= totalPages" (click)="pageChange.emit(page + 1)">Next</button>
    </nav>
  `,
  styles: [`
    .pagination { display: flex; align-items: center; gap: 1rem; margin-top: 1rem; }
    button { padding: 0.375rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; background: #fff; cursor: pointer; }
    button:disabled { opacity: 0.5; cursor: not-allowed; }
  `]
})
export class PaginationComponent {
  @Input({ required: true }) page = 1;
  @Input({ required: true }) totalPages = 1;
  readonly pageChange = output<number>();
}
