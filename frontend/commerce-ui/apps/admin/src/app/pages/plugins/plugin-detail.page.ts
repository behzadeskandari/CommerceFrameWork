import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  PluginDetail,
  PluginMigrationStatus,
  PluginPermissionEntry,
  PluginSettingEntry,
  PluginStoreConfiguration,
  PluginsApi
} from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { LocalizationService, TranslatePipe } from '@commerce/localization';
import {
  ConfirmDialogComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

type DetailTab = 'overview' | 'configuration' | 'permissions' | 'stores' | 'migrations';
type ConfirmAction = 'install' | 'enable' | 'disable' | 'uninstall' | 'uninstallRemoveData' | 'reload';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    BreadcrumbsComponent,
    TranslatePipe,
    LoadingStateComponent,
    ErrorStateComponent,
    ConfirmDialogComponent
  ],
  template: `
    @if (plugin) {
      <cmr-breadcrumbs [items]="[
        { label: 'Dashboard', link: '/dashboard' },
        { label: ('plugins.title' | translate), link: '/plugins' },
        { label: plugin.name }
      ]" />
    }

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        @if (plugin) {
          <header class="page-header">
            <div>
              <h1>{{ plugin.name }}</h1>
              <p class="meta"><code>{{ plugin.systemName }}</code> · v{{ plugin.version }}</p>
              @if (plugin.requiresRestartForServiceChanges) {
                <p class="restart-note">{{ 'plugins.restartNote' | translate }}</p>
              }
            </div>
            <div class="actions">
              @if (!plugin.isInstalled && permissions.hasPermission('Plugins.Install')) {
                <button type="button" (click)="setConfirmAction('install')">{{ 'plugins.install' | translate }}</button>
              }
              @if (plugin.isInstalled && !plugin.isEnabled && permissions.hasPermission('Plugins.Manage')) {
                <button type="button" (click)="setConfirmAction('enable')">{{ 'plugins.enable' | translate }}</button>
              }
              @if (plugin.isEnabled && permissions.hasPermission('Plugins.Manage')) {
                <button type="button" class="secondary" (click)="setConfirmAction('disable')">{{ 'plugins.disable' | translate }}</button>
              }
              @if (plugin.isInstalled && !plugin.isRequired && permissions.hasPermission('Plugins.Manage')) {
                <button type="button" class="danger" (click)="setConfirmAction('uninstall')">{{ 'plugins.uninstall' | translate }}</button>
                <button type="button" class="danger" (click)="setConfirmAction('uninstallRemoveData')">{{ 'plugins.uninstallRemoveData' | translate }}</button>
              }
              @if (permissions.hasPermission('Plugins.Manage')) {
                <button type="button" class="secondary" (click)="setConfirmAction('reload')">{{ 'plugins.reload' | translate }}</button>
              }
            </div>
          </header>

          @if (plugin.lastError) {
            <div class="error-banner" role="alert">{{ plugin.lastError }}</div>
          }

          <nav class="tabs" aria-label="Plugin sections">
            @for (tab of tabs; track tab.id) {
              <button type="button" [class.active]="activeTab === tab.id" (click)="selectTab(tab.id)">
                {{ tab.labelKey | translate }}
              </button>
            }
          </nav>

          @switch (activeTab) {
            @case ('overview') {
              <dl class="detail-grid">
                <div><dt>{{ 'plugins.status' | translate }}</dt><dd>{{ plugin.state }}</dd></div>
                <div><dt>{{ 'plugins.author' | translate }}</dt><dd>{{ plugin.author || '—' }}</dd></div>
                <div><dt>{{ 'plugins.assembly' | translate }}</dt><dd><code>{{ plugin.assemblyName }}</code></dd></div>
                <div><dt>{{ 'plugins.directory' | translate }}</dt><dd><code>{{ plugin.pluginDirectory }}</code></dd></div>
                <div><dt>{{ 'plugins.minVersion' | translate }}</dt><dd>{{ plugin.minimumCommerceVersion || '—' }}</dd></div>
                @if (plugin.installedAt) {
                  <div><dt>{{ 'plugins.installedAt' | translate }}</dt><dd>{{ plugin.installedAt | date:'medium' }}</dd></div>
                }
                @if (plugin.updatedAt) {
                  <div><dt>{{ 'plugins.updatedAt' | translate }}</dt><dd>{{ plugin.updatedAt | date:'medium' }}</dd></div>
                }
              </dl>

              @if (plugin.description) {
                <section>
                  <h2>{{ 'plugins.description' | translate }}</h2>
                  <p>{{ plugin.description }}</p>
                </section>
              }

              @if (plugin.dependencies.length) {
                <section>
                  <h2>{{ 'plugins.dependencies' | translate }}</h2>
                  <ul>
                    @for (dep of plugin.dependencies; track dep.systemName) {
                      <li><code>{{ dep.systemName }}</code></li>
                    }
                  </ul>
                </section>
              }
            }
            @case ('configuration') {
              @if (permissions.hasPermission('Plugins.Configure')) {
                @if (settings.length === 0) {
                  <p class="muted">{{ 'plugins.noSettings' | translate }}</p>
                } @else {
                  <form class="settings-form" (ngSubmit)="saveSettings()">
                    @for (setting of settings; track setting.key) {
                      <label>
                        <span>{{ setting.description }} <code>{{ setting.key }}</code></span>
                        @if (setting.isSecret) {
                          <input type="password" [(ngModel)]="settingDraft[setting.key]" [name]="setting.key" [placeholder]="setting.hasValue ? '••••••••' : ''" />
                        } @else if (setting.valueType === 'Boolean') {
                          <select [(ngModel)]="settingDraft[setting.key]" [name]="setting.key">
                            <option value="true">true</option>
                            <option value="false">false</option>
                          </select>
                        } @else {
                          <input type="text" [(ngModel)]="settingDraft[setting.key]" [name]="setting.key" />
                        }
                      </label>
                    }
                    <button type="submit">{{ 'action.save' | translate }}</button>
                  </form>
                }
              } @else {
                <p class="muted">{{ 'unauthorized.message' | translate }}</p>
              }
            }
            @case ('permissions') {
              @if (pluginPermissions.length === 0) {
                <p class="muted">{{ 'plugins.noPermissions' | translate }}</p>
              } @else {
                <ul class="list">
                  @for (permission of pluginPermissions; track permission.key) {
                    <li><code>{{ permission.key }}</code> — {{ permission.description }}</li>
                  }
                </ul>
              }
            }
            @case ('stores') {
              @if (permissions.hasPermission('Plugins.Configure')) {
                @if (storeConfigurations.length === 0) {
                  <p class="muted">{{ 'plugins.noStoreConfig' | translate }}</p>
                } @else {
                  <ul class="list">
                    @for (store of storeConfigurations; track store.storeId) {
                      <li>
                        {{ 'plugins.store' | translate }} #{{ store.storeId }} —
                        {{ store.isEnabled ? ('plugins.enabled' | translate) : ('plugins.disabled' | translate) }}
                      </li>
                    }
                  </ul>
                }
              } @else {
                <p class="muted">{{ 'unauthorized.message' | translate }}</p>
              }
            }
            @case ('migrations') {
              @if (migrations.length === 0) {
                <p class="muted">{{ 'plugins.noMigrations' | translate }}</p>
              } @else {
                <table>
                  <thead>
                    <tr>
                      <th>{{ 'plugins.migrationName' | translate }}</th>
                      <th>{{ 'plugins.migrationVersion' | translate }}</th>
                      <th>{{ 'plugins.status' | translate }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (migration of migrations; track migration.name) {
                      <tr>
                        <td>{{ migration.name }}</td>
                        <td>{{ migration.version }}</td>
                        <td>{{ migration.isApplied ? ('plugins.applied' | translate) : ('plugins.pending' | translate) }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              }
            }
          }

          @if (actionError) {
            <p class="action-error" role="alert">{{ actionError }}</p>
          }
        }
      }
    }

    <cmr-confirm-dialog
      [open]="confirmAction !== null"
      [title]="confirmTitle"
      [message]="confirmMessage()"
      (confirm)="runAction()"
      (cancel)="confirmAction = null"
    />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; gap: 1rem; margin-bottom: 1.5rem; flex-wrap: wrap; }
    .meta, .restart-note { color: #6b7280; margin: 0.25rem 0 0; }
    .restart-note { font-size: 0.875rem; }
    .actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    button { padding: 0.5rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer; background: var(--primary, #0f766e); color: #fff; }
    button.secondary { background: #fff; border: 1px solid #d1d5db; color: #111827; }
    button.danger { background: #b91c1c; }
    .tabs { display: flex; gap: 0.5rem; margin-bottom: 1rem; flex-wrap: wrap; }
    .tabs button { background: #f3f4f6; color: #111827; border: 1px solid #e5e7eb; }
    .tabs button.active { background: var(--primary, #0f766e); color: #fff; border-color: transparent; }
    .detail-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }
    .detail-grid div { display: grid; gap: 0.25rem; }
    dt { font-size: 0.875rem; color: #6b7280; }
    .error-banner { background: #fef2f2; border: 1px solid #fca5a5; padding: 0.75rem 1rem; border-radius: 0.5rem; margin-bottom: 1rem; }
    .action-error { color: #b91c1c; margin-top: 1rem; }
    section { margin-top: 1.5rem; }
    .muted { color: #6b7280; }
    .settings-form { display: grid; gap: 1rem; max-width: 36rem; }
    .settings-form label { display: grid; gap: 0.35rem; }
    .settings-form input, .settings-form select { padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 0.5rem; border-bottom: 1px solid #e5e7eb; }
    .list { padding-left: 1.25rem; }
  `]
})
export class PluginDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly pluginsApi = inject(PluginsApi);
  private readonly localization = inject(LocalizationService);
  readonly permissions = inject(PermissionService);

  readonly tabs: { id: DetailTab; labelKey: string }[] = [
    { id: 'overview', labelKey: 'plugins.tab.overview' },
    { id: 'configuration', labelKey: 'plugins.tab.configuration' },
    { id: 'permissions', labelKey: 'plugins.tab.permissions' },
    { id: 'stores', labelKey: 'plugins.tab.stores' },
    { id: 'migrations', labelKey: 'plugins.tab.migrations' }
  ];

  state: PageState = 'loading';
  plugin: PluginDetail | null = null;
  settings: PluginSettingEntry[] = [];
  settingDraft: Record<string, string> = {};
  pluginPermissions: PluginPermissionEntry[] = [];
  storeConfigurations: PluginStoreConfiguration[] = [];
  migrations: PluginMigrationStatus[] = [];
  activeTab: DetailTab = 'overview';
  errorMessage = '';
  actionError = '';
  confirmAction: ConfirmAction | null = null;
  systemName = '';
  confirmTitle = '';

  ngOnInit(): void {
    this.systemName = this.route.snapshot.paramMap.get('systemName') ?? '';
    void this.load();
  }

  async load(): Promise<void> {
    if (!this.systemName) {
      this.state = 'error';
      this.errorMessage = 'Plugin not found.';
      return;
    }

    this.state = 'loading';
    this.errorMessage = '';
    try {
      const [plugin, settings, permissions, stores, migrations] = await Promise.all([
        firstValueFrom(this.pluginsApi.get(this.systemName)),
        firstValueFrom(this.pluginsApi.getSettings(this.systemName)),
        firstValueFrom(this.pluginsApi.getPermissions(this.systemName)),
        firstValueFrom(this.pluginsApi.getStoreConfigurations(this.systemName)),
        firstValueFrom(this.pluginsApi.getMigrationStatus(this.systemName))
      ]);

      this.plugin = plugin;
      this.settings = settings;
      this.settingDraft = Object.fromEntries(
        settings.map(setting => [setting.key, setting.isSecret ? '' : (setting.value ?? '')])
      );
      this.pluginPermissions = permissions;
      this.storeConfigurations = stores;
      this.migrations = migrations;
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load plugin.';
      this.state = 'error';
    }
  }

  selectTab(tab: DetailTab): void {
    this.activeTab = tab;
  }

  confirmMessage(): string {
    if (this.confirmAction === 'uninstallRemoveData') {
      return this.localization.translate('plugins.uninstallRemoveDataMessage');
    }
    return this.plugin?.name ?? this.systemName;
  }

  setConfirmAction(action: ConfirmAction): void {
    this.confirmAction = action;
    this.confirmTitle = this.localization.translate(`plugins.${action}Title`);
  }

  async saveSettings(): Promise<void> {
    const values = Object.fromEntries(
      Object.entries(this.settingDraft).filter(([, value]) => value !== '')
    );
    this.actionError = '';
    try {
      await firstValueFrom(this.pluginsApi.saveSettings(this.systemName, values));
      await this.load();
      this.activeTab = 'configuration';
    } catch (error) {
      this.actionError = error instanceof ApiClientError ? error.message : 'Failed to save settings.';
    }
  }

  async runAction(): Promise<void> {
    const action = this.confirmAction;
    this.confirmAction = null;
    if (!action || !this.plugin) return;

    this.actionError = '';
    try {
      switch (action) {
        case 'install':
          await firstValueFrom(this.pluginsApi.install(this.systemName));
          break;
        case 'enable':
          await firstValueFrom(this.pluginsApi.enable(this.systemName));
          break;
        case 'disable':
          await firstValueFrom(this.pluginsApi.disable(this.systemName));
          break;
        case 'uninstall':
          await firstValueFrom(this.pluginsApi.uninstall(this.systemName, 'KeepData'));
          break;
        case 'uninstallRemoveData':
          await firstValueFrom(this.pluginsApi.uninstall(this.systemName, 'RemoveData'));
          break;
        case 'reload':
          await firstValueFrom(this.pluginsApi.reload(this.systemName));
          break;
      }
      await this.load();
    } catch (error) {
      this.actionError = error instanceof ApiClientError ? error.message : 'Action failed.';
    }
  }
}
