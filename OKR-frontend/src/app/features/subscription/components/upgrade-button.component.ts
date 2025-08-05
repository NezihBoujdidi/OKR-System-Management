import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-upgrade-button',
  template: `
    <button 
      [class]="getButtonClasses()"
      (click)="navigateToUpgrade()">
      <span class="flex items-center">
        <svg *ngIf="showIcon" class="w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
        </svg>
        {{ label }}
      </span>
    </button>
  `
})
export class UpgradeButtonComponent {
  @Input() variant: 'primary' | 'secondary' | 'text' = 'primary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() label: string = 'Upgrade to Pro';
  @Input() showIcon: boolean = true;

  constructor(private router: Router) {}

  navigateToUpgrade(): void {
    this.router.navigate(['/subscription/upgrade']);
  }

  getButtonClasses(): string {
    const baseClasses = 'font-medium rounded transition-colors';
    
    // Size variations
    const sizeClasses = {
      'sm': 'px-3 py-1.5 text-sm',
      'md': 'px-4 py-2 text-sm',
      'lg': 'px-6 py-3 text-base'
    };
    
    // Variant variations
    const variantClasses = {
      'primary': 'bg-accent text-black hover:bg-accent-hover',
      'secondary': 'bg-white text-primary border border-gray-300 hover:bg-gray-50 hover:text-accent',
      'text': 'text-accent hover:text-accent-hover underline'
    };
    
    return `${baseClasses} ${sizeClasses[this.size]} ${variantClasses[this.variant]}`;
  }
} 