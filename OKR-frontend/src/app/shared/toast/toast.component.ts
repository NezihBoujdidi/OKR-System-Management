import { Component } from '@angular/core';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-toast',
  template: `
    <div class="fixed bottom-6 right-6 z-50 flex flex-col gap-2">
      <div *ngFor="let toast of toasts$ | async"
           class="flex items-center gap-3 min-w-[300px] p-4 rounded-lg shadow-lg transform transition-all duration-300 ease-in-out"
           [ngClass]="{
             'bg-green-500': toast.type === 'success',
             'bg-red-500': toast.type === 'error',
             'bg-blue-500': toast.type === 'info'
           }">
        <div class="flex-1">
          <p class="text-white text-sm font-medium">{{ toast.message }}</p>
        </div>
        <button (click)="removeToast(toast.id)" 
                class="text-white/80 hover:text-white transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>
      </div>
    </div>
  `
})
export class ToastComponent {
  toasts$ = this.toastService.toasts$;

  constructor(private toastService: ToastService) {}

  removeToast(id: number) {
    this.toastService.remove(id);
  }
} 