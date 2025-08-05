import { Component, EventEmitter, Input, OnInit, OnChanges, SimpleChanges, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Objective } from '../../../../models/objective.interface';
import { TeamService } from '../../../../services/team.service';
import { Observable, of } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { Team } from '../../../../models/team.interface';

@Component({
  selector: 'app-new-objective-form',
  templateUrl: './new-objective-form.component.html'
})
export class NewObjectiveFormComponent implements OnInit, OnChanges {
  @Input() data: Objective | null = null;
  @Input() isEdit = false;
  @Input() sessionId: string = '';
  @Input() userId: string = '';
  @Input() isSubmitting = false;
  @Input() teams: Team[] = [];
  @Output() onSubmit = new EventEmitter<Partial<Objective>>();
  @Output() onCancel = new EventEmitter<void>();
  @Output() onDelete = new EventEmitter<void>();

  objectiveForm: FormGroup;
  
  priorities = [
    { value: 'Low', label: 'Low', color: 'bg-blue-100 text-blue-800' },
    { value: 'Medium', label: 'Medium', color: 'bg-yellow-100 text-yellow-800' },
    { value: 'High', label: 'High', color: 'bg-orange-100 text-orange-800' },
    { value: 'Urgent', label: 'Urgent', color: 'bg-red-100 text-red-800' }
  ];
  
  statuses = [
    { value: 'NotStarted', label: 'Not Started', color: 'bg-gray-100 text-gray-800' },
    { value: 'InProgress', label: 'In Progress', color: 'bg-blue-100 text-blue-800' },
    { value: 'Overdue', label: 'Overdue', color: 'bg-yellow-100 text-yellow-800' },
    { value: 'Completed', label: 'Completed', color: 'bg-green-100 text-green-800' }
  ];

  constructor(
    private fb: FormBuilder,
    private teamService: TeamService,
    private route: ActivatedRoute
  ) {
    // Initialize form with default values
    this.objectiveForm = this.createForm();
  }

  ngOnInit() {
    console.log('Form component initialized');
    this.resetForm();
  }

  ngOnChanges(changes: SimpleChanges) {
    // Always reset form when inputs change to ensure we have a fresh form
    console.log('Form inputs changed - resetting form');
    this.resetForm();
  }

  private resetForm() {
    // First, reset the form completely to clear all values
    this.objectiveForm.reset();
    
    // Always reset isSubmitting state
    this.isSubmitting = false;
    
    if (this.data && this.isEdit) {
      // For editing, patch the form with existing data
      console.log('Editing mode - patching form with existing data');
      // Convert backend status and priority strings to enum numbers
      let statusValue = 'NotStarted';
      let priorityValue = 'Medium';
      if (typeof this.data.status === 'string') {
        statusValue = this.data.status;
      } else if (typeof this.data.status === 'number') {
        // If already a number, map to string for switch
        switch (this.data.status) {
          case 1: statusValue = 'NotStarted'; break;
          case 2: statusValue = 'InProgress'; break;
          case 3: statusValue = 'Completed'; break;
          case 4: statusValue = 'Overdue'; break;
          default: statusValue = 'NotStarted';
        }
      }
      if (typeof this.data.priority === 'string') {
        priorityValue = this.data.priority;
      } else if (typeof this.data.priority === 'number') {
        switch (this.data.priority) {
          case 1: priorityValue = 'Low'; break;
          case 2: priorityValue = 'Medium'; break;
          case 3: priorityValue = 'High'; break;
          case 4: priorityValue = 'Urgent'; break;
          default: priorityValue = 'Medium';
        }
      }
      this.objectiveForm.patchValue({
        title: this.data.title,
        description: this.data.description,
        priority: priorityValue,
        status: statusValue,
        startedDate: this.formatDateForInput(this.data.startedDate),
        endDate: this.formatDateForInput(this.data.endDate),
        responsibleTeamId: this.data.responsibleTeamId
      });
    } else {
      // For new objectives, set default values
      console.log('Creation mode - setting default values');
      
      const today = new Date();
      const nextMonth = new Date();
      nextMonth.setMonth(nextMonth.getMonth() + 1);
      
      this.objectiveForm.patchValue({
        title: '',  // Explicitly set empty title
        description: '',  // Explicitly set empty description
        status: 'NotStarted',
        priority: 'Medium',
        startedDate: this.formatDateForInput(today),
        endDate: this.formatDateForInput(nextMonth),
        responsibleTeamId: ''  // Explicitly reset team selection
      });
    }
  }

  private createForm(): FormGroup {
    return this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      priority: ['Medium', Validators.required],
      status: ['NotStarted', Validators.required],
      startedDate: ['', Validators.required],
      endDate: ['', Validators.required],
      responsibleTeamId: ['']
    });
  }

  handleSubmit() {
    if (this.objectiveForm.invalid) {
      this.objectiveForm.markAllAsTouched();
      return;
    }
    
    this.isSubmitting = true;
    
    const formValues = this.objectiveForm.value;
    const objectiveData: Partial<Objective> = {
      ...formValues,
      okrSessionId: this.sessionId,
      userId: this.userId
    };
    
    // If editing, preserve the ID
    if (this.isEdit && this.data) {
      objectiveData.id = this.data.id;
    }
    
    // Add a delay of 800ms to make the loading spinner visible
    setTimeout(() => {
      this.onSubmit.emit(objectiveData);
      
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
    this.onDelete.emit();
  }
  
  // Helper method to format dates for the date input
  private formatDateForInput(date: Date | string): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    
    // Format as YYYY-MM-DD for the date input
    return d.toISOString().split('T')[0];
  }

  // Form validation helpers
  get title() { return this.objectiveForm.get('title'); }
  get description() { return this.objectiveForm.get('description'); }
  get priority() { return this.objectiveForm.get('priority'); }
  get status() { return this.objectiveForm.get('status'); }
  get startedDate() { return this.objectiveForm.get('startedDate'); }
  get endDate() { return this.objectiveForm.get('endDate'); }

  // Public method to force reset form from parent component
  public reset() {
    this.resetForm();
  }
}
