import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

@Component({
  selector: 'app-home',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly router = inject(Router);

  protected readonly form = new FormGroup({
    paymentOrderId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(guidPattern)],
    }),
  });

  protected get paymentOrderId(): FormControl<string> {
    return this.form.controls.paymentOrderId;
  }

  protected findPaymentOrder(): void {
    const normalizedId = this.paymentOrderId.value.trim();
    this.paymentOrderId.setValue(normalizedId);

    if (this.paymentOrderId.invalid) {
      this.paymentOrderId.markAsTouched();
      return;
    }

    void this.router.navigate(['/payment-orders', normalizedId]);
  }
}
