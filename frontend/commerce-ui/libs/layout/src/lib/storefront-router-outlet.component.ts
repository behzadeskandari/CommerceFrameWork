import { Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { ThemeLayoutService, StorefrontContentShellComponent } from '@commerce/theme';

@Component({
  selector: 'cmr-storefront-router-outlet',
  standalone: true,
  imports: [RouterOutlet, StorefrontContentShellComponent],
  template: `
    <cmr-storefront-content-shell [layoutType]="layoutType()">
      <router-outlet />
    </cmr-storefront-content-shell>
  `
})
export class StorefrontRouterOutletComponent {
  private readonly router = inject(Router);
  private readonly themeLayout = inject(ThemeLayoutService);

  readonly layoutType = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.themeLayout.resolveLayout(this.router.routerState.snapshot.root)),
      startWith(this.themeLayout.resolveLayout(this.router.routerState.snapshot.root))
    ),
    { initialValue: 'Homepage' as const }
  );
}
