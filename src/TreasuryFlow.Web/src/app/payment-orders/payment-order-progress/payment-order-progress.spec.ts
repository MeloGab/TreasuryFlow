import { TestBed } from '@angular/core/testing';
import { PaymentOrderProgress } from './payment-order-progress';

describe('PaymentOrderProgress', () => {
  beforeEach(async () => {
    localStorage.clear();
    localStorage.setItem('treasuryflow.theme', 'light');

    await TestBed.configureTestingModule({
      imports: [PaymentOrderProgress],
    }).compileComponents();
  });

  afterEach(() => localStorage.clear());

  it('should mark previous steps as complete and Processing as active', () => {
    const fixture = TestBed.createComponent(PaymentOrderProgress);
    fixture.componentRef.setInput('status', 'Processing');
    fixture.detectChanges();

    const states = Array.from(
      fixture.nativeElement.querySelectorAll('.progress li') as NodeListOf<HTMLElement>,
    ).map((item) => item.dataset['state']);

    expect(states).toEqual(['complete', 'complete', 'active', 'upcoming']);
    expect(fixture.nativeElement.querySelector('[aria-current="step"]')?.textContent).toContain(
      'Em processamento',
    );
  });

  it('should represent a failed processing without marking Completed', () => {
    const fixture = TestBed.createComponent(PaymentOrderProgress);
    fixture.componentRef.setInput('status', 'Failed');
    fixture.detectChanges();

    const states = Array.from(
      fixture.nativeElement.querySelectorAll('.progress li') as NodeListOf<HTMLElement>,
    ).map((item) => item.dataset['state']);

    expect(states).toEqual(['complete', 'complete', 'failed', 'upcoming']);
  });

  it('should not invent completed steps for a cancelled order', () => {
    const fixture = TestBed.createComponent(PaymentOrderProgress);
    fixture.componentRef.setInput('status', 'Cancelled');
    fixture.detectChanges();

    const states = Array.from(
      fixture.nativeElement.querySelectorAll('.progress li') as NodeListOf<HTMLElement>,
    ).map((item) => item.dataset['state']);

    expect(states).toEqual(['halted', 'halted', 'halted', 'halted']);
  });
});
