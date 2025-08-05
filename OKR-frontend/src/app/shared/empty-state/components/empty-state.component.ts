import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="flex flex-col items-center justify-center p-8">
      <div class="mb-4">
        <svg class="w-16 h-16 text-gray-300" viewBox="0 0 24 24" fill="none" stroke="currentColor">
          <path [attr.d]="getIconPath()" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/>
        </svg>
      </div>
      <h3 class="text-lg font-medium text-gray-900">{{message || title || 'No items found'}}</h3>
      <p class="mt-2 text-sm text-gray-500">{{description}}</p>
      <button *ngIf="buttonText" 
              (click)="buttonClick.emit()"
              class="mt-4 px-4 py-2 text-sm font-medium text-white bg-primary rounded-lg hover:bg-primary/90">
        {{buttonText}}
      </button>
    </div>
  `
})
export class EmptyStateComponent {
  @Input() icon: 'search' | 'empty' | 'users' | 'inbox' | 'session' |'organization' = 'empty';
  @Input() title: string = '';
  @Input() message: string = ''; // For backward compatibility
  @Input() description: string = '';
  @Input() buttonText?: string;
  @Output() buttonClick = new EventEmitter<void>();

  private iconPaths = {
    search: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z',
    empty: 'M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4',
    users: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
    inbox: 'M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4',
    organization: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
    session: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4'
  };

  getIconPath(): string {
    return this.iconPaths[this.icon] || this.iconPaths.empty;
  }
} 