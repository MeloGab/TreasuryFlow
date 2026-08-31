import { Component, inject, input } from '@angular/core';
import { I18nService } from '../../core/i18n.service';
import { PaymentOrderStatus } from '../payment-order.model';

type ProgressState = 'complete' | 'active' | 'upcoming' | 'failed' | 'halted';

const progressSteps = ['Draft', 'Pending', 'Processing', 'Completed'] as const;
type ProgressStep = (typeof progressSteps)[number];

@Component({
  selector: 'app-payment-order-progress',
  templateUrl: './payment-order-progress.html',
  styleUrl: './payment-order-progress.scss',
})
export class PaymentOrderProgress {
  readonly status = input.required<PaymentOrderStatus>();

  protected readonly i18n = inject(I18nService);
  protected readonly steps = progressSteps;

  protected state(step: ProgressStep): ProgressState {
    const orderStatus = this.status();
    const stepIndex = progressSteps.indexOf(step);

    if (orderStatus === 'Cancelled') {
      return 'halted';
    }

    if (orderStatus === 'Failed') {
      if (stepIndex < 2) {
        return 'complete';
      }

      return step === 'Processing' ? 'failed' : 'upcoming';
    }

    const currentIndex = progressSteps.indexOf(orderStatus);

    if (stepIndex < currentIndex) {
      return 'complete';
    }

    return stepIndex === currentIndex ? 'active' : 'upcoming';
  }

  protected symbol(state: ProgressState): string {
    switch (state) {
      case 'complete':
        return '✓';
      case 'active':
        return '●';
      case 'failed':
        return '!';
      case 'halted':
        return '—';
      default:
        return '○';
    }
  }
}
