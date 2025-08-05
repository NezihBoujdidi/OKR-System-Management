import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-sort-button',
  templateUrl: './sort-button.component.html'
})
export class SortButtonComponent {
  @Input() showSorting = false;
  @Input() currentSort: { field: string; direction: string } = { field: '', direction: 'asc' };
  @Input() sortOptions: { label: string; value: string }[] = [];
  @Output() toggleSorting = new EventEmitter<void>();
  @Output() closeSorting = new EventEmitter<void>();
  @Output() sortChange = new EventEmitter<{ field: string; direction: string }>();

  onToggleSorting(event: Event) {
    event.stopPropagation();
    this.toggleSorting.emit();
  }

  onClickOutside() {
    if (this.showSorting) {
      this.closeSorting.emit();
    }
  }

  applySorting(field: string) {
    const direction = this.currentSort.field === field && this.currentSort.direction === 'asc' ? 'desc' : 'asc';
    this.sortChange.emit({ field, direction });
  }

  clearSort() {
    this.sortChange.emit({ field: '', direction: 'asc' });
    this.showSorting = false;
  }

  getSortIcon(field: string): 'none' | 'asc' | 'desc' {
    if (this.currentSort.field !== field) return 'none';
    return this.currentSort.direction as 'asc' | 'desc';
  }
} 