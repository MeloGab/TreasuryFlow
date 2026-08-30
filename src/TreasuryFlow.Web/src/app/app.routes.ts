import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./home/home').then((module) => module.Home),
    title: 'TreasuryFlow',
  },
  {
    path: 'payment-orders/new',
    loadComponent: () =>
      import('./payment-orders/payment-order-form/payment-order-form').then(
        (module) => module.PaymentOrderForm,
      ),
    title: 'Nova ordem | TreasuryFlow',
  },
  {
    path: 'payment-orders/:id/edit',
    loadComponent: () =>
      import('./payment-orders/payment-order-form/payment-order-form').then(
        (module) => module.PaymentOrderForm,
      ),
    title: 'Editar ordem | TreasuryFlow',
  },
  {
    path: 'payment-orders/:id',
    loadComponent: () =>
      import('./payment-orders/payment-order-details/payment-order-details').then(
        (module) => module.PaymentOrderDetails,
      ),
    title: 'Ordem de pagamento | TreasuryFlow',
  },
  { path: '**', redirectTo: '' },
];
