import { Component, OnInit } from '@angular/core';
import { TeamService } from '../../../../services/team.service';
import { Team, ExtendedTeam } from '../../../../models/team.interface';
import { UserService } from '../../../../services/user.service';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthStateService } from '../../../../services/auth-state.service';
import { UserDetailsWithRole, UserDetails } from '../../../../models/user.interface';
import { OrganizationService } from '../../../../services/organization.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RoleType } from '../../../../models/role-type.enum';
import { AuthService } from '../../../../services/auth.service';
import { SupabaseAuthService } from '../../../../services/supabase-auth.service';
import { ToastService } from '@shared/services/toast.service';


@Component({
  selector: 'app-super-admin-teams',
  templateUrl: './super-admin-teams.component.html'
})
export class SuperAdminTeamsComponent implements OnInit {
  teams: any[] = [];
  selectedTeam: ExtendedTeam | null = null;
  showCreateTeamDrawer = false;
  showTeamDetailsModal = false;
  showAddMemberModal = false;
  currentUser: UserDetailsWithRole | null = null;
  
  // Properties for organization context
  currentOrganizationId: string | null = null;
  organizationName: string = '';
  
  // Properties for team creation
  createTeamForm: FormGroup;
  availableManagers: UserDetails[] = [];
  showInviteManagerForm = false;
  isInviting = false;
  isEditMode = false;
  teamToEdit: any = null;
  showDeleteConfirm = false;
  teamToDelete: any = null;
  // Added property for custom manager dropdown
  showManagerDropdown = false;

  constructor(
    private teamService: TeamService,
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private authState: AuthStateService,
    private organizationService: OrganizationService,
    private fb: FormBuilder,
    private authService: AuthService,
    private supabaseAuth: SupabaseAuthService,
    private toastService: ToastService
  ) {
    this.createTeamForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      teamManagerId: [''],
      inviteManagerEmail: ['']
    });
  }

  ngOnInit() {
    // Always set the current user first
    this.currentUser = this.authState.getCurrentUser();
    
    // Try to extract org ID directly from URL as fallback
    const directUrlMatch = this.router.url.match(/\/organizations\/([^\/]+)\/teams/);
    if (directUrlMatch && directUrlMatch[1]) {
      const orgIdFromUrl = directUrlMatch[1];
      this.currentOrganizationId = orgIdFromUrl;
      this.loadOrganizationDetails(orgIdFromUrl);
      this.loadOrganizationTeams(orgIdFromUrl);
      this.loadAvailableManagers(orgIdFromUrl);
      return; // Skip the rest of the checks if we found it in the URL
    }
    
    // First check for organizationId in route parameters
    this.route.paramMap.subscribe(params => {
      const organizationId = params.get('id');
      
      if (organizationId) {
        this.currentOrganizationId = organizationId;
        this.loadOrganizationDetails(organizationId);
        this.loadOrganizationTeams(organizationId);
        this.loadAvailableManagers(organizationId);
      } else {
        // Next check for organizationId in query parameters
        this.route.queryParamMap.subscribe(queryParams => {
          const queryOrgId = queryParams.get('organizationId');
          if (queryOrgId) {
            this.currentOrganizationId = queryOrgId;
            this.loadOrganizationDetails(queryOrgId);
            this.loadOrganizationTeams(queryOrgId);
            this.loadAvailableManagers(queryOrgId);
          }
        });
      }
    });
  }

  private loadOrganizationDetails(organizationId: string) {
    this.organizationService.getOrganizationById(organizationId).subscribe({
      next: (organization) => {
        this.organizationName = organization.name;
      },
      error: (error) => {
        console.error('Error loading organization details:', error);
      }
    });
  }

  private loadAvailableManagers(organizationId: string) {
    this.userService.getTeamManagersByOrganizationId(organizationId).subscribe({
      next: (managers) => {
        this.availableManagers = managers;
      },
      error: (error) => {
        console.error('Error loading available managers:', error);
      }
    });
  }

  private loadOrganizationTeams(organizationId: string) {
    console.log('Reloading teams for organization ID:', organizationId);
    this.teams = []; // Clear the teams array
    
    this.teamService.getTeamsByOrganizationId(organizationId).subscribe({
      next: (teams) => {
        if (!teams || teams.length === 0) {
          return;
        }
        
        // Process each team to get members and manager details
        teams.forEach(team => {
          // Create a team object with default values
          const teamWithDetails: any = {
            ...team,
            managerName: 'No Manager',
            managerRole: '',
            managerImage: '',
            teamMembers: []
          };
          
          // Get team members
          this.teamService.getUsersByTeamId(team.id).subscribe({
            next: (members) => {
              teamWithDetails.teamMembers = members.filter(member => member.id !== team.teamManagerId);
              
              // Check if manager exists and get details
              if (team.teamManagerId) {
                // First check if manager is in team members
                const managerInMembers = members.find(m => m.id === team.teamManagerId);
                
                if (managerInMembers) {
                  // Use manager from team members
                  teamWithDetails.managerName = `${managerInMembers.firstName} ${managerInMembers.lastName}`;
                  teamWithDetails.managerRole = managerInMembers.role;
                  teamWithDetails.managerImage = managerInMembers.profilePictureUrl;
                  teamWithDetails.teamManager = managerInMembers;
                  
                  this.teams.push(teamWithDetails);
                } else {
                  // Get manager separately
                  this.userService.getUserById(team.teamManagerId).subscribe({
                    next: (manager) => {
                      if (manager) {
                        teamWithDetails.managerName = `${manager.firstName} ${manager.lastName}`;
                        teamWithDetails.managerRole = manager.role;
                        teamWithDetails.managerImage = manager.profilePictureUrl;
                        teamWithDetails.teamManager = manager;
                      }
                      this.teams.push(teamWithDetails);
                    },
                    error: (error) => {
                      this.teams.push(teamWithDetails);
                    }
                  });
                }
              } else {
                // No manager assigned
                this.teams.push(teamWithDetails);
              }
            },
            error: (error) => {
              // Continue with manager lookup
              if (team.teamManagerId) {
                this.userService.getUserById(team.teamManagerId).subscribe({
                  next: (manager) => {
                    if (manager) {
                      teamWithDetails.managerName = `${manager.firstName} ${manager.lastName}`;
                      teamWithDetails.managerRole = manager.role;
                      teamWithDetails.managerImage = manager.profilePictureUrl;
                      teamWithDetails.teamManager = manager;
                    }
                    this.teams.push(teamWithDetails);
                  },
                  error: (managerError) => {
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
        console.error('Error loading organization teams:', error);
      }
    });
  }

  selectTeam(team: ExtendedTeam) {
    this.selectedTeam = team;
    this.showTeamDetailsModal = true;
  }

  openCreateTeamDrawer() {
    this.isEditMode = false;
    this.teamToEdit = null;
    this.resetForm();
    this.showCreateTeamDrawer = true;
    this.lockBodyScroll();
  }

  toggleInviteManagerForm(show: boolean) {
    this.showInviteManagerForm = show;
    
    // Reset the appropriate field
    if (show) {
      this.createTeamForm.get('teamManagerId')?.setValue('');
    } else {
      this.createTeamForm.get('inviteManagerEmail')?.setValue('');
    }
  }

  createTeam() {
    if (!this.createTeamForm.valid || !this.currentOrganizationId) {
      return;
    }

    const formValue = this.createTeamForm.value;
    
    // Check if we're in edit mode first
    if (this.isEditMode) {
      // Update existing team
      this.teamService.updateTeam(this.teamToEdit.id, {
        name: formValue.name,
        description: formValue.description || '',
        organizationId: this.currentOrganizationId!,
        teamManagerId: this.showInviteManagerForm ? null : formValue.teamManagerId || this.teamToEdit.teamManagerId
      }).subscribe({
        next: () => {
          this.loadOrganizationTeams(this.currentOrganizationId!);
          this.showCreateTeamDrawer = false;
          this.unlockBodyScroll();
          this.resetForm();
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

    // For creating a new team, always create the team first
    const newTeam = {
      name: formValue.name,
      description: formValue.description || '',
      // Only include teamManagerId if using existing manager
      teamManagerId: this.showInviteManagerForm ? null : formValue.teamManagerId,
      organizationId: this.currentOrganizationId
    };

    // Create team first in both scenarios
    this.teamService.createTeam(newTeam).subscribe({
      next: (teamId) => {
        if (this.showInviteManagerForm && formValue.inviteManagerEmail) {
          // Now that we have the team ID, send invitation with it
          this.isInviting = true;
          const command = {
            email: formValue.inviteManagerEmail,
            roleName: RoleType.TeamManager,
            organizationId: this.currentOrganizationId!,
            teamId: teamId
          };
          this.authService.generateInvitationLink(command).subscribe({
            next: () => {
              console.log('Team created and invitation sent successfully');
              this.loadOrganizationTeams(this.currentOrganizationId!);
              this.showCreateTeamDrawer = false;
              this.unlockBodyScroll();
              this.resetForm();
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
          this.loadOrganizationTeams(this.currentOrganizationId!);
          this.showCreateTeamDrawer = false;
          this.unlockBodyScroll();
          this.resetForm();
          this.toastService.showSuccess('Team created successfully!');
        }
      },
      error: (error) => {
        console.error('Error creating team:', error);
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

  editTeam(team: any) {
    this.isEditMode = true;
    this.teamToEdit = { ...team };
    this.createTeamForm.patchValue({
      name: team.name,
      description: team.description || '',
      teamManagerId: team.teamManagerId || ''
    });
    this.showInviteManagerForm = false; // Default to existing manager view
    this.showCreateTeamDrawer = true;
    this.lockBodyScroll();
  }

  confirmDeleteTeam(team: any) {
    this.teamToDelete = { ...team };
    this.showDeleteConfirm = true;
  }

  deleteTeam() {
    if (!this.teamToDelete || !this.currentOrganizationId) return;

    this.teamService.deleteTeam(this.teamToDelete.id).subscribe({
      next: () => {
        this.loadOrganizationTeams(this.currentOrganizationId!);
        this.cancelDeleteTeam();
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
    this.resetForm();
    this.showCreateTeamDrawer = false;
    this.isEditMode = false;
    this.teamToEdit = null;
    this.unlockBodyScroll();
  }

  private resetForm() {
    this.createTeamForm.reset({
      name: '',
      description: '',
      teamManagerId: '',
      inviteManagerEmail: ''
    });
    this.showInviteManagerForm = false;
  }

  openAddMemberModal() {
    this.showAddMemberModal = true;
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'on-track': return 'text-green-600';
      case 'at-risk': return 'text-yellow-600';
      case 'behind': return 'text-red-600';
      default: return 'text-gray-600';
    }
  }

  handleTeamsUpdated() {
    console.log('Team details modal reported an update, reloading teams...');
    if (this.currentOrganizationId) {
      this.loadOrganizationTeams(this.currentOrganizationId);
      // Optionally, also refresh available managers
      // this.loadAvailableManagers(this.currentOrganizationId);
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
} 