import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';
import { TableConfig } from '../../interfaces/table.interface';

type TableSource = 'employees' | 'users' | 'organization' | 'default';

@Component({
  selector: 'app-table',
  templateUrl: './table.component.html'
})
export class TableComponent {
  @Input() config!: TableConfig;
  @Input() actionTemplate?: TemplateRef<any>;
  @Output() rowClick = new EventEmitter<any>();

  currentPage = 1;
  itemsPerPage = 10;

  ngOnInit() {
    if (this.config.itemsPerPage) {
      this.itemsPerPage = this.config.itemsPerPage;
    }
  }

  get totalPages(): number {
    return Math.ceil(this.config.data.length / this.itemsPerPage);
  }

  get paginatedData(): any[] {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.config.data.slice(startIndex, startIndex + this.itemsPerPage);
  }

  onPageChange(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;
    let startPage = Math.max(1, this.currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(this.totalPages, startPage + maxVisiblePages - 1);

    // Adjust start page if we're near the end
    if (endPage - startPage + 1 < maxVisiblePages) {
      startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }

  getBadgeClass(value: any, columnType?: string): string {
    // Status badges
    if (columnType === 'status') {
      // Handle boolean values for status
      if (typeof value === 'boolean') {
        if (value === true) {
          return 'bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs font-medium';
        } else {
          return 'bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs font-medium';
        }
      }
      
      // Handle string values
      if (value === 'Active' || value === 'Enabled') {
        return 'bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs font-medium';
      } else if (value === 'Inactive' || value === 'Disabled') {
        return 'bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs font-medium';
      }
    }
    
    // Role badges
    if (columnType === 'role') {
      // For employees component
      if (this.config.source === 'employees') {
        return 'bg-accent text-primary px-2 py-1 rounded-full text-xs font-medium';
      }
      // For users component
      return 'bg-indigo-100 text-indigo-800 px-2 py-1 rounded-full text-xs font-medium';
    }
    
    // Default case
    return 'bg-gray-100 text-gray-800 px-2 py-1 rounded-full text-xs font-medium';
  }

  getEmptyStateMessage(): { message: string; description: string } {
    const source = (this.config.source || 'default') as TableSource;
    
    const messages: Record<TableSource, { message: string; description: string }> = {
      'employees': {
        message: 'No employees found',
        description: 'Try adjusting your search or filter criteria'
      },
      'users': {
        message: 'No users found',
        description: 'Try adjusting your search or filter criteria'
      },
      'organization': {
        message: 'No organizations found',
        description: 'Try adjusting your search or filter criteria'
      },
      'default': {
        message: 'No items found',
        description: 'Try adjusting your search or filter criteria'
      }
    };

    return messages[source];
  }
} 