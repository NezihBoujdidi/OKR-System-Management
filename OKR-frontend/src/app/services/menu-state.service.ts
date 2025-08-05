import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class MenuStateService {
  private activeMenuId = new BehaviorSubject<string | null>(null);
  activeMenuId$ = this.activeMenuId.asObservable();

  setActiveMenu(menuId: string | null) {
    this.activeMenuId.next(menuId);
  }

  getActiveMenu() {
    return this.activeMenuId.value;
  }
} 