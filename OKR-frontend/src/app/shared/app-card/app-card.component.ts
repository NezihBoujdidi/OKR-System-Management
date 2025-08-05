import { Component, Input, Output, EventEmitter, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { User } from '../../models/user.interface';
import { AuthStateService } from '../../services/auth-state.service';
import { RoleType } from '../../models/role-type.enum';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app-card.component.html'
})
export class AppCardComponent implements OnInit {
  @Input() title: string = '';
  @Input() description: string = '';
  @Input() teamManagerId: string = '';
  @Input() teamMembers: User[] = [];
  @Input() isActive: boolean = true;
  @Input() createdDate: Date = new Date();
  @Input() organizationId: string = '';
  @Input() managerName: string = '';
  @Input() managerRole: string = '';
  @Input() managerImage: string = '';
  @Input() hasEditPermission: boolean = false;

  @Output() editTeam = new EventEmitter<void>();
  @Output() deleteTeam = new EventEmitter<void>();
  @Output() cardClick = new EventEmitter<void>();

  showMenu = false;
  canEditTeam = false;

  constructor(private authState: AuthStateService) {}

  ngOnInit() {
    this.checkUserPermissions();
  }

  private checkUserPermissions() {
    const currentUser = this.authState.getCurrentUser();
    if (currentUser) {
      this.canEditTeam = this.hasEditPermission || 
                         this.authState.hasAnyRole([
                           RoleType.SuperAdmin, 
                           RoleType.OrganizationAdmin
                         ]);
    }
  }

  onMenuClick(event: Event) {
    event.stopPropagation();
    this.showMenu = !this.showMenu;
  }

  onEditClick(event: Event) {
    event.stopPropagation();
    this.showMenu = false;
    this.editTeam.emit();
  }

  onDeleteClick(event: Event) {
    event.stopPropagation();
    this.showMenu = false;
    this.deleteTeam.emit();
  }

  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (this.showMenu && !target.closest('.member-actions')) {
      this.showMenu = false;
    }
  }

  /* getStatusClass(): string {
     switch (this.status) {
      case 'on-track':
        return 'text-green-600 bg-green-50';
      case 'at-risk':
        return 'text-orange-600 bg-orange-50';
      case 'behind':
        return 'text-red-600 bg-red-50';
      default:
        return 'text-gray-600 bg-gray-50';
    } 
  }*/
} 