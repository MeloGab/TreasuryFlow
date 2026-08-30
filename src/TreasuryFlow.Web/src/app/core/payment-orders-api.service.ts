import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreatePaymentOrderResponse,
  PaymentOrder,
  SavePaymentOrderRequest,
} from '../payment-orders/payment-order.model';

@Injectable({ providedIn: 'root' })
export class PaymentOrdersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/payment-orders';

  getById(id: string): Observable<PaymentOrder> {
    return this.http.get<PaymentOrder>(`${this.baseUrl}/${id}`);
  }

  create(request: SavePaymentOrderRequest): Observable<CreatePaymentOrderResponse> {
    return this.http.post<CreatePaymentOrderResponse>(this.baseUrl, request);
  }

  update(id: string, request: SavePaymentOrderRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  submit(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/submit`, null);
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, null);
  }
}
