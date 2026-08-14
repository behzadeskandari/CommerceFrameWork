import { Component, OnInit, inject } from '@angular/core';
import { CmsApi, TopicSummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'cms.topics' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <table>
          <thead><tr><th>System name</th><th>Title</th><th>Published</th></tr></thead>
          <tbody>
            @for (topic of topics; track topic.id) {
              <tr>
                <td>{{ topic.systemName }}</td>
                <td>{{ topic.defaultTitle }}</td>
                <td>{{ topic.isPublished ? 'Yes' : 'No' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`table { width: 100%; border-collapse: collapse; } th, td { border: 1px solid #e5e7eb; padding: 0.5rem; }`]
})
export class CmsTopicListPageComponent implements OnInit {
  private readonly api = inject(CmsApi);
  state: PageState = 'loading';
  errorMessage = '';
  topics: TopicSummary[] = [];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.topics = await firstValueFrom(this.api.listTopics());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load topics.';
      this.state = 'error';
    }
  }
}
