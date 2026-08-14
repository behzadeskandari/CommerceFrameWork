import { Component, OnInit, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { CmsApi, StorefrontPage } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { Meta, Title } from '@angular/platform-browser';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [LoadingStateComponent, ErrorStateComponent],
  template: `
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        @if (page) {
          <article class="cms-page">
            <h1>{{ page.title }}</h1>
            <div class="cms-body" [innerHTML]="safeBody"></div>
          </article>
        }
      }
    }
  `,
  styles: [`.cms-page { max-width: 48rem; margin: 0 auto; } .cms-body :is(img) { max-width: 100%; }`]
})
export class CmsPageComponent implements OnInit {
  private readonly api = inject(CmsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly sanitizer = inject(DomSanitizer);
  state: PageState = 'loading';
  errorMessage = '';
  page: StorefrontPage | null = null;
  safeBody: SafeHtml = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe(() => void this.load());
  }

  async load(): Promise<void> {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.errorMessage = 'Page not found.';
      this.state = 'error';
      return;
    }

    this.state = 'loading';
    try {
      this.page = await firstValueFrom(this.api.getStorefrontPage(slug));
      this.safeBody = this.sanitizer.bypassSecurityTrustHtml(this.page.body);
      this.title.setTitle(this.page.metaTitle ?? this.page.title);
      if (this.page.metaDescription) {
        this.meta.updateTag({ name: 'description', content: this.page.metaDescription });
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Page not found.';
      this.state = 'error';
    }
  }
}
