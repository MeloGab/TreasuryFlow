import { TestBed } from '@angular/core/testing';
import { ConfirmationDialog } from './confirmation-dialog';

describe('ConfirmationDialog', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmationDialog],
    }).compileComponents();
  });

  it('should emit confirmation from the destructive action', () => {
    const fixture = TestBed.createComponent(ConfirmationDialog);
    fixture.componentRef.setInput('title', 'Cancel order');
    fixture.componentRef.setInput('message', 'Confirm cancellation');
    fixture.componentRef.setInput('confirmLabel', 'Cancel order');
    fixture.componentRef.setInput('cancelLabel', 'Keep order');
    const confirmed = vi.fn();
    fixture.componentInstance.confirmed.subscribe(confirmed);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    buttons[1].click();

    expect(confirmed).toHaveBeenCalledOnce();
  });

  it('should emit cancellation when Escape is pressed', () => {
    const fixture = TestBed.createComponent(ConfirmationDialog);
    fixture.componentRef.setInput('title', 'Cancel order');
    fixture.componentRef.setInput('message', 'Confirm cancellation');
    fixture.componentRef.setInput('confirmLabel', 'Cancel order');
    fixture.componentRef.setInput('cancelLabel', 'Keep order');
    const cancelled = vi.fn();
    fixture.componentInstance.cancelled.subscribe(cancelled);
    fixture.detectChanges();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(cancelled).toHaveBeenCalledOnce();
  });
});
