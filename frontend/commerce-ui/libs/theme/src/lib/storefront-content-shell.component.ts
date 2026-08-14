import { Component, Input, computed } from '@angular/core';
import { ThemeLayoutType } from '@commerce/api';
import { WidgetZoneComponent } from './widget-zone.component';
import { ThemeRuntimeService } from './theme-runtime.service';

@Component({
  selector: 'cmr-storefront-content-shell',
  standalone: true,
  imports: [WidgetZoneComponent],
  template: `
    @if (layout(); as currentLayout) {
      <div class="content-shell" [class.with-sidebar]="currentLayout.showSidebar">
        @for (zone of contentZones(); track zone) {
          @if (zone === 'main-content') {
            <div class="layout-main">
              <cmr-widget-zone [zone]="zone" />
              <div class="layout-page-content"><ng-content /></div>
            </div>
          } @else if (zone === 'sidebar') {
            @if (currentLayout.showSidebar) {
              <aside class="layout-sidebar"><cmr-widget-zone [zone]="zone" /></aside>
            }
          } @else {
            <cmr-widget-zone [zone]="zone" />
          }
        }
        @if (!hasMainZone()) {
          <div class="layout-page-content"><ng-content /></div>
        }
      </div>
    } @else {
      <ng-content />
    }
  `,
  styles: [`
    .content-shell { display: grid; gap: 1rem; }
    .content-shell.with-sidebar { grid-template-columns: minmax(0, 1fr); }
    @media (min-width: 960px) {
      .content-shell.with-sidebar { grid-template-columns: minmax(0, 1fr) 280px; }
      .layout-sidebar { order: 2; }
      .layout-main { order: 1; }
    }
    .layout-page-content { min-width: 0; }
  `]
})
export class StorefrontContentShellComponent {
  @Input({ required: true }) layoutType: ThemeLayoutType | string = 'CmsPage';

  constructor(private readonly themeRuntime: ThemeRuntimeService) {}

  readonly layout = computed(() => this.themeRuntime.getLayout(this.layoutType));

  readonly contentZones = computed(() =>
    (this.layout()?.zones ?? []).filter(zone => zone !== 'header' && zone !== 'footer')
  );

  readonly hasMainZone = computed(() => this.contentZones().includes('main-content'));
}
