import { Component, OnInit } from '@angular/core';
import { SubscriptionPlanService } from '../../../services/subscription-plan.service';
import { Plan } from '../../../models/plan.interface';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ModalComponent } from '../../../shared/modal/modal.component';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-subscription',
  templateUrl: './subscription.component.html',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ModalComponent,
    SharedModule
  ]
})
export class SubscriptionComponent implements OnInit {
  plans: Plan[] = [];
  loading = false;
  error: string | null = null;
  
  showPlanModal = false;
  editingPlan: Plan | null = null;
  planForm!: FormGroup;
  planFeatures: string[] = [];
  featureInput = '';

  // Delete confirmation properties
  showDeleteModal = false;
  planToDelete: Plan | null = null;
  isDeleting = false;

  constructor(
    private subscriptionPlanService: SubscriptionPlanService,
    private fb: FormBuilder
  ) {}

  ngOnInit() {
    this.loadPlans();
    this.initPlanForm();
  }

  initPlanForm() {
    this.planForm = this.fb.group({
      id: [''],
      planId: ['', Validators.required],
      name: ['', Validators.required],
      planType: ['Basic', Validators.required],
      description: ['', Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      interval: ['month', Validators.required],
      recommended: [false],
      buttonText: ['Start Now']
    });
  }

  loadPlans() {
    this.loading = true;
    this.subscriptionPlanService.getAllPlans().subscribe(
      (plans: Plan[]) => {
        // Set frontend-specific fields for backward compatibility
        this.plans = plans.map(plan => ({
          ...plan,
          period: plan.interval,
          buttonText: plan.buttonText || 'Start Now',
          recommended: plan.recommended || false
        }));
        this.loading = false;
      },
      (error: any) => {
        console.error('Error loading plans', error);
        this.error = 'Failed to load subscription plans';
        this.loading = false;
      }
    );
  }

  openAddPlanModal() {
    this.editingPlan = null;
    this.planFeatures = [];
    this.planForm.reset({
      id: '',
      planId: '',
      name: '',
      planType: 'Basic',
      description: '',
      price: 0,
      interval: 'month',
      recommended: false,
      buttonText: 'Start Now'
    });
    this.showPlanModal = true;
  }

  openEditPlanModal(plan: Plan) {
    this.editingPlan = {...plan};
    this.planFeatures = [...plan.features];
    this.planForm.patchValue({
      id: plan.id,
      planId: plan.planId,
      name: plan.name,
      planType: plan.planType,
      description: plan.description,
      price: plan.price,
      interval: plan.interval,
      recommended: plan.recommended || false,
      buttonText: plan.buttonText || 'Start Now'
    });
    this.showPlanModal = true;
  }

  closeModal() {
    this.showPlanModal = false;
    this.editingPlan = null;
  }

  addFeature() {
    if (this.featureInput.trim()) {
      this.planFeatures.push(this.featureInput.trim());
      this.featureInput = '';
    }
  }

  removeFeature(index: number) {
    this.planFeatures.splice(index, 1);
  }

  savePlan() {
    if (this.planForm.invalid) return;
    
    const formData = this.planForm.value;
    
    // Prepare the data object for the API
    const planData = {
      id: formData.id,
      planId: formData.planId,
      name: formData.name,
      planType: formData.planType,
      description: formData.description,
      price: formData.price,
      interval: formData.interval,
      features: this.planFeatures
    };
    
    // For UI display purposes, save the frontend fields
    const uiData = {
      ...planData,
      period: formData.interval,
      recommended: formData.recommended,
      buttonText: formData.buttonText
    };
    
    this.loading = true;
    
    if (planData.id) {
      // Update existing plan
      this.subscriptionPlanService.updatePlan(planData.id, planData).subscribe(
        (updatedPlan: Plan) => {
          const index = this.plans.findIndex(p => p.id === updatedPlan.id);
          if (index !== -1) {
            // Merge backend data with UI fields
            this.plans[index] = {
              ...updatedPlan,
              period: updatedPlan.interval,
              recommended: formData.recommended,
              buttonText: formData.buttonText
            };
          }
          this.closeModal();
          this.loading = false;
        },
        (error: any) => {
          console.error('Error updating plan', error);
          this.error = error.error || 'Failed to update plan';
          this.loading = false;
        }
      );
    } else {
      // Create new plan
      this.subscriptionPlanService.createPlan(planData).subscribe(
        (newPlan: Plan) => {
          // Add the new plan with UI fields
          this.plans.push({
            ...newPlan,
            period: newPlan.interval,
            recommended: formData.recommended,
            buttonText: formData.buttonText
          });
          this.closeModal();
          this.loading = false;
        },
        (error: any) => {
          console.error('Error creating plan', error);
          this.error = error.error || 'Failed to create plan';
          this.loading = false;
        }
      );
    }
  }

  deletePlan(plan: Plan) {
    this.loading = true;
    this.error = null;
    
    this.subscriptionPlanService.deletePlan(plan.id).subscribe(
      () => {
        // Remove from plans array
        this.plans = this.plans.filter(p => p.id !== plan.id);
        this.loading = false;
        
        // If we're in delete confirmation flow, close the modal and reset state
        if (this.showDeleteModal) {
          this.isDeleting = false;
          this.showDeleteModal = false;
          this.planToDelete = null;
        }
      },
      (error: any) => {
        console.error('Error deleting plan', error);
        this.error = error.error || 'Failed to delete plan';
        this.loading = false;
        this.isDeleting = false;
      }
    );
  }

  // New methods for delete confirmation
  openDeleteConfirmation(plan: Plan) {
    this.planToDelete = plan;
    this.showDeleteModal = true;
  }

  cancelDelete() {
    this.showDeleteModal = false;
    this.planToDelete = null;
  }

  confirmDelete() {
    if (!this.planToDelete) return;
    
    this.isDeleting = true;
    setTimeout(() => {
      this.deletePlan(this.planToDelete!);
    }, 800); // Add a slight delay for visual feedback
  }
}
