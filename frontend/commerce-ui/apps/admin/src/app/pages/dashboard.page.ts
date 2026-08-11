import { Component } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <section>
      <h1>{{ 'nav.dashboard' | translate }}</h1>
      <p>Welcome to Commerce Admin. Use the sidebar to manage catalog and customers.</p>
    </section>
  `
})
export class DashboardPageComponent {}
