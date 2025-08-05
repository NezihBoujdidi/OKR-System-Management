import { Component, OnInit, HostListener } from '@angular/core';
import { TableConfig } from '../../../shared/interfaces/table.interface';
import { OrganizationService } from '../../../services/organization.service';
import { Organization, UpdateOrganizationCommand, CreateOrganizationCommand } from '../../../models/organization.interface';

@Component({
  selector: 'app-organization',
  templateUrl: './organization.component.html',
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
    
    /* Organization Details Drawer Styles */
    .drawer-container {
      position: fixed;
      top: 0;
      right: 0;
      bottom: 0;
      width: 24rem; /* 384px */
      background-color: white;
      box-shadow: -4px 0 6px -1px rgba(0, 0, 0, 0.1), -2px 0 4px -1px rgba(0, 0, 0, 0.06);
      z-index: 50;
      transform: translateX(100%);
      transition: transform 0.3s ease-in-out;
      overflow-y: auto;
    }
    
    .drawer-container.show {
      transform: translateX(0);
    }
    
    .drawer-content {
      padding: 1.5rem;
    }
  `]
})
export class OrganizationComponent implements OnInit {
  organizations: Organization[] = [];
  filteredOrganizations: Organization[] = [];
  showFilters = false;
  showSorting = false;
  showAddModal = false;
  isLoading = false;
  errorMessage = '';
  showLoadingOverlay = false;
  loadingMessage = '';
  
  // Organization details drawer properties
  showOrganizationDetailsDrawer = false;
  selectedOrganization: Organization | null = null;
  organizationForm: any = {};
  isUpdating = false;
  
  // Add Organization drawer properties
  showAddOrganizationDrawer = false;
  newOrganizationForm: any = {
    name: '',
    description: '',
    country: '',
    industry: '',
    email: '',
    phone: '',
    size: 0,
    isActive: true
  };
  isCreating = false;
  
  // Form validation errors
  formErrors = {
    name: '',
    email: '',
    phone: '',
    size: ''
  };
  
  // Organization actions properties
  selectedOrgForActions: Organization | null = null;

  currentSort = {
    field: '',
    direction: 'asc'
  };

  activeFilters = {
    status: [] as string[]
  };

  // Table configuration
  tableConfig: TableConfig = {
    columns: [
      { 
        key: 'organization', 
        label: 'ORGANIZATION', 
        type: 'organization'
      },
      { 
        key: 'subscription', 
        label: 'SUBSCRIPTION PLAN', 
        type: 'text'
      },
      { 
        key: 'createdDate', 
        label: 'CREATED DATE', 
        type: 'text'
      },
      { 
        key: 'status', 
        label: 'STATUS', 
        type: 'badge',
        badgeType: 'status'
      },
      { 
        key: 'size', 
        label: 'SIZE', 
        type: 'text' 
      },
      { 
        key: 'actions', 
        label: '', 
        type: 'actions',
        width: '70px'
      }
    ],
    data: [],
    itemsPerPage: 10,
    enablePagination: true,
    source: 'organization'
  };

  sortOptions = [
    { value: 'organization.name', label: 'Organization Name' },
    { value: 'createdDate', label: 'Created Date' },
    { value: 'status', label: 'Status' },
    { value: 'size', label: 'Size' }
  ];

  constructor(private organizationService: OrganizationService) {}

  ngOnInit() {
    this.fetchAllOrganizations();
  }

  fetchAllOrganizations() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.organizationService.getOrganizations().subscribe({
      next: (organizations) => {
        console.log('Organizations loaded:', organizations);
        this.organizations = organizations;
        this.filteredOrganizations = [...organizations];
        this.updateTableData();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching organizations:', error);
        this.errorMessage = 'Failed to load organizations. Please try again later.';
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

  applySorting(field: string) {
    if (this.currentSort.field === field) {
      this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
      this.currentSort.field = field;
      this.currentSort.direction = 'asc';
    }
    this.applyFiltersAndSort();
  }

  clearSort() {
    this.currentSort = { field: '', direction: 'asc' };
    this.showSorting = false;
    this.applyFiltersAndSort();
  }

  getActiveFiltersCount(): number {
    return this.activeFilters.status.length;
  }

  get availableFilters() {
    return {
      status: ['Active', 'Inactive']
    };
  }

  isFilterActive(category: 'status', value: string): boolean {
    return this.activeFilters[category].includes(value);
  }

  toggleFilterItem(category: 'status', value: string) {
    const index = this.activeFilters[category].indexOf(value);
    if (index === -1) {
      this.activeFilters[category].push(value);
    } else {
      this.activeFilters[category].splice(index, 1);
    }
    this.applyFiltersAndSort();
  }

  clearFilters() {
    this.activeFilters = { status: [] };
    this.applyFiltersAndSort();
  }

  private applyFiltersAndSort() {
    // Apply filters
    this.filteredOrganizations = this.organizations.filter(org => {
      const statusFilter = this.activeFilters.status.length === 0 || 
                         (org.isActive && this.activeFilters.status.includes('Active')) ||
                         (!org.isActive && this.activeFilters.status.includes('Inactive'));
      return statusFilter;
    });

    // Apply sorting
    if (this.currentSort.field) {
      this.filteredOrganizations.sort((a, b) => {
        let aValue, bValue;
        
        // Handle nested properties
        if (this.currentSort.field.includes('.')) {
          aValue = this.getNestedValue(a, this.currentSort.field);
          bValue = this.getNestedValue(b, this.currentSort.field);
        } else {
          aValue = (a as any)[this.currentSort.field];
          bValue = (b as any)[this.currentSort.field];
        }
        
        // Convert to string for comparison if needed
        if (aValue !== undefined && bValue !== undefined) {
          if (typeof aValue !== 'string') aValue = String(aValue);
          if (typeof bValue !== 'string') bValue = String(bValue);
          
          const comparison = aValue.toLowerCase().localeCompare(bValue.toLowerCase());
          return this.currentSort.direction === 'asc' ? comparison : -comparison;
        }
        return 0;
      });
    }
    
    this.updateTableData();
  }

  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((current, key) => current?.[key], obj);
  }

  searchOrganizations(event: Event) {
    const searchTerm = (event.target as HTMLInputElement).value.toLowerCase();
    
    if (!searchTerm.trim()) {
      this.filteredOrganizations = [...this.organizations];
    } else {
      this.filteredOrganizations = this.organizations.filter(org => 
        org.name.toLowerCase().includes(searchTerm) ||
        org.email?.toLowerCase().includes(searchTerm) ||
        org.country?.toLowerCase().includes(searchTerm) ||
        org.industry?.toLowerCase().includes(searchTerm)
      );
    }
    
    this.updateTableData();
  }

  private updateTableData() {
    // Transform data for the table component
    this.tableConfig = {
      ...this.tableConfig,
      data: this.filteredOrganizations.map(org => {
        return {
          id: org.id,
          organization: {
            name: org.name,
            email: org.email,
            initials: this.getInitials(org.name)
          },
          subscription: org.subscriptionPlan? org.subscriptionPlan : 'Free', // Keeping subscription empty for now
          createdDate: org.createdDate ? new Date(org.createdDate).toLocaleDateString() : 'N/A',
          status: org.isActive ? 'Active' : 'Inactive',
          size: `${org.size} employees`,
          actions: ['view', 'edit', 'delete']
        };
      })
    };
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(part => part.charAt(0))
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  onOrganizationClick(organization: any) {
    console.log('Organization clicked:', organization);
  }

  addOrganization() {
    // Reset the form
    this.newOrganizationForm = {
      name: '',
      description: '',
      country: '',
      industry: '',
      email: '',
      phone: '',
      size: 0,
      isActive: true
    };
    
    // Show the drawer
    this.showAddOrganizationDrawer = true;
  }

  // Organization actions
  onOrgActions(organization: any, event: Event) {
    event.stopPropagation();
    
    // Find the full organization object from our list
    const fullOrg = this.organizations.find(org => org.id === organization.id);
    
    if (this.selectedOrgForActions === fullOrg) {
      this.selectedOrgForActions = null;
    } else {
      this.selectedOrgForActions = fullOrg || null;
    }
  }

  // View organization details
  viewOrganizationDetails(organization: any) {
    console.log('viewOrganizationDetails called with:', organization);
    
    // Find the full organization object
    const fullOrg = this.organizations.find(org => org.id === organization.id);
    console.log('Full organization found:', fullOrg);
    
    if (fullOrg) {
      this.selectedOrganization = { ...fullOrg };
      this.organizationForm = { ...fullOrg };
      this.showOrganizationDetailsDrawer = true;
      this.selectedOrgForActions = null;
      console.log('Organization details drawer should be shown now');
    } else {
      console.error('Could not find organization with ID:', organization.id);
    }
  }

  // Disable organization
  disableOrganization(organization: any) {
    // Find the full organization object
    const fullOrg = this.organizations.find(org => org.id === organization.id);
    if (fullOrg) {
      this.selectedOrgForActions = null;
      
      const updateCommand: UpdateOrganizationCommand = {
        name: fullOrg.name,
        description: fullOrg.description,
        country: fullOrg.country,
        industry: fullOrg.industry,
        email: fullOrg.email,
        phone: fullOrg.phone,
        size: fullOrg.size,
        isActive: false
      };
      
      this.organizationService.updateOrganization(fullOrg.id, updateCommand).subscribe({
        next: () => {
          console.log('Organization disabled:', fullOrg.id);
          this.fetchAllOrganizations(); // Refresh the list
        },
        error: (error) => {
          console.error('Error disabling organization:', error);
        }
      });
    }
  }

  // Enable organization
  enableOrganization(organization: any) {
    // Find the full organization object
    const fullOrg = this.organizations.find(org => org.id === organization.id);
    if (fullOrg) {
      this.selectedOrgForActions = null;
      
      const updateCommand: UpdateOrganizationCommand = {
        name: fullOrg.name,
        description: fullOrg.description,
        country: fullOrg.country,
        industry: fullOrg.industry,
        email: fullOrg.email,
        phone: fullOrg.phone,
        size: fullOrg.size,
        isActive: true
      };
      
      this.organizationService.updateOrganization(fullOrg.id, updateCommand).subscribe({
        next: () => {
          console.log('Organization enabled:', fullOrg.id);
          this.fetchAllOrganizations(); // Refresh the list
        },
        error: (error) => {
          console.error('Error enabling organization:', error);
        }
      });
    }
  }

  // Close organization details drawer
  closeOrganizationDetailsDrawer() {
    this.showOrganizationDetailsDrawer = false;
    this.selectedOrganization = null;
    this.organizationForm = {};
    this.isUpdating = false;
  }

  // Save organization changes
  saveOrganizationChanges() {
    if (!this.selectedOrganization) return;
    
    if (!this.validateEditOrganizationForm()) {
      return;
    }
    
    this.isUpdating = true;
    this.showLoadingOverlay = true;
    this.loadingMessage = 'Saving changes...';
    
    const updateCommand: UpdateOrganizationCommand = {
      name: this.organizationForm.name,
      description: this.organizationForm.description,
      country: this.organizationForm.country,
      industry: this.organizationForm.industry,
      email: this.organizationForm.email,
      phone: this.organizationForm.phone,
      size: this.organizationForm.size,
      isActive: this.organizationForm.isActive
    };
    
    this.organizationService.updateOrganization(this.selectedOrganization.id, updateCommand).subscribe({
      next: (updatedOrg) => {
        console.log('Organization updated:', updatedOrg);
        setTimeout(() => {
          this.showLoadingOverlay = false;
          this.loadingMessage = '';
          this.closeOrganizationDetailsDrawer();
          this.fetchAllOrganizations(); // Refresh the list
        }, 500);
      },
      error: (error) => {
        console.error('Error updating organization:', error);
        this.showLoadingOverlay = false;
        this.loadingMessage = '';
        this.isUpdating = false;
      }
    });
  }

  onSortChange(sort: { field: string; direction: string }) {
    this.currentSort = sort;
    this.applyFiltersAndSort();
  }

  closeFilters() {
    this.showFilters = false;
  }

  closeSorting() {
    this.showSorting = false;
  }
  
  // Add click outside handler
  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event) {
    const target = event.target as HTMLElement;
    if (this.selectedOrgForActions && !target.closest('.org-actions')) {
      this.selectedOrgForActions = null;
    }
  }

  closeAddOrganizationDrawer() {
    this.showAddOrganizationDrawer = false;
    this.isCreating = false;
  }

  createOrganization() {
    if (!this.validateOrganizationForm()) {
      return;
    }
    
    this.isCreating = true;
    this.showLoadingOverlay = true;
    this.loadingMessage = 'Creating organization...';
    
    const createCommand: CreateOrganizationCommand = {
      name: this.newOrganizationForm.name,
      description: this.newOrganizationForm.description,
      country: this.newOrganizationForm.country,
      industry: this.newOrganizationForm.industry,
      email: this.newOrganizationForm.email,
      phone: this.newOrganizationForm.phone,
      size: this.newOrganizationForm.size,
      isActive: this.newOrganizationForm.isActive
    };
    
    this.organizationService.createOrganization(createCommand).subscribe({
      next: (response) => {
        console.log('Organization created:', response);
        setTimeout(() => {
          this.showLoadingOverlay = false;
          this.loadingMessage = '';
          this.closeAddOrganizationDrawer();
          this.fetchAllOrganizations(); // Refresh the list
        }, 500);
      },
      error: (error) => {
        console.error('Error creating organization:', error);
        this.showLoadingOverlay = false;
        this.loadingMessage = '';
        this.isCreating = false;
      }
    });
  }
  
  validateOrganizationForm(): boolean {
    // Reset all errors first
    this.formErrors = {
      name: '',
      email: '',
      phone: '',
      size: ''
    };
    
    let isValid = true;
    
    // Perform validation
    if (!this.newOrganizationForm.name) {
      this.formErrors.name = 'Organization name is required';
      isValid = false;
    }
    
    if (this.newOrganizationForm.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.newOrganizationForm.email)) {
      this.formErrors.email = 'Please enter a valid email address';
      isValid = false;
    }
    
    // Updated phone validation to be more flexible
    if (this.newOrganizationForm.phone && !/^[+]?[\d() \-.]{8,15}$/.test(this.newOrganizationForm.phone)) {
      this.formErrors.phone = 'Please enter a valid phone number (minimum 8 digits)';
      isValid = false;
    }
    
    // Updated size validation to ensure it's greater than zero
    if (this.newOrganizationForm.size !== undefined && this.newOrganizationForm.size !== null) {
      if (this.newOrganizationForm.size <= 0) {
        this.formErrors.size = 'Size must be greater than zero';
        isValid = false;
      }
    }
    
    return isValid;
  }

  validateEditOrganizationForm(): boolean {
    // Reset all errors first
    this.formErrors = {
      name: '',
      email: '',
      phone: '',
      size: ''
    };
    
    let isValid = true;
    
    // Perform validation
    if (!this.organizationForm.name) {
      this.formErrors.name = 'Organization name is required';
      isValid = false;
    }
    
    if (this.organizationForm.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.organizationForm.email)) {
      this.formErrors.email = 'Please enter a valid email address';
      isValid = false;
    }
    
    // Updated phone validation to be more flexible
    if (this.organizationForm.phone && !/^[+]?[\d() \-.]{8,15}$/.test(this.organizationForm.phone)) {
      this.formErrors.phone = 'Please enter a valid phone number (minimum 8 digits)';
      isValid = false;
    }
    
    // Updated size validation to ensure it's greater than zero
    if (this.organizationForm.size !== undefined && this.organizationForm.size !== null) {
      if (this.organizationForm.size <= 0) {
        this.formErrors.size = 'Size must be greater than zero';
        isValid = false;
      }
    }
    
    return isValid;
  }
}

