import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-notification',
  template: `
    <div *ngIf="show" 
         class="fixed top-4 right-4 flex items-center p-4 mb-4 text-sm rounded-lg shadow-lg z-50"
         [ngClass]="typeClasses[type]"
         role="alert">
      <div class="inline-flex items-center justify-center flex-shrink-0 w-8 h-8 rounded-lg"
           [ngClass]="iconBackgroundClasses[type]">
        <svg class="w-5 h-5" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20">
          <path *ngIf="type === 'success'" d="M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5Zm3.707 8.207-4 4a1 1 0 0 1-1.414 0l-2-2a1 1 0 0 1 1.414-1.414L9 10.586l3.293-3.293a1 1 0 0 1 1.414 1.414Z"/>
          <path *ngIf="type === 'error'" d="M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5Zm3.5 11.793-2.793 2.793a1 1 0 0 1-1.414 0L6.5 12.293a1 1 0 0 1 0-1.414L9.293 8.086a1 1 0 0 1 1.414 0l2.793 2.793a1 1 0 0 1 0 1.414Z"/>
        </svg>
      </div>
      <div class="ml-3 text-sm font-medium">{{ message }}</div>
      <button type="button" 
              class="ml-auto -mx-1.5 -my-1.5 rounded-lg focus:ring-2 p-1.5 inline-flex items-center justify-center h-8 w-8"
              [ngClass]="buttonClasses[type]"
              (click)="close()">
        <span class="sr-only">Close</span>
        <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 14 14">
          <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m1 1 6 6m0 0 6 6M7 7l6-6M7 7l-6 6"/>
        </svg>
      </button>
    </div>
  `
})
export class NotificationComponent {
  @Input() show: boolean = false;
  @Input() message: string = '';
  @Input() type: 'success' | 'error' = 'success';
  @Output() closeNotification = new EventEmitter<void>();

  typeClasses = {
    success: 'text-green-800 bg-green-50 dark:bg-gray-800 dark:text-green-400',
    error: 'text-red-800 bg-red-50 dark:bg-gray-800 dark:text-red-400'
  };

  iconBackgroundClasses = {
    success: 'text-green-500 bg-green-100 dark:bg-green-800 dark:text-green-200',
    error: 'text-red-500 bg-red-100 dark:bg-red-800 dark:text-red-200'
  };

  buttonClasses = {
    success: 'bg-green-50 text-green-500 hover:bg-green-100 focus:ring-green-400 dark:bg-gray-800 dark:text-green-400 dark:hover:bg-gray-700',
    error: 'bg-red-50 text-red-500 hover:bg-red-100 focus:ring-red-400 dark:bg-gray-800 dark:text-red-400 dark:hover:bg-gray-700'
  };

  close() {
    this.closeNotification.emit();
  }
} 