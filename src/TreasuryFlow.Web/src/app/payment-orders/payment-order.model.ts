export type PaymentOrderStatus =
  'Draft' | 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';

export interface PaymentOrder {
  id: string;
  description: string;
  amount: number;
  currency: string;
  beneficiary: string;
  status: PaymentOrderStatus;
  createdAt: string;
  processedAt: string | null;
}

export interface SavePaymentOrderRequest {
  description: string;
  amount: number;
  currency: string;
  beneficiary: string;
}

export interface CreatePaymentOrderResponse {
  id: string;
}
