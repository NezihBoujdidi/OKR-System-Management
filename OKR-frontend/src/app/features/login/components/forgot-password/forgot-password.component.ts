import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { SupabaseAuthService } from '../../../../services/supabase-auth.service';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent {
  forgotPasswordForm: FormGroup;
  isLoading = false;
  emailSent = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private supabaseAuth: SupabaseAuthService
  ) {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit() {
    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const email = this.forgotPasswordForm.value.email;

    this.supabaseAuth.resetPassword(email).subscribe({
      next: () => {
        this.isLoading = false;
        this.emailSent = true;
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Password reset error:', error);

        if (typeof error === 'string') {
          this.errorMessage = error;
        } else if (error instanceof Error) {
          this.errorMessage = error.message;
        } else {
          this.errorMessage = 'An error occurred while processing your request';
        }
      }
    });
  }

  goBack() {
    this.router.navigate(['/login']);
  }
} 