import { inject } from '@angular/core';
import { ResolveFn, Routes } from '@angular/router';
import { I18nService, TranslationKey } from './core/i18n.service';

const localizedTitle: ResolveFn<string> = (route) =>
  inject(I18nService).t(route.data['titleKey'] as TranslationKey);

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./home/home').then((module) => module.Home),
    title: localizedTitle,
    data: { titleKey: 'route.home' satisfies TranslationKey },
  },
  {
    path: 'payment-orders/new',
    loadComponent: () =>
      import('./payment-orders/payment-order-form/payment-order-form').then(
        (module) => module.PaymentOrderForm,
      ),
    title: localizedTitle,
    data: { titleKey: 'route.newOrder' satisfies TranslationKey },
  },
  {
    path: 'payment-orders/:id/edit',
    loadComponent: () =>
      import('./payment-orders/payment-order-form/payment-order-form').then(
        (module) => module.PaymentOrderForm,
      ),
    title: localizedTitle,
    data: { titleKey: 'route.editOrder' satisfies TranslationKey },
  },
  {
    path: 'payment-orders/:id',
    loadComponent: () =>
      import('./payment-orders/payment-order-details/payment-order-details').then(
        (module) => module.PaymentOrderDetails,
      ),
    title: localizedTitle,
    data: { titleKey: 'route.orderDetails' satisfies TranslationKey },
  },
  { path: '**', redirectTo: '' },
];
