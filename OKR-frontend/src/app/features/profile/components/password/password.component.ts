import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../../../../services/user.service';
import { User, UserDetailsWithRole } from '../../../../models/user.interface';
import { AuthStateService } from '../../../../services/auth-state.service';
import { SupabaseAuthService } from '../../../../services/supabase-auth.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-password',
  templateUrl: './password.component.html'
})
export class PasswordComponent implements OnInit, OnDestroy {
  currentUser: User | undefined;
  passwordForm: FormGroup;
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private fb: FormBuilder,
    public router: Router,
    private authState: AuthStateService,
    private supabaseAuth: SupabaseAuthService
  ) {
    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[-+_!@#$%^&*.,?])[A-Za-z\d-+_!@#$%^&*.,?]{8,}$/)
      ]],
      confirmPassword: ['', Validators.required]
    }, { validator: this.passwordMatchValidator });

    // Subscribe to value changes to clear errors when password becomes valid
    this.passwordForm.get('newPassword')?.valueChanges.subscribe(value => {
      const control = this.passwordForm.get('newPassword');
      if (control?.valid) {
        this.errorMessage = ''; // Clear error message when password becomes valid
      }
    });
  }

  ngOnInit() {
    // Get the current logged-in user from AuthStateService
    this.authState.getUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(userData => {
        if (userData) {
          // Fetch full user data by ID
          this.userService.getUserById(userData.id).subscribe(user => {
            if (user) {
              this.currentUser = user;
            }
          });
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('newPassword')?.value === g.get('confirmPassword')?.value
      ? null
      : { mismatch: true };
  }

  togglePasswordVisibility(field: 'current' | 'new' | 'confirm') {
    switch (field) {
      case 'current':
        this.showCurrentPassword = !this.showCurrentPassword;
        break;
      case 'new':
        this.showNewPassword = !this.showNewPassword;
        break;
      case 'confirm':
        this.showConfirmPassword = !this.showConfirmPassword;
        break;
    }
  }

  async changePassword() {
    if (this.passwordForm.valid && this.currentUser) {
      this.isSubmitting = true;
      this.errorMessage = '';
      this.successMessage = '';

      const { currentPassword, newPassword } = this.passwordForm.value;

      // Step 1: Re-authenticate user with current password
      this.supabaseAuth.reauthenticate(this.currentUser.email, currentPassword).subscribe({
        next: () => {
          // Step 2: Update the password
          this.supabaseAuth.updateUserPassword(newPassword).subscribe({
            next: () => {
              this.successMessage = 'Password successfully updated';
              this.passwordForm.reset();
              // Wait a brief moment to show the success message before redirecting
              setTimeout(() => {
                this.router.navigate(['/profile']);
              }, 1200);
              this.isSubmitting = false;
            },
            error: (error: any) => {
              this.errorMessage = error.message || 'Failed to update password';
              this.isSubmitting = false;
            }
          });
        },
        error: (error: any) => {
          this.errorMessage = error.message || 'Current password is incorrect';
          this.isSubmitting = false;
        }
      });
    }
  }

  get newPasswordHasError(): boolean {
    const control = this.passwordForm.get('newPassword');
    if (!control) return false;
    
    // Only show error if the field is invalid AND has been touched/modified
    return control.invalid && (control.dirty || control.touched);
  }

  get newPasswordErrors(): string {
    const control = this.passwordForm.get('newPassword');
    if (!control || !control.errors) return ''; // Remove touched check here

    if (control.errors['required']) {
      return 'Password is required';
    }
    if (control.errors['minlength']) {
      return 'Password must be at least 8 characters long';
    }
    if (control.errors['pattern']) {
      return 'Password must contain uppercase, lowercase, number and special character';
    }
    return '';
  }

  get passwordMatchError(): boolean {
    const confirmControl = this.passwordForm.get('confirmPassword');
    return Boolean(this.passwordForm.errors?.['mismatch'] && 
           confirmControl?.touched && confirmControl?.dirty);
  }
} 