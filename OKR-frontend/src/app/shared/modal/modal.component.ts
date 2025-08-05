import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  standalone: true,
  imports: [CommonModule]
})
export class ModalComponent implements OnChanges {
  @Input() title: string = '';
  @Input() isOpen: boolean = false;
  @Input() useDarkTheme = false;
  @Output() close = new EventEmitter<void>();

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.lockBodyScroll();
      } else {
        this.unlockBodyScroll();
      }
    }
  }

  onClose() {
    this.close.emit();
  }

  private lockBodyScroll(): void {
    document.body.style.overflow = 'hidden';
  }

  private unlockBodyScroll(): void {
    document.body.style.overflow = 'auto';
  }
}