import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UserService } from '../../../../services/user.service';
import { User, UpdateUserCommand, UserDetailsWithRole } from '../../../../models/user.interface';
import { AuthStateService } from '../../../../services/auth-state.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit, OnDestroy {
  currentUser: User | undefined;
  profileForm: FormGroup;
  previewImage?: string;
  isUpdatingPhoto = false;
  successMessage = '';
  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private fb: FormBuilder,
    private authState: AuthStateService
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      position: [''],
      address: [''],
      dateOfBirth: [''],
      isNotificationEnabled: [false]
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
              this.profileForm.patchValue({
                firstName: user.firstName,
                lastName: user.lastName,
                email: user.email,
                position: user.position,
                address: user.address,
                dateOfBirth: user.dateOfBirth,
                isNotificationEnabled: user.isNotificationEnabled
              });
            }
          });
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onImageSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file && this.currentUser) {
      this.isUpdatingPhoto = true;
      const reader = new FileReader();
      reader.onload = (e) => {
        this.previewImage = e.target?.result as string;
        // Update the user's profile picture
        const updateData: UpdateUserCommand = {
          firstName: this.currentUser!.firstName,
          lastName: this.currentUser!.lastName,
          email: this.currentUser!.email,
          address: this.currentUser!.address || '',
          position: this.currentUser!.position || '',
          dateOfBirth: this.currentUser!.dateOfBirth || '',
          isNotificationEnabled: this.currentUser!.isNotificationEnabled,
          isEnabled: this.currentUser!.isEnabled,
          gender: this.currentUser!.gender,
          profilePictureUrl: this.previewImage,
          organizationId: this.currentUser!.organizationId
        };

        this.userService.updateUser(this.currentUser!.id, updateData)
          .subscribe({
            next: (updatedUser) => {
              if (updatedUser) {
                this.currentUser = updatedUser;
                this.successMessage = 'Profile picture updated successfully';
                
                // Update the user data in AuthState - cast to UserDetailsWithRole
                this.authState.updateUserData(updatedUser as UserDetailsWithRole);
                
                setTimeout(() => {
                  this.successMessage = '';
                }, 3000);
              }
              this.isUpdatingPhoto = false;
            },
            error: (error) => {
              console.error('Error updating profile picture:', error);
              this.successMessage = 'Failed to update profile picture';
              this.isUpdatingPhoto = false;
              setTimeout(() => {
                this.successMessage = '';
              }, 3000);
            }
          });
      };
      reader.readAsDataURL(file);
    }
  }
} 