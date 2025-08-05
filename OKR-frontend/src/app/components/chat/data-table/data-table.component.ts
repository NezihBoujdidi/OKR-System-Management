import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { TableColumn, TableConfig } from './table.models';

@Component({
  selector: 'app-data-table',
  templateUrl: './data-table.component.html',
  styleUrls: ['./data-table.component.scss']
})
export class DataTableComponent implements OnChanges {
  @Input() data: any[] = [];
  @Input() config: TableConfig = { columns: [] };
  @Input() pageSize: number = 5;
  @Input() currentPage: number = 1;
  @Input() totalItems: number = 0;
  @Input() serverPagination: boolean = false; // Flag to indicate server-side pagination
  
  @Output() rowClick = new EventEmitter<any>();
  @Output() pageChange = new EventEmitter<number>();
  @Output() sortChange = new EventEmitter<{column: string, direction: 'asc' | 'desc'}>();
  @Output() serverPageChange = new EventEmitter<{page: number, pageSize: number}>();

  filteredData: any[] = [];
  displayedData: any[] = [];
  
  searchTerm: string = '';
  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  
  totalPages: number = 0;
  
  // Making Math available for the template
  Math = Math;
  
  constructor() { }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data'] || changes['config']) {
      this.initializeTable();
    }

    if (changes['totalItems'] || changes['pageSize']) {
      if (this.serverPagination) {
        // When using server pagination, calculate total pages based on totalItems
        this.totalPages = Math.ceil(this.totalItems / this.pageSize);
      }
    }
  }

  initializeTable(): void {
    // First ensure filteredData is populated with all data
    this.filteredData = [...this.data];
    
    // Apply default sorting if specified
    if (this.config.defaultSort) {
      this.sortColumn = this.config.defaultSort.column;
      this.sortDirection = this.config.defaultSort.direction;
      this.sortData();
    }
    
    if (this.serverPagination) {
      // For server-side pagination, we already have the correct slice of data
      this.displayedData = this.filteredData;
      // Calculate totalPages based on the total count from server
      this.totalPages = Math.ceil(this.totalItems / this.pageSize);
    } else {
      // For client-side pagination, calculate total pages and update displayed data
      this.calculateTotalPages();
      this.updateDisplayedData();
    }
  }

  calculateTotalPages(): void {
    this.totalPages = Math.ceil(this.filteredData.length / this.pageSize);
  }

  updateDisplayedData(): void {
    if (this.serverPagination) {
      // Server already gave us the correct slice
      this.displayedData = this.filteredData;
    } else {
      // Client-side pagination: slice the data
      const startIndex = (this.currentPage - 1) * this.pageSize;
      const endIndex = Math.min(startIndex + this.pageSize, this.filteredData.length);
      this.displayedData = this.filteredData.slice(startIndex, endIndex);
    }
  }

  onSearch(event: Event): void {
    const searchValue = (event.target as HTMLInputElement).value.toLowerCase();
    this.searchTerm = searchValue;
    
    if (!searchValue) {
      this.filteredData = [...this.data];
    } else {
      this.filteredData = this.data.filter(item => {
        return this.config.searchFields?.some(field => {
          const value = this.getNestedProperty(item, field);
          return value && value.toString().toLowerCase().includes(searchValue);
        });
      });
    }
    
    this.currentPage = 1;

    if (this.serverPagination) {
      // Emit event to inform parent about search with page reset
      this.serverPageChange.emit({ page: 1, pageSize: this.pageSize });
    } else {
      // Handle client-side pagination
      this.calculateTotalPages();
      this.updateDisplayedData();
    }
  }

  getNestedProperty(obj: any, path: string): any {
    if (!obj) return null;
    
    // Handle special cases
    if (path === 'members.length') {
      // Check if members exists in any form (uppercase or lowercase)
      if (obj.members && Array.isArray(obj.members)) {
        return obj.members.length;
      } else if (obj.Members && Array.isArray(obj.Members)) {
        return obj.Members.length;
      } else {
        return 0; // Default to 0 if members array doesn't exist
      }
    }
    
    // Regular path traversal
    return path.split('.').reduce((prev, curr) => prev ? prev[curr] : null, obj);
  }

  onSort(column: TableColumn): void {
    if (!column.sortable) return;
    
    if (this.sortColumn === column.key) {
      // Toggle direction
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column.key;
      this.sortDirection = 'asc';
    }
    
    this.sortData();
    this.updateDisplayedData();
    this.sortChange.emit({ column: this.sortColumn, direction: this.sortDirection });
  }

  sortData(): void {
    this.filteredData.sort((a, b) => {
      const valueA = this.getNestedProperty(a, this.sortColumn);
      const valueB = this.getNestedProperty(b, this.sortColumn);
      
      if (valueA === valueB) return 0;
      
      const comparison = valueA < valueB ? -1 : 1;
      return this.sortDirection === 'asc' ? comparison : -comparison;
    });
  }

  onPageChange(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    
    this.currentPage = page;
    
    if (this.serverPagination) {
      // Emit event to request new page from server
      this.serverPageChange.emit({ page, pageSize: this.pageSize });
    } else {
      // Client-side pagination
      this.updateDisplayedData();
    }
    
    this.pageChange.emit(page);
  }

  onRowClick(row: any): void {
    this.rowClick.emit(row);
  }

  getCellValue(row: any, column: TableColumn): string {
    const value = this.getNestedProperty(row, column.key);
    
    if (column.format) {
      return column.format(value, row);
    }
    
    if (value === null || value === undefined) {
      return '';
    }
    
    if (column.type === 'date' && value) {
      return new Date(value).toLocaleDateString();
    }
    
    return value.toString();
  }

  getPages(): number[] {
    const visiblePages = 5;
    let startPage = Math.max(1, this.currentPage - Math.floor(visiblePages / 2));
    let endPage = startPage + visiblePages - 1;
    
    if (endPage > this.totalPages) {
      endPage = this.totalPages;
      startPage = Math.max(1, endPage - visiblePages + 1);
    }
    
    return Array.from({ length: endPage - startPage + 1 }, (_, i) => startPage + i);
  }
} 