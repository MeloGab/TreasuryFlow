import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';

export type AppLocale = 'pt-BR' | 'en-US';
export type ColorTheme = 'light' | 'dark';

const localeStorageKey = 'treasuryflow.locale';
const themeStorageKey = 'treasuryflow.theme';

@Injectable({ providedIn: 'root' })
export class PreferencesService {
  private readonly document = inject(DOCUMENT);

  readonly locale = signal<AppLocale>(this.readLocale());
  readonly theme = signal<ColorTheme>(this.readTheme());

  constructor() {
    this.applyLocale(this.locale());
    this.applyTheme(this.theme());
  }

  setLocale(locale: AppLocale): void {
    this.locale.set(locale);
    this.applyLocale(locale);
    this.writePreference(localeStorageKey, locale);
  }

  toggleTheme(): void {
    const theme: ColorTheme = this.theme() === 'light' ? 'dark' : 'light';
    this.theme.set(theme);
    this.applyTheme(theme);
    this.writePreference(themeStorageKey, theme);
  }

  private readLocale(): AppLocale {
    const storedLocale = this.readPreference(localeStorageKey);
    return storedLocale === 'en-US' ? 'en-US' : 'pt-BR';
  }

  private readTheme(): ColorTheme {
    const storedTheme = this.readPreference(themeStorageKey);

    if (storedTheme === 'light' || storedTheme === 'dark') {
      return storedTheme;
    }

    return this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
  }

  private applyLocale(locale: AppLocale): void {
    this.document.documentElement.lang = locale;
  }

  private applyTheme(theme: ColorTheme): void {
    this.document.documentElement.dataset['theme'] = theme;
    this.document.documentElement.style.colorScheme = theme;
  }

  private readPreference(key: string): string | null {
    try {
      return this.document.defaultView?.localStorage.getItem(key) ?? null;
    } catch {
      return null;
    }
  }

  private writePreference(key: string, value: string): void {
    try {
      this.document.defaultView?.localStorage.setItem(key, value);
    } catch {
      // The preference still works for the current session when storage is unavailable.
    }
  }
}
