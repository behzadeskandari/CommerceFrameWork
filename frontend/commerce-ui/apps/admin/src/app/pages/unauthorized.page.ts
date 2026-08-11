import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@commerce/localization';

@Component({
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <section>
      <h1>{{ 'unauthorized.title' | translate }}</h1>
      <p>{{ 'unauthorized.message' | translate }}</p>
      <a routerLink="/dashboard">Back to dashboard</a>
    </section>
  `
})
export class UnauthorizedPageComponent {}
