import { Component, OnInit, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CmsApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ isNew() ? ('cms.newPage' | translate) : ('cms.editPage' | translate) }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <form [formGroup]="form" (ngSubmit)="save()">
          <label>{{ 'common.title' | translate }}<input formControlName="title" /></label>
          <label>Slug<input formControlName="slug" /></label>
          <label><input type="checkbox" formControlName="isPublished" /> {{ 'cms.published' | translate }}</label>
          <label>Body<textarea formControlName="body" rows="12"></textarea></label>
          <label>SEO Title<input formControlName="metaTitle" /></label>
          <label>SEO Description<textarea formControlName="metaDescription" rows="3"></textarea></label>
          <button type="submit">{{ 'action.save' | translate }}</button>
        </form>
      }
    }
  `,
  styles: [`form { display: grid; gap: 1rem; max-width: 48rem; } label { display: grid; gap: 0.375rem; }`]
})
export class CmsPageFormPageComponent implements OnInit {
  readonly id = input<string>();
  private readonly api = inject(CmsApi);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  state: PageState = 'loading';
  errorMessage = '';
  languageId = 1;

  readonly form = this.fb.group({
    title: ['', Validators.required],
    slug: ['', Validators.required],
    body: [''],
    metaTitle: [''],
    metaDescription: [''],
    isPublished: [false]
  });

  isNew(): boolean { return !this.id() || this.id() === 'new'; }

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    if (this.isNew()) {
      this.state = 'success';
      return;
    }

    this.state = 'loading';
    try {
      const page = await firstValueFrom(this.api.getPage(Number(this.id())));
      const loc = page.localizations[0];
      if (loc) {
        this.languageId = loc.languageId;
        this.form.patchValue({
          title: loc.title,
          slug: loc.slug,
          body: loc.body,
          metaTitle: loc.metaTitle ?? '',
          metaDescription: loc.metaDescription ?? '',
          isPublished: page.isPublished
        });
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load page.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    const localization = {
      languageId: this.languageId,
      title: value.title!,
      slug: value.slug!,
      body: value.body ?? '',
      metaTitle: value.metaTitle || null,
      metaDescription: value.metaDescription || null,
      metaKeywords: null,
      canonicalUrl: null
    };

    try {
      if (this.isNew()) {
        await firstValueFrom(this.api.createPage({
          storeId: 1,
          isPublished: !!value.isPublished,
          localizations: [localization]
        }));
      } else {
        await firstValueFrom(this.api.updatePage(Number(this.id()), {
          isPublished: !!value.isPublished,
          localizations: [localization]
        }));
      }
      await this.router.navigate(['/cms/pages']);
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to save page.';
      this.state = 'error';
    }
  }
}
