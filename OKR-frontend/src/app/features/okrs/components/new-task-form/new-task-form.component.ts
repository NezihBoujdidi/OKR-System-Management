import { Component, EventEmitter, Input, OnInit, OnChanges, SimpleChanges, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { KeyResultTask } from '../../../../models/key-result-task.interface';
import { User, UserDetailsWithRole, RoleType } from '../../../../models/user.interface';
import { UserService } from '../../../../services/user.service';
import { Status } from '../../../../models/Status.enum';
import { Priority } from '../../../../models/Priority.enum';
import { AuthStateService } from '../../../../services/auth-state.service';

@Component({
  selector: 'app-new-task-form',
  templateUrl: './new-task-form.component.html'
})
export class NewTaskFormComponent implements OnInit, OnChanges {
  @Input() data: KeyResultTask | null = null;
  @Input() keyResultId: string = '';
  @Input() isEdit = false;
  @Input() isSubmitting = false;
  @Output() onSubmit = new EventEmitter<Partial<KeyResultTask>>();
  @Output() onCancel = new EventEmitter<void>();
  @Output() onDelete = new EventEmitter<void>();

  taskForm: FormGroup;
  collaborators: UserDetailsWithRole[] = [];
  
  // For template access
  Status = Status;
  Priority = Priority;
  RoleType = RoleType;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private authStateService: AuthStateService
  ) {
    this.taskForm = this.createForm();
  }

  ngOnInit() {
    this.loadCollaborators();
    this.taskForm = this.createForm();
    this.resetForm();
  }

  ngOnChanges(changes: SimpleChanges) {
    console.log('Task form inputs changed:', Object.keys(changes).join(', '));
    
    // Reset the form when relevant inputs change
    if (changes['data'] || changes['isEdit'] || changes['keyResultId']) {
      console.log('Resetting task form due to input changes');
      
      // Only reset if the form has been initialized
      if (this.taskForm) {
        this.resetForm();
      }
    }
  }

  private createForm(): FormGroup {
    // Create a fresh form with default values
    const today = new Date();
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 7);
    
    return this.fb.group({
      title: ['', Validators.required],
      description: [''],
      startedDate: [this.getCurrentDateString(), Validators.required],
      endDate: [this.getFutureDate(7), Validators.required],
      collaboratorId: [''],
      progress: [0],
      priority: [Priority.Medium, Validators.required],
      status: [Status.NotStarted, Validators.required]
    });
  }

  private loadCollaborators() {
    const currentUser = this.authStateService.getCurrentUser();
    if (currentUser && currentUser.organizationId) {
      // Fetch only collaborators from the user's organization
      this.userService.getUsersByOrganizationId(currentUser.organizationId).subscribe(users => {
        // Filter to only include users with Collaborator role and the current user
        this.collaborators = users.filter(user => 
          user.role === RoleType.Collaborator || 
          (currentUser.id === user.id && user.role !== RoleType.SuperAdmin)
        );
        console.log(`Filtered ${this.collaborators.length} collaborators from ${users.length} organization users`);
      });
    } else {
      console.warn('No organization ID available for current user, cannot load collaborators');
      this.collaborators = [];
    }
  }

  // Helper method to format dates for the date input
  private formatDate(date: Date | undefined | null): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    
    // Format as YYYY-MM-DD for the date input
    return d.toISOString().split('T')[0];
  }

  private getCurrentDateString(): string {
    return this.formatDate(new Date());
  }

  private getFutureDate(daysFromNow: number): string {
    const date = new Date();
    date.setDate(date.getDate() + daysFromNow);
    return this.formatDate(date);
  }

  handleSubmit() {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }
    
    const userId = this.authStateService.getCurrentUser()?.id;
    if (!userId) {
      console.error('No user logged in');
      return;
    }

    // Set isSubmitting to true
    this.isSubmitting = true;
    
    const formValues = this.taskForm.value;
    
    // Get the date strings from the form and create Date objects with time set to start of day
    const startedDate = new Date(formValues.startedDate + 'T00:00:00.000Z');
    const endDate = new Date(formValues.endDate + 'T00:00:00.000Z');

    const taskData: Partial<KeyResultTask> = {
      title: formValues.title,
      description: formValues.description,
      startedDate: startedDate,
      endDate: endDate,
      collaboratorId: formValues.collaboratorId,
      progress: formValues.progress,
      priority: formValues.priority,
      status: formValues.status,
      keyResultId: this.keyResultId || this.data?.keyResultId,
      userId: userId,
      isDeleted: false
    };
    
    // If editing, preserve the ID
    if (this.isEdit && this.data) {
      taskData.id = this.data.id;
    }
    
    console.log('Submitting task data:', taskData); // For debugging
    
    // Add a delay of 800ms to make the loading spinner visible
    setTimeout(() => {
      this.onSubmit.emit(taskData);
      
      // Reset the submitting state after a reasonable timeout
      // This handles cases where the parent doesn't properly handle the response
      setTimeout(() => {
        if (this.isSubmitting) {
          console.log('Form still in submitting state after 2s - auto-resetting');
          this.isSubmitting = false;
        }
      }, 2000);
    }, 800);
  }

  handleCancel() {
    // Reset submitting state when user cancels
    this.isSubmitting = false;
    this.onCancel.emit();
  }

  handleDelete() {
    // Reset submitting state when user deletes
    this.isSubmitting = false;
    this.onDelete.emit();
  }

  private resetForm() {
    // First, completely reset the form to clear all values
    this.taskForm.reset();
    
    // Always reset isSubmitting state
    this.isSubmitting = false;
    
    // Set default values for a new task
    if (!this.isEdit || !this.data) {
      console.log('Creating a new task - setting default values');
      
      const today = new Date();
      const nextWeek = new Date();
      nextWeek.setDate(nextWeek.getDate() + 7);
      
      this.taskForm.patchValue({
        title: '',  // Explicitly set empty title
        description: '',  // Explicitly set empty description
        startedDate: this.formatDate(today),
        endDate: this.formatDate(nextWeek),
        collaboratorId: '',
        progress: 0,
        priority: Priority.Medium,
        status: Status.NotStarted
      });
    } else if (this.isEdit && this.data) {
      // If editing, patch with existing data
      console.log('Editing mode - patching form with existing data', this.data);
      
      this.taskForm.patchValue({
        title: this.data.title || '',
        description: this.data.description || '',
        startedDate: this.formatDate(this.data.startedDate),
        endDate: this.formatDate(this.data.endDate),
        collaboratorId: this.data.collaboratorId || '',
        progress: this.data.progress || 0,
        priority: this.data.priority || Priority.Medium,
        status: this.data.status || Status.NotStarted
      });
    }
  }

  // Public method to force reset form from parent component
  public reset() {
    this.resetForm();
  }
}