import { Component, Input, Output, EventEmitter, HostListener, OnInit, OnDestroy } from '@angular/core';
import { ExtendedTeam } from '../../../../models/team.interface';
import { User, UserDetailsWithRole, RoleType } from '../../../../models/user.interface';
import { TeamService } from '../../../../services/team.service';
import { UserService } from '../../../../services/user.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { Router } from '@angular/router';
import { finalize, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-team-details-modal',
  templateUrl: './team-details-modal.component.html'
})
export class TeamDetailsModalComponent implements OnInit, OnDestroy {
  @Input() team: ExtendedTeam | null = null;
  @Input() currentUser: UserDetailsWithRole | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() teamsUpdated = new EventEmitter<void>();
  
  teamMembers: User[] = [];
  teamManager: User | null = null;
  showMemberDrawer = false;
  selectedMember: User | null = null;
  showDeleteConfirm = false;
  memberToDelete: User | null = null;
  selectedMemberForMenu: User | null = null;
  showRemoveConfirm = false;
  availableTeams: ExtendedTeam[] = [];
  showMoveToTeamConfirm = false;
  selectedTargetTeam: ExtendedTeam | null = null;
  isMoving = false;
  hasOngoingTasks: boolean = false;
  moveErrorMessage: string = '';

  // New properties for add collaborators functionality
  showAddCollaboratorsModal = false;
  organizationCollaborators: UserDetailsWithRole[] = [];
  selectedCollaborators: UserDetailsWithRole[] = [];
  isAddingCollaborators = false;
  searchQuery = '';
  filteredCollaborators: UserDetailsWithRole[] = [];

  private destroy$ = new Subject<void>();

  constructor(
    private teamService: TeamService,
    private userService: UserService,
    private toastService: ToastService,
    private router: Router
  ) {}

  ngOnInit() {
    if (this.team) {
      this.loadTeamDetails();
    }
  }

  private loadTeamDetails() {
    if (!this.team) return;

    if (this.team.teamMembers && this.team.teamMembers.length > 0) {
      this.teamMembers = this.team.teamMembers;
    } else {
      this.teamMembers = [];
    }

    if (this.team.teamManager) {
      this.teamManager = this.team.teamManager;
    } else if (this.team.teamManagerId) {
      this.userService.getUserById(this.team.teamManagerId).pipe(takeUntil(this.destroy$)).subscribe({
        next: (manager) => {
          if (manager) {
            this.teamManager = manager;
          }
        },
        error: (error) => {
          console.error('Error loading team manager:', error);
        }
      });
    } else {
      this.teamManager = null;
    }
  }

  openMemberDrawer(member: User) {
    this.selectedMember = member;
    this.showMemberDrawer = true;
    this.selectedMemberForMenu = null;
  }

  closeMemberDrawer() {
    this.showMemberDrawer = false;
    this.selectedMember = null;
  }

  confirmDeleteMember(member: User) {
    this.memberToDelete = member;
    this.showDeleteConfirm = true;
    this.selectedMemberForMenu = null;
    this.showMemberDrawer = false;
  }

  deleteMember() {
    if (!this.memberToDelete) return;

    // If user is already disabled, we'll enable them instead
    if (!this.memberToDelete.isEnabled) {
      this.enableMember(this.memberToDelete);
      return;
    }

    this.userService.disableUserById(this.memberToDelete.id).subscribe({
      next: () => {
        // Update the disabled status in the local data
        const memberIndex = this.teamMembers.findIndex(m => m.id === this.memberToDelete?.id);
        if (memberIndex !== -1) {
          // Create a new copy of the member with updated status
          this.teamMembers[memberIndex] = {
            ...this.teamMembers[memberIndex],
            isEnabled: false
          };
        }
        
        // Close modals
        this.showDeleteConfirm = false;
        this.memberToDelete = null;
        this.showMemberDrawer = false;
        
        // Clean up any open menus
        this.selectedMemberForMenu = null;
      },
      error: (error) => {
        console.error('Error disabling user:', error);
      }
    });
  }

  enableMember(member: User) {
    this.userService.enableUserById(member.id).subscribe({
      next: () => {
        // Update the enabled status in the local data
        const memberIndex = this.teamMembers.findIndex(m => m.id === member.id);
        if (memberIndex !== -1) {
          // Create a new copy of the member with updated status
          this.teamMembers[memberIndex] = {
            ...this.teamMembers[memberIndex],
            isEnabled: true
          };
        }
        
        // Close modals
        this.showDeleteConfirm = false;
        this.memberToDelete = null;
        this.showMemberDrawer = false;
        
        // Clean up any open menus
        this.selectedMemberForMenu = null;
      },
      error: (error) => {
        console.error('Error enabling user:', error);
      }
    });
  }

  updateMember() {
    this.closeMemberDrawer();
    this.loadTeamDetails();
  }

  toggleMemberMenu(member: User, event?: Event) {
    event?.stopPropagation();
    
    if (this.selectedMemberForMenu === member) {
      this.selectedMemberForMenu = null;
    } else {
      this.selectedMemberForMenu = member;
      
      // Add auto-scroll functionality
      setTimeout(() => {
        const memberElement = event?.target as HTMLElement;
        const dropdownMenu = memberElement?.closest('.member-actions')?.querySelector('.dropdown-menu');
        
        if (dropdownMenu) {
          const modalContent = document.querySelector('.modal-content');
          if (modalContent) {
            const dropdownBottom = dropdownMenu.getBoundingClientRect().bottom;
            const modalBottom = modalContent.getBoundingClientRect().bottom;
            
            if (dropdownBottom > modalBottom) {
              modalContent.scrollBy({
                top: dropdownBottom - modalBottom + 16, // 16px padding
                behavior: 'smooth'
              });
            }
          }
        }
      });
    }
  }

  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (this.selectedMemberForMenu && 
        !target.closest('.member-actions')) {
      this.selectedMemberForMenu = null;
    }
  }

  confirmRemoveFromTeam(member: User) {
    this.selectedMember = member;
    this.showRemoveConfirm = true;
    this.showMemberDrawer = false;
  }

  confirmMoveToTeam(member: User) {
    this.selectedMember = member;
    this.showMemberDrawer = false;
    this.loadAvailableTeams();
    this.showMoveToTeamConfirm = true;
  }

  loadAvailableTeams() {
    if (!this.currentUser) return;
    
    if (this.currentUser.role === RoleType.SuperAdmin || this.currentUser.role === RoleType.OrganizationAdmin) {
      // Get all teams from the organization
      if (this.currentUser.organizationId) {
        this.teamService.getTeamsByOrganizationId(this.currentUser.organizationId).subscribe({
          next: (teams) => {
            // Filter out the current team
            this.availableTeams = teams.filter(t => t.id !== this.team?.id);
          },
          error: (error) => {
            console.error('Error loading available teams:', error);
          }
        });
      }
    } else if (this.currentUser.role === RoleType.TeamManager) {
      // Get only teams managed by this user
      this.teamService.getTeamsByManagerId(this.currentUser.id).subscribe({
        next: (teams) => {
          // Filter out the current team
          this.availableTeams = teams.filter(t => t.id !== this.team?.id);
        },
        error: (error) => {
          console.error('Error loading available teams:', error);
        }
      });
    }
  }

  moveToTeam(): void {
    if (!this.selectedMember || !this.selectedTargetTeam || !this.team) return;
    
    this.hasOngoingTasks = false;
    this.moveErrorMessage = '';
    this.isMoving = true;
    
    this.teamService.moveMemberFromTeamToTeam(
      this.selectedMember.id,
      this.team.id,
      this.selectedTargetTeam.id
    ).pipe(
      takeUntil(this.destroy$),
      finalize(() => {
        this.isMoving = false;
      })
    ).subscribe({
      next: (response) => {
        console.log('Member moved successfully:', response);
        this.teamMembers = this.teamMembers.filter(member => member.id !== this.selectedMember?.id);
        this.selectedMember = null;
        this.selectedTargetTeam = null;
        this.showMoveToTeamConfirm = false;
        this.toastService.show('Team member moved successfully', 'success');
        this.teamsUpdated.emit();
      },
      error: (error) => {
        console.error('Error moving team member:', error);
        if (error?.error && typeof error.error === 'string' && error.error.includes('User has an ongoing task.')) {
          this.hasOngoingTasks = true;
          this.moveErrorMessage = error.error;
        } else if (error?.error?.message && error.error.message.includes('User has an ongoing task')) {
          this.hasOngoingTasks = true;
          this.moveErrorMessage = 'This user has ongoing tasks. Please reassign them before moving.';
        } else {
          this.selectedMember = null;
          this.selectedTargetTeam = null;
          this.showMoveToTeamConfirm = false;
          this.toastService.show('Failed to move team member. Please try again.', 'error');
        }
      }
    });
  }

  navigateToReassignTasks(): void {
    if (!this.selectedMember || !this.team) return;
    
    this.showMoveToTeamConfirm = false;
    this.router.navigate(['/okrs']);
  }

  removeFromTeam(member: User) {
    if (!this.team || !member) return;

    this.teamService.removeUserFromTeam(this.team.id, member.id)
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: () => {
        console.log('Member removed successfully');
        this.teamMembers = this.teamMembers.filter(m => m.id !== member.id);
        if (this.team && this.team.teamMembers) {
          this.team.teamMembers = this.team.teamMembers.filter(m => m.id !== member.id);
        }
        this.showRemoveConfirm = false;
        this.showMemberDrawer = false;
        this.selectedMember = null;
        this.toastService.show('Member removed from team', 'success');
        this.teamsUpdated.emit();
      },
      error: (error) => {
        console.error('Error removing member:', error);
        this.toastService.show('Failed to remove member', 'error');
      }
    });
  }

  shouldShowDropupMenu(member: User): boolean {
    const memberIndex = this.teamMembers.indexOf(member);
    const totalMembers = this.teamMembers.length;
    return memberIndex >= totalMembers - 2;
  }

  get canManageEmployees(): boolean {
    return this.currentUser?.role === RoleType.OrganizationAdmin || this.currentUser?.role === RoleType.SuperAdmin;
  }

  get isCollaborator(): boolean {
    return this.currentUser?.role === RoleType.Collaborator;
  }

  // New method to open the add collaborators modal
  openAddCollaboratorsModal() {
    if (!this.currentUser?.organizationId || !this.team) {
      this.toastService.show('Unable to load collaborators. Missing organization information.', 'error');
      return;
    }

    this.showAddCollaboratorsModal = true;
    this.selectedCollaborators = [];
    this.searchQuery = '';
    this.loadOrganizationCollaborators(this.currentUser.organizationId);
  }

  // Load collaborators from the organization
  private loadOrganizationCollaborators(organizationId: string) {
    this.organizationCollaborators = []; // Reset the array
    this.filteredCollaborators = []; // Reset filtered results too
    
    this.teamService.getCollaboratorsInOrganization(organizationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (collaborators) => {
          // Filter out collaborators already in this team
          const currentMemberIds = this.teamMembers.map(member => member.id);
          this.organizationCollaborators = collaborators.filter(
            collaborator => !currentMemberIds.includes(collaborator.id)
          );
          this.filteredCollaborators = [...this.organizationCollaborators];
        },
        error: (error) => {
          console.error('Error loading organization collaborators:', error);
          this.toastService.show('Failed to load collaborators', 'error');
        }
      });
  }

  // Handle search filtering
  filterCollaborators() {
    if (!this.searchQuery) {
      this.filteredCollaborators = [...this.organizationCollaborators];
      return;
    }

    const query = this.searchQuery.toLowerCase();
    this.filteredCollaborators = this.organizationCollaborators.filter(
      collaborator => 
        collaborator.firstName?.toLowerCase().includes(query) ||
        collaborator.lastName?.toLowerCase().includes(query) ||
        collaborator.email?.toLowerCase().includes(query) ||
        collaborator.position?.toLowerCase().includes(query)
    );
  }

  // Toggle selection of a collaborator
  toggleCollaboratorSelection(collaborator: UserDetailsWithRole) {
    const index = this.selectedCollaborators.findIndex(c => c.id === collaborator.id);
    
    if (index === -1) {
      this.selectedCollaborators.push(collaborator);
    } else {
      this.selectedCollaborators.splice(index, 1);
    }
  }

  // Check if a collaborator is selected
  isCollaboratorSelected(collaborator: UserDetailsWithRole): boolean {
    return this.selectedCollaborators.some(c => c.id === collaborator.id);
  }

  // Add selected collaborators to the team
  addSelectedCollaboratorsToTeam() {
    if (!this.team || this.selectedCollaborators.length === 0) {
      this.toastService.show('Please select at least one collaborator to add', 'error');
      return;
    }

    const userIds = this.selectedCollaborators.map(c => c.id);
    this.isAddingCollaborators = true;

    this.teamService.addUsersToTeam(this.team.id, userIds)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isAddingCollaborators = false)
      )
      .subscribe({
        next: (response) => {
          console.log('Added collaborators to team:', response);
          
          // Show success message
          this.toastService.show(response.message || 'Collaborators added successfully', 'success');
          
          // Emit teamsUpdated event so parent component refreshes data
          this.teamsUpdated.emit();
          
          // Close both modals
          this.showAddCollaboratorsModal = false;
          this.close.emit();
        },
        error: (error) => {
          console.error('Error adding collaborators to team:', error);
          this.toastService.show('Failed to add collaborators to team', 'error');
        }
      });
  }

  // Allow selecting all filtered collaborators
  selectAllCollaborators() {
    // Only add collaborators that aren't already selected
    this.filteredCollaborators.forEach(collaborator => {
      if (!this.isCollaboratorSelected(collaborator)) {
        this.selectedCollaborators.push(collaborator);
      }
    });
  }

  // Clear all selections
  clearAllSelections() {
    this.selectedCollaborators = [];
  }

  // Close the add collaborators modal
  closeAddCollaboratorsModal() {
    this.showAddCollaboratorsModal = false;
    this.selectedCollaborators = [];
  }
  
  // Check if user has permission to add collaborators
  get canAddCollaborators(): boolean {
    return this.currentUser?.role !== RoleType.Collaborator;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
} 