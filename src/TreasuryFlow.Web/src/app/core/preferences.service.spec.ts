import { TestBed } from '@angular/core/testing';
import { PreferencesService } from './preferences.service';

describe('PreferencesService', () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('treasuryflow.theme', 'light');
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.lang = 'pt-BR';
  });

  it('should persist and apply the selected locale', () => {
    const service = TestBed.inject(PreferencesService);

    service.setLocale('en-US');

    expect(service.locale()).toBe('en-US');
    expect(document.documentElement.lang).toBe('en-US');
    expect(localStorage.getItem('treasuryflow.locale')).toBe('en-US');
  });

  it('should persist and apply the selected theme', () => {
    const service = TestBed.inject(PreferencesService);

    service.toggleTheme();

    expect(service.theme()).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(localStorage.getItem('treasuryflow.theme')).toBe('dark');
  });
});
