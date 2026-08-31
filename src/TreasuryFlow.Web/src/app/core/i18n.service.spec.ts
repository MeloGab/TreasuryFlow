import { TestBed } from '@angular/core/testing';
import { I18nService } from './i18n.service';
import { PreferencesService } from './preferences.service';

describe('I18nService', () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('treasuryflow.theme', 'light');
    TestBed.configureTestingModule({});
  });

  afterEach(() => localStorage.clear());

  it('should translate status labels when the locale changes', () => {
    const preferences = TestBed.inject(PreferencesService);
    const i18n = TestBed.inject(I18nService);

    expect(i18n.statusLabel('Processing')).toBe('Em processamento');

    preferences.setLocale('en-US');

    expect(i18n.statusLabel('Processing')).toBe('Processing');
  });

  it('should format currencies using the selected locale', () => {
    const preferences = TestBed.inject(PreferencesService);
    const i18n = TestBed.inject(I18nService);

    const portugueseValue = i18n.formatCurrency(1234.56, 'BRL');
    preferences.setLocale('en-US');
    const englishValue = i18n.formatCurrency(1234.56, 'BRL');

    expect(portugueseValue).toContain('1.234,56');
    expect(englishValue).toContain('1,234.56');
  });
});
