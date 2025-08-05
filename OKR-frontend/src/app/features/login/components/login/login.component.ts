import { Component, OnInit, Injector } from '@angular/core';
import { Router, Navigation } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RoleType } from '../../../../models/role-type.enum';
import { AuthStateService } from '../../../../services/auth-state.service';
import { SupabaseAuthService } from '../../../../services/supabase-auth.service';
import { UserService } from '../../../../services/user.service';
import { AuthResponse } from '@supabase/supabase-js';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from '../../../../interceptors/auth.interceptor';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: []
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  errorMessage: string = '';
  showPassword = false;
  isLoading = true;
  showLoadingOverlay = false;
  loadingMessage = 'Logging in...';
  comingFromLogout = false; // Flag to track if coming from logout

  constructor(
    private authService: AuthService,
    private router: Router,
    private fb: FormBuilder,
    private authState: AuthStateService,
    private supabaseAuth: SupabaseAuthService,
    private userService: UserService,
    private injector: Injector
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      rememberMe: [false]
    });
    
    // Check if coming from logout by looking at router state
    const navigation = this.router.getCurrentNavigation();
    if (navigation && navigation.extras.state) {
      this.comingFromLogout = navigation.extras.state['fromLogout'] === true;
      console.log('Login component initialized from logout:', this.comingFromLogout);
    }
  }

  ngOnInit() {
    // Skip loading animation if coming from logout for better UX
    if (this.comingFromLogout) {
      console.log('Login component loaded from logout - skipping loading animation');
      this.isLoading = false;
      
      // Clear any potential blocking flags to ensure a clean state
      window.forceBlockAllRequests = false;
      
      // Remove any logout-related classes from the body
      document.body.classList.remove('logout-in-progress');
      document.body.classList.remove('cursor-wait');
      
      // Reset HTTP request blocking in any interceptors
      try {
        // Directly access HTTP_INTERCEPTORS using the injector
        const interceptors = this.injector.get(HTTP_INTERCEPTORS, []);
        if (interceptors) {
          // Explicitly cast to any to avoid type issues
          const authInterceptor = interceptors.find((i: any) => i instanceof AuthInterceptor);
          if (authInterceptor) {
            (authInterceptor as AuthInterceptor).forceBlockAllRequests = false;
            console.log('Interceptor block flags reset in login component');
          }
        }
      } catch (error) {
        console.error('Error resetting HTTP interceptor flags:', error);
      }
      
      // Make sure auth state is properly reset
      this.authState.finishLogout();
    } else {
      // Simulate loading time
      setTimeout(() => {
        this.isLoading = false;
      }, 1500); // Adjust time as needed
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      // Mark all fields as touched to trigger validation messages
      Object.keys(this.loginForm.controls).forEach(key => {
        const control = this.loginForm.get(key);
        control?.markAsTouched();
      });

      if (this.loginForm.get('email')?.hasError('required')) {
        this.errorMessage = 'Email is required';
        return;
      }
      if (this.loginForm.get('email')?.hasError('email')) {
        this.errorMessage = 'Please enter a valid email address';
        return;
      }
      if (this.loginForm.get('password')?.hasError('required')) {
        this.errorMessage = 'Password is required';
        return;
      }
      return;
    }

    console.log('DEBUG-NAV: Login form submitted');
    this.showLoadingOverlay = true;
    this.loadingMessage = 'Logging in...';
    this.errorMessage = '';
    
    const { email, password } = this.loginForm.value;

    this.supabaseAuth.login(email, password).subscribe({
      next: (role) => {
        console.log(`DEBUG-NAV: Login successful, user role: ${role}`);
        this.showLoadingOverlay = false;
        
        // Navigate based on role
        let targetRoute = '';
        switch (role) {
          case RoleType.SuperAdmin:
            targetRoute = '/home';
            break;
          case RoleType.OrganizationAdmin:
            targetRoute = '/home/organizationAdmin';
            break;
          case RoleType.TeamManager:
            targetRoute = '/home/teamManager';
            break;
          case RoleType.Collaborator:
            targetRoute = '/home/collaborator';
            break;
          default:
            this.errorMessage = 'Invalid user role';
            console.error(`DEBUG-NAV: Invalid user role: ${role}`);
            return;
        }
        
        console.log(`DEBUG-NAV: Navigating to ${targetRoute} based on role: ${role}`);
        this.router.navigate([targetRoute]).then(success => {
          console.log(`DEBUG-NAV: Navigation result: ${success ? 'success' : 'failed'} to ${targetRoute}`);
        }).catch(error => {
          console.error(`DEBUG-NAV: Navigation error to ${targetRoute}:`, error);
        });
      },
      error: (error) => {
        console.error('DEBUG-NAV: Login error:', error);
        this.showLoadingOverlay = false;
        this.errorMessage = error.message || 'Invalid credentials';
      }
    });
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }
}
