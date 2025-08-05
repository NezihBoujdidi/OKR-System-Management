import { Component, OnInit } from '@angular/core';
import { Router, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { applyLocksPolyfill } from './polyfills/locks-polyfill';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'OKR';
  isLoading = false;
  loadingMessage = 'Loading...';
  
  // Define routes that should show loading overlay
  private loadingRoutes = [
    '/login',
    '/okrs',
    '/signup'
  ];

  constructor(private router: Router) {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        // Only show loading for specific routes
        const shouldShowLoading = this.loadingRoutes.some(route => 
          event.url.startsWith(route)
        );

        if (shouldShowLoading) {
          this.isLoading = true;
          // Set custom messages based on route
          if (event.url.startsWith('/login')) {
            this.loadingMessage = 'Loading...';
          } else if (event.url.startsWith('/okrs')) {
            this.loadingMessage = 'Loading OKRs...';
          } else if (event.url.startsWith('/signup')) {
            this.loadingMessage = 'Loading...';
          }
        }
      }
      
      // Always hide loading when navigation ends
      if (event instanceof NavigationEnd || 
          event instanceof NavigationCancel || 
          event instanceof NavigationError) {
        this.isLoading = false;
      }
    });
  }

  ngOnInit() {
    // Apply polyfill for Navigator.locks API
    applyLocksPolyfill();
  }

  // Helper method to check if current navigation is between the same route with different params
  private isSameRouteNavigation(url: string): boolean {
    const currentUrl = this.router.url.split('?')[0];
    const newUrl = url.split('?')[0];
    return currentUrl === newUrl;
  }
}
