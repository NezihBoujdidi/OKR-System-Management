import { Component, OnInit, OnDestroy, Input, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { filter, takeUntil, map, finalize, switchMap } from 'rxjs/operators';
import { OKRSessionService } from '../../../services/okr-session.service';
import { Subject, Subscription, of } from 'rxjs';
import { OKRSession } from '../../../models/okr-session.interface';
import { Status } from 'src/app/models/Status.enum';
import { AuthStateService } from '../../../services/auth-state.service';
import { OrganizationService } from '../../../services/organization.service';
import { RoleType } from '../../../models/role-type.enum';
import { Organization } from '../../../models/organization.interface';
import { TeamService } from '../../../services/team.service';
import { Team } from '../../../models/team.interface';

interface SidebarItems {
  [key: string]: {
    title: string;
    items: {
      id: string;
      title: string;
      icon: string;
      route?: string;
      sessions?: Array<{
        id: string;
        title: string;
        period: string;
        status: Status;
        color: string;
      }>;
      organizations?: Array<{
        id: string;
        name: string;
        sessions?: Array<{
          id: string;
          title: string;
          period: string;
          status: Status;
          color: string;
        }>;
        isLoadingSessions?: boolean;
      }>;
      teams?: Array<{
        id: string;
        name: string;
        description: string;
        sessions?: Array<{
          id: string;
          title: string;
          period: string;
          status: Status;
          color: string;
        }>;
        isLoadingSessions?: boolean;
      }>;
      isActive?: boolean;
    }[];
  };
}

@Component({
  selector: 'app-content-sidebar',
  templateUrl: './content-sidebar.component.html'
})
export class ContentSidebarComponent implements OnInit, OnDestroy {
  @Input() activeSection: string = '';
  activeSessionId: string | null = null;
  activeOrganizationId: string | null = null;
  activeTeamId: string | null = null;
  currentSection: string = '';
  isSuperAdmin: boolean = false;
  isTeamManager: boolean = false;
  isCollaborator: boolean = false;
  organizations: Organization[] = [];
  userTeams: Team[] = [];
  teamSessions: {[teamId: string]: OKRSession[]} = {};
  // Expose RoleType enum for template access
  roleType = RoleType;
  private sessionSubscription?: Subscription;
  private routerSubscription?: Subscription;
  private organizationSubscription?: Subscription;
  private teamsSubscription?: Subscription;
  private teamSessionsSubscription?: Subscription;
  private destroy$ = new Subject<void>();

  sidebarItems: SidebarItems = {
    home: {
      title: 'Navigation',
      items: [
        {
          id: 'home',
          title: 'Home',
          route: '/home',
          icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6'
        },
        {
          id: 'dashboard',
          title: 'Dashboard',
          route: '/dashboard',
          icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z'
        }
      ]
    },
    okrs: {
      title: 'OKRs',
      items: [
        {
          id: 'all-sessions',
          title: 'All sessions',
          route: '/okrs',
          icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4',
          sessions: []
        },
        {
          id: 'all-organizations',
          title: 'All Organizations',
          icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
          organizations: []
        }
      ]
    },
    employees: {
      title: 'Employee Management',
      items: [
        {
          id: 'all-employees',
          title: 'All Employees',
          route: '/employees',
          icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z'
        },
        {
          id: 'teams',
          title: 'Teams',
          route: '/teams',
          icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z'
        },
        {
          id: 'all-organizations-employees',
          title: 'All Organizations',
          icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
          organizations: []
        }
      ]
    },
    manage: {
      title: 'Management',
      items: [
        {
          id: 'users',
          title: 'Users',
          route: '/users',
          icon: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z'
        },
        {
          id: 'organization',
          title: 'Organization',
          route: '/organization',
          icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4'
        }
      ]
    },
    settings: {
      title: 'Settings',
      items: [
        {
          id: 'users',
          title: 'Users',
          route: '/users',
          icon: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z'
        },
        {
          id: 'organization',
          title: 'Organization',
          route: '/organization',
          icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4'
        }
      ]
    },
    profile: {
      title: 'Profile',
      items: [
        { 
          id:'overall-view',
          title: 'Overall View',
          route: '/profile',
          icon: 'M5.121 17.804A13.937 13.937 0 0112 16c2.5 0 4.847.655 6.879 1.804M15 10a3 3 0 11-6 0 3 3 0 016 0zm6 2a9 9 0 11-18 0 9 9 0 0118 0z'
        },
        {  
          id: 'manage-account',
          title: 'Manage Account',
          route: '/profile/account',
          icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z'
        },
        {  
          id: 'password',
          title: 'Password',
          route: '/profile/password',
          icon: 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'
        },
        { 
          id: 'explanations-guidelines',
          title: 'Explanations & Guidelines',
          route: '/profile/guidelines',
          icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4'
        }
      ]
    },
    subscription: {
      title: 'Subscription',
      items: [
        {
          id: 'upgrade',
          title: 'Upgrade Plan',
          route: '/subscription/upgrade',
          icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6'
        }
      ]
    }
  };

  constructor(
    public router: Router,
    private okrSessionService: OKRSessionService,
    public authStateService: AuthStateService,
    private organizationService: OrganizationService,
    private teamService: TeamService,
    private changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit() {
    const userRole = this.authStateService.getUserRole();
    this.isSuperAdmin = userRole === RoleType.SuperAdmin;
    const isOrgAdmin = userRole === RoleType.OrganizationAdmin;
    this.isTeamManager = userRole === RoleType.TeamManager;
    this.isCollaborator = userRole === RoleType.Collaborator;
    
    const currentUser = this.authStateService.getCurrentUser();
    if (isOrgAdmin && currentUser?.organizationId) {
      this.activeOrganizationId = currentUser.organizationId;
      this.okrSessionService.getOKRSessionsByOrganizationId(currentUser.organizationId).subscribe();
    } else if ((this.isTeamManager || this.isCollaborator) && currentUser?.id) {
      this.loadUserTeams(currentUser.id);
    }
    
    this.updateCurrentSection();
    this.updateSidebarForRole();
    
    // For team managers and collaborators, ensure no sessions are loaded under "All OKR Sessions"
    if (this.isTeamManager || this.isCollaborator) {
      const okrsItems = this.sidebarItems['okrs'].items;
      const sessionsItem = okrsItems.find(item => item.id === 'all-sessions');
      if (sessionsItem) {
        sessionsItem.sessions = [];
      }
      
      // Subscribe to okrSessions$ to ensure the sessions list stays empty for these roles
      this.sessionSubscription = this.okrSessionService.okrSessions$.subscribe(() => {
        const sessionsItemCheck = this.sidebarItems['okrs']?.items.find(item => item.id === 'all-sessions');
        if (sessionsItemCheck) {
          sessionsItemCheck.sessions = [];
          this.changeDetectorRef.markForCheck();
        }
      });
    }
    
    if (this.isSuperAdmin) {
      this.loadOrganizations();
    } else if (!isOrgAdmin && !this.isTeamManager && !this.isCollaborator) {
      // Only load all sessions for roles other than TeamManager, Collaborator, and OrgAdmin
      this.okrSessionService.getOKRSessions().subscribe();
    }
    this.routerSubscription = this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        const url = event.urlAfterRedirects;
        if (!isOrgAdmin) {
          const orgMatch = url.match(/\/organizations\/([^\/]+)/);
          if (orgMatch && orgMatch[1]) {
            const orgId = orgMatch[1];
            this.updateOrganizationSessions(orgId);
            const sessionMatch = url.match(/\/okrs\/([^\/]+)/);
            this.activeSessionId = sessionMatch && sessionMatch[1] ? sessionMatch[1] : null;
      } else {
            this.activeOrganizationId = null;
            const sessionMatch = url.match(/\/okrs\/([^\/]+)/);
            this.activeSessionId = sessionMatch && sessionMatch[1] ? sessionMatch[1] : null;
          }
        }
        const sessionMatch = url.match(/\/okrs\/([^\/]+)/);
        this.activeSessionId = sessionMatch && sessionMatch[1] ? sessionMatch[1] : null;
      }
    });
  }

  private loadOrganizations() {
    this.organizationSubscription = this.organizationService.getOrganizations().subscribe(
      orgs => {
        this.organizations = orgs;
        
        // Update the organizations in the OKRs sidebar
        const allOrgsItemOkrs = this.sidebarItems['okrs'].items.find(item => item.id === 'all-organizations');
        if (allOrgsItemOkrs) {
          allOrgsItemOkrs.organizations = orgs.map(org => ({
            id: org.id,
            name: org.name,
            sessions: []
          }));
        }
        
        // Update the organizations in the Employees sidebar
        const allOrgsItemEmployees = this.sidebarItems['employees'].items.find(item => item.id === 'all-organizations-employees');
        if (allOrgsItemEmployees) {
          allOrgsItemEmployees.organizations = orgs.map(org => ({
            id: org.id,
            name: org.name
          }));
        }
      },
      error => console.error('Error loading organizations:', error)
    );
  }

  private updateOrganizationSessions(organizationId: string) {
    console.log('Update organization sessions: ', organizationId);
    
    if (!this.sidebarItems['okrs']) {
      console.error('OKRs section not found in sidebar items');
      return;
    }

    const okrItem = this.sidebarItems['okrs'].items.find(item => item.id === 'all-organizations');
    if (!okrItem || !okrItem.organizations) {
      console.error('All Organizations item not found in OKRs section');
      return;
    }

    const organization = okrItem.organizations.find(org => org.id === organizationId);
    if (!organization) {
      console.error(`Organization with ID ${organizationId} not found in sidebar`);
      return;
    }

    // Set loading state
    organization.isLoadingSessions = true;
    this.changeDetectorRef.markForCheck();
    console.log(`Loading organization sessions for org ID: ${organizationId}`);

    this.okrSessionService
      .getOKRSessionsByOrganizationId(organizationId)
      .pipe(
        map((sessions: OKRSession[]) =>
          sessions.map((session: OKRSession) => ({
              id: session.id,
              title: session.title,
              period: `${new Date(session.startedDate).toLocaleDateString()} - ${new Date(session.endDate).toLocaleDateString()}`,
              status: session.status,
              color: session.color || '#4299E1',
          }))
        ),
        finalize(() => {
          if (organization) {
            organization.isLoadingSessions = false;
            this.changeDetectorRef.markForCheck();
          }
        })
      )
      .subscribe({
        next: (sessions) => {
          if (organization) {
            organization.sessions = sessions;
            setTimeout(() => {
              this.changeDetectorRef.detectChanges();
            }, 0);
          }
        },
        error: () => {
          if (organization) {
            organization.sessions = [];
            this.changeDetectorRef.markForCheck();
          }
        }
      });
  }

  private loadUserTeams(userId: string) {
    this.teamsSubscription?.unsubscribe();
    this.teamsSubscription = this.teamService.getTeamsByUserId(userId).subscribe(teams => {
      this.userTeams = teams;
      
      // Update the sidebar items for OKRs section to include user teams
      const okrsItems = this.sidebarItems['okrs'].items;
      
      // Find or create the "My Teams" item
      let myTeamsItem = okrsItems.find(item => item.id === 'my-teams');
      
      if (!myTeamsItem) {
        myTeamsItem = {
          id: 'my-teams',
          title: 'My Teams',
          icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z',
          teams: teams.map(team => ({
            id: team.id,
            name: team.name,
            description: team.description || '',
            sessions: []
          }))
        };
        
        // Add the "My Teams" item to the okrs items (after the All OKR Sessions item)
        const allSessionsIndex = okrsItems.findIndex(item => item.id === 'all-sessions');
        if (allSessionsIndex !== -1) {
          okrsItems.splice(allSessionsIndex + 1, 0, myTeamsItem);
        } else {
          okrsItems.push(myTeamsItem);
        }
      } else {
        // Update the teams list
        myTeamsItem.teams = teams.map(team => ({
          id: team.id,
          name: team.name,
          description: team.description || '',
          sessions: []
        }));
      }
      
      // Now load combined sessions for all teams if user is TeamManager or Collaborator
      if (this.isTeamManager || this.isCollaborator) {
        this.loadAllTeamSessions(teams.map(team => team.id));
      }
      
      this.changeDetectorRef.markForCheck();
    });
  }

  // New method to load sessions from all teams combined
  private loadAllTeamSessions(teamIds: string[]) {
    if (!teamIds.length) return;
    
    // Clear any existing sessions
    const allSessionsItem = this.sidebarItems['okrs']?.items.find(item => item.id === 'all-sessions');
    if (allSessionsItem) {
      allSessionsItem.sessions = [];
    }
    
    // Load and combine sessions from all teams
    let combinedSessions: OKRSession[] = [];
    let completedCount = 0;
    
    teamIds.forEach(teamId => {
      this.okrSessionService.getOKRSessionsByTeamId(teamId).subscribe({
        next: (sessions) => {
          // Add sessions from this team to the combined list, avoiding duplicates
          sessions.forEach(session => {
            if (!combinedSessions.some(s => s.id === session.id)) {
              combinedSessions.push(session);
            }
          });
          
          completedCount++;
          
          // If all team requests are complete, update the sessions item
          if (completedCount === teamIds.length) {
            if (allSessionsItem) {
              allSessionsItem.sessions = combinedSessions.map(session => ({
                id: session.id,
                title: session.title,
                period: `${session.startedDate} - ${session.endDate}`,
                status: session.status,
                color: session.color || '#4299E1'
              }));
            }
            
            this.changeDetectorRef.markForCheck();
          }
        },
        error: (error) => {
          console.error(`Error loading sessions for team ${teamId}:`, error);
          completedCount++;
          
          // Even if there's an error, check if all requests are complete
          if (completedCount === teamIds.length) {
            if (allSessionsItem) {
              allSessionsItem.sessions = combinedSessions.map(session => ({
                id: session.id,
                title: session.title,
                period: `${session.startedDate} - ${session.endDate}`,
                status: session.status,
                color: session.color || '#4299E1'
              }));
            }
            
            this.changeDetectorRef.markForCheck();
          }
        }
      });
    });
  }

  loadTeamSessions(teamId: string) {
    if (this.teamSessions[teamId]) {
      // Sessions already loaded, no need to fetch again
      return;
    }
    
    this.teamSessionsSubscription?.unsubscribe();
    this.teamSessionsSubscription = this.okrSessionService.getOKRSessionsByTeamId(teamId).subscribe(sessions => {
      this.teamSessions[teamId] = sessions;
      
      // Update the sessions in the sidebar
      const myTeamsItem = this.sidebarItems['okrs'].items.find(item => item.id === 'my-teams');
      if (myTeamsItem && myTeamsItem.teams) {
        const team = myTeamsItem.teams.find(t => t.id === teamId);
        if (team) {
          team.sessions = sessions.map(session => ({
            id: session.id,
            title: session.title,
            period: `${session.startedDate} - ${session.endDate}`,
            status: session.status,
            color: session.color || '#4299E1'
          }));
        }
      }
      
      this.changeDetectorRef.markForCheck();
    });
  }

  // Add method to handle team click
  onTeamClick(teamId: string) {
    this.activeTeamId = teamId;
    this.loadTeamSessions(teamId);
    this.activeSessionId = null;
  }

  // Add method to check if team is active
  isTeamActive(teamId: string): boolean {
    return this.activeTeamId === teamId;
  }

  private updateSidebarForRole() {
    const userRole = this.authStateService.getUserRole();
    const currentUser = this.authStateService.getCurrentUser();
    
    // Add or remove billing items based on role
    if (userRole === RoleType.SuperAdmin) {
      // For SuperAdmin, ensure billing items are present in manage and settings sections
      // First check if the billing item exists in the manage section
      const manageItems = this.sidebarItems['manage'].items;
      const billingItemExists = manageItems.some(item => item.id === 'billing');
      
      // If not, add it
      if (!billingItemExists) {
        manageItems.push({
          id: 'billing',
          title: 'Billing & Subscription',
          route: '/subscription',
          icon: 'M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z'
        });
      }
      
      // Check if billing item exists in settings section
      const settingsItems = this.sidebarItems['settings'].items;
      const billingItemExistsInSettings = settingsItems.some(item => item.id === 'billing');
      
      // If not, add it
      if (!billingItemExistsInSettings) {
        settingsItems.push({
          id: 'billing',
          title: 'Billing & Subscription',
          route: '/subscription',
          icon: 'M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z'
        });
      }
      
      // SuperAdmin specific code
      const okrsItems = this.sidebarItems['okrs'].items;
      const allSessionsItem = okrsItems.findIndex(item => item.id === 'all-sessions');
      if (allSessionsItem !== -1) okrsItems.splice(allSessionsItem, 1);
      const allOrgsItem = okrsItems.find(item => item.id === 'all-organizations');
      if (allOrgsItem) allOrgsItem.title = 'All Organizations';
      const employeesItems = this.sidebarItems['employees'].items;
      const allEmployeesItem = employeesItems.findIndex(item => item.id === 'all-employees');
      if (allEmployeesItem !== -1) employeesItems.splice(allEmployeesItem, 1);
      const teamsItem = employeesItems.findIndex(item => item.id === 'teams');
      if (teamsItem !== -1) employeesItems.splice(teamsItem > allEmployeesItem ? teamsItem - 1 : teamsItem, 1);
      const allOrgsEmployeesItem = employeesItems.find(item => item.id === 'all-organizations-employees');
      if (allOrgsEmployeesItem) allOrgsEmployeesItem.title = 'All Organizations';
      this.loadOrganizations();
    } else {
      // For non-SuperAdmin users, remove billing items if they exist
      // Remove from manage section
      if (this.sidebarItems['manage'] && this.sidebarItems['manage'].items) {
        const manageItems = this.sidebarItems['manage'].items;
        const billingItemIndex = manageItems.findIndex(item => item.id === 'billing');
        if (billingItemIndex !== -1) {
          manageItems.splice(billingItemIndex, 1);
        }
      }
      
      // Remove from settings section
      if (this.sidebarItems['settings'] && this.sidebarItems['settings'].items) {
        const settingsItems = this.sidebarItems['settings'].items;
        const billingItemIndex = settingsItems.findIndex(item => item.id === 'billing');
        if (billingItemIndex !== -1) {
          settingsItems.splice(billingItemIndex, 1);
        }
      }
      
      if (userRole === RoleType.OrganizationAdmin) {
        const organizationId = currentUser?.organizationId;
        if (organizationId) {
          this.activeOrganizationId = organizationId;
          const sessionsItem = this.sidebarItems['okrs']?.items.find(item => item.id === 'all-sessions');
          if (sessionsItem) {
            sessionsItem.title = 'My Organization Sessions';
            // Clear any existing sessions to ensure we show fresh data
            sessionsItem.sessions = [];
          }
          
          // Fetch sessions for this organization
          this.okrSessionService.getOKRSessionsByOrganizationId(organizationId)
            .subscribe({
              next: sessions => {
                console.log('[Content Sidebar] Loaded organization sessions:', sessions);
                const sessionsItem = this.sidebarItems['okrs']?.items.find(item => item.id === 'all-sessions');
                if (sessionsItem) {
                  sessionsItem.sessions = sessions.map(session => ({
                    id: session.id,
                    title: session.title,
                    period: `${session.startedDate} - ${session.endDate}`,
                    status: session.status,
                    color: session.color || '#4299E1'
                  }));
                  this.changeDetectorRef.markForCheck();
                }
              },
              error: error => {
                console.error('[Content Sidebar] Error fetching organization sessions:', error);
              }
            });
        }
        
        // Remove organizations item for org admins
        const okrsItems = this.sidebarItems['okrs'].items;
        const allOrgsItem = okrsItems.findIndex(item => item.id === 'all-organizations');
        if (allOrgsItem !== -1) okrsItems.splice(allOrgsItem, 1);
      } else if (userRole === RoleType.TeamManager || userRole === RoleType.Collaborator) {
        // For Team Manager or Collaborator, load their teams
        if (currentUser?.id) {
          this.loadUserTeams(currentUser.id);
        }
        
        // Remove organization-specific items
        const okrsItems = this.sidebarItems['okrs'].items;
        const allOrgsItemIndex = okrsItems.findIndex(item => item.id === 'all-organizations');
        if (allOrgsItemIndex !== -1) {
          okrsItems.splice(allOrgsItemIndex, 1);
        }
        
        // Find the "All sessions" item and update its title but clear its sessions
        const sessionsItem = okrsItems.find(item => item.id === 'all-sessions');
        if (sessionsItem) {
          sessionsItem.title = 'All OKR Sessions';
          // Clear any sessions for team managers and collaborators
          sessionsItem.sessions = [];
          
          // Make sure it's the first item in the list
          if (okrsItems.indexOf(sessionsItem) !== 0) {
            // Remove it from current position
            okrsItems.splice(okrsItems.indexOf(sessionsItem), 1);
            // Add it at the beginning
            okrsItems.unshift(sessionsItem);
          }
        }
      } else {
        const okrsItems = this.sidebarItems['okrs'].items;
        const allOrgsItemOkrs = okrsItems.findIndex(item => item.id === 'all-organizations');
        if (allOrgsItemOkrs !== -1) okrsItems.splice(allOrgsItemOkrs, 1);
        const employeesItems = this.sidebarItems['employees'].items;
        const allOrgsItemEmployees = employeesItems.findIndex(item => item.id === 'all-organizations-employees');
        if (allOrgsItemEmployees !== -1) employeesItems.splice(allOrgsItemEmployees, 1);
        if (!employeesItems.some(item => item.id === 'all-employees')) {
          employeesItems.push({
            id: 'all-employees',
            title: 'All Employees',
            route: '/employees',
            icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z'
          });
        }
        if (!employeesItems.some(item => item.id === 'teams')) {
          employeesItems.push({
            id: 'teams',
            title: 'Teams',
            route: '/teams',
            icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z'
          });
        }
        
        // Only load OKR sessions for roles other than TeamManager and Collaborator
        if (userRole !== RoleType.TeamManager && userRole !== RoleType.Collaborator) {
          this.okrSessionService.getOKRSessions().subscribe();
        }
      }
    }
  }

  ngOnDestroy() {
    this.sessionSubscription?.unsubscribe();
    this.routerSubscription?.unsubscribe();
    this.organizationSubscription?.unsubscribe();
    this.teamsSubscription?.unsubscribe();
    this.teamSessionsSubscription?.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  updateContent(section: string) {
    this.currentSection = section;
  }

  private updateCurrentSection() {
    // If activeSection is explicitly provided, use that instead of deriving from URL
    if (this.activeSection) {
      this.currentSection = this.activeSection;
      return;
    }
    
    const url = this.router.url;
    // Check dashboard and home routes first with more specific checks
    if (url.startsWith('/home') || url.startsWith('/dashboard')) {
      this.currentSection = 'home';
      return;
    } else if (url.includes('/employees') || url.includes('/teams') || 
              (url.includes('/organizations/') && url.includes('/employees')) || 
              url.includes('/OrgTeams/')) {
      this.currentSection = 'employees';
    } else if (url.includes('/organization') || url.includes('/users') || url.includes('/subscription')) {
      this.currentSection = 'manage';
    } else if (url.includes('/okrs')) {
      this.currentSection = 'okrs';
    } else if (url.includes('/profile')) {
      this.currentSection = 'profile';
    } else {
      this.currentSection = 'home';
    }
  }

  navigateToRoute(route: string) {
    console.log(`Navigating to route: ${route}`);
    
    // Handle session routes
    const sessionMatch = route.match(/\/okrs\/([^\/]+)/);
    if (sessionMatch && sessionMatch[1]) {
      const sessionId = sessionMatch[1];
      this.activeSessionId = sessionId;
      
      // If we're an OrgAdmin, use organization route format instead
      const isOrgAdmin = this.authStateService.getUserRole() === RoleType.OrganizationAdmin;
      if (isOrgAdmin && this.activeOrganizationId) {
        this.router.navigateByUrl(`/organizations/${this.activeOrganizationId}/okrs/${sessionId}`);
        return;
      }
    } else {
      // Handle organization routes
      const orgMatch = route.match(/\/organizations\/([^\/]+)/);
      if (orgMatch && orgMatch[1]) {
        const orgId = orgMatch[1];
        this.activeOrganizationId = orgId;
        
        // When navigating to organization's base route, ensure sessions are loaded
        if (!route.includes('/okrs/')) {
          this.updateOrganizationSessions(orgId);
          this.activeSessionId = null;
        } else {
          // Handle organization session routes: /organizations/:orgId/okrs/:sessionId
          const orgSessionMatch = route.match(/\/organizations\/[^\/]+\/okrs\/([^\/]+)/);
          if (orgSessionMatch && orgSessionMatch[1]) {
            this.activeSessionId = orgSessionMatch[1];
          }
        }
      } else {
        // Check if we're an OrgAdmin - if so, don't clear activeOrganizationId
        const isOrgAdmin = this.authStateService.getUserRole() === RoleType.OrganizationAdmin;
        if (!isOrgAdmin) {
          this.activeOrganizationId = null;
        }
      }
    }
    this.router.navigateByUrl(route);
  }

  navigateToOrganizationSession(orgId: string, sessionId: string) {
    console.log(`Navigating to organization session: ${orgId} -> ${sessionId}`);
    
    // First ensure sessions are loaded/updated
    this.updateOrganizationSessions(orgId);
    
    // Set active IDs
    this.activeOrganizationId = orgId;
    this.activeSessionId = sessionId;
    
    // Navigate to the session
    this.router.navigate(['/organizations', orgId, 'okrs', sessionId]);
  }

  reloadOrganizationSessionsIfEmpty(orgId: string) {
    console.log(`Checking if organization sessions need reloading for org: ${orgId}`);
    
    // Get the organization in the sidebar items
    const orgsItem = this.sidebarItems['okrs']?.items.find(item => item.id === 'all-organizations');
    if (!orgsItem || !orgsItem.organizations) {
      console.log('Could not find organizations item in sidebar');
      return;
    }
    
    // Find the specific organization
    const org = orgsItem.organizations.find(o => o.id === orgId);
    
    // If the organization is found but has no sessions or empty sessions array, reload them
    if (org) {
      console.log(`Found organization ${orgId} with ${org.sessions?.length || 0} sessions`);
      if (!org.sessions || org.sessions.length === 0) {
        console.log(`Organization ${orgId} has no sessions, reloading...`);
        this.updateOrganizationSessions(orgId);
      }
    } else {
      console.log(`Organization ${orgId} not found in sidebar items`);
    }
  }

  isRouteActive(route: string): boolean {
    if (!route) return false;
    
    // Basic OKR route checks - special case for Organization Admin
    if (route === '/okrs' && this.router.url === '/okrs') {
      // For organization admins, always return true for the root "/okrs" path
      // since this is where their organization sessions are listed
      if (this.authStateService.getUserRole() === RoleType.OrganizationAdmin) {
        return true;
      }
      
      // For super admins on /okrs, we don't highlight this item
      // since we want to highlight "All Organizations" instead
      if (this.isSuperAdmin) {
        return false;
      }
      
      // For other users, only return true if we're not viewing a specific session or organization
      return !this.activeOrganizationId && !this.activeSessionId;
    }
    
    if (route === '/okrs' && this.activeSessionId) return false;
    if (route === '/okrs' && this.activeOrganizationId) return false;
    
    // Special case for teams route
    if (route === '/teams' && this.router.url.includes('/teams/')) {
      return true;
    }

    // Special case for employees route
    if (route === '/employees' && this.router.url.includes('/employees/')) {
      return true;
    }

    // Special cases for home and dashboard
    if (route === '/home' && this.router.url.includes('/home/')) {
      return true;
    }
    
    if (route === '/dashboard' && this.router.url.includes('/dashboard/')) {
      return true;
    }
    
    // Check for exact organization employee routes
    if (route.includes('/organizations/') && route.includes('/employees')) {
      return this.router.url.includes(route);
    }
    
    // Check for OrgTeams routes
    if (route.includes('/OrgTeams/') && route.includes('/teams')) {
      return this.router.url.includes(route);
    }
    
    return this.router.url === route;
  }

  isOrganizationActive(orgId: string): boolean {
    if (!orgId) return false;
    
    // Check if the URL contains the organization ID in organizations path
    if (this.router.url.includes(`/organizations/${orgId}`)) {
      this.activeOrganizationId = orgId;
      return true;
    }
    
    // Check if the URL contains the organization ID in OrgTeams path
    if (this.router.url.includes(`/OrgTeams/${orgId}`)) {
      this.activeOrganizationId = orgId;
      return true;
    }
    
    return this.activeOrganizationId === orgId;
  }

  getTitle(): string {
    switch(this.currentSection) {
      case 'home':
        return 'Navigation';
      case 'employees':
        return 'Employees';
      case 'okrs':
        return 'OKRs';
      case 'manage':
        return 'Management';
      case 'profile':
        return 'Profile Settings';
      case 'subscription':
        return 'Subscription';
      default:
        return 'Navigation';
    }
  }

  isSessionActive(sessionId: string): boolean {
    return this.activeSessionId === sessionId;
  }

  get subscriptions() {
    return new Subscription();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['activeSection'] && !changes['activeSection'].firstChange) {
      this.updateCurrentSection();
    }
  }

  // Update the navigation for team sessions
  navigateToTeamSession(sessionId: string) {
    const currentUser = this.authStateService.getCurrentUser();
    const orgId = currentUser?.organizationId;
    
    if (orgId) {
      // If user has an organization ID, use it for proper routing
      this.activeSessionId = sessionId;
      this.router.navigate(['/organizations', orgId, 'okrs', sessionId]);
    } else {
      // Fallback to regular session navigation
      this.navigateToRoute(`/okrs/${sessionId}`);
    }
  }
}