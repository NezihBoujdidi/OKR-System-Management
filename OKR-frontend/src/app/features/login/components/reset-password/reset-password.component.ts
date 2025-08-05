import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SupabaseAuthService } from '../../../../services/supabase-auth.service';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html'
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm: FormGroup;
  isLoading = false;
  showSuccessModal = false;
  errorMessage = '';
  isValidToken = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private supabaseAuth: SupabaseAuthService
  ) {
    this.resetPasswordForm = this.fb.group({
      password: ['', [
        Validators.required, 
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { 
      validators: this.passwordMatchValidator.bind(this)
    });
  }

  ngOnInit() {
    // Check if we have access to the hash fragment from Supabase
    if (!window.location.hash) {
      console.error('No hash fragment found in URL');
      this.errorMessage = 'Invalid password reset link';
      setTimeout(() => this.router.navigate(['/login']), 2000);
      return;
    }

    // Extract and validate the token
    const params = new URLSearchParams(window.location.hash.substring(1));
    const accessToken = params.get('access_token');
    const refreshToken = params.get('refresh_token');
    console.log('accessToken', accessToken);
    console.log('refreshToken', refreshToken);

    if (!accessToken) {
      console.error('No access token found in URL');
      this.errorMessage = 'Invalid password reset link';
      setTimeout(() => this.router.navigate(['/login']), 2000);
      return;
    }

    // Set the session with the tokens from the URL
    this.supabaseAuth.setSession({
      access_token: accessToken,
      refresh_token: refreshToken || ''
    }).subscribe({
      next: () => {
        this.isValidToken = true;
        console.log('isValidToken', this.isValidToken);
      },
      error: (error: Error) => {
        console.error('Error validating reset token:', error);
        this.errorMessage = 'Invalid or expired password reset link';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      }
    });
  }

  private passwordMatchValidator(g: FormGroup): null | { mismatch: true } {
    const password = g.get('password')?.value;
    const confirmPassword = g.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { mismatch: true };
  }

  onSubmit() {
    if (this.resetPasswordForm.invalid) {
      this.resetPasswordForm.markAllAsTouched();
      return;
    }

    if (!this.isValidToken) {
      this.errorMessage = 'Invalid or expired password reset link';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const newPassword = this.resetPasswordForm.get('password')?.value;

    // Update password using Supabase
    this.supabaseAuth.updatePassword(newPassword).subscribe({
      next: () => {
        this.isLoading = false;
        this.showSuccessModal = true;
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (error: unknown) => {
        this.isLoading = false;
        console.error('Reset password error:', error);

        if (error instanceof Error) {
          this.errorMessage = error.message;
        } else if (typeof error === 'string') {
          this.errorMessage = error;
        } else {
          this.errorMessage = 'An error occurred while resetting your password';
        }
      }
    });
  }

  getPasswordErrors(): string {
    const control = this.resetPasswordForm.get('password');
    if (!control?.errors || !control.touched) return '';

    if (control.errors['required']) return 'Password is required';
    if (control.errors['minlength']) return 'Password must be at least 8 characters';
    if (control.errors['pattern']) {
      return 'Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character';
    }
    return '';
  }

  goBack() {
    this.router.navigate(['/login']);
  }
} 