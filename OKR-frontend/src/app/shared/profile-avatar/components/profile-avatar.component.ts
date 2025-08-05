import { Component, Input, ElementRef, HostListener, OnInit, Injector } from '@angular/core';
import { Router } from '@angular/router';
import { UserService } from '../../../services/user.service';
import { SupabaseAuthService } from '../../../services/supabase-auth.service';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from '../../../interceptors/auth.interceptor';

@Component({
  selector: 'app-profile-avatar',
  templateUrl: './profile-avatar.component.html'
})
export class ProfileAvatarComponent implements OnInit {
  @Input() imageUrl?: string;
  @Input() name: string = '';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() showDropdown: boolean = false;
  @Input() currentUser?: { email: string; name?: string; imageUrl?: string };

  isDropdownOpen = false;
  showLoadingOverlay = false;
  loadingMessage = 'Logging out...';

  constructor(
    private elementRef: ElementRef,
    private router: Router,
    private userService: UserService,
    private supabaseService: SupabaseAuthService,
    private injector: Injector // Add injector to access interceptors
  ) {}

  ngOnInit() {
    // If no name is provided but currentUser has name, use it
    if (!this.name && this.currentUser?.name) {
      this.name = this.currentUser.name;
    }
    
    // If no imageUrl is provided but currentUser has imageUrl, use it
    if (!this.imageUrl && this.currentUser?.imageUrl) {
      this.imageUrl = this.currentUser.imageUrl;
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isDropdownOpen = false;
    }
  }

  get initials(): string {
    return this.name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  get sizeClasses(): string {
    switch (this.size) {
      case 'sm': return 'w-8 h-8 text-sm';
      case 'lg': return 'w-12 h-12 text-lg';
      default: return 'w-10 h-10 text-base';
    }
  }

  toggleDropdown(event: Event) {
    event.stopPropagation();
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  viewAccount() {
    this.router.navigate(['/profile']);
    this.isDropdownOpen = false;
  }

  // Method to find and force block all HTTP requests
  private blockAllHttpRequests() {
    try {
      // Get all HTTP interceptors
      const interceptors = this.injector.get(HTTP_INTERCEPTORS, []);
      
      // Find our AuthInterceptor instance
      const authInterceptor = interceptors.find(i => i instanceof AuthInterceptor);
      
      if (authInterceptor) {
        // Set the force block flag
        (authInterceptor as AuthInterceptor).forceBlockAllRequests = true;
        console.log('All HTTP requests forcefully blocked for logout');
      } else {
        console.warn('AuthInterceptor not found - cannot block requests');
      }
    } catch (error) {
      console.error('Error while trying to block HTTP requests:', error);
    }
  }

  logout() {
    this.isDropdownOpen = false;
    
    console.log('ProfileAvatarComponent: STARTING LOGOUT - BLOCK ALL REQUESTS');
    
    // Show loading overlay
    this.showLoadingOverlay = true;
    
    // CRITICAL CHANGE: First disable all Angular change detection by adding a class to body
    document.body.classList.add('logout-in-progress');
    
    // CRITICAL: First block ALL HTTP requests before anything else happens
    // 1. Global flag - block ALL requests immediately
    window.forceBlockAllRequests = true;
    
    // 2. Interceptor flag
    this.blockAllHttpRequests();
    
    // 3. Set cursor to wait
    document.body.classList.add('cursor-wait');
    
    console.log('ProfileAvatarComponent: Initiating logout process...');
    
    // Now that blocks are in place, we can set the logging out flag
    const authState = this.supabaseService['authState']; // Access the authState from the supabaseService
    if (authState) {
      authState.startLogout();
    }
    
    // IMPORTANT: First navigate to login, then perform the actual logout after
    // This prevents any components from making requests when auth state changes
    const navigateToLogin = () => {
      console.log('ProfileAvatarComponent: Navigating to login page...');
      
      // Hide loading overlay before navigation
      this.showLoadingOverlay = false;
      
      // Navigate to login page with replace to prevent back navigation
      this.router.navigate(['/login'], { 
        replaceUrl: true,
        skipLocationChange: false,
        state: { fromLogout: true } // Pass state to indicate this came from logout
      }).then(() => {
        // Final check after navigation to make sure no user data is left
        console.log('ProfileAvatarComponent: Final storage cleanup check after navigation');
        sessionStorage.clear();
        localStorage.clear();
        
        // Reset the force block flag after navigation completes
        this.resetHttpBlocking();
        
        // Remove the logout class
        document.body.classList.remove('logout-in-progress');
        
        // Finish logout to reset the loggingOut flag
        if (authState) {
          authState.finishLogout();
        }
        
        // This helps avoid certain browser caching issues
        if (navigator.serviceWorker && navigator.serviceWorker.controller) {
          navigator.serviceWorker.controller.postMessage({
            type: 'CLEAR_AUTH_DATA'
          });
        }
      });
    };
    
    // Perform the actual logout after a short delay
    // This ensures all components have had time to process the navigation
    setTimeout(() => {
      // Properly subscribe to the logout Observable to ensure it completes
      this.supabaseService.logout().subscribe({
        next: () => {
          console.log('ProfileAvatarComponent: Successfully logged out');
          
          // Remove cursor wait
          document.body.classList.remove('cursor-wait');
          
          // CHANGE: We've already navigated, so just finish up the logout process
        },
        error: (error) => {
          console.error('ProfileAvatarComponent: Logout error:', error);
          
          // Remove cursor wait
          document.body.classList.remove('cursor-wait');
          
          // Even on error, we want to navigate away
        }
      });
    }, 100);
    
    // Navigate immediately, don't wait for logout to complete
    // This prevents any new HTTP requests from being triggered by components
    // responding to auth state changes
    navigateToLogin();
    // Add page reload after a delay to ensure a clean state for the next login
    setTimeout(() => {
      console.log('ProfileAvatarComponent: Reloading page to ensure clean application state');
      window.location.reload();
    }, 3000); // 3 second delay before reload
  }
  
  // Method to reset HTTP blocking after logout is complete
  private resetHttpBlocking() {
    try {
      const interceptors = this.injector.get(HTTP_INTERCEPTORS, []);
      const authInterceptor = interceptors.find(i => i instanceof AuthInterceptor);
      
      if (authInterceptor) {
        (authInterceptor as AuthInterceptor).forceBlockAllRequests = false;
        console.log('HTTP request blocking reset after logout');
      }
    } catch (error) {
      console.error('Error while trying to reset HTTP request blocking:', error);
    }
  }
} 