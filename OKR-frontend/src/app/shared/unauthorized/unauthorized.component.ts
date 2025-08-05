import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  templateUrl: './unauthorized.component.html'
})
export class UnauthorizedComponent {
  showLoadingOverlay = false;
  loadingMessage = 'Redirecting to dashboard...';
  
  constructor(private router: Router) {}
  
  goHome() {
    this.showLoadingOverlay = true;
    
    // Add a small delay to show the loading animation
    setTimeout(() => {
      this.router.navigate(['/home']);
    }, 800);
  }
} 