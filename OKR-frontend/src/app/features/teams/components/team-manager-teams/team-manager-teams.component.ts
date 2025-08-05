import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TeamService } from '../../../../services/team.service';
import { UserService } from '../../../../services/user.service';
import { ExtendedTeam, Team } from '../../../../models/team.interface';
import { RoleType, UserDetailsWithRole, User } from '../../../../models/user.interface';
import { AuthStateService } from '../../../../services/auth-state.service';
import { SupabaseAuthService } from '../../../../services/supabase-auth.service';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-team-manager-teams',
  templateUrl: './team-manager-teams.component.html'
})
export class TeamManagerTeamsComponent implements OnInit {
  teams: any[] = [];
  selectedTeam: ExtendedTeam | null = null;
  currentUser: UserDetailsWithRole | null = null;
  showTeamDetailsModal = false;
  showInviteModal = false;
  inviteForm: FormGroup;
  isInviting: boolean = false;

  constructor(
    private teamService: TeamService,
    private userService: UserService,
    private authState: AuthStateService,
    private fb: FormBuilder,
    private supabaseAuth: SupabaseAuthService,
    private authService: AuthService
  ) {
    this.inviteForm = this.fb.group({
      teamId: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]]
    });
  }

  ngOnInit() {
    this.currentUser = this.authState.getCurrentUser();
    if (this.currentUser?.id) {
      this.loadManagerTeams(this.currentUser.id);
    }
  }

  private loadManagerTeams(managerId: string) {
    console.log('Reloading teams for manager ID:', managerId);
    // Clear the teams array first
    this.teams = [];
    
    this.teamService.getTeamsByManagerId(managerId).subscribe({
      next: (teams) => {
        console.log('Received teams from API:', teams);
        if (!teams || teams.length === 0) {
          console.log('No teams found for manager');
          return;
        }
        
        // Process each team sequentially to get team members
        teams.forEach((team, index) => {
          console.log(`Processing team ${index + 1}/${teams.length}:`, team);
          
          // Create a temporary team object that will be updated with manager and members
          const teamWithDetails: any = {
            ...team,
            teamMembers: [], // Add property for team members
            managerName: this.currentUser ? `${this.currentUser.firstName} ${this.currentUser.lastName}` : 'No Manager',
            managerRole: this.currentUser ? this.currentUser.role : '',
            managerImage: this.currentUser ? this.currentUser.profilePictureUrl : ''
          };
          
          // Store the manager info if available
          if (this.currentUser) {
            teamWithDetails.teamManager = this.currentUser;
          }
          
          // Get team members
          this.teamService.getUsersByTeamId(team.id).subscribe({
            next: (members) => {
              console.log('Retrieved members for team:', team.id, members);
              teamWithDetails.teamMembers = members.filter(member => member.id !== team.teamManagerId);
              
              // Add the completed team to the array
              this.teams.push(teamWithDetails);
              console.log('Updated teams array with members:', this.teams);
            },
            error: (error) => {
              console.error('Error loading team members for team:', team.id, error);
              // Still add the team even if we couldn't get members
              this.teams.push(teamWithDetails);
            }
          });
        });
      },
      error: (error) => {
        console.error('Error loading teams:', error);
      }
    });
  }

  selectTeam(team: any) {
    console.log('Selected team with details:', team);
    this.selectedTeam = team; // The team object now includes teamMembers and teamManager
    this.showTeamDetailsModal = true;
  }

  openInviteModal() {
    this.showInviteModal = true;
    this.inviteForm.patchValue({
        teamId: '',
        email: ''
    });
  }

  closeInviteModal() {
    this.showInviteModal = false;
    this.inviteForm.patchValue({
        teamId: '',
        email: ''
    });
  }

  sendInvite() {
    if (this.inviteForm.valid && this.currentUser?.organizationId) {
      const { teamId, email } = this.inviteForm.value;
      this.isInviting = true;
      const command = {
        email: email,
        roleName: RoleType.Collaborator,
        organizationId: this.currentUser.organizationId,
        teamId: teamId
      };
      this.authService.generateInvitationLink(command).subscribe({
        next: () => {
          console.log('Invitation sent successfully!');
          this.showInviteModal = false;
          this.inviteForm.reset();
          this.isInviting = false;
        },
        error: (error) => {
          this.isInviting = false;
          let errorMsg = 'Failed to invite collaborator. Please try again later.';
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
          /* this.toastService?.showError?.(errorMsg); */
        }
      });
    }
  }

  handleTeamsUpdated() {
    console.log('Team details modal reported an update, reloading teams...');
    if (this.currentUser?.id) {
      this.loadManagerTeams(this.currentUser.id);
    }
  }
} 