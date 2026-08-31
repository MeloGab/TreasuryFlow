import { Title } from '@angular/platform-browser';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { I18nService, TranslationKey } from './core/i18n.service';
import { AppLocale, PreferencesService } from './core/preferences.service';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app-shell.scss',
  templateUrl: './app-shell.html',
})
export class App {
  private readonly router = inject(Router);
  private readonly title = inject(Title);
  protected readonly i18n = inject(I18nService);
  protected readonly preferences = inject(PreferencesService);

  protected changeLocale(event: Event): void {
    this.preferences.setLocale((event.target as HTMLSelectElement).value as AppLocale);
    this.updateDocumentTitle();
  }

  private updateDocumentTitle(): void {
    let route = this.router.routerState.snapshot.root;

    while (route.firstChild) {
      route = route.firstChild;
    }

    const titleKey = route.data['titleKey'] as TranslationKey | undefined;

    if (titleKey) {
      this.title.setTitle(this.i18n.t(titleKey));
    }
  }
}
