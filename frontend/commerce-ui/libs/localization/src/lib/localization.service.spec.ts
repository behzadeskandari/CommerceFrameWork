import { LocalizationService } from './localization.service';

describe('LocalizationService', () => {
  it('switches to RTL for Persian', () => {
    const service = new LocalizationService();
    service.setLocale('fa');
    expect(service.direction()).toBe('rtl');
    expect(document.documentElement.dir).toBe('rtl');
    expect(document.documentElement.lang).toBe('fa');
  });

  it('translates keys', () => {
    const service = new LocalizationService();
    service.setLocale('en');
    expect(service.translate('nav.dashboard')).toBe('Dashboard');
    service.setLocale('fa');
    expect(service.translate('nav.dashboard')).toBe('داشبورد');
  });
});
