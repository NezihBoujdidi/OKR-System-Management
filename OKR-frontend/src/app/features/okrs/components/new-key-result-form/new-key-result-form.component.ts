import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { KeyResult } from '../../../../models/key-result.interface';
import { Status } from '../../../../models/Status.enum';

@Component({
  selector: 'app-new-key-result-form',
  templateUrl: './new-key-result-form.component.html'
})
export class NewKeyResultFormComponent implements OnInit, OnChanges {
  @Input() data: KeyResult | null = null;
  @Input() isEdit = false;
  @Input() objectiveId: string = '';
  @Input() userId: string = '';
  @Input() isSubmitting = false;  // Add isSubmitting input
  @Output() onSubmit = new EventEmitter<Partial<KeyResult>>();
  @Output() onCancel = new EventEmitter<void>();
  @Output() onDelete = new EventEmitter<void>();

  keyResultForm: FormGroup;
  
  // Expose the Status enum to the template
  Status = Status;
  
  // Status options for the form
  statuses = [
    { value: Status.NotStarted, label: 'Not Started', color: 'bg-gray-100 text-gray-800' },
    { value: Status.InProgress, label: 'In Progress', color: 'bg-blue-100 text-blue-800' },
    { value: Status.Completed, label: 'Completed', color: 'bg-green-100 text-green-800' },
    { value: Status.Overdue, label: 'Overdue', color: 'bg-yellow-100 text-yellow-800' }
  ];

  constructor(private fb: FormBuilder) {
    this.keyResultForm = this.createForm();
  }

  ngOnInit() {
    console.log('Key Result form initialized');
    this.resetForm();
  }

  ngOnChanges(changes: SimpleChanges) {
    // Always reset form when inputs change to ensure we have a fresh form
    console.log('Key Result form inputs changed - resetting form');
    console.log('Changes:', Object.keys(changes).join(', '));
    
    // Handle data and edit mode changes
    if (changes['data'] || changes['isEdit'] || changes['objectiveId']) {
      console.log('Reset form due to data/edit/objective change');
      this.resetForm();
    }
  }

  private resetForm() {
    // First, completely reset the form to clear all values
    this.keyResultForm.reset();
    console.log('Form reset called');
    
    // Always reset isSubmitting to false when initializing the form
    this.isSubmitting = false;
    
    if (this.data && this.isEdit) {
      // Patch form with existing data for editing
      console.log('Editing mode - patching form with existing data');
      this.keyResultForm.patchValue({
        title: this.data.title || '',
        description: this.data.description || '',
        progress: this.data.progress || 0,
        status: this.data.status || Status.NotStarted,
        startDate: this.formatDateForInput(this.data.startDate),
        endDate: this.formatDateForInput(this.data.endDate)
      });
    } else {
      // Reset form with defaults for new key result
      console.log('Creation mode - setting default values');
      
      const today = new Date();
      const nextMonth = new Date();
      nextMonth.setMonth(nextMonth.getMonth() + 1);
      
      this.keyResultForm.patchValue({
        title: '',  // Explicitly set empty title
        description: '',  // Explicitly set empty description
        progress: 0,
        status: Status.NotStarted,
        startDate: this.formatDateForInput(today),
        endDate: this.formatDateForInput(nextMonth)
      });
    }
  }

  private createForm(): FormGroup {
    return this.fb.group({
      title: ['', Validators.required],
      description: [''],
      progress: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      status: [Status.NotStarted, Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required]
    });
  }

  // Helper method to format dates for the date input
  private formatDateForInput(date: Date | string): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    
    // Format as YYYY-MM-DD for the date input
    return d.toISOString().split('T')[0];
  }

  handleSubmit() {
    if (this.keyResultForm.invalid) {
      this.keyResultForm.markAllAsTouched();
      return;
    }
    
    // Set isSubmitting to true
    this.isSubmitting = true;
    
    const formValues = this.keyResultForm.value;
    const keyResultData: Partial<KeyResult> = {
      ...formValues,
      objectiveId: this.isEdit && this.data ? this.data.objectiveId : this.objectiveId,
      userId: this.isEdit && this.data ? this.data.userId : this.userId
    };
    
    // If editing, preserve the ID
    if (this.isEdit && this.data) {
      keyResultData.id = this.data.id;
    }
    
    // Add a delay of 800ms to make the loading spinner visible
    setTimeout(() => {
      this.onSubmit.emit(keyResultData);
      
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

  // Public method to force reset form from parent component
  public reset() {
    this.resetForm();
  }
}