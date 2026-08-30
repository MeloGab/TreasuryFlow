import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, map } from 'rxjs';
import { getApiErrorMessage } from '../../core/api-error';
import { PaymentOrdersApiService } from '../../core/payment-orders-api.service';
import { SavePaymentOrderRequest } from '../payment-order.model';

@Component({
  selector: 'app-payment-order-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './payment-order-form.html',
  styleUrl: './payment-order-form.scss',
})
export class PaymentOrderForm implements OnInit {
  private readonly api = inject(PaymentOrdersApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly id = this.route.snapshot.paramMap.get('id');

  protected readonly isEdit = this.id !== null;
  protected readonly loading = signal(this.isEdit);
  protected readonly saving = signal(false);
  protected readonly editable = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly currencies = ['BRL', 'USD', 'EUR'];

  protected readonly form = new FormGroup({
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    amount: new FormControl<number | null>(null, {
      validators: [
        Validators.required,
        Validators.min(0.01),
        Validators.pattern(/^\d+(\.\d{1,2})?$/),
      ],
    }),
    currency: new FormControl('BRL', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    beneficiary: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  ngOnInit(): void {
    if (this.id) {
      this.loadPaymentOrder(this.id);
    }
  }

  protected save(): void {
    if (this.form.invalid || !this.editable()) {
      this.form.markAllAsTouched();
      return;
    }

    const request: SavePaymentOrderRequest = {
      description: this.form.controls.description.value.trim(),
      amount: Number(this.form.controls.amount.value),
      currency: this.form.controls.currency.value,
      beneficiary: this.form.controls.beneficiary.value.trim(),
    };

    this.saving.set(true);
    this.error.set(null);

    const operation = this.id
      ? this.api.update(this.id, request).pipe(map(() => this.id!))
      : this.api.create(request).pipe(map((response) => response.id));

    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (paymentOrderId) => {
        void this.router.navigate(['/payment-orders', paymentOrderId]);
      },
      error: (error: unknown) => {
        this.error.set(getApiErrorMessage(error, 'Não foi possível salvar a ordem de pagamento.'));
      },
    });
  }

  private loadPaymentOrder(id: string): void {
    this.api
      .getById(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (paymentOrder) => {
          this.form.patchValue({
            description: paymentOrder.description,
            amount: paymentOrder.amount,
            currency: paymentOrder.currency,
            beneficiary: paymentOrder.beneficiary,
          });

          if (paymentOrder.status !== 'Draft') {
            this.editable.set(false);
            this.form.disable();
            this.error.set('Somente ordens em rascunho podem ser editadas.');
          }
        },
        error: (error: unknown) => {
          this.editable.set(false);
          this.error.set(
            getApiErrorMessage(error, 'Não foi possível carregar a ordem de pagamento.'),
          );
        },
      });
  }
}
