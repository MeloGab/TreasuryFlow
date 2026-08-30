import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, Observable } from 'rxjs';
import { getApiErrorMessage } from '../../core/api-error';
import { PaymentOrdersApiService } from '../../core/payment-orders-api.service';
import { PaymentOrder, PaymentOrderStatus } from '../payment-order.model';

const statusLabels: Record<PaymentOrderStatus, string> = {
  Draft: 'Rascunho',
  Pending: 'Pendente',
  Processing: 'Em processamento',
  Completed: 'Concluída',
  Failed: 'Falhou',
  Cancelled: 'Cancelada',
};

const statusDescriptions: Record<PaymentOrderStatus, string> = {
  Draft: 'Os dados ainda podem ser revisados antes do envio.',
  Pending: 'A ordem aguarda o Worker iniciar o processamento.',
  Processing: 'O processamento financeiro está acontecendo em segundo plano.',
  Completed: 'A ordem foi processada com sucesso.',
  Failed: 'O processamento terminou com falha.',
  Cancelled: 'A ordem foi preservada, mas não seguirá para processamento.',
};

@Component({
  selector: 'app-payment-order-details',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './payment-order-details.html',
  styleUrl: './payment-order-details.scss',
})
export class PaymentOrderDetails implements OnInit {
  private readonly api = inject(PaymentOrdersApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';
  private refreshTimer: ReturnType<typeof setTimeout> | undefined;

  protected readonly order = signal<PaymentOrder | null>(null);
  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.destroyRef.onDestroy(() => this.clearRefreshTimer());
  }

  ngOnInit(): void {
    this.loadPaymentOrder();
  }

  protected statusLabel(status: PaymentOrderStatus): string {
    return statusLabels[status];
  }

  protected statusDescription(status: PaymentOrderStatus): string {
    return statusDescriptions[status];
  }

  protected submit(): void {
    this.runAction(this.api.submit(this.id), 'Não foi possível enviar a ordem para processamento.');
  }

  protected deleteDraft(): void {
    const confirmed = window.confirm(
      'Excluir este rascunho? O registro será mantido como cancelado para fins de rastreabilidade.',
    );

    if (confirmed) {
      this.runAction(this.api.cancel(this.id), 'Não foi possível excluir o rascunho.');
    }
  }

  protected cancelPending(): void {
    const confirmed = window.confirm(
      'Cancelar esta ordem pendente? Ela não seguirá para processamento.',
    );

    if (confirmed) {
      this.runAction(this.api.cancel(this.id), 'Não foi possível cancelar a ordem.');
    }
  }

  protected refresh(): void {
    this.loadPaymentOrder();
  }

  private runAction(operation: Observable<void>, fallbackMessage: string): void {
    this.acting.set(true);
    this.error.set(null);
    this.clearRefreshTimer();

    operation.pipe(finalize(() => this.acting.set(false))).subscribe({
      next: () => this.loadPaymentOrder(),
      error: (error: unknown) => {
        this.error.set(getApiErrorMessage(error, fallbackMessage));
      },
    });
  }

  private loadPaymentOrder(): void {
    this.clearRefreshTimer();

    this.api
      .getById(this.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (paymentOrder) => {
          this.order.set(paymentOrder);
          this.error.set(null);
          this.scheduleRefresh(paymentOrder.status);
        },
        error: (error: unknown) => {
          this.error.set(
            getApiErrorMessage(error, 'Não foi possível carregar a ordem de pagamento.'),
          );
        },
      });
  }

  private scheduleRefresh(status: PaymentOrderStatus): void {
    if (status !== 'Pending' && status !== 'Processing') {
      return;
    }

    this.refreshTimer = setTimeout(() => this.loadPaymentOrder(), 2000);
  }

  private clearRefreshTimer(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = undefined;
    }
  }
}
