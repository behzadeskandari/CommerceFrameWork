import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CmsApi, StorefrontWidget } from '@commerce/api';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'cmr-widget-zone',
  standalone: true,
  template: `
    @if (widgets().length) {
      <section class="cmr-widget-zone" [attr.data-zone]="zone">
        @for (widget of widgets(); track widget.id) {
          <div class="cmr-widget" [attr.data-widget-type]="widget.widgetType" [innerHTML]="render(widget)"></div>
        }
      </section>
    }
  `,
  styles: [`:host { display: block; } .cmr-widget :is(img) { max-width: 100%; }`]
})
export class WidgetZoneComponent implements OnInit {
  @Input({ required: true }) zone!: string;

  private readonly api = inject(CmsApi);
  private readonly sanitizer = inject(DomSanitizer);
  readonly widgets = signal<StorefrontWidget[]>([]);

  ngOnInit(): void {
    void this.load();
  }

  render(widget: StorefrontWidget): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(widget.renderedHtml);
  }

  private async load(): Promise<void> {
    try {
      const widgets = await firstValueFrom(this.api.getStorefrontWidgets(this.zone));
      this.widgets.set(widgets);
    } catch {
      this.widgets.set([]);
    }
  }
}
