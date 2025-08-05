import { Component, OnInit } from '@angular/core';
import { UserService } from '../../../services/user.service';
import { User, RoleType, UserDetailsWithRole } from '../../../models/user.interface';
import { BulkEditData, FilterOptions, SortOption } from '../../../models/employee.interface';
import { TableConfig } from '../../../shared/interfaces/table.interface';
import { GridConfig } from '../../../shared/interfaces/grid.interface';
import { HostListener } from '@angular/core';
import { TeamService } from '../../../services/team.service';
import { Team } from '../../../models/team.interface';
import { AuthStateService } from '../../../services/auth-state.service';
import { SupabaseAuthService } from '../../../services/supabase-auth.service';
import { ActivatedRoute } from '@angular/router';
import { OrganizationService } from '../../../services/organization.service';
import { ToastService } from '../../../shared/services/toast.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-employees',
  templateUrl: './employees.component.html'
})
export class EmployeesComponent implements OnInit {
  currentUser: UserDetailsWithRole | null = null;
  employees: User[] = [];
  filteredEmployees: User[] = [];
  selectedView: 'list' | 'grid' = 'list';
  showFilters = false;
  showSorting = false;
  showInviteModal = false;
  organizationName: string = '';
  organizationId: string | null = null;
  isSuperAdmin: boolean = false;
 /*  showBulkEditModal = false; */
  inviteEmails: string = '';
  selectedRole: RoleType = RoleType.Collaborator;
  availableRoles = [RoleType.TeamManager, RoleType.Collaborator];
  isInviting = false;

  currentSort = {
    field: '',
    direction: 'asc'
  };

  /* bulkEditData: BulkEditData = {
    selectedEmployees: [],
    newTeam: null,
    newRole: null
  }; */

  sortOptions = [
    { label: 'Name', value: 'firstName' },
    { label: 'Email', value: 'email' },
    { label: 'Role', value: 'role' },
    { label: 'Position', value: 'position' }
  ];

  activeFilters = {
    teams: [] as string[],
    roles: [] as string[],
    positions: [] as string[],
    status: [] as string[]
  };

  tableConfig: TableConfig = {
    columns: [
      { 
        key: 'user',
        label: 'EMPLOYEE',
        type: 'user'
      },
      {
        key: 'role',
        label: 'ROLE',
        type: 'text'
      },
      {
        key: 'position',
        label: 'POSITION',
        type: 'badge',
        badgeType: 'role'
      },
      {
        key: 'email',
        label: 'EMAIL',
        type: 'email'
      },
      {
        key: 'status',
        label: '',
        type: 'badge',
        badgeType: 'status'
      },
      {
        key: 'actions',
        label: '',
        type: 'actions' as const,
        width: '70px'
      }
    ],
    data: [],
    itemsPerPage: 9,
    enablePagination: true,
    source: 'employees'
  };

  gridConfig: GridConfig = {
    data: [],
    imageKey: 'profilePictureUrl',
    titleKey: 'fullName',
    subtitleKey: 'position',
    badgeKey: 'role',
    teamKey: 'team',
    itemsPerPage: 24,
    enablePagination: true
  };

  showDeleteConfirm = false;
  employeeToDelete: User | null = null;
  selectedEmployee: User | null = null;

  selectedMemberForActions: User | null = null;
  dropdownPosition: { x: number; y: number } | null = null;

  showMemberDetailsDrawer = false;
  memberTeams: Team[] = [];

  constructor(
    private userService: UserService,
    private teamService: TeamService,
    private authState: AuthStateService,
    private supabaseAuth: SupabaseAuthService,
    private route: ActivatedRoute,
    private organizationService: OrganizationService,
    private toastService: ToastService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    // Always set the current user first
    this.currentUser = this.authState.getCurrentUser();
    this.isSuperAdmin = this.currentUser?.role === RoleType.SuperAdmin;
    
    // First check for organizationId in route parameters (for SuperAdmin)
    this.route.paramMap.subscribe(params => {
      const organizationId = params.get('id');
      
      if (organizationId) {
        this.organizationId = organizationId;
        console.log('Loading employees for organization ID from route params:', organizationId);
        this.loadOrganizationEmployees(organizationId);
        this.loadOrganizationDetails(organizationId);
      } else {
        // If no org ID in route, use current user's organization
        if (this.currentUser?.organizationId) {
          this.organizationId = this.currentUser.organizationId;
          console.log('Loading employees for current user organization:', this.currentUser.organizationId);
          this.loadOrganizationEmployees(this.currentUser.organizationId);
        }
      }
    });
  }

  private loadOrganizationDetails(organizationId: string) {
    this.organizationService.getOrganizationById(organizationId).subscribe(
      organization => {
        this.organizationName = organization.name;
        console.log('Organization name loaded:', this.organizationName);
      },
      error => {
        console.error('Error loading organization details:', error);
      }
    );
  }

  private loadOrganizationEmployees(organizationId: string) {
    this.userService.getUsersByOrganizationId(organizationId).subscribe(users => {
      // If not an organization admin, filter out disabled users
      this.employees = users.map(user => ({
        ...user,
        fullName: `${user.firstName} ${user.lastName}`
      })) as (User & { fullName: string })[];

      if (this.currentUser?.role !== RoleType.OrganizationAdmin) {
        this.employees = this.employees.filter(user => user.isEnabled);
      }

      this.filteredEmployees = [...this.employees];
      this.updateTableData();
    });
  }

  toggleView(view: 'list' | 'grid') {
    this.selectedView = view;
  }

  toggleFilters(event?: Event) {
    if (event) {
      event.stopPropagation();
    }
    this.showFilters = !this.showFilters;
    if (this.showFilters) {
      this.showSorting = false; // Close sorting if open
    }
  }

  toggleSorting(event?: Event) {
    if (event) {
      event.stopPropagation();
    }
    this.showSorting = !this.showSorting;
    if (this.showSorting) {
      this.showFilters = false; // Close filters if open
    }
  }

  closeFilters() {
    this.showFilters = false;
  }

  closeSorting() {
    this.showSorting = false;
  }

  searchEmployees(event: Event): void {
    const searchTerm = (event.target as HTMLInputElement).value.toLowerCase();
    
    this.filteredEmployees = this.employees.filter(employee => {
      const matchesSearch = 
        employee.fullName!.toLowerCase().includes(searchTerm) ||
        employee.email.toLowerCase().includes(searchTerm) ||
        employee.position.toLowerCase().includes(searchTerm) ||
        employee.role.toLowerCase().includes(searchTerm);

      // Also apply any active filters
      const roleFilter = this.activeFilters.roles.length === 0 || 
                        this.activeFilters.roles.includes(employee.role);
      const positionFilter = this.activeFilters.positions.length === 0 || 
                            this.activeFilters.positions.includes(employee.position);

      return matchesSearch && roleFilter && positionFilter;
    });

    // Apply any active sorting
    if (this.currentSort.field) {
      this.sortEmployees();
    }

    this.updateTableData();
  }

  isFilterActive(category: 'roles' | 'positions' | 'teams' | 'status', value: string): boolean {
    return this.activeFilters[category].includes(value);
  }

  toggleFilterItem(category: 'roles' | 'positions' | 'teams' | 'status', value: string) {
    const index = this.activeFilters[category].indexOf(value);
    if (index === -1) {
      // Add the filter
      this.activeFilters[category].push(value);
    } else {
      // Remove the filter
      this.activeFilters[category].splice(index, 1);
    }
    this.applyFilters();
  }

  getActiveFiltersCount(): number {
    return this.activeFilters.roles.length + 
           this.activeFilters.positions.length + 
           this.activeFilters.status.length;
  }

  clearFilters() {
    this.activeFilters = { teams: [], roles: [], positions: [], status: [] };
    this.filteredEmployees = [...this.employees];
    
    // Apply any active sorting
    if (this.currentSort.field) {
      this.sortEmployees();
    }
    
    this.updateTableData();
  }

  applyFilters() {
    this.filteredEmployees = this.employees.filter(employee => {
      const roleFilter = this.activeFilters.roles.length === 0 || 
                        this.activeFilters.roles.includes(employee.role);
      const positionFilter = this.activeFilters.positions.length === 0 || 
                            this.activeFilters.positions.includes(employee.position);
      const statusFilter = this.activeFilters.status.length === 0 || 
                         (this.activeFilters.status.includes('Disabled') && !employee.isEnabled) ||
                         (this.activeFilters.status.includes('Enabled') && employee.isEnabled);
      return roleFilter && positionFilter && statusFilter;
    });

    // Apply any active sorting
    if (this.currentSort.field) {
      this.sortEmployees();
    }

    this.updateTableData();
  }

  // Bulk edit methods
  /* isEmployeeSelected(id: string): boolean {
    return this.bulkEditData.selectedEmployees.includes(id);
  } */

  /* toggleEmployeeSelection(id: string) {
    const index = this.bulkEditData.selectedEmployees.indexOf(id);
    if (index === -1) {
      this.bulkEditData.selectedEmployees.push(id);
    } else {
      this.bulkEditData.selectedEmployees.splice(index, 1);
    }
  }

  bulkEdit() {
    this.showBulkEditModal = true;
  }

  closeBulkEditModal() {
    this.showBulkEditModal = false;
    this.bulkEditData = { selectedEmployees: [], newTeam: null, newRole: null };
  }

  submitBulkEdit() {
    // Implement bulk edit logic
    this.closeBulkEditModal();
  }
 */
  // Invite methods
  inviteUser() {
    if (this.isSuperAdmin && !this.organizationId) {
      this.toastService.show('Please select an organization before inviting a user.', 'info');
      return;
    }
    this.showInviteModal = true;
  }

  closeInviteModal() {
    this.showInviteModal = false;
    this.inviteEmails = '';
    this.selectedRole = RoleType.Collaborator;
    this.isInviting = false;
  }

  isValidEmail(email: string): boolean {
    if (!email) return false;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  submitInvites() {
    if (!this.currentUser?.organizationId || !this.inviteEmails || !this.isValidEmail(this.inviteEmails)) {
      return;
    }

    this.isInviting = true;

    const command = {
      email: this.inviteEmails,
      roleName: this.selectedRole,
      organizationId: this.currentUser.organizationId
    };

    this.authService.generateInvitationLink(command).subscribe({
      next: () => {
        this.toastService.showSuccess('Invitation sent!');
        this.closeInviteModal();
      },
      error: (error) => {
        let errorMsg = 'Failed to invite user. Please try again later.';
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
      },
      complete: () => {
        this.isInviting = false;
      }
    });
  }

  generateInvitationLink() {
    // Implement link generation logic
  }

  getSortIcon(field: string): 'asc' | 'desc' | 'none' {
    if (this.currentSort.field !== field) return 'none';
    return this.currentSort.direction as 'asc' | 'desc';
  }

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
    this.applyFilters();
  }

  // Update the selectAllEmployees method to handle the event
 /*  selectAllEmployees(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      this.bulkEditData.selectedEmployees = this.employees.map(emp => emp.id);
    } else {
      this.bulkEditData.selectedEmployees = [];
    }
  } */

  // Add this computed property
  /* get areAllEmployeesSelected(): boolean {
    return this.employees.length > 0 && 
           this.employees.length === this.bulkEditData.selectedEmployees.length;
  } */

  // Add this computed property to get unique positions for filtering
  get availableFilters(): FilterOptions {
    return {
      teams: [],
      roles: [...new Set(this.employees.map(emp => emp.role))].sort(),
      positions: [...new Set(this.employees.map(emp => emp.position))].sort(),
      status: ['Enabled', 'Disabled']
    };
  }

  onEmployeeClick(employee: any) {
    console.log('Employee clicked:', employee);
  }

  updateTableData() {
    const tableData = this.filteredEmployees.map(emp => ({
      ...emp,
      user: {
        name: `${emp.firstName} ${emp.lastName}`,
        imageUrl: emp.profilePictureUrl || 'assets/placeholder-avatar.jpg'
      },
      // Always show status for all users (Enabled or Disabled)
      status: !emp.isEnabled ? 'Disabled' : null,
      // Add isDisabled flag for styling
      isDisabled: !emp.isEnabled
    }));

    this.tableConfig = {
      ...this.tableConfig,
      columns: [
        { 
          key: 'user',
          label: 'EMPLOYEE',
          type: 'user'
        },
        {
          key: 'role',
          label: 'ROLE',
          type: 'text'
        },
        {
          key: 'position',
          label: 'POSITION',
          type: 'badge',
          badgeType: 'role'
        },
        {
          key: 'email',
          label: 'EMAIL',
          type: 'email'
        },
        {
          key: 'status',
          label: '',
          type: 'badge',
          badgeType: 'status'
        },
        {
          key: 'actions',
          label: '',
          type: 'actions' as const,
          width: '70px'
        }
      ],
      data: tableData
    };

    // Update grid data
    this.gridConfig = {
      ...this.gridConfig,
      data: this.filteredEmployees.map(emp => ({
        ...emp,
        fullName: `${emp.firstName} ${emp.lastName}`,
        isDisabled: !emp.isEnabled
      }))
    };
  }

  onSortChange(sort: { field: string; direction: string }) {
    this.currentSort = sort;
    this.showSorting = false;
    
    // If no sort field (clear sorting), restore original order
    if (!sort.field) {
      this.filteredEmployees = [...this.employees];
    } else {
      this.filteredEmployees.sort((a, b) => {
        let aValue: any;
        let bValue: any;

        // Special handling for name sorting
        if (sort.field === 'firstName') {
          aValue = `${a.firstName} ${a.lastName}`;
          bValue = `${b.firstName} ${b.lastName}`;
        } else {
          aValue = a[sort.field as keyof User];
          bValue = b[sort.field as keyof User];
        }

        // Handle null/undefined values
        aValue = aValue || '';
        bValue = bValue || '';
        
        const comparison = String(aValue).localeCompare(String(bValue));
        return sort.direction === 'asc' ? comparison : -comparison;
      });
    }

    // Update table data while preserving the required format
    const tableData = this.filteredEmployees.map(emp => ({
      ...emp,
      user: {
        name: `${emp.firstName} ${emp.lastName}`,
        imageUrl: emp.profilePictureUrl || 'assets/placeholder-avatar.jpg'
      }
    }));

    this.tableConfig = {
      ...this.tableConfig,
      data: tableData
    };

    this.gridConfig = {
      ...this.gridConfig,
      data: this.filteredEmployees.map(emp => ({
        ...emp,
        fullName: `${emp.firstName} ${emp.lastName}`
      }))
    };
  }

  // Add a helper method for sorting
  private sortEmployees() {
    this.filteredEmployees.sort((a: any, b: any) => {
      const aValue = String(a[this.currentSort.field]).toLowerCase();
      const bValue = String(b[this.currentSort.field]).toLowerCase();
      
      if (this.currentSort.direction === 'asc') {
        return aValue.localeCompare(bValue);
      } else {
        return bValue.localeCompare(aValue);
      }
    });
  }

  confirmDeleteEmployee(employee: User) {
    this.employeeToDelete = employee;
    this.showDeleteConfirm = true;
  }

  deleteEmployee() {
    if (!this.employeeToDelete || !this.currentUser?.organizationId) return;

    this.userService.disableUserById(this.employeeToDelete.id).subscribe({
      next: () => {
        // Close the modal and reset state
        this.showDeleteConfirm = false;
        this.employeeToDelete = null;
        this.selectedMemberForActions = null;
        this.dropdownPosition = null;
        
        // Refresh the list from API
        this.loadOrganizationEmployees(this.currentUser!.organizationId!);
      },
      error: (error) => {
        console.error('Error disabling user:', error);
      }
    });
  }

  // Add method to enable user
  enableEmployee(employee: User) {
    if (!this.currentUser?.organizationId) return;

    this.userService.enableUserById(employee.id).subscribe({
      next: () => {
        // Reset state
        this.selectedMemberForActions = null;
        this.dropdownPosition = null;
        
        // Refresh the list from API
        this.loadOrganizationEmployees(this.currentUser!.organizationId!);
      },
      error: (error) => {
        console.error('Error enabling user:', error);
      }
    });
  }

  showEmployeeDetails(employee: User) {
    this.selectedEmployee = employee;
    this.showMemberDetailsDrawer = true;
    this.selectedMemberForActions = null;
    
    // Load teams based on role
    if (employee.role === RoleType.TeamManager) {
      this.teamService.getTeamsByManagerId(employee.id).subscribe(teams => {
        this.memberTeams = teams;
      });
    } else {
      this.teamService.getTeamsByCollaboratorId(employee.id).subscribe(teams => {
        this.memberTeams = teams;
      });
    }
  }

  // Update the canManageEmployees getter to be more specific
  get canManageEmployees(): boolean {
    return this.currentUser?.role === RoleType.OrganizationAdmin || this.currentUser?.role === RoleType.SuperAdmin;
  }

  onMemberActions(employee: User, event: Event) {
    event.stopPropagation();
    
    if (this.selectedMemberForActions === employee) {
      this.selectedMemberForActions = null;
      this.dropdownPosition = null;
    } else {
      const buttonElement = event.target as HTMLElement;
      const rect = buttonElement.getBoundingClientRect();
      this.dropdownPosition = {
        x: rect.right - 220, // Position it to the left of the three dots
        y: rect.top // Align with the top of the button
      };
      this.selectedMemberForActions = employee;
    }
  }

  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (this.selectedMemberForActions && !target.closest('.member-actions')) {
      this.selectedMemberForActions = null;
      this.dropdownPosition = null;
    }
  }
}