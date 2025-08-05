import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface Toast {
  message: string;
  type: 'success' | 'error' | 'info';
  id: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toasts: Toast[] = [];
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  toasts$ = this.toastsSubject.asObservable();

  show(message: string, type: 'success' | 'error' | 'info' = 'info') {
    const toast: Toast = {
      message,
      type,
      id: Date.now()
    };
    
    this.toasts.push(toast);
    this.toastsSubject.next(this.toasts);

    setTimeout(() => {
      this.remove(toast.id);
    }, 3000);
  }
  
  showSuccess(message: string) {
    this.show(message, 'success');
  }
  
  showError(message: string) {
    this.show(message, 'error');
  }
  
  showInfo(message: string) {
    this.show(message, 'info');
  }

  remove(id: number) {
    this.toasts = this.toasts.filter(t => t.id !== id);
    this.toastsSubject.next(this.toasts);
  }
} 