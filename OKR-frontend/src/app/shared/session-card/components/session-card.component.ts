import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { OKRSession } from 'src/app/models/okr-session.interface';
import { Status } from 'src/app/models/Status.enum';
import { OKRSessionService } from 'src/app/services/okr-session.service';
import { Subscription } from 'rxjs';
import { Router } from '@angular/router';
import { ToastService } from 'src/app/shared/services/toast.service';
import { MenuStateService } from 'src/app/services/menu-state.service';
import { AuthStateService } from 'src/app/services/auth-state.service';
import { RoleType } from 'src/app/models/role-type.enum';

@Component({
  selector: 'app-session-card',
  templateUrl: './session-card.component.html'
})
export class SessionCardComponent implements OnInit, OnDestroy {
  @Input() session!: OKRSession;
  @Output() sessionClick = new EventEmitter<string>();
  @Output() menuClick = new EventEmitter<{event: MouseEvent, session: OKRSession}>();
  @Output() sessionDeleted = new EventEmitter<string>();
  @Output() sessionEdit = new EventEmitter<OKRSession>();

  // Add Status enum for template usage
  Status = Status;
  // Add Role enum for template usage
  roleType = RoleType;

  sessionTeamManagerName: string = '';
  sessionTeamManagerAvatar: string = '';
  private sessionTeamManagerSubscription?: Subscription;

  showMenu = false;
  isDeletingSession = false;
  showDeleteConfirm = false;
  private menuSubscription?: Subscription;

  constructor(
    private sessionService: OKRSessionService,
    private router: Router,
    private toastService: ToastService,
    private menuStateService: MenuStateService,
    public authStateService: AuthStateService
  ) {}

  ngOnInit() {
    console.log('Session data:', {
      id: this.session.id,
      title: this.session.title,
      status: this.session.status,
      statusType: typeof this.session.status
    });
    this.loadOwnerDetails();
    
    // Subscribe to menu state changes
    this.menuSubscription = this.menuStateService.activeMenuId$.subscribe(activeMenuId => {
      if (activeMenuId !== this.session.id) {
        this.showMenu = false;
      }
    });
  }

  ngOnDestroy() {
    document.removeEventListener('click', this.closeMenu);
    if (this.sessionTeamManagerSubscription) {
      this.sessionTeamManagerSubscription.unsubscribe();
    }
    this.menuSubscription?.unsubscribe();
  }

  private loadOwnerDetails() {
    if (!this.session.userId) {
      console.warn('No userId provided for session:', this.session);
      return;
    }

    // this.sessionTeamManagerSubscription = this.sessionService.getSessionTeamManager(this.session.teamManagerId)
    //   .subscribe({
    //     next: (sessionTeamManager) => {
    //       console.log('Session Team Manager details received:', sessionTeamManager);
    //       if (sessionTeamManager) {
    //         this.sessionTeamManagerName = sessionTeamManager.firstName ? `${sessionTeamManager.firstName} ${sessionTeamManager.lastName}` : 'Unknown User';
    //         this.sessionTeamManagerAvatar = sessionTeamManager.profilePictureUrl || '';
    //       } else {
    //         console.warn('No owner details returned for userId:', this.session.userId);
    //         this.sessionTeamManagerName = 'Unknown User';
    //       }
    //     },
    //     error: (error) => {
    //       console.error('Error fetching owner details:', error);
    //       this.sessionTeamManagerName = 'Error loading user';
    //     }
    //   });
  }

  onCardClick() {
    if (!this.showMenu && !this.isDeletingSession && !this.showDeleteConfirm) {
      this.sessionClick.emit(this.session.id);
    }
  }

  toggleMenu(event: MouseEvent) {
    event.stopPropagation();
    
    if (this.showMenu) {
      this.showMenu = false;
      this.menuStateService.setActiveMenu(null);
    } else {
      this.menuStateService.setActiveMenu(this.session.id);
      this.showMenu = true;
    }

    if (this.showMenu) {
      // Add click listener to close menu when clicking outside
      setTimeout(() => {
        document.addEventListener('click', this.closeMenu);
      });
    }
  }

  private closeMenu = () => {
    this.showMenu = false;
    this.menuStateService.setActiveMenu(null);
    document.removeEventListener('click', this.closeMenu);
  }

  onEdit(event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    
    // Check permissions
    if (this.authStateService.getUserRole() !== RoleType.SuperAdmin && 
        this.authStateService.getUserRole() !== RoleType.OrganizationAdmin) {
      this.toastService.showError('You do not have permission to edit a session');
      return;
    }
    
    console.log('Edit clicked for session:', this.session);
    this.showMenu = false;
    this.sessionEdit.emit(this.session);
  }

  onDelete(event: MouseEvent) {
    event.stopPropagation();
    
    // Check permissions
    if (this.authStateService.getUserRole() !== RoleType.SuperAdmin && 
        this.authStateService.getUserRole() !== RoleType.OrganizationAdmin) {
      this.toastService.showError('You do not have permission to delete a session');
      return;
    }
    
    this.showMenu = false;
    this.showDeleteConfirm = true; // Show confirmation dialog instead of immediate delete
  }

  confirmDelete(event: MouseEvent) {
    event.stopPropagation();
    this.showDeleteConfirm = false;
    this.isDeletingSession = true;

    console.log('Attempting to delete session:', this.session.id);

    this.sessionService.deleteOKRSession(this.session.id).subscribe({
      next: () => {
        console.log('Delete successful in component');
        
        // Keep spinner visible for a bit longer after success
        setTimeout(() => {
          this.isDeletingSession = false;
          this.sessionDeleted.emit(this.session.id);
        }, 1000);
      },
      error: (error) => {
        console.log('Delete error in component:', error);
        
        if (error.status === 200 || error.status === 204) {
          // Keep spinner visible for a bit longer after success
          setTimeout(() => {
            this.isDeletingSession = false;
            this.sessionDeleted.emit(this.session.id);
          }, 1000);
        } else {
          console.error('Delete failed:', error);
          this.isDeletingSession = false;
        }
      }
    });
  }

  cancelDelete(event: MouseEvent) {
    event.stopPropagation();
    this.showDeleteConfirm = false;
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(part => part[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  getStatusText(status: Status | null | undefined): string {
    if (status === null || status === undefined) {
      return 'Not Started';
    }
    
    switch (status) {
      case Status.NotStarted:
        return 'Not Started';
      case Status.InProgress:
        return 'In Progress';
      case Status.Completed:
        return 'Completed';
      case Status.Overdue:
        return 'Overdue';
      default:
        console.warn('Unhandled status value:', status);
        return 'Not Started'; // Default fallback
    }
  }

  getStatusBadgeClass(): string {
    const baseClasses = 'text-sm font-medium px-2.5 py-1 rounded-full';
    const status = this.session.status;
    
    if (status === null || status === undefined) {
      return `${baseClasses} bg-blue-50 text-blue-700`; // Default to Not Started style
    }
    
    switch (status) {
      case Status.NotStarted:
        return `${baseClasses} bg-blue-50 text-blue-700`;
      case Status.InProgress:
        return `${baseClasses} bg-green-50 text-green-700`;
      case Status.Completed:
        return `${baseClasses} bg-gray-50 text-gray-700`;
      case Status.Overdue:
        return `${baseClasses} bg-emerald-50 text-emerald-700`;
      default:
        return `${baseClasses} bg-blue-50 text-blue-700`; // Default to Not Started style
    }
  }

  getProgress(): number {
    return this.session.progress || 0;
  }

  getTeamMembersCount(): number {
    return this.session.teamIds.length || 0;
  }
}