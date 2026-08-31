import { Component, HostListener, input, output } from '@angular/core';

@Component({
  selector: 'app-confirmation-dialog',
  templateUrl: './confirmation-dialog.html',
  styleUrl: './confirmation-dialog.scss',
})
export class ConfirmationDialog {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input.required<string>();
  readonly cancelLabel = input.required<string>();

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  @HostListener('document:keydown.escape')
  protected closeOnEscape(): void {
    this.cancelled.emit();
  }

  protected confirm(): void {
    this.confirmed.emit();
  }

  protected cancel(): void {
    this.cancelled.emit();
  }
}
