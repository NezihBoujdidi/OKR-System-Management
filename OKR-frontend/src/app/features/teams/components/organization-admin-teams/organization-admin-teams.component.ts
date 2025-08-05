import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TeamService } from '../../../../services/team.service';
import { UserService } from '../../../../services/user.service';
import { AuthStateService } from '../../../../services/auth-state.service';
import { AuthService } from '../../../../services/auth.service';
import { ExtendedTeam, Team } from '../../../../models/team.interface';
import { UserDetails, UserDetailsWithRole, RoleType, User } from '../../../../models/user.interface';
import { SupabaseAuthService } from '../../../../services/supabase-auth.service';
import { ToastService } from '@shared/services/toast.service';

@Component({
  selector: 'app-organization-admin-teams',
  templateUrl: './organization-admin-teams.component.html'
})
export class OrganizationAdminTeamsComponent implements OnInit {
  teams: any[] = [];
  selectedTeam: ExtendedTeam | null = null;
  showCreateTeamDrawer = false;
  showTeamDetailsModal = false;
  createTeamForm: FormGroup;
  availableManagers: UserDetails[] = [];
  showInviteManagerForm = false;
  currentUser: UserDetailsWithRole | null = null;
  isInviting = false;
  isEditMode = false;
  teamToEdit: any = null;
  showDeleteConfirm = false;
  teamToDelete: any = null;
  showManagerDropdown = false;

  constructor(
    private teamService: TeamService,
    private userService: UserService,
    private fb: FormBuilder,
    private authState: AuthStateService,
    private authService: AuthService,
    private supabaseAuth: SupabaseAuthService,
    private toastService: ToastService
  ) {
    this.createTeamForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      teamManagerId: [''],
      inviteManagerEmail: ['', [Validators.email]]
    });
  }

  ngOnInit() {
    this.currentUser = this.authState.getCurrentUser();
    if (this.currentUser?.organizationId) {
      this.loadAvailableManagers(this.currentUser.organizationId);
      this.loadOrganizationTeams(this.currentUser.organizationId);
    }
  }

  private loadAvailableManagers(organizationId: string) {
    this.userService.getTeamManagersByOrganizationId(organizationId).subscribe({
      next: (managers) => {
        this.availableManagers = managers.filter(manager => manager.isEnabled);
      },
      error: (error) => {
        console.error('Error loading team managers:', error);
      }
    });
  }

  private loadOrganizationTeams(organizationId: string) {
    console.log('Reloading organization teams for ID:', organizationId);
    console.log('Starting to load teams for organization:', organizationId);
    // Clear the teams array first
    this.teams = [];
    
    this.teamService.getTeamsByOrganizationId(organizationId).subscribe({
      next: (teams) => {
        console.log('Received teams from API:', teams);
        if (!teams || teams.length === 0) {
          console.log('No teams found for organization');
          return;
        }
        
        // Process each team sequentially
        teams.forEach((team, index) => {
          console.log(`Processing team ${index + 1}/${teams.length}:`, team);
          
          // Create a temporary team object that will be updated with manager and members
          const teamWithDetails: any = {
            ...team,
            managerName: 'No Manager',
            managerRole: '',
            managerImage: '',
            teamMembers: [] // Add property for team members
          };
          
          // Get team members
          this.teamService.getUsersByTeamId(team.id).subscribe({
            next: (members) => {
              console.log('Retrieved members for team:', team.id, members);
              
              // Exclude the team manager from the members list
              teamWithDetails.teamMembers = members.filter(member => member.id !== team.teamManagerId);
              
              // Get manager details if available
              if (team.teamManagerId) {
                // First check if the manager is already in the teamMembers list
                const managerInTeamMembers = members.find(member => member.id === team.teamManagerId);
                
                if (managerInTeamMembers) {
                  // Use the manager from team members without making an extra API call
                  console.log('Found manager in team members:', managerInTeamMembers);
                  teamWithDetails.managerName = `${managerInTeamMembers.firstName} ${managerInTeamMembers.lastName}`;
                  teamWithDetails.managerRole = managerInTeamMembers.role;
                  teamWithDetails.managerImage = managerInTeamMembers.profilePictureUrl;
                  teamWithDetails.teamManager = managerInTeamMembers; // Store the full manager object
                  
                  // Add the completed team to the array
                  this.teams.push(teamWithDetails);
                  console.log('Updated teams array with manager from team members:', this.teams);
                } else {
                  // Manager not found in team members, make a separate API call
                  this.userService.getUserById(team.teamManagerId).subscribe({
                    next: (manager) => {
                      console.log('Retrieved manager for team:', team.id, manager);
                      if (manager) {
                        teamWithDetails.managerName = `${manager.firstName} ${manager.lastName}`;
                        teamWithDetails.managerRole = manager.role;
                        teamWithDetails.managerImage = manager.profilePictureUrl;
                        teamWithDetails.teamManager = manager; // Store the full manager object
                      }
                      // Add the completed team to the array
                      this.teams.push(teamWithDetails);
                      console.log('Updated teams array with manager from API call:', this.teams);
                    },
                    error: (error) => {
                      console.error('Error loading manager details for team:', team.id, error);
                      // Add the team anyway, but without manager details
                      this.teams.push(teamWithDetails);
                    }
                  });
                }
              } else {
                console.log('Team has no manager ID:', team.id);
                // Add the team without manager details
                this.teams.push(teamWithDetails);
              }
            },
            error: (error) => {
              console.error('Error loading team members for team:', team.id, error);
              // Still continue with manager lookup even if members failed
              if (team.teamManagerId) {
                this.userService.getUserById(team.teamManagerId).subscribe({
                  next: (manager) => {
                    if (manager) {
                      teamWithDetails.managerName = `${manager.firstName} ${manager.lastName}`;
                      teamWithDetails.managerRole = manager.role;
                      teamWithDetails.managerImage = manager.profilePictureUrl;
                      teamWithDetails.teamManager = manager; // Store the full manager object
                    }
                    this.teams.push(teamWithDetails);
                  },
                  error: (managerError) => {
                    console.error('Error loading manager details for team:', team.id, managerError);
                    this.teams.push(teamWithDetails);
                  }
                });
              } else {
                this.teams.push(teamWithDetails);
              }
            }
          });
        });
      },
      error: (error) => {
        console.error('Error loading teams:', error);
      }
    });
  }

  openCreateTeamDrawer() {
    if (this.currentUser?.organizationId) {
      // Refresh the list of available managers
      this.loadAvailableManagers(this.currentUser.organizationId);
    }
    
    this.showCreateTeamDrawer = true;
    this.createTeamForm.patchValue({
      name: '',
      description: '',
      teamManagerId: '',
      inviteManagerEmail: ''
    });
    this.showInviteManagerForm = false;
    this.isEditMode = false;
    this.teamToEdit = null;
    this.lockBodyScroll();
  }

  toggleInviteManagerForm(show: boolean) {
    this.showInviteManagerForm = show;
    if (show) {
      this.createTeamForm.get('teamManagerId')?.clearValidators();
      this.createTeamForm.get('teamManagerId')?.setValue('');
      this.createTeamForm.get('inviteManagerEmail')?.addValidators(Validators.required);
    } else {
      this.createTeamForm.get('teamManagerId')?.addValidators(Validators.required);
      this.createTeamForm.get('inviteManagerEmail')?.clearValidators();
      this.createTeamForm.get('inviteManagerEmail')?.setValue('');
    }
    this.createTeamForm.get('teamManagerId')?.updateValueAndValidity();
    this.createTeamForm.get('inviteManagerEmail')?.updateValueAndValidity();
  }

  createTeam() {
    if (!this.createTeamForm.valid || !this.currentUser?.organizationId) {
      return;
    }

    const formValue = this.createTeamForm.value;

    // Check if in edit mode
    if (this.isEditMode && this.teamToEdit) {
      const teamUpdates = {
        name: formValue.name,
        description: formValue.description || '',
        teamManagerId: this.showInviteManagerForm ? null : formValue.teamManagerId,
        organizationId: this.currentUser.organizationId
      };

      this.teamService.updateTeam(this.teamToEdit.id, teamUpdates).subscribe({
        next: () => {
          console.log('Team updated successfully');
          if (this.currentUser?.organizationId) {
            this.loadOrganizationTeams(this.currentUser.organizationId);
          }
          this.showCreateTeamDrawer = false;
          this.unlockBodyScroll();
          this.isEditMode = false;
          this.teamToEdit = null;
          this.toastService.showSuccess('Team updated successfully!');
        },
        error: (error) => {
          console.error('Error updating team:', error);
          let errorMsg = 'Failed to update team. Please try again later.';
          if (Array.isArray(error)) {
            errorMsg = error.map((e: any) => e.errorMessage || e).join('. ');
          } else if (typeof error === 'string') {
            errorMsg = error;
          } else if (error?.error) {
            if (typeof error.error === 'string') {
              errorMsg = error.error;
            } else if (typeof error.error.message === 'string') {
              errorMsg = error.error.message;
            }
          }
          this.toastService.showError(errorMsg);
        }
      });
      
      return;
    }

    // Create team object with or without manager ID based on selection
    const newTeam = {
      name: formValue.name,
      description: formValue.description || '',
      teamManagerId: this.showInviteManagerForm ? null : formValue.teamManagerId,
      organizationId: this.currentUser.organizationId
    };

    // Create team first in both scenarios
    this.teamService.createTeam(newTeam).subscribe({
      next: (teamId) => {
        if (this.showInviteManagerForm && formValue.inviteManagerEmail) {
          // If inviting new manager, send invitation with team ID
          this.isInviting = true;
          const command = {
            email: formValue.inviteManagerEmail,
            roleName: RoleType.TeamManager,
            organizationId: this.currentUser!.organizationId!,
            teamId: teamId
          };
          this.authService.generateInvitationLink(command).subscribe({
            next: () => {
              console.log('Team created and invitation sent successfully');
              this.loadOrganizationTeams(this.currentUser!.organizationId!);
              this.showCreateTeamDrawer = false;
              this.unlockBodyScroll();
              this.createTeamForm.reset();
              this.isInviting = false;
              this.toastService.showSuccess('Team created and invitation sent successfully!');
            },
            error: (error) => {
              this.isInviting = false;
              let errorMsg = 'Failed to invite manager. Please try again later.';
              if (Array.isArray(error)) {
                errorMsg = error.map((e: any) => e.errorMessage || e).join('. ');
              } else if (typeof error === 'string') {
                errorMsg = error;
              } else if (error?.error) {
                if (typeof error.error === 'string') {
                  errorMsg = error.error;
                } else if (typeof error.error.message === 'string') {
                  errorMsg = error.error.message;
                }
              }
              this.toastService.showError(errorMsg);
            }
          });
        } else {
          // If using existing manager, just refresh the teams list
          this.loadOrganizationTeams(this.currentUser!.organizationId!);
          this.showCreateTeamDrawer = false;
          this.unlockBodyScroll();
          this.resetForm();
          this.toastService.showSuccess('Team created successfully!');
        }
      },
      error: (error) => {
        console.error('Error creating team:', error);
        this.isInviting = false;
        let errorMsg = 'Failed to create team. Please try again later.';
        if (Array.isArray(error)) {
          errorMsg = error.map((e: any) => e.errorMessage || e).join('. ');
        } else if (typeof error === 'string') {
          errorMsg = error;
        } else if (error?.error) {
          if (typeof error.error === 'string') {
            errorMsg = error.error;
          } else if (typeof error.error.message === 'string') {
            errorMsg = error.error.message;
          }
        }
        this.toastService.showError(errorMsg);
      }
    });
  }

  selectTeam(team: any) {
    console.log('Selected team with details:', team);
    this.selectedTeam = team;
    this.showTeamDetailsModal = true;
  }

  editTeam(team: any) {
    console.log('Editing team:', team);
    this.teamToEdit = team;
    this.isEditMode = true;
    this.showCreateTeamDrawer = true;
    
    // Populate form with team data
    this.createTeamForm.patchValue({
      name: team.name,
      description: team.description || '',
      teamManagerId: team.teamManagerId || '',
    });

    // Make sure the correct form is shown based on the team's current manager
    this.showInviteManagerForm = false;
    
    // If there's no team manager, show the manager dropdown by default
    if (!team.teamManagerId) {
      this.toggleInviteManagerForm(false);
    }
    
    this.lockBodyScroll();
  }

  confirmDeleteTeam(team: any) {
    console.log('Confirming delete for team:', team);
    this.teamToDelete = team;
    this.showDeleteConfirm = true;
  }

  deleteTeam() {
    if (!this.teamToDelete) return;

    this.teamService.deleteTeam(this.teamToDelete.id).subscribe({
      next: () => {
        console.log('Team deleted successfully');
        if (this.currentUser?.organizationId) {
          this.loadOrganizationTeams(this.currentUser.organizationId);
        }
        this.teamToDelete = null;
        this.showDeleteConfirm = false;
        this.toastService.showSuccess('Team deleted successfully!');
      },
      error: (error) => {
        console.error('Error deleting team:', error);
      }
    });
  }

  cancelDeleteTeam() {
    this.teamToDelete = null;
    this.showDeleteConfirm = false;
  }

  cancelEdit() {
    this.showCreateTeamDrawer = false;
    this.isEditMode = false;
    this.teamToEdit = null;
    // Reset the form
    this.createTeamForm.reset();
    this.unlockBodyScroll();
  }

  handleTeamsUpdated() {
    console.log('Team details modal reported an update, reloading teams...');
    if (this.currentUser?.organizationId) {
      this.loadOrganizationTeams(this.currentUser.organizationId);
      // Optionally, also refresh available managers if team manager changes could affect this list
      // this.loadAvailableManagers(this.currentUser.organizationId);
    }
  }

  // Methods to handle body scroll
  private lockBodyScroll(): void {
    document.body.style.overflow = 'hidden';
  }

  private unlockBodyScroll(): void {
    document.body.style.overflow = '';
  }

  // Toggle the visibility of the custom manager dropdown
  toggleManagerDropdown() {
    this.showManagerDropdown = !this.showManagerDropdown;
    
    // Close dropdown when clicking outside
    if (this.showManagerDropdown) {
      setTimeout(() => {
        const closeDropdown = (event: MouseEvent) => {
          if (!(event.target as HTMLElement).closest('.relative')) {
            this.showManagerDropdown = false;
            document.removeEventListener('click', closeDropdown);
          }
        };
        document.addEventListener('click', closeDropdown);
      }, 0);
    }
  }

  // Select a manager from the dropdown
  selectManager(managerId: string) {
    this.createTeamForm.get('teamManagerId')?.setValue(managerId);
    this.showManagerDropdown = false;
  }

  // Get the name of the selected manager
  getSelectedManagerName(): string {
    const selectedId = this.createTeamForm.get('teamManagerId')?.value;
    if (!selectedId) return 'Select existing manager';
    
    const manager = this.availableManagers.find(m => m.id === selectedId);
    return manager ? `${manager.firstName} ${manager.lastName}` : 'Select existing manager';
  }

  resetForm() {
    this.createTeamForm.reset();
    this.showInviteManagerForm = false;
    this.isEditMode = false;
    this.teamToEdit = null;
  }
} 