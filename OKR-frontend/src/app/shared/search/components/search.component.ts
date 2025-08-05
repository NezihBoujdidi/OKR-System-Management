import { Component, Output, EventEmitter, Input } from '@angular/core';

@Component({
  selector: 'app-search',
  templateUrl: './search.component.html'
})
export class SearchComponent {
  @Input() placeholder: string = 'Search...';
  @Output() onSearch = new EventEmitter<string>();
  searchQuery: string = '';

  search(event: Event): void {
    const query = (event.target as HTMLInputElement).value;
    this.onSearch.emit(query);
  }
} 