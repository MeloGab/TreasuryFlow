import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PaymentOrder, SavePaymentOrderRequest } from '../payment-orders/payment-order.model';
import { PaymentOrdersApiService } from './payment-orders-api.service';

describe('PaymentOrdersApiService', () => {
  let service: PaymentOrdersApiService;
  let http: HttpTestingController;

  const id = '1d954c4a-660f-454a-97ce-d10c46b662e1';
  const request: SavePaymentOrderRequest = {
    description: 'Pagamento de fornecedor',
    amount: 1250.5,
    currency: 'BRL',
    beneficiary: 'Fornecedor Exemplo',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PaymentOrdersApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should get a payment order by id', () => {
    const response: PaymentOrder = {
      id,
      ...request,
      status: 'Draft',
      createdAt: '2026-08-30T00:00:00Z',
      processedAt: null,
    };

    service.getById(id).subscribe((result) => expect(result).toEqual(response));
    const pendingRequest = http.expectOne(`/api/payment-orders/${id}`);
    expect(pendingRequest.request.method).toBe('GET');
    pendingRequest.flush(response);
  });

  it('should create a payment order', () => {
    service.create(request).subscribe((result) => expect(result.id).toBe(id));
    const pendingRequest = http.expectOne('/api/payment-orders');
    expect(pendingRequest.request.method).toBe('POST');
    expect(pendingRequest.request.body).toEqual(request);
    pendingRequest.flush({ id });
  });

  it('should update a payment order', () => {
    service.update(id, request).subscribe();
    const pendingRequest = http.expectOne(`/api/payment-orders/${id}`);
    expect(pendingRequest.request.method).toBe('PUT');
    expect(pendingRequest.request.body).toEqual(request);
    pendingRequest.flush(null);
  });

  it('should submit a payment order', () => {
    service.submit(id).subscribe();
    const pendingRequest = http.expectOne(`/api/payment-orders/${id}/submit`);
    expect(pendingRequest.request.method).toBe('POST');
    expect(pendingRequest.request.body).toBeNull();
    pendingRequest.flush(null);
  });

  it('should cancel a payment order', () => {
    service.cancel(id).subscribe();
    const pendingRequest = http.expectOne(`/api/payment-orders/${id}/cancel`);
    expect(pendingRequest.request.method).toBe('POST');
    expect(pendingRequest.request.body).toBeNull();
    pendingRequest.flush(null);
  });
});
