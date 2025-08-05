import { Component, OnInit } from '@angular/core';
import { TeamService } from '../../../../services/team.service';
import { UserService } from '../../../../services/user.service';
import { ExtendedTeam, Team } from '../../../../models/team.interface';
import { User, UserDetailsWithRole } from '../../../../models/user.interface';
import { AuthStateService } from '../../../../services/auth-state.service';

@Component({
  selector: 'app-collaborator-teams',
  templateUrl: './collaborator-teams.component.html'
})
export class CollaboratorTeamsComponent implements OnInit {
  teams: any[] = [];
  selectedTeam: ExtendedTeam | null = null;
  currentUser: UserDetailsWithRole | null = null;
  showTeamDetailsModal = false;

  constructor(
    private teamService: TeamService,
    private authState: AuthStateService,
    private userService: UserService
  ) {}

  ngOnInit() {
    this.currentUser = this.authState.getCurrentUser();
    if (this.currentUser?.id) {
      this.loadCollaboratorTeams(this.currentUser.id);
    }
  }

  private loadCollaboratorTeams(userId: string) {
    console.log('Reloading teams for collaborator ID:', userId);
    // Clear the teams array first
    this.teams = [];
    
    this.teamService.getTeamsByCollaboratorId(userId).subscribe({
      next: (teams) => {
        console.log('Received teams from API:', teams);
        if (!teams || teams.length === 0) {
          console.log('No teams found for collaborator');
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

  handleTeamsUpdated() {
    console.log('Team details modal reported an update, reloading teams...');
    if (this.currentUser?.id) {
      this.loadCollaboratorTeams(this.currentUser.id);
    }
  }

  selectTeam(team: any) {
    console.log('Selected team with details:', team);
    this.selectedTeam = team; // The team object now includes teamMembers and teamManager
    this.showTeamDetailsModal = true;
  }
} 