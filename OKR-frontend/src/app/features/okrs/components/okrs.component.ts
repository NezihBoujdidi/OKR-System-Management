import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy, NgZone } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { TimelineConfig } from '../../../models/timeline.interface';
import { OKRSession, CreateOkrCommand, UpdateOkrCommand } from '../../../models/okr-session.interface';
import { OKRSessionService } from '../../../services/okr-session.service';
import { TeamService } from '../../../services/team.service';
import { AuthStateService } from '../../../services/auth-state.service';
import { from, of, mergeMap, map, filter, toArray, catchError, Subscription, forkJoin } from 'rxjs';
import { Status } from '../../../models/Status.enum';
import { NewSessionFormComponent } from '../components/new-session-form/new-session-form.component';
import { HttpErrorResponse } from '@angular/common/http';
import { RoleType } from '../../../models/role-type.enum';

@Component({
  selector: 'app-okrs',
  templateUrl: './okrs.component.html'
})
export class OKRsComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('cardsContainer') cardsContainer?: ElementRef;
  @ViewChild('newSessionForm') newSessionForm?: NewSessionFormComponent;
  
  scrollThumbWidth = 20;
  scrollThumbPosition = 0;

  showSessionsSidebar = true;
  showNewSessionDrawer = false;
  showEditSessionDrawer = false;
  sessions: OKRSession[] = [];
  filteredSessions: OKRSession[] = [];
  displayedSessions: OKRSession[] = [];
  isLoading = false;
  selectedSession?: OKRSession;
  organizationId: string | undefined = undefined;
  isSuperAdmin = false;
  isOrgAdmin = false;
  organizationName = '';
  roleType = RoleType;
  isAuthorized = false;

  timelineConfig: TimelineConfig = {
    sessions: [],
    showYearNavigation: true,
    currentYear: new Date().getFullYear()
  };

  searchQuery = '';
  showFilters = false;
  currentTimelineYear = new Date().getFullYear();

  currentPage = 1;
  sessionsPerPage = 9;
  
  get totalPages(): number {
    return Math.ceil(this.filteredSessions.length / this.sessionsPerPage);
  }
  
  get pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }
  
  get paginatedSessions(): OKRSession[] {
    const startIndex = (this.currentPage - 1) * this.sessionsPerPage;
    return this.filteredSessions.slice(startIndex, startIndex + this.sessionsPerPage);
  }

  private subscriptions = new Subscription();

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private okrSessionService: OKRSessionService,
    private teamService: TeamService,
    public authStateService: AuthStateService,
    private ngZone: NgZone
  ) {}

  ngOnInit(): void {
    // Check user role
    const userRole = this.authStateService.getUserRole();
    this.isSuperAdmin = userRole === RoleType.SuperAdmin;
    this.isOrgAdmin = userRole === RoleType.OrganizationAdmin;
    const isTeamManager = userRole === RoleType.TeamManager;
    const isCollaborator = userRole === RoleType.Collaborator;
    
    // Set the isAuthorized flag for template use
    this.isAuthorized = this.isSuperAdmin || this.isOrgAdmin;
    
    const currentUser = this.authStateService.getCurrentUser();
    
    // Check for organization ID in route parameters or from user data
    this.subscriptions.add(
      this.route.params.subscribe(paramMap => {
        this.organizationId = paramMap['orgId'] || undefined;
        
        // If not from route params, and user is OrganizationAdmin, get from user data
        if (!this.organizationId && this.isOrgAdmin) {
          this.organizationId = currentUser?.organizationId || undefined;
        }
        
        if (this.organizationId) {
          // Load organization-specific sessions
          this.loadOrganizationSessions(this.organizationId);
        } else if (isTeamManager || isCollaborator) {
          // For Team Manager or Collaborator, load only their team sessions
          if (currentUser?.id) {
            this.loadUserTeamSessions(currentUser.id);
          } else {
            this.sessions = [];
            this.filteredSessions = [];
            this.updateTimelineConfig();
          }
        } else {
          // Load all sessions for SuperAdmin or other roles
          this.loadSessions();
        }
      })
    );
    
    this.subscriptions.add(
      this.okrSessionService.okrSessions$.subscribe(sessions => {
        // Only update sessions when we're not in organization context
        // and not a team manager or collaborator (who have their own loading logic)
        const isTeamMgrOrCollab = isTeamManager || isCollaborator;
        
        if (!this.organizationId && !isTeamMgrOrCollab) {
          this.sessions = sessions;
          this.filteredSessions = sessions;
          this.updateTimelineConfig();
          this.ngZone.runOutsideAngular(() => {
            setTimeout(() => {
              this.initializeCustomScrollbar();
            }, 100);
          });
        }
      })
    );

    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        if (params['openDrawer'] === 'true') {
          this.showNewSessionDrawer = true; 
        }
      })
    );

    // Set the organizationId for OrgAdmins
    if (this.isOrgAdmin && currentUser) {
      this.organizationId = currentUser?.organizationId || undefined;
    } else if (!this.isSuperAdmin) {
      // For regular users, clear organizationId
      this.organizationId = undefined;
    }
  }

  ngAfterViewInit(): void {
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        if (this.cardsContainer) {
          this.initializeCustomScrollbar();
        }
      }, 100);
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadSessions(): void {
    this.isLoading = true;
    this.subscriptions.add(
      this.okrSessionService.getOKRSessions().subscribe({
        next: (sessions) => {
          this.sessions = sessions;
          this.filteredSessions = sessions;
          this.updateTimelineConfig();
          this.loadInitialSessions();
          this.isLoading = false;
        },
        error: (error: HttpErrorResponse) => {
          this.isLoading = false;
          this.showErrorMessage(`Failed to load OKR sessions: ${error.status}`);
        }
      })
    );
  }

  private loadOrganizationSessions(orgId: string): void {
    this.isLoading = true;
    console.log(`Loading organization sessions for org ID: ${orgId}`);
    this.subscriptions.add(
      this.okrSessionService.getOKRSessionsByOrganizationId(orgId).subscribe({
        next: (sessions) => {
          console.log(`Received ${sessions.length} sessions for organization ${orgId}`);
          this.sessions = sessions;
          this.filteredSessions = sessions;
          this.updateTimelineConfig();
          this.loadInitialSessions();
          this.isLoading = false;
        },
        error: (error: HttpErrorResponse) => {
          this.isLoading = false;
          console.error(`Error loading organization sessions: ${error.status}`, error);
          this.showErrorMessage(`Failed to load organization sessions: ${error.status}`);
        }
      })
    );
  }

  private loadUserTeamSessions(userId: string): void {
    this.isLoading = true;
    console.log('Loading sessions for teams of user:', userId);
    
    // First get all teams the user is part of
    this.teamService.getTeamsByUserId(userId).subscribe({
      next: (teams) => {
        console.log(`User belongs to ${teams.length} teams`);
        
        if (!teams.length) {
          // If user has no teams, return empty sessions array
          this.sessions = [];
          this.filteredSessions = [];
          this.updateTimelineConfig();
          this.loadInitialSessions();
          this.isLoading = false;
          return;
        }
        
        // Get sessions for each team and combine them
        const teamIds = teams.map(team => team.id);
        const uniqueSessions: { [key: string]: OKRSession } = {};
        let completedRequests = 0;
        
        teamIds.forEach(teamId => {
          this.okrSessionService.getOKRSessionsByTeamId(teamId).subscribe({
            next: (sessions) => {
              console.log(`Received ${sessions.length} sessions for team ${teamId}`);
              // Add sessions to map to avoid duplicates
              sessions.forEach(session => {
                uniqueSessions[session.id] = session;
              });
              
              completedRequests++;
              
              // When all requests are complete, update the component
              if (completedRequests === teamIds.length) {
                const combinedSessions = Object.values(uniqueSessions);
                console.log(`Total unique sessions: ${combinedSessions.length}`);
                
                this.sessions = combinedSessions;
                this.filteredSessions = combinedSessions;
                this.updateTimelineConfig();
                this.loadInitialSessions();
                this.isLoading = false;
              }
            },
            error: (error) => {
              console.error(`Error fetching sessions for team ${teamId}:`, error);
              completedRequests++;
              
              // Continue even if one team fails
              if (completedRequests === teamIds.length) {
                const combinedSessions = Object.values(uniqueSessions);
                this.sessions = combinedSessions;
                this.filteredSessions = combinedSessions;
                this.updateTimelineConfig();
                this.loadInitialSessions();
                this.isLoading = false;
              }
            }
          });
        });
      },
      error: (error) => {
        console.error('Error fetching user teams:', error);
        this.isLoading = false;
        this.showErrorMessage('Failed to load user teams');
      }
    });
  }
 
  private initializeCustomScrollbar(): void {
    if (!this.cardsContainer?.nativeElement) return;
    
    const container = this.cardsContainer.nativeElement;
    
    if (container.scrollWidth <= container.clientWidth) {
      this.scrollThumbWidth = 100;
      return;
    }
    
    const scrollRatio = container.clientWidth / container.scrollWidth;
    this.scrollThumbWidth = Math.max(scrollRatio * 100, 20);
    
    container.addEventListener('scroll', () => {
      if (container.scrollWidth <= container.clientWidth) return;
      const scrollRatio = container.scrollLeft / (container.scrollWidth - container.clientWidth);
      const maxTranslate = 100 - this.scrollThumbWidth;
      this.scrollThumbPosition = scrollRatio * maxTranslate;
    });
  }

  toggleSessionsSidebar(): void {
    this.showSessionsSidebar = !this.showSessionsSidebar;
  }

  goBack(): void {
    if (this.organizationId && this.isSuperAdmin) {
      this.router.navigate(['/organizations']);
    } else {
      this.router.navigate(['/home']);
    }
  }

  onSearch(query: string): void {
    if (!query) {
      this.filteredSessions = [...this.sessions];
      this.currentPage = 1;
      return;
    }

    const searchStr = query.toLowerCase();
    
    const basicFiltered = this.sessions.filter(session => 
      session.title.toLowerCase().includes(searchStr)
    );
  }

  private updateTimelineConfig(): void {
    if (!this.sessions?.length) return;
    
    this.timelineConfig = {
      ...this.timelineConfig,
      sessions: this.sessions.map(session => ({
        id: session.id,
        title: `${session.title} (${session.startedDate} - ${session.endDate})`,
        startDate: new Date(session.startedDate).toISOString(),
        endDate: new Date(session.endDate).toISOString(),
        color: session.color || '#4299E1'
      }))
    };
  }

  createNewSession(): void {
    // Check permissions once and store the result
    const userRole = this.authStateService.getUserRole();
    const isAuthorized = userRole === RoleType.SuperAdmin || userRole === RoleType.OrganizationAdmin;
    
    if (!isAuthorized) {
      this.showErrorMessage('You do not have permission to create a session');
      return;
    }
    
    if (this.newSessionForm) {
      this.newSessionForm.resetForm();
      this.newSessionForm.isSubmitting = false;
    }
    this.showNewSessionDrawer = true;
  }

  onNewSessionSubmit(command: CreateOkrCommand): void {
    if (!command) {
      return;
    }

    // Check if user is authorized
    const userRole = this.authStateService.getUserRole();
    if (userRole !== RoleType.SuperAdmin && userRole !== RoleType.OrganizationAdmin) {
      this.showErrorMessage('You do not have permission to create a session');
      return;
    }

    // Determine which organization ID to use
    let organizationIdToUse: string | undefined;
    
    // For SuperAdmin, use the organization ID from the URL if available
    if (userRole === RoleType.SuperAdmin && this.organizationId) {
      organizationIdToUse = this.organizationId;
      console.log('SuperAdmin using organization ID from URL for new session:', organizationIdToUse);
    } else {
      // For other roles, use their own organization ID
      organizationIdToUse = this.authStateService.getCurrentUser()?.organizationId;
      console.log('Using user organization ID for new session:', organizationIdToUse);
    }

    // Create the command with the determined organization ID
    const finalCommand = {
      ...command,
      organizationId: organizationIdToUse
    };

    console.log('Creating new session with command:', finalCommand);
    
    this.subscriptions.add(
      this.okrSessionService.createOkrSession(finalCommand).subscribe({
        next: () => {
          this.showSuccessMessage('Session created successfully');
          
          if (this.newSessionForm) {
            this.newSessionForm.resetForm();
            this.newSessionForm.isSubmitting = false;
          }
          
          this.showNewSessionDrawer = false;
          this.currentTimelineYear = new Date(finalCommand.startedDate).getFullYear();
          this.updateTimelineConfig();

          // Reload sessions based on context
          if (this.organizationId) {
            this.loadOrganizationSessions(this.organizationId);
          } else {
            this.loadSessions();
          }
        },
        error: (error: HttpErrorResponse) => {
          this.showErrorMessage(`Failed to create session: ${error.status}`);
          
          if (this.newSessionForm) {
            this.newSessionForm.isSubmitting = false;
            this.newSessionForm.errorMessage = error.message || 'Error creating session';
          }
        }
      })
    );
  }

  onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeDrawer();
    }
  }
  
  onEditOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeEditDrawer();
    }
  }

  closeDrawer(): void {
    if (this.newSessionForm) {
      this.newSessionForm.resetForm();
      this.newSessionForm.isSubmitting = false;
    }
    this.showNewSessionDrawer = false;
  }

  formatPeriod(period: { startDate: string; endDate: string }): string {
    return `${period.startDate} - ${period.endDate}`;
  }

  onTimelineYearChange(year: number): void {
    this.currentTimelineYear = year;
    
    // Update the timeline config with the new year
    this.timelineConfig = {
      ...this.timelineConfig,
      currentYear: year,
      sessions: this.sessions.map(session => ({
        id: session.id,
        title: `${session.title} (${session.startedDate} - ${session.endDate})`,
        startDate: new Date(session.startedDate).toISOString(),
        endDate: new Date(session.endDate).toISOString(),
        color: session.color || '#4299E1'
      }))
    };
  }

  onSessionClick(sessionId: string): void {
    if (!this.showEditSessionDrawer) {
      const userRole = this.authStateService.getUserRole();
      const currentUser = this.authStateService.getCurrentUser();
      const isTeamManager = userRole === RoleType.TeamManager;
      const isCollaborator = userRole === RoleType.Collaborator;
      const isOrgAdmin = userRole === RoleType.OrganizationAdmin;
      
      // Determine the organization ID
      let orgId = this.organizationId;
      
      // If we don't have an organization ID from the route, but the user is a Team Manager, 
      // Collaborator, or Organization Admin, get it from the user object
      if (!orgId && (isTeamManager || isCollaborator || isOrgAdmin) && currentUser?.organizationId) {
        orgId = currentUser.organizationId;
      }
      
      if (orgId) {
        console.log(`Navigating to session detail with org context: /organizations/${orgId}/okrs/${sessionId}`);
        this.router.navigate(['/organizations', orgId, 'okrs', sessionId]);
      } else {
        console.log(`Navigating to session detail: /okrs/${sessionId}`);
        this.router.navigate(['/okrs', sessionId]);
      }
    }
  }

  loadInitialSessions(): void {
    this.displayedSessions = this.filteredSessions.slice(0, this.sessionsPerPage);
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  getPageNumbers(): number[] {
    const totalPages = Math.ceil(this.filteredSessions.length / this.sessionsPerPage);
    return Array.from({ length: totalPages }, (_, i) => i + 1);
  }

  onSessionMenuClick(event: {event: MouseEvent, session: OKRSession}): void {
    event.event.stopPropagation();
  }

  onSessionEdit(session: OKRSession): void {
    // Check permissions once and store the result
    const userRole = this.authStateService.getUserRole();
    const isAuthorized = userRole === RoleType.SuperAdmin || userRole === RoleType.OrganizationAdmin;
    
    if (!isAuthorized) {
      this.showErrorMessage('You do not have permission to edit a session');
      return;
    }
    
    console.log('Initial session data:', {
      id: session.id,
      title: session.title,
      teamIds: session.teamIds
    });
    
    // Make sure we have the complete session data with teams
    this.okrSessionService.getOKRSessionById(session.id).subscribe({
      next: (completeSession) => {
        console.log('Fetched complete session data:', {
          id: completeSession?.id,
          title: completeSession?.title,
          teamIds: completeSession?.teamIds,
          teamIdsType: completeSession?.teamIds ? typeof completeSession.teamIds : 'undefined',
          isArray: completeSession?.teamIds ? Array.isArray(completeSession.teamIds) : false,
          rawData: JSON.stringify(completeSession)
        });
        
        // Ensure all required properties exist before assigning
        if (completeSession && completeSession.id) {
          // Create a properly typed OKRSession object
          this.selectedSession = {
            id: completeSession.id,
            title: completeSession.title || '',
            description: completeSession.description || '',
            startedDate: completeSession.startedDate || new Date().toISOString(),
            endDate: completeSession.endDate || new Date().toISOString(),
            teamIds: completeSession.teamIds || [],
            userId: completeSession.userId || '',
            organizationId: completeSession.organizationId || '',
            status: completeSession.status || Status.NotStarted,
            color: completeSession.color || '#4299E1',
            isActive: completeSession.isActive !== undefined ? completeSession.isActive : true
          };
          
          console.log('Processed session for edit drawer:', {
            id: this.selectedSession.id,
            title: this.selectedSession.title,
            teamIds: this.selectedSession.teamIds
          });
          
          this.showEditSessionDrawer = true;
        } else {
          console.error('Incomplete session data received:', completeSession);
          this.showErrorMessage('Unable to load complete session data');
        }
    
    setTimeout(() => {
      if (this.newSessionForm) {
        this.newSessionForm.isSubmitting = false;
        
        if (this.newSessionForm.sessionForm) {
          this.newSessionForm.sessionForm.markAsPristine();
          this.newSessionForm.sessionForm.markAsUntouched();
        }
            
            // Log the team IDs in the form component
            console.log('Form component state:', {
              selectedTeamIds: this.newSessionForm.selectedTeamIds,
              availableTeams: this.newSessionForm.availableTeams.map(t => ({ id: t.id, name: t.name }))
            });
          }
        }, 500);
      },
      error: (error) => {
        console.error('Failed to get complete session data:', error);
        // Fall back to the session data we have, ensuring it's properly typed
        this.selectedSession = {
          id: session.id,
          title: session.title || '',
          description: session.description || '',
          startedDate: session.startedDate || new Date().toISOString(),
          endDate: session.endDate || new Date().toISOString(),
          teamIds: session.teamIds || [],
          userId: session.userId || '',
          organizationId: session.organizationId || '',
          status: session.status || Status.NotStarted,
          color: session.color || '#4299E1',
          isActive: session.isActive !== undefined ? session.isActive : true
        };
        this.showEditSessionDrawer = true;
      }
    });
  }

  closeEditDrawer(): void {
    if (this.newSessionForm) {
      this.newSessionForm.isSubmitting = false;
      this.newSessionForm.errorMessage = '';
    }
    this.showEditSessionDrawer = false;
    this.selectedSession = undefined;
  }

  updateSession(updates: UpdateOkrCommand): void {
    // Check permissions once and store the result
    const userRole = this.authStateService.getUserRole();
    const isAuthorized = userRole === RoleType.SuperAdmin || userRole === RoleType.OrganizationAdmin;
    
    if (!isAuthorized) {
      this.showErrorMessage('You do not have permission to update a session');
      return;
    }
    
    if (!this.selectedSession) {
      this.showErrorMessage('No session selected for update');
      return;
    }
    
    // Make sure we have teamIds as a non-empty array
    if (!updates.teamIds || !Array.isArray(updates.teamIds) || updates.teamIds.length === 0) {
      if (this.newSessionForm) {
        this.newSessionForm.errorMessage = 'At least one team must be selected';
        this.newSessionForm.isSubmitting = false;
      }
      return;
    }
    
    // Determine which organization ID to use
    let organizationIdToUse: string | undefined;
    
    // For SuperAdmin, use the organization ID from the URL if available
    if (userRole === RoleType.SuperAdmin && this.organizationId) {
      organizationIdToUse = this.organizationId;
      console.log('SuperAdmin using organization ID from URL for update:', organizationIdToUse);
    } else {
      // For other roles, use their own organization ID
      organizationIdToUse = this.authStateService.getCurrentUser()?.organizationId;
      console.log('Using user organization ID for update:', organizationIdToUse);
    }
    
    // Create an extended version of the update command with the organizationId
    const requestBody = {
      ...updates,
      organizationId: organizationIdToUse
    };
    
    const sessionId = this.selectedSession.id;
    console.log('Updating session', sessionId, 'with', requestBody);

    this.subscriptions.add(
      this.okrSessionService.updateOKRSession(sessionId, requestBody as UpdateOkrCommand).subscribe({
        next: () => {
          this.showSuccessMessage('Session updated successfully');
          
          if (this.newSessionForm) {
            this.newSessionForm.resetForm();
            this.newSessionForm.isSubmitting = false;
          }
          
          this.showEditSessionDrawer = false;
          this.handleSuccessfulUpdate();
        },
        error: (error: HttpErrorResponse) => {
            this.showErrorMessage(`Failed to update session: ${error.status}`);
            
          if (this.newSessionForm) {
              this.newSessionForm.isSubmitting = false;
            this.newSessionForm.errorMessage = error.message || 'Error updating session';
          }
        }
      })
    );
  }

  private handleSuccessfulUpdate(): void {
    if (this.newSessionForm) {
      this.newSessionForm.isSubmitting = false;
      this.newSessionForm.errorMessage = '';
    }

    this.showEditSessionDrawer = false;
    this.selectedSession = undefined;
    
    this.showSuccessMessage('Session updated successfully');
    
    // Reload sessions based on context
    if (this.organizationId) {
      // If we're in an organization context, clear cache and reload
      this.okrSessionService.clearOrganizationSessionsCache(this.organizationId);
      this.loadOrganizationSessions(this.organizationId);
    } else {
      // Otherwise load all sessions
      this.loadSessions();
    }
    
    // Update the timeline with the latest data
    this.updateTimelineConfig();
  }

  onSessionDeleted(sessionId: string): void {
    // Check permissions once and store the result
    const userRole = this.authStateService.getUserRole();
    const isAuthorized = userRole === RoleType.SuperAdmin || userRole === RoleType.OrganizationAdmin;
    
    if (!isAuthorized) {
      this.showErrorMessage('You do not have permission to delete a session');
      return;
    }
    
    console.log('Session deleted:', sessionId);
    
    // First check if the session is in our list
    const sessionIndex = this.sessions.findIndex(s => s.id === sessionId);
    if (sessionIndex === -1) {
      console.warn('Session not found in list:', sessionId);
      return;
    }
    
    // Remove the session from the list
    this.sessions = this.sessions.filter(s => s.id !== sessionId);
    
    // Update filtered sessions
    this.filteredSessions = this.sessions.filter(s => 
      this.searchQuery ? 
      s.title.toLowerCase().includes(this.searchQuery.toLowerCase()) : 
      true
    );
    
    // Update the timeline config
    this.updateTimelineConfig();
    
    this.showSuccessMessage('Session deleted successfully');
  }

  private showSuccessMessage(message: string): void {
    // Implementation would use a toast or notification service
    console.log('Success:', message);
  }

  private showErrorMessage(message: string): void {
    // Implementation would use a toast or notification service
    console.error('Error:', message);
  }

  get pageTitle(): string {
    return `Sessions`;
  }
}