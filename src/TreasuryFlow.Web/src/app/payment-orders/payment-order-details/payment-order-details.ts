import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, Observable } from 'rxjs';
import { getApiErrorMessage } from '../../core/api-error';
import { I18nService } from '../../core/i18n.service';
import { PaymentOrdersApiService } from '../../core/payment-orders-api.service';
import { ConfirmationDialog } from '../../shared/confirmation-dialog/confirmation-dialog';
import { PaymentOrderProgress } from '../payment-order-progress/payment-order-progress';
import { PaymentOrder, PaymentOrderStatus } from '../payment-order.model';

type ConfirmationKind = 'deleteDraft' | 'cancelPending';

@Component({
  selector: 'app-payment-order-details',
  imports: [ConfirmationDialog, PaymentOrderProgress, RouterLink],
  templateUrl: './payment-order-details.html',
  styleUrl: './payment-order-details.scss',
})
export class PaymentOrderDetails implements OnInit {
  private readonly api = inject(PaymentOrdersApiService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly i18n = inject(I18nService);
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';
  private refreshTimer: ReturnType<typeof setTimeout> | undefined;

  protected readonly order = signal<PaymentOrder | null>(null);
  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly confirmation = signal<ConfirmationKind | null>(null);

  constructor() {
    this.destroyRef.onDestroy(() => this.clearRefreshTimer());
  }

  ngOnInit(): void {
    this.loadPaymentOrder();
  }

  protected statusLabel(status: PaymentOrderStatus): string {
    return this.i18n.statusLabel(status);
  }

  protected statusDescription(status: PaymentOrderStatus): string {
    return this.i18n.statusDescription(status);
  }

  protected submit(): void {
    this.runAction(this.api.submit(this.id), this.i18n.t('error.submit'));
  }

  protected deleteDraft(): void {
    // "Excluir" é um conceito de apresentação. A API preserva o histórico
    // financeiro mudando o estado da ordem para Cancelled, sem apagar o
    // registro fisicamente.
    this.confirmation.set('deleteDraft');
  }

  protected cancelPending(): void {
    this.confirmation.set('cancelPending');
  }

  protected closeConfirmation(): void {
    this.confirmation.set(null);
  }

  protected confirmAction(): void {
    const confirmation = this.confirmation();
    this.closeConfirmation();

    if (confirmation === 'deleteDraft') {
      this.runAction(
        this.api.cancel(this.id),
        this.i18n.t('error.deleteDraft'),
        this.i18n.t('error.draftChanged'),
      );
    } else if (confirmation === 'cancelPending') {
      this.runAction(
        this.api.cancel(this.id),
        this.i18n.t('error.cancel'),
        this.i18n.t('error.cancelRace'),
      );
    }
  }

  private runAction(
    operation: Observable<void>,
    fallbackMessage: string,
    conflictMessage = fallbackMessage,
  ): void {
    this.acting.set(true);
    this.error.set(null);
    this.clearRefreshTimer();

    operation.pipe(finalize(() => this.acting.set(false))).subscribe({
      next: () => this.loadPaymentOrder(),
      error: (error: unknown) => {
        const message =
          error instanceof HttpErrorResponse && error.status === 409
            ? conflictMessage
            : getApiErrorMessage(error, fallbackMessage);

        this.error.set(message);
        this.loadPaymentOrder(true);
      },
    });
  }

  private loadPaymentOrder(preserveError = false): void {
    this.clearRefreshTimer();

    this.api
      .getById(this.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (paymentOrder) => {
          this.order.set(paymentOrder);

          if (!preserveError) {
            this.error.set(null);
          }

          this.scheduleRefresh(paymentOrder.status);
        },
        error: (error: unknown) => {
          this.error.set(getApiErrorMessage(error, this.i18n.t('error.load')));
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
