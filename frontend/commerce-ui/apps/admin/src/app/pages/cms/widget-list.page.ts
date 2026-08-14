import { Component, OnInit, inject } from '@angular/core';
import { CmsApi, WidgetInstance, WidgetZone } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'cms.widgets' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <h2>{{ 'cms.widgetZones' | translate }}</h2>
        <table>
          <thead><tr><th>Zone</th><th>Name</th><th>Order</th></tr></thead>
          <tbody>
            @for (zone of zones; track zone.id) {
              <tr>
                <td>{{ zone.systemName }}</td>
                <td>{{ zone.name }}</td>
                <td>{{ zone.displayOrder }}</td>
              </tr>
            }
          </tbody>
        </table>
        <h2>{{ 'cms.widgetInstances' | translate }}</h2>
        <table>
          <thead><tr><th>Zone</th><th>Type</th><th>Active</th><th>Order</th></tr></thead>
          <tbody>
            @for (instance of instances; track instance.id) {
              <tr>
                <td>{{ instance.zoneSystemName }}</td>
                <td>{{ instance.widgetType }}</td>
                <td>{{ instance.isActive ? 'Yes' : 'No' }}</td>
                <td>{{ instance.displayOrder }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`table { width: 100%; border-collapse: collapse; margin-bottom: 1.5rem; } th, td { border: 1px solid #e5e7eb; padding: 0.5rem; }`]
})
export class CmsWidgetListPageComponent implements OnInit {
  private readonly api = inject(CmsApi);
  state: PageState = 'loading';
  errorMessage = '';
  zones: WidgetZone[] = [];
  instances: WidgetInstance[] = [];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      [this.zones, this.instances] = await Promise.all([
        firstValueFrom(this.api.listWidgetZones()),
        firstValueFrom(this.api.listWidgetInstances())
      ]);
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load widgets.';
      this.state = 'error';
    }
  }
}
