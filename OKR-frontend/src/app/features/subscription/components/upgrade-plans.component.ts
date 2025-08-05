import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { SubscriptionPlanService } from '../../../services/subscription-plan.service';
import { SubscriptionService } from '../../../services/subscription.service';
import { Plan } from '../../../models/plan.interface';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../../../shared/shared.module';
import { FormsModule } from '@angular/forms';
import { AuthStateService } from '../../../services/auth-state.service';
import { RoleType } from '../../../models/role-type.enum';

// Declare Stripe as a global variable since it's loaded from a CDN
declare var Stripe: any;

@Component({
  selector: 'app-upgrade-plans',
  standalone: true,
  imports: [CommonModule, SharedModule, FormsModule],
  templateUrl: './upgrade-plans.component.html',
  styleUrls: []
})
export class UpgradePlansComponent implements OnInit {
  @ViewChild('cardElement') cardElementRef!: ElementRef;
  
  plans: Plan[] = [];
  loading = false;
  error: string | null = null;
  selectedPlanId: string = '';
  
  // Payment steps
  currentStep: number = 1;
  
  // Stripe related properties
  private stripe: any;
  private elements: any;
  private cardElement: any;
  cardName: string = '';
  isProcessing: boolean = false;
  paymentSuccess: boolean = false;
  errorMessage: string = '';

  // Mock plans for initial display
  mockPlans: Plan[] = [
    {
      id: 'free',
      planId: 'free-plan',
      name: 'Free',
      planType: 'Free',
      description: 'Basic features for individuals',
      price: 0,
      interval: 'month',
      period: 'month',
      features: [
        'Up to 3 projects',
        'Basic reporting',
        'Limited collaboration',
        'Email support'
      ],
      recommended: false,
      buttonText: 'Start Now'
    },
    {
      id: 'pro',
      planId: 'pro-plan',
      name: 'Professional',
      planType: 'Professional',
      description: 'Everything you need for teams',
      price: 19.99,
      interval: 'month',
      period: 'month',
      features: [
        'Unlimited projects',
        'Advanced reporting',
        'Team collaboration',
        'Priority support',
        'Custom branding'
      ],
      recommended: true,
      buttonText: 'Go Pro'
    },
    {
      id: 'enterprise',
      planId: 'enterprise-plan',
      name: 'Enterprise',
      planType: 'Enterprise',
      description: 'Advanced features for large organizations',
      price: 49.99,
      interval: 'month',
      period: 'month',
      features: [
        'All Professional features',
        'Enterprise-grade security',
        'Dedicated account manager',
        'Custom integrations',
        'Advanced analytics',
        'SLA guarantees'
      ],
      recommended: false,
      buttonText: 'Contact Sales'
    }
  ];

  constructor(
    private subscriptionPlanService: SubscriptionPlanService,
    private subscriptionService: SubscriptionService,
    private authState: AuthStateService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.loadPlans();
    
    // Get pre-selected plan from URL if any
    this.route.queryParams.subscribe(params => {
      if (params['plan']) {
        this.selectedPlanId = params['plan'];
      }
    });
  }

  loadPlans() {
    this.loading = true;
    this.plans = this.mockPlans; // Initialize with mock plans to prevent empty state
    
    this.subscriptionPlanService.getAllPlans().subscribe(
      (plans: Plan[]) => {
        if (plans && plans.length > 0) {
          // Set frontend-specific fields for backward compatibility
          this.plans = plans.map(plan => ({
            ...plan,
            period: plan.interval,
            buttonText: plan.buttonText || 'Start Now',
            recommended: plan.recommended || false
          }));
          console.log('Loaded plans from API:', this.plans);
        } else {
          console.log('No plans returned from API, keeping mock plans');
        }
        this.loading = false;
      },
      (error: any) => {
        console.error('Error loading plans', error);
        this.error = 'Failed to load subscription plans';
        // Keep the mock plans if API fails
        console.log('Keeping mock plans due to API error');
        this.loading = false;
      }
    );
  }

  selectPlan(planId: string) {
    this.selectedPlanId = planId;
  }

  getSelectedPlan(): Plan {
    return this.plans.find(p => p.id === this.selectedPlanId) || this.plans[0];
  }

  goToStep(step: number) {
    this.currentStep = step;
    
    // Initialize Stripe when going to the payment step
    if (step === 2) {
      // Allow time for the DOM to update before initializing Stripe Elements
      setTimeout(() => this.initializeStripe(), 200);
    }
  }

  initializeStripe() {
    this.subscriptionService.getStripeConfig().subscribe(
      config => {
        // Initialize Stripe with the publishable key
        this.stripe = Stripe(config.publishableKey);
        this.elements = this.stripe.elements();
        
        // Custom styling for the card Element
        const style = {
          base: {
            color: '#1a1a1a',
            fontFamily: '"Inter", sans-serif',
            fontSmoothing: 'antialiased',
            fontSize: '16px',
            '::placeholder': {
              color: '#6B7280'
            },
            backgroundColor: 'transparent'
          },
          invalid: {
            color: '#ef4444',
            iconColor: '#ef4444'
          }
        };
        
        // Create a card Element and mount it to the dom
        this.cardElement = this.elements.create('card', { style });
        
        // Mount the card Element to the DOM
        setTimeout(() => {
          if (this.cardElementRef && this.cardElementRef.nativeElement) {
            this.cardElement.mount(this.cardElementRef.nativeElement);
            
            // Listen for errors from the card Element
            this.cardElement.on('change', (event: any) => {
              if (event.error) {
                this.errorMessage = event.error.message;
              } else {
                this.errorMessage = '';
              }
            });
          }
        }, 100);
        
        console.log('Stripe Elements initialized successfully');
      },
      error => {
        console.error('Failed to initialize Stripe:', error);
        this.errorMessage = 'Failed to initialize payment system. Please try again later.';
      }
    );
  }

  processPayment() {
    // Validate form
    if (!this.cardName) {
      this.errorMessage = 'Please enter the cardholder name';
      return;
    }
    
    console.log('Setting isProcessing to true');
    this.isProcessing = true;
    this.errorMessage = '';
    
    // Force change detection to ensure UI updates
    setTimeout(() => {
      console.log('isProcessing status:', this.isProcessing);
      console.log('Starting payment process with card element and name:', this.cardName);
      
      // Use Stripe Elements to create a payment method
      this.stripe.createPaymentMethod({
        type: 'card',
        card: this.cardElement,
        billing_details: {
          name: this.cardName
        }
      }).then((result: any) => {
        if (result.error) {
          // Show error to your customer
          console.log('Setting isProcessing to false due to error');
          this.isProcessing = false;
          this.errorMessage = result.error.message;
          console.error('Stripe payment method creation failed:', result.error);
        } else {
          console.log('Stripe payment method created successfully:', result.paymentMethod.id);
          
          // Get current user information
          const currentUser = this.authState.getCurrentUser();
          console.log('Current user during payment processing:', currentUser);
          
          // Get the selected plan object
          const selectedPlan = this.getSelectedPlan();
          console.log('Selected plan:', selectedPlan);
          
          // Send paymentMethod.id to the server
          const subscriptionRequest: {
            planId: string;
            paymentMethodId: string;
            organizationId?: string;
          } = {
            planId: selectedPlan.planId || selectedPlan.id, // Use planId if available, otherwise fall back to id
            paymentMethodId: result.paymentMethod.id
          };
          
          // Explicitly add organization ID if available
          if (currentUser?.organizationId) {
            subscriptionRequest.organizationId = currentUser.organizationId;
            console.log('Added organizationId to subscription request:', currentUser.organizationId);
          } else {
            console.warn('No organizationId available in current user!');
          }
          
          console.log('Sending subscription request to API:', subscriptionRequest);
          
          // Use the actual API call
          this.subscriptionService.createSubscription(subscriptionRequest)
            .subscribe(
              response => {
                console.log('Subscription created successfully:', response);
                this.isProcessing = false;
                this.paymentSuccess = true;
                
                // Go to confirmation step
                this.goToStep(3);
              },
              error => {
                this.isProcessing = false;
                console.error('Payment failed:', error);
                
                // Extract more detailed error information
                let errorMessage = 'Payment processing failed. Please try again later.';
                
                if (error.error) {
                  console.error('Error details:', error.error);
                  if (typeof error.error === 'string') {
                    errorMessage = error.error;
                  } else if (error.error.message) {
                    errorMessage = error.error.message;
                  } else if (error.error.error && error.error.error.message) {
                    errorMessage = error.error.error.message;
                  } else if (error.status === 401) {
                    errorMessage = 'Authorization error. Please log in again.';
                  } else if (error.status === 403) {
                    errorMessage = 'You do not have permission to perform this action.';
                  }
                }
                
                this.errorMessage = errorMessage;
              }
            );
        }
      });
    }, 0);
  }

  goBack() {
    // Check user role and navigate accordingly
    const currentUser = this.authState.getCurrentUser();
    const userRole = currentUser?.role;
    
    console.log('Navigation: User role for back navigation:', userRole);
    
    // If user is SuperAdmin, go to subscription plan management page
    if (this.authState.hasRole(RoleType.SuperAdmin)) {
      console.log('Navigation: SuperAdmin detected, navigating to subscription plan management');
      this.router.navigate(['/subscription']);
    } else {
      // For OrganizationAdmin or other roles, go to dashboard
      console.log('Navigation: Non-SuperAdmin detected, navigating to dashboard');
      this.router.navigate(['/dashboard/organizationAdmin']);
    }
  }
}