import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { OrganizationService } from '../../../services/organization.service';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RegisterUserCommand } from '../../../models/auth.interface';
import { Gender } from '../../../models/user.interface';
import { RoleType } from 'src/app/models/role-type.enum';
import { UserService } from '../../../services/user.service';
import { CreateOrganizationCommand } from '../../../models/organization.interface';
import { SupabaseAuthService } from '../../../services/supabase-auth.service';
import { Location } from '@angular/common';
import { jwtDecode } from 'jwt-decode';

@Component({
  selector: 'app-signup',
  templateUrl: './signup.component.html',
  styleUrls: []
})
export class SignupComponent implements OnInit {
  signupForm!: FormGroup;
  errorMessage: string = '';
  showPassword = false;
  showConfirmPassword = false;
  isLoading = false;
  hasInviteKey = false;
  inviteKey: string = '';
  registrationSuccess = false;
  genderOptions = [
    { value: Gender.Male, label: 'Male' },
    { value: Gender.Female, label: 'Female' }
  ];
  inviteTokenPayload: any = null;
  inviteEmail: string = '';
  inviteRoleName: string = '';
  inviteOrganizationId: string = '';
  inviteTeamId: string = '';

  constructor(
    private authService: AuthService,
    private organizationService: OrganizationService,
    private router: Router,
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private userService: UserService,
    private supabaseAuth: SupabaseAuthService,
    private location: Location
  ) {
    this.route.queryParams.subscribe(params => {
      const token = params['token'];
      if (token) {
        try {
          const decoded: any = jwtDecode(token);
          this.inviteTokenPayload = decoded;
          this.inviteEmail = decoded.email || '';
          this.inviteRoleName = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '';
          this.inviteOrganizationId = decoded.OrganizationId || '';
          this.inviteTeamId = decoded.TeamId || '';
          this.hasInviteKey = true;
          this.inviteKey = token;
          console.log('Decoded invite token:', decoded);
        } catch (e) {
          this.hasInviteKey = false;
          this.inviteKey = '';
          console.error('Failed to decode invite token:', e);
        }
      } else {
        this.hasInviteKey = false;
        this.inviteKey = '';
      }
    });
  }

  ngOnInit() {
    this.initializeForm();
  }

  private initializeForm() {
    const baseControls = {
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      address: ['', [Validators.required]],
      dateOfBirth: ['', [Validators.required]],
      gender: [null, [Validators.required]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[0-9\s-()]{8,}$/)]],
      position: ['', [Validators.required]],
      password: ['', [Validators.required, this.passwordValidator]],
      confirmPassword: ['', [Validators.required]],
      acceptTerms: [false, [Validators.requiredTrue]]
    };

    if (!this.hasInviteKey) {
      const orgControls = {
        organizationName: ['', [Validators.required]],
        organizationDescription: ['', [Validators.required]],
        organizationCountry: ['', [Validators.required]],
        organizationIndustry: ['', [Validators.required]],
        organizationSize: ['', [Validators.required, Validators.min(1)]],
        organizationEmail: ['', [Validators.required, Validators.email]],
        organizationPhone: ['', [Validators.required]]
      };
      Object.assign(baseControls, orgControls);
    }

    this.signupForm = this.fb.group(baseControls, {
      validators: this.passwordMatchValidator
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');
    
    if (password?.value !== confirmPassword?.value) {
      confirmPassword?.setErrors({ passwordMismatch: true });
    } else {
      const errors = confirmPassword?.errors || {};
      if (errors['passwordMismatch']) {
        delete errors['passwordMismatch'];
        confirmPassword?.setErrors(Object.keys(errors).length ? errors : null);
      }
    }
  }

  private passwordValidator(control: AbstractControl): ValidationErrors | null {
    const value: string = control.value;
    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(value);

    const errors: ValidationErrors = {};
    
    if (!hasUpperCase) errors['noUpperCase'] = true;
    if (!hasLowerCase) errors['noLowerCase'] = true;
    if (!hasNumeric) errors['noNumeric'] = true;
    if (!hasSpecialChar) errors['noSpecialChar'] = true;
    if (value.length < 8) errors['minLength'] = true;

    return Object.keys(errors).length ? errors : null;
  }

  onSubmit(): void {
    if (this.signupForm.invalid) {
      Object.keys(this.signupForm.controls).forEach(key => {
        const control = this.signupForm.get(key);
        control?.markAsTouched();
      });
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const formValues = this.signupForm.value;
    const email = formValues.email;
    const password = formValues.password;

    // Base register command without password-related fields
    const registerCommand: RegisterUserCommand = {
      email: email,
      firstName: formValues.firstName,
      lastName: formValues.lastName,
      address: formValues.address,
      dateOfBirth: formValues.dateOfBirth,
      gender: Number(formValues.gender),
      phoneNumber: formValues.phoneNumber,
      position: formValues.position,
      isEnabled: true,
      password: password,
      confirmPassword: formValues.confirmPassword,
      roleName: ''
    };

    if (this.hasInviteKey) {
      // Registration with invitation
      console.log('Registering user with invite key:', this.inviteKey);
      this.supabaseAuth.signUp(email, password).subscribe(({ user, error }) => {
        if (error) {
          this.isLoading = false;
          this.handleError(error.message || error);
          return;
        }
        if (!user) {
          this.isLoading = false;
          this.handleError('No user returned from Supabase');
          return;
        }
        // Check if entered email matches invite email
        if (this.inviteEmail && email !== this.inviteEmail) {
          this.isLoading = false;
          this.handleError('The email you entered does not match the invitation email.');
          return;
        }
        // Prepare register command using invite token info
        registerCommand.roleName = this.inviteRoleName;
        registerCommand.organizationId = this.inviteOrganizationId;
        if (this.inviteTeamId) {
          registerCommand.teamId = this.inviteTeamId;
        }
        // Register in backend with Supabase userId
        this.authService.register({ ...registerCommand, supabaseId: user.id }).subscribe({
          next: (response) => {
            console.log('Registration successful:', response);
            this.isLoading = false;
            this.router.navigate(['/login']);
          },
          error: (error) => {
            console.error('Registration error:', error);
            this.isLoading = false;
            this.handleError(error);
          }
        });
      });
    } else {
      // Regular registration with organization creation
      registerCommand.roleName = this.getRoleName(RoleType.OrganizationAdmin);
      registerCommand.isEnabled = false;

      // First register with Supabase to get userId
      this.supabaseAuth.signUp(email, password).subscribe(({ user, error }) => {
        if (error) {
          this.isLoading = false;
          this.handleError(error.message || error);
          return;
        }
        if (!user) {
          this.isLoading = false;
          this.handleError('No user returned from Supabase');
          return;
        }
        // Then create the organization
        const organizationData: CreateOrganizationCommand = {
          name: formValues.organizationName,
          description: formValues.organizationDescription,
          country: formValues.organizationCountry,
          industry: formValues.organizationIndustry,
          size: Number(formValues.organizationSize),
          email: formValues.organizationEmail,
          phone: formValues.organizationPhone,
          isActive: false
        };

        this.organizationService.createOrganization(organizationData).subscribe({
          next: (orgResponse: any) => {
            const organizationId = orgResponse.id || orgResponse.Id;
            if (!organizationId) {
              this.handleError('Failed to get organization ID from response' + JSON.stringify(orgResponse));
              return;
            }

            // Finally register in backend with both IDs
            registerCommand.organizationId = organizationId;
            console.log('Registering user with command:', registerCommand);
            console.log('supabase User ID:', user.id);
            this.authService.register({ ...registerCommand, supabaseId: user.id }).subscribe({
              next: (response) => {
                console.log('Registration successful:', response);
                this.isLoading = false;
                this.registrationSuccess = true;
                setTimeout(() => {
                  window.scrollTo(0, 0);
                });
              },
              error: (error) => {
                console.error('Registration error:', error);
                this.isLoading = false;
                this.handleError(error);
              }
            });
          },
          error: (error) => {
            this.handleError(error);
          }
        });
      });
    }
  }

  private handleError(error: any): void {
    this.isLoading = false;
    this.registrationSuccess = false;

    if (error.error?.errors) {
      const errorMessages = [];
      for (const field in error.error.errors) {
        errorMessages.push(...error.error.errors[field]);
      }
      this.errorMessage = errorMessages.join('. ');
    } else if (error.error && typeof error.error === 'string') {
      this.errorMessage = error.error;
    } else if (typeof error === 'string') {
      this.errorMessage = error;
    } else {
      this.errorMessage = 'An error occurred during registration';
    }
  }

  togglePasswordVisibility(field: 'password' | 'confirmPassword'): void {
    if (field === 'password') {
      this.showPassword = !this.showPassword;
    } else {
      this.showConfirmPassword = !this.showConfirmPassword;
    }
  }

  private getRoleName(role: RoleType): string {
    switch (role) {
      case RoleType.OrganizationAdmin:
        return 'OrganizationAdmin';
      case RoleType.TeamManager:
        return 'TeamManager';
      case RoleType.Collaborator:
        return 'Collaborator';
      default:
        return '';
    }
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  // Helper method to check if a field is invalid
  isFieldInvalid(fieldName: string): boolean {
    const field = this.signupForm.get(fieldName);
    return field ? field.invalid && field.touched : false;
  }

  // Helper method to get error message
  getErrorMessage(fieldName: string): string {
    const field = this.signupForm.get(fieldName);
    if (!field || !field.errors) return '';

    // Password specific errors
    if (fieldName === 'password') {
      if (field.hasError('required')) return 'Password is required';
      if (field.hasError('minLength')) return 'Password must be at least 8 characters';
      if (field.hasError('noUpperCase')) return 'Password must contain at least one uppercase letter';
      if (field.hasError('noLowerCase')) return 'Password must contain at least one lowercase letter';
      if (field.hasError('noNumeric')) return 'Password must contain at least one number';
      if (field.hasError('noSpecialChar')) return 'Password must contain at least one special character';
    }

    // Confirm Password errors
    if (fieldName === 'confirmPassword') {
      if (field.hasError('required')) return 'Please confirm your password';
      if (field.hasError('passwordMismatch')) return 'Passwords do not match';
    }

    // Email specific errors
    if (fieldName === 'email' || fieldName === 'organizationEmail') {
      if (field.hasError('required')) return `${this.formatFieldName(fieldName)} is required`;
      if (field.hasError('email')) return 'Please enter a valid email address';
    }

    // Organization size specific errors
    if (fieldName === 'organizationSize') {
      if (field.hasError('required')) return 'Organization size is required';
      if (field.hasError('min')) return 'Organization size must be greater than 0';
    }

    // Date of Birth specific errors
    if (fieldName === 'dateOfBirth') {
      if (field.hasError('required')) return 'Date of birth is required';
      if (field.hasError('futureDate')) return 'Date of birth cannot be in the future';
    }

    // Gender specific errors
    if (fieldName === 'gender') {
      if (field.hasError('required')) return 'Please select your gender';
    }

    // Phone number specific errors
    if (fieldName === 'phoneNumber' || fieldName === 'organizationPhone') {
      if (field.hasError('required')) return `${this.formatFieldName(fieldName)} is required`;
      if (field.hasError('pattern')) return 'Please enter a valid phone number';
    }

    // Default required message for other fields
    if (field.hasError('required')) {
      return `${this.formatFieldName(fieldName)} is required`;
    }

    return '';
  }

  // Helper method to format field names
  private formatFieldName(fieldName: string): string {
    // Convert camelCase to space-separated words and capitalize first letter
    const formatted = fieldName
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase());
    
    // Handle organization fields
    return formatted.replace('Organization ', '');
  }
} 