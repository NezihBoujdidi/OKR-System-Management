import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';
import { GridConfig } from '../../interfaces/grid.interface';

@Component({
  selector: 'app-grid',
  templateUrl: './grid.component.html'
})
export class GridComponent {
  @Input() config!: GridConfig;
  @Input() actionTemplate?: TemplateRef<any>;
  @Output() itemClick = new EventEmitter<any>();

  currentPage = 1;
  itemsPerPage = 12;

  constructor() {}

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

    if (endPage - startPage + 1 < maxVisiblePages) {
      startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }
} 