import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../../../../services/user.service';
import { User, UpdateUserCommand, UserDetailsWithRole } from '../../../../models/user.interface';
import { AuthStateService } from '../../../../services/auth-state.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-manage-account',
  templateUrl: './manage-account.component.html'
})
export class ManageAccountComponent implements OnInit, OnDestroy {
  currentUser: User | undefined;
  accountForm: FormGroup;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  previewImage?: string;
  canEditPersonalDetails = false;
  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private fb: FormBuilder,
    public router: Router,
    private authState: AuthStateService
  ) {
    this.accountForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      position: ['', [Validators.required]],
      address: ['', Validators.required],
      dateOfBirth: ['', Validators.required]
    });
  }

  ngOnInit() {
    // Get the logged-in user from AuthStateService
    this.authState.getUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(userData => {
        if (userData) {
          // Determine if user can edit personal details
          this.canEditPersonalDetails =
            userData.role === 'SuperAdmin' || userData.role === 'OrganizationAdmin';
          console.log("userData", userData);
          console.log("canEditPersonalDetails", this.canEditPersonalDetails);  
          // Fetch full user data by ID
          this.userService.getUserById(userData.id).subscribe(user => {
            if (user) {
              this.currentUser = user;
              this.accountForm.patchValue({
                firstName: user.firstName,
                lastName: user.lastName,
                email: user.email,
                position: user.position || '',
                address: user.address,
                dateOfBirth: this.formatDateForInput(user.dateOfBirth)
              });
              if (!this.canEditPersonalDetails) {
                this.accountForm.get('firstName')?.disable();
                this.accountForm.get('lastName')?.disable();
                this.accountForm.get('email')?.disable();
                this.accountForm.get('position')?.disable();
              }
            }
          });
        }
      });
  }
  
  formatDateForInput(dateString: string): string {
    if (!dateString) return '';
    
    try {
      const date = new Date(dateString);
      // Check if the date is valid
      if (isNaN(date.getTime())) {
        console.error('Invalid date:', dateString);
        return '';
      }
      
      // Format as YYYY-MM-DD
      return date.toISOString().split('T')[0];
    } catch (error) {
      console.error('Error formatting date:', error);
      return '';
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  saveChanges() {
    if (this.accountForm.valid && this.currentUser) {
      this.isSubmitting = true;
      this.errorMessage = '';
      this.successMessage = '';

      // Create a complete update command with all required fields
      const updateData: UpdateUserCommand = {
        firstName: this.accountForm.get('firstName')?.value,
        lastName: this.accountForm.get('lastName')?.value,
        email: this.accountForm.get('email')?.value,
        address: this.accountForm.value.address,
        position: this.accountForm.get('position')?.value,
        dateOfBirth: this.accountForm.value.dateOfBirth,
        isNotificationEnabled: this.currentUser.isNotificationEnabled,
        isEnabled: this.currentUser.isEnabled,
        gender: this.currentUser.gender,
        profilePictureUrl: this.currentUser.profilePictureUrl,
        organizationId: this.currentUser.organizationId
      };
      
      this.userService.updateUser(this.currentUser.id, updateData).subscribe({
        next: updatedUser => {
          if (updatedUser) {
            this.currentUser = updatedUser;
            this.successMessage = 'Account information updated successfully';
            
            // Update the user data in AuthState
            this.authState.updateUserData(updatedUser as UserDetailsWithRole);
            
            // Wait a brief moment to show the success message before redirecting
            setTimeout(() => {
              this.router.navigate(['/profile']);
            }, 1200);
          } else {
            this.errorMessage = 'Failed to update account information. Please try again.';
          }
          this.isSubmitting = false;
        },
        error: error => {
          console.error('Error updating user:', error);
          this.errorMessage = 'An error occurred while updating account information';
          this.isSubmitting = false;
        }
      });
    }
  }
} 