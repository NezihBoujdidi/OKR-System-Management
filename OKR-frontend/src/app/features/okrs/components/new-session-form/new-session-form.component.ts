import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OKRSession, CreateOkrCommand } from 'src/app/models/okr-session.interface';
import { Status } from  'src/app/models/Status.enum' 
import { User, UserDetails } from 'src/app/models/user.interface';
import { UserService } from 'src/app/services/user.service';
import { OKRSessionService } from 'src/app/services/okr-session.service';
import { AuthStateService } from 'src/app/services/auth-state.service';
import { TeamService } from 'src/app/services/team.service';
import { Team } from 'src/app/models/team.interface';
import { RoleType } from 'src/app/models/role-type.enum';
import { OkrSuggestion } from 'src/app/services/ai-assistant.service';

interface ColorOption {
  value: string;
  code: string;
  label: string;
}

interface StatusOption {
  value: number;
  label: string;
}

@Component({
  selector: 'app-new-session-form',
  templateUrl: './new-session-form.component.html'
})
export class NewSessionFormComponent implements OnInit, OnChanges {
  @Input() isEditMode = false;
  @Input() organizationId?: string;
  @Input() set sessionData(data: OKRSession | undefined) {
    if (data) {
      this._sessionData = data;
      // Update selected teams when session data changes
      if (data.teamIds && data.teamIds.length > 0) {
        this.selectedTeamIds = [...data.teamIds];
        // If form is already initialized, update it
        if (this.sessionForm) {
          this.updateFormTeamIds();
        }
      }
    }
  }
  get sessionData(): OKRSession | undefined {
    return this._sessionData;
  }
  private _sessionData?: OKRSession;

  @Output() submitForm = new EventEmitter<CreateOkrCommand>();
  @Output() cancelForm = new EventEmitter<void>();

  sessionForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  isSubmitting = false;

  // Team selector properties
  teamSearchQuery = '';
  selectedTeamIds: string[] = [];
  
  get filteredTeams(): Team[] {
    if (!this.teamSearchQuery) {
      return this.availableTeams;
    }
    const query = this.teamSearchQuery.toLowerCase();
    return this.availableTeams.filter(team => 
      team.name.toLowerCase().includes(query) || 
      (team.description && team.description.toLowerCase().includes(query))
    );
  }

  colorOptions: ColorOption[] = [
    { value: '#4299E1', code: '#4299E1', label: 'Blue' },
    { value: '#10B981', code: '#10B981', label: 'Green' },
    { value: '#F59E0B', code: '#F59E0B', label: 'Yellow' },
    { value: '#EF4444', code: '#EF4444', label: 'Red' },
    { value: '#8B5CF6', code: '#8B5CF6', label: 'Purple' }
  ];

  statusOptions: StatusOption[] = [
    { value: Status.NotStarted, label: 'Not Started' },
    { value: Status.InProgress, label: 'In Progress' },
    { value: Status.Completed, label: 'Completed' },
    { value: Status.Overdue, label: 'Overdue' }
  ];

  availableTeams: Team[] = [];
  currentUser: UserDetails | null = null;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private okrSessionService: OKRSessionService,
    private authStateService: AuthStateService,
    private teamService: TeamService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authStateService.getCurrentUser();
    
    // Initialize selected teams from session data if in edit mode
    if (this.isEditMode && this._sessionData && this._sessionData.teamIds) {
      this.selectedTeamIds = [...this._sessionData.teamIds];
    }
    
    this.initializeForm();
    this.loadTeams();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['organizationId'] && !changes['organizationId'].firstChange) {
      this.loadTeams();
    }
    
    if (changes['sessionData'] && this.sessionForm) {
      this.resetFormState();
      
      if (this.isEditMode && this._sessionData) {
        // Update selected teams when session data changes
        if (this._sessionData.teamIds) {
          this.selectedTeamIds = [...this._sessionData.teamIds];
        }
        this.populateForm(this._sessionData);
      }
    }
    
    if (changes['isEditMode'] && this.sessionForm) {
      this.resetFormState();
      
      if (this.isEditMode && this._sessionData) {
        // Update selected teams when edit mode changes
        if (this._sessionData.teamIds) {
          this.selectedTeamIds = [...this._sessionData.teamIds];
        }
        this.populateForm(this._sessionData);
      } else {
        this.resetForm();
      }
    }
  }

  initializeForm(): void {
    this.sessionForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      color: ['#4299E1'],
      teamIds: [this.selectedTeamIds, Validators.required],
      status: [Status.NotStarted, Validators.required]
    }, { validators: this.dateRangeValidator });
    
    if (this.isEditMode && this._sessionData) {
      this.populateForm(this._sessionData);
    } else {
      this.resetForm();
    }
  }

  loadTeams(): void {
    let orgIdToUse: string | undefined;
    
    if (this.authStateService.getUserRole() === RoleType.SuperAdmin && this.organizationId) {
      console.log('SuperAdmin using organization ID from URL:', this.organizationId);
      orgIdToUse = this.organizationId;
    } else {
      orgIdToUse = this.currentUser?.organizationId;
      console.log('Using current user organization ID:', orgIdToUse);
    }
    
    if (!orgIdToUse) {
      this.errorMessage = 'No organization ID found';
      return;
    }

    this.isLoading = true;
    console.log('Loading teams for organization ID:', orgIdToUse);
    
    this.teamService.getTeamsByOrganizationId(orgIdToUse).subscribe({
      next: (teams) => {
        this.availableTeams = teams;
        this.isLoading = false;
        
        if (this.isEditMode && this._sessionData && this._sessionData.teamIds) {
          this.selectedTeamIds = [...this._sessionData.teamIds];
          this.updateFormTeamIds();
          
          console.log('Teams loaded in edit mode:', {
            availableTeams: this.availableTeams.length,
            selectedTeamIds: this.selectedTeamIds,
            sessionTeamIds: this._sessionData.teamIds
          });
        }
      },
      error: (error) => {
        console.error('Failed to load teams:', error);
        this.errorMessage = 'Failed to load teams for the organization';
        this.isLoading = false;
      }
    });
  }

  toggleTeamSelection(teamId: string): void {
    console.log(`Toggling team selection for ID: ${teamId}`);
    const index = this.selectedTeamIds.indexOf(teamId);
    if (index > -1) {
      this.selectedTeamIds.splice(index, 1);
      console.log(`Removed team ID: ${teamId}, remaining: ${JSON.stringify(this.selectedTeamIds)}`);
    } else {
      this.selectedTeamIds.push(teamId);
      console.log(`Added team ID: ${teamId}, updated: ${JSON.stringify(this.selectedTeamIds)}`);
    }
    this.updateFormTeamIds();
  }

  isTeamSelected(teamId: string): boolean {
    const normalizedTeamId = teamId.toString().trim().toLowerCase();
    
    for (const id of this.selectedTeamIds) {
      if (!id) continue;
      const normalizedId = id.toString().trim().toLowerCase();
      if (normalizedId === normalizedTeamId) {
        return true;
      }
    }
    
    return false;
  }

  getTeamName(teamId: string): string {
    const team = this.availableTeams.find(t => t.id === teamId || t.id.toString() === teamId.toString());
    return team ? team.name : 'Unknown Team';
  }

  private updateFormTeamIds(): void {
    console.log(`Updating form with team IDs: ${JSON.stringify(this.selectedTeamIds)}`);
    this.sessionForm.patchValue({ teamIds: this.selectedTeamIds });
    this.sessionForm.get('teamIds')?.markAsDirty();
    this.sessionForm.get('teamIds')?.markAsTouched();
  }

  dateRangeValidator(group: FormGroup) {
    const start = group.get('startDate')?.value;
    const end = group.get('endDate')?.value;
    if (start && end) {
      const startDate = new Date(start);
      const endDate = new Date(end);
      return startDate <= endDate ? null : { dateRange: true };
    }
    return null;
  }

  selectColor(color: string): void {
    this.sessionForm.patchValue({ color });
  }

  get submitButtonText(): string {
    return this.isEditMode ? 'Save Changes' : 'Create Session';
  }

  resetFormState(): void {
    this.isSubmitting = false;
    this.errorMessage = '';
    
    if (this.sessionForm) {
      this.sessionForm.markAsPristine();
      this.sessionForm.markAsUntouched();
    }
  }

  resetForm(): void {
    this.resetFormState();
    
    if (!this.sessionForm) return;
    
    this.sessionForm.reset();
    
    if (this.isEditMode && this._sessionData) {
      if (this._sessionData.teamIds) {
        this.selectedTeamIds = [...this._sessionData.teamIds];
      } else {
        this.selectedTeamIds = [];
      }
      this.populateForm(this._sessionData);
    } else {
      this.selectedTeamIds = [];
      this.sessionForm.patchValue({
        name: '',
        description: '',
        startDate: '',
        endDate: '',
        color: '#4299E1',
        teamIds: [],
        status: Status.NotStarted
      });
    }
  }

  onSubmit(): void {
    if (this.sessionForm.invalid) return;
    
    this.isSubmitting = true;
    const formValue = this.sessionForm.value;
  
    const command: CreateOkrCommand = {
      title: formValue.name.trim(),
      description: formValue.description.trim(),
      startedDate: new Date(formValue.startDate),
      endDate: new Date(formValue.endDate),
      teamIds: this.selectedTeamIds,
      userId: this.currentUser?.id || '',
      color: formValue.color,
      organizationId: this.currentUser?.organizationId || '',
      status: Number(formValue.status)
    };
    
    this.submitForm.emit(command);
  }

  onCancel(): void {
    this.resetForm();
    this.cancelForm.emit();
  }

  private populateForm(data: OKRSession): void {
    if (!this.sessionForm) return;
    
    const startDate = data.startedDate ? new Date(data.startedDate).toISOString().split('T')[0] : '';
    const endDate = data.endDate ? new Date(data.endDate).toISOString().split('T')[0] : '';
    
    if (data.teamIds) {
      this.selectedTeamIds = [...data.teamIds];
    }
    
    // Convert backend status string to Status enum number
    let statusValue = Status.NotStarted;
    if (typeof data.status === 'string') {
      switch (data.status) {
        case 'NotStarted':
          statusValue = Status.NotStarted;
          break;
        case 'InProgress':
          statusValue = Status.InProgress;
          break;
        case 'Completed':
          statusValue = Status.Completed;
          break;
        case 'Overdue':
          statusValue = Status.Overdue;
          break;
        default:
          statusValue = Status.NotStarted;
      }
    } else if (typeof data.status === 'number') {
      statusValue = data.status;
    }

    this.sessionForm.patchValue({
      name: data.title || '',
      description: data.description || '',
      startDate: startDate,
      endDate: endDate,
      teamIds: this.selectedTeamIds,
      color: data.color || '#4299E1',
      status: statusValue
    });
  }

  // Add method to handle AI suggestions
  handleAiSuggestion(suggestion: OkrSuggestion): void {
    console.log('Applying AI suggestion:', suggestion);
    
    // Apply the suggestion to the form
    this.sessionForm.patchValue({
      name: suggestion.title,
      description: suggestion.description
    });
    
    // Set the dates if provided
    if (suggestion.suggestedStartDate) {
      this.sessionForm.get('startDate')?.setValue(this.formatDateForInput(suggestion.suggestedStartDate));
    }
    
    if (suggestion.suggestedEndDate) {
      this.sessionForm.get('endDate')?.setValue(this.formatDateForInput(suggestion.suggestedEndDate));
    }
    
    // Apply team selection if suggested
    if (suggestion.suggestedTeams && suggestion.suggestedTeams.length > 0) {
      this.selectedTeamIds = [...suggestion.suggestedTeams];
      this.updateFormTeamIds();
    }
    
    // Set status to In Progress by default for AI-generated suggestions
    this.sessionForm.get('status')?.setValue(Status.InProgress);
    
    // Set color based on suggestion type - for better visual differentiation
    if (suggestion.title.toLowerCase().includes('growth') || suggestion.title.toLowerCase().includes('expansion')) {
      this.selectColor('#10B981'); // Green for growth
    } else if (suggestion.title.toLowerCase().includes('customer')) {
      this.selectColor('#F59E0B'); // Yellow for customer focus
    } else if (suggestion.title.toLowerCase().includes('innovation')) {
      this.selectColor('#8B5CF6'); // Purple for innovation
    } else if (suggestion.title.toLowerCase().includes('efficiency') || suggestion.title.toLowerCase().includes('operational')) {
      this.selectColor('#4299E1'); // Blue for operational
    } else {
      // If no specific keywords, select a color based on quarter
      const dateStr = suggestion.suggestedStartDate ? new Date(suggestion.suggestedStartDate).getMonth() : new Date().getMonth();
      const quarter = Math.floor(dateStr / 3) + 1;
      
      switch(quarter) {
        case 1: this.selectColor('#4299E1'); break; // Blue for Q1
        case 2: this.selectColor('#10B981'); break; // Green for Q2
        case 3: this.selectColor('#F59E0B'); break; // Yellow for Q3
        case 4: this.selectColor('#EF4444'); break; // Red for Q4
        default: this.selectColor('#4299E1'); break; // Default blue
      }
    }
    
    // Show a success message to the user
    // This would be handled by a notification service in a real app
    
    // Mark form as touched to trigger validation
    Object.keys(this.sessionForm.controls).forEach(key => {
      this.sessionForm.get(key)?.markAsTouched();
    });
  }
  
  // Helper method to format date for input fields
  private formatDateForInput(date: Date): string {
    const d = new Date(date);
    let month = '' + (d.getMonth() + 1);
    let day = '' + d.getDate();
    const year = d.getFullYear();

    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;

    return [year, month, day].join('-');
  }
}