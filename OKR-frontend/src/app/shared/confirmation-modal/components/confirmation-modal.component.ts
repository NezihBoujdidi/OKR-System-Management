import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-confirmation-modal',
  templateUrl: './confirmation-modal.component.html'
})
export class ConfirmationModalComponent {
  @Input() isOpen = false;
  @Input() title = 'Confirm Delete';
  @Input() message = 'Are you sure you want to delete this item?';
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onConfirm() {
    this.confirm.emit();
  }

  onCancel() {
    this.cancel.emit();
  }
} 