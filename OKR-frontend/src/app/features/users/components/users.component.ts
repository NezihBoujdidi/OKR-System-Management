import { Component, OnInit, HostListener } from '@angular/core';
import { TableConfig } from '../../../shared/interfaces/table.interface';
import { UserService } from '../../../services/user.service';
import { OrganizationService } from '../../../services/organization.service';
import { User, RoleType } from '../../../models/user.interface';
import { Organization } from '../../../models/organization.interface';
import { SupabaseAuthService } from '../../../services/supabase-auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { GenerateInvitationLinkCommand } from '../../../models/auth.interface';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styles: [`
    .relative.inline-block {
      position: relative;
    }
    .fixed-dropdown {
      position: absolute;
      top: calc(100% + 0.5rem);
      left: 0;
      transform: none;
      z-index: 50;
      width: 18rem;
      background: white;
      border: 1px solid rgba(0, 0, 0, 0.1);
      border-radius: 0.5rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
    }
    app-sort-button ::ng-deep .sort-dropdown {
      left: 0 !important;
      transform: none !important;
      width: 18rem !important;
    }
  `]
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  filteredUsers: User[] = [];
  organizations: Organization[] = [];
  organizationsMap: Map<string, string> = new Map(); // Map of organizationId to name
  showFilters = false;
  showSorting = false;
  showAddModal = false;
  isLoading = false;
  errorMessage = '';
  showLoadingOverlay = false;
  loadingMessage = '';
  
  // Invite user properties
  inviteEmail: string = '';
  selectedOrganizationId: string | null = null;
  selectedRole: RoleType | null = null;
  availableRoles = Object.values(RoleType);
  isInviting = false;
  emailError = '';
  organizationError = '';
  roleError = '';
  
  // User actions properties
  selectedUserForActions: User | null = null;
  dropdownPosition: { x: number; y: number } | null = null;
  
  // User details drawer properties
  showUserDetailsDrawer = false;
  selectedUser: User | null = null;
  userForm: any = {};
  isUpdating = false;
  showDeleteConfirm = false;
  userToDelete: User | null = null;
  showEnableConfirm = false;
  userToEnable: User | null = null;

  currentSort = {
    field: '',
    direction: 'asc'
  };

  activeFilters = {
    roles: [] as RoleType[],
    status: [] as boolean[]
  };

  tableConfig: TableConfig = {
    columns: [
      { 
        key: 'profilePictureUrl', 
        label: 'PROFILE', 
        type: 'image'
      },
      { 
        key: 'fullName', 
        label: 'NAME', 
        type: 'text'
      },
      { key: 'email', label: 'EMAIL', type: 'email' },
      { key: 'organization', label: 'ORGANIZATION', type: 'text' },
      { key: 'role', label: 'ROLE', type: 'badge', badgeType: 'role' },
      { key: 'statusLabel', label: 'STATUS', type: 'badge', badgeType: 'status' },
      { key: 'actions', label: '', type: 'actions', width: '70px' }
    ],
    data: [],
    itemsPerPage: 10,
    enablePagination: true,
    source: 'users'
  };

  sortOptions = [
    { value: 'fullName', label: 'Name' },
    { value: 'email', label: 'Email' },
    { value: 'organization', label: 'Organization' },
    { value: 'isEnabled', label: 'Status' }
  ];

  // Add filter labels for better UI display
  statusFilterLabels: Record<string, string> = {
    'true': 'Enabled',
    'false': 'Disabled'
  };

  constructor(
    private userService: UserService,
    private organizationService: OrganizationService,
    private supabaseAuth: SupabaseAuthService,
    private toastService: ToastService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.fetchAllOrganizations();
    this.fetchAllUsers();
  }

  fetchAllOrganizations() {
    this.organizationService.getOrganizations().subscribe({
      next: (organizations) => {
        this.organizations = organizations;
        
        // Create a map for quick lookup of organization names by ID
        this.organizationsMap = new Map<string, string>();
        organizations.forEach(org => {
          this.organizationsMap.set(org.id, org.name);
        });
        
        console.log('Organizations loaded:', this.organizations);
        
        // If users are already loaded, update their organization names
        if (this.users.length > 0) {
          this.updateTableData();
        }
      },
      error: (error) => {
        console.error('Error fetching organizations:', error);
      }
    });
  }

  fetchAllUsers() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.userService.getUsers().subscribe({
      next: (users: User[]) => {
        console.log('Received users in component:', users);
        
        // Transform the users for display
        if (Array.isArray(users)) {
          this.users = users.map((user: User) => ({
            ...user,
            fullName: `${user.firstName} ${user.lastName}`,
            // Ensure we have a default profile picture
            profilePictureUrl: user.profilePictureUrl || 'assets/default-avatar.png'
          }));
          
          this.filteredUsers = [...this.users];
          this.updateTableData();
        } else {
          console.error('Expected an array of users but received:', users);
          this.errorMessage = 'Failed to process users data.';
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching users:', error);
        this.errorMessage = 'Failed to load users. Please try again later.';
        this.isLoading = false;
      }
    });
  }

  // Filter methods
  toggleFilters(event?: Event) {
    if (event) {
      event.stopPropagation();
    }
    this.showFilters = !this.showFilters;
    if (this.showFilters) {
      this.showSorting = false;
    }
  }

  toggleSorting(event?: Event) {
    if (event) {
      event.stopPropagation();
    }
    this.showSorting = !this.showSorting;
    if (this.showSorting) {
      this.showFilters = false;
    }
  }

  closeFilters() {
    this.showFilters = false;
  }

  closeSorting() {
    this.showSorting = false;
  }

  onSearch(event: Event) {
    const searchTerm = (event.target as HTMLInputElement).value.toLowerCase();
    this.filteredUsers = this.users.filter(user => {
      // Get organization name if it exists
      const organizationName = user.organizationId 
        ? this.organizationsMap.get(user.organizationId)?.toLowerCase() || ''
        : '';
        
      return (user.fullName || `${user.firstName} ${user.lastName}`).toLowerCase().includes(searchTerm) ||
        user.email.toLowerCase().includes(searchTerm) ||
        user.role.toLowerCase().includes(searchTerm) ||
        organizationName.includes(searchTerm);
    });
    this.updateTableData();
  }

  // Sorting methods
  applySorting(field: string) {
    if (this.currentSort.field === field) {
      this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
      this.currentSort.field = field;
      this.currentSort.direction = 'asc';
    }
    this.applyFilters();
  }

  clearSort() {
    this.currentSort = { field: '', direction: 'asc' };
    this.showSorting = false;
    this.filteredUsers = [...this.users];
    this.updateTableData();
  }

  // Filter methods
  applyFilters() {
    this.filteredUsers = this.users.filter(user => {
      const roleFilter = this.activeFilters.roles.length === 0 || 
                        this.activeFilters.roles.includes(user.role);
      const statusFilter = this.activeFilters.status.length === 0 || 
                          this.activeFilters.status.includes(user.isEnabled);
      return roleFilter && statusFilter;
    });

    if (this.currentSort.field) {
      if (this.currentSort.field === 'organization') {
        // Special handling for organization sorting
        this.filteredUsers.sort((a, b) => {
          const aOrgName = a.organizationId ? (this.organizationsMap.get(a.organizationId) || '') : '';
          const bOrgName = b.organizationId ? (this.organizationsMap.get(b.organizationId) || '') : '';
          
          const comparison = aOrgName.toLowerCase().localeCompare(bOrgName.toLowerCase());
          return this.currentSort.direction === 'asc' ? comparison : -comparison;
        });
      } else {
        this.filteredUsers.sort((a: any, b: any) => {
          const aValue = String(a[this.currentSort.field]).toLowerCase();
          const bValue = String(b[this.currentSort.field]).toLowerCase();
          
          return this.currentSort.direction === 'asc' 
            ? aValue.localeCompare(bValue)
            : bValue.localeCompare(aValue);
        });
      }
    }

    this.updateTableData();
  }

  private updateTableData() {
    this.tableConfig = {
      ...this.tableConfig,
      data: this.filteredUsers.map(user => {
        // Get organization name if it exists
        const organizationName = user.organizationId 
          ? this.organizationsMap.get(user.organizationId) || 'Unknown'
          : 'None';

        return {
          ...user,
          // Ensure these fields are properly formatted 
          profilePictureUrl: user.profilePictureUrl || 'assets/default-avatar.png',
          fullName: `${user.firstName} ${user.lastName}`,
          isEnabled: user.isEnabled, // keep as boolean for logic
          statusLabel: user.isEnabled ? 'Enabled' : 'Disabled', // for display
          // Add organization name
          organization: organizationName
        };
      })
    };
  }

  // User actions
  onUserClick(user: any) {
    console.log('User clicked:', user);
  }

  addUser() {
    // Reset invite form fields
    this.inviteEmail = '';
    this.selectedOrganizationId = null;
    this.selectedRole = null;
    this.emailError = '';
    this.organizationError = '';
    this.roleError = '';
    
    // Show the invite modal
    this.showAddModal = true;
  }

  closeAddModal() {
    this.showAddModal = false;
  }

  async submitInvite() {
    // Reset error messages
    this.emailError = '';
    this.organizationError = '';
    this.roleError = '';
    
    // Validate inputs
    let isValid = true;
    
    if (!this.inviteEmail) {
      this.emailError = 'Email is required';
      isValid = false;
    } else if (!this.isValidEmail(this.inviteEmail)) {
      this.emailError = 'Please enter a valid email address';
      isValid = false;
    }
    
    if (!this.selectedOrganizationId) {
      this.organizationError = 'Organization is required';
      isValid = false;
    }
    
    if (!this.selectedRole) {
      this.roleError = 'Role is required';
      isValid = false;
    }
    
    if (!isValid) return;
    
    this.isInviting = true;
    this.showLoadingOverlay = true;
    this.loadingMessage = 'Inviting user...';
    
    // Add a delay for the loading overlay to be visible
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    // Use generateInvitationLink instead of inviteByEmail
    const command: GenerateInvitationLinkCommand = {
      email: this.inviteEmail,
      roleName: this.selectedRole!,
      organizationId: this.selectedOrganizationId!,
      // teamId: undefined // add if needed
    };
    this.authService.generateInvitationLink(command).subscribe({
      next: () => {
        this.toastService.showSuccess('Invitation sent!');
        setTimeout(() => {
          this.showLoadingOverlay = false;
          this.isInviting = false;
          this.closeAddModal();
          this.fetchAllUsers(); // Refresh the list
          this.loadingMessage = '';
        }, 500);
      },
      error: (error) => {
        this.showLoadingOverlay = false;
        this.isInviting = false;
        let errorMsg = 'Failed to invite user. Please try again later.';

        if (Array.isArray(error?.error)) {
          // Extract all error messages from the array
          errorMsg = error.error.map((e: any) => e.errorMessage).join('. ');
        } else if (error && typeof error === 'string') {
          errorMsg = error;
        } else if (error?.error) {
          if (typeof error.error === 'string') {
            errorMsg = error.error;
          } else if (typeof error.error.message === 'string') {
            errorMsg = error.error.message;
          }
        }
        this.toastService.show(errorMsg, 'error');
      }
    });
  }
  
  isValidEmail(email: string): boolean {
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.test(email);
  }

  getFilterOptions() {
    return {
      roles: [...new Set(this.users.map(user => user.role))] as RoleType[],
      status: [true, false] // Explicitly add both enabled and disabled options
    };
  }

  isFilterActive(category: 'roles', value: RoleType): boolean;
  isFilterActive(category: 'status', value: boolean): boolean;
  isFilterActive(category: 'roles' | 'status', value: RoleType | boolean): boolean {
    if (category === 'roles' && typeof value === 'string') {
      return this.activeFilters.roles.includes(value as RoleType);
    }
    if (category === 'status' && typeof value === 'boolean') {
      return this.activeFilters.status.includes(value);
    }
    return false;
  }

  toggleFilterItem(category: 'roles', value: RoleType): void;
  toggleFilterItem(category: 'status', value: boolean): void;
  toggleFilterItem(category: 'roles' | 'status', value: RoleType | boolean): void {
    if (category === 'roles' && typeof value === 'string') {
      const index = this.activeFilters.roles.indexOf(value as RoleType);
      if (index === -1) {
        this.activeFilters.roles.push(value as RoleType);
      } else {
        this.activeFilters.roles.splice(index, 1);
      }
    } else if (category === 'status' && typeof value === 'boolean') {
      const index = this.activeFilters.status.indexOf(value);
      if (index === -1) {
        this.activeFilters.status.push(value);
      } else {
        this.activeFilters.status.splice(index, 1);
      }
    }
    this.applyFilters();
  }

  getActiveFiltersCount(): number {
    return this.activeFilters.roles.length + this.activeFilters.status.length;
  }

  getStatusFilterLabel(status: boolean): string {
    return this.statusFilterLabels[String(status)];
  }

  clearFilters() {
    this.activeFilters = { roles: [], status: [] };
    this.applyFilters();
  }

  // Add this method to handle sort changes
  onSortChange(sort: { field: string; direction: string }) {
    this.currentSort = sort;
    
    if (!sort.field) {
        this.filteredUsers = [...this.users];
    } else if (sort.field === 'organization') {
        // Special handling for organization sorting since it's a derived field
        this.filteredUsers.sort((a, b) => {
            const aOrgName = a.organizationId ? (this.organizationsMap.get(a.organizationId) || '') : '';
            const bOrgName = b.organizationId ? (this.organizationsMap.get(b.organizationId) || '') : '';
            
            const comparison = aOrgName.toLowerCase().localeCompare(bOrgName.toLowerCase());
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    } else {
        this.filteredUsers.sort((a, b) => {
            const aValue = a[sort.field as keyof typeof a] || '';
            const bValue = b[sort.field as keyof typeof b] || '';
            
            const comparison = aValue.toString().localeCompare(bValue.toString());
            return sort.direction === 'asc' ? comparison : -comparison;
        });
    }
    this.updateTableData();
  }

  // User actions methods
  onUserActions(user: User, event: Event) {
    event.stopPropagation();
    
    if (this.selectedUserForActions === user) {
      this.selectedUserForActions = null;
    } else {
      this.selectedUserForActions = user;
    }
  }

  editUser(user: User) {
    this.selectedUser = { ...user };
    this.userForm = { ...user };
    this.showUserDetailsDrawer = true;
    this.selectedUserForActions = null;
  }

  confirmDisableUser(user: User) {
    this.userToDelete = user;
    this.showDeleteConfirm = true;
    this.selectedUserForActions = null;
  }

  disableUser() {
    if (!this.userToDelete) return;

    this.userService.disableUserById(this.userToDelete.id).subscribe({
      next: () => {
        this.showDeleteConfirm = false;
        this.userToDelete = null;
        this.fetchAllUsers(); // Refresh the list
      },
      error: (error) => {
        console.error('Error disabling user:', error);
      }
    });
  }

  enableUser(user: User) {
    this.userService.enableUserById(user.id).subscribe({
      next: () => {
        this.selectedUserForActions = null;
        this.fetchAllUsers(); // Refresh the list
      },
      error: (error) => {
        console.error('Error enabling user:', error);
      }
    });
  }

  openEnableConfirm(user: User) {
    this.userToEnable = user;
    this.showEnableConfirm = true;
  }
  
  confirmEnableUser() {
    if (!this.userToEnable) return;
    this.enableUser(this.userToEnable);
    this.showEnableConfirm = false;
    this.userToEnable = null;
  }

  async saveUserChanges() {
    if (!this.selectedUser) return;
    
    this.isUpdating = true;
    this.showLoadingOverlay = true;
    this.loadingMessage = 'Updating user...';
    
    // Create the update command with only the fields we want to update
    const updateCommand = {
      firstName: this.userForm.firstName,
      lastName: this.userForm.lastName,
      email: this.userForm.email,
      address: this.userForm.address,
      position: this.userForm.position,
      dateOfBirth: this.userForm.dateOfBirth,
      profilePictureUrl: this.userForm.profilePictureUrl,
      isNotificationEnabled: this.userForm.isNotificationEnabled,
      isEnabled: this.userForm.isEnabled,
      gender: this.userForm.gender,
      organizationId: this.userForm.organizationId
    };
    
    // Add a small delay for the animation to be visible
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    this.userService.updateUser(this.selectedUser.id, updateCommand).subscribe({
      next: () => {
        setTimeout(() => {
          this.closeUserDetailsDrawer();
          this.fetchAllUsers(); // Refresh the list
          this.showLoadingOverlay = false;
          this.loadingMessage = '';
        }, 500); // Add a slight delay after the response
      },
      error: (error) => {
        console.error('Error updating user:', error);
        this.showLoadingOverlay = false;
        this.isUpdating = false;
        this.loadingMessage = 'Failed to update user. Please try again later.';
      }
    });
  }

  closeUserDetailsDrawer() {
    this.showUserDetailsDrawer = false;
    this.selectedUser = null;
    this.userForm = {};
    this.isUpdating = false;
  }

  // Add click outside handler
  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (this.selectedUserForActions && !target.closest('.user-actions')) {
      this.selectedUserForActions = null;
    }
  }
} 