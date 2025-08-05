import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-filter-button',
  templateUrl: './filter-button.component.html'
})
export class FilterButtonComponent {
  @Input() showFilters = false;
  @Input() filtersCount = 0;
  @Output() toggleFilters = new EventEmitter<void>();
  @Output() closeFilters = new EventEmitter<void>();

  onToggleFilters(event: Event) {
    event.stopPropagation(); // Prevent event from bubbling up
    this.toggleFilters.emit();
  }

  onClickOutside() {
    if (this.showFilters) {
      this.closeFilters.emit();
    }
  }
} 