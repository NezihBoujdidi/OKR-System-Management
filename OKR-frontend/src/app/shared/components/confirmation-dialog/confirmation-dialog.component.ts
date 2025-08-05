import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirmation-dialog',
  templateUrl: './confirmation-dialog.component.html'
})
export class ConfirmationDialogComponent {
  @Input() isOpen: boolean = false;
  @Input() title: string = 'Confirm Action';
  @Input() message: string = 'Are you sure you want to proceed?';
  @Input() confirmButtonText: string = 'Confirm';
  @Input() cancelButtonText: string = 'Cancel';
  @Input() confirmButtonClass: string = 'bg-red-600 hover:bg-red-700';
  @Input() isLoading: boolean = false;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onConfirm(): void {
    if (!this.isLoading) {
      this.confirm.emit();
    }
  }

  onCancel(): void {
    if (!this.isLoading) {
      this.cancel.emit();
    }
  }
} 