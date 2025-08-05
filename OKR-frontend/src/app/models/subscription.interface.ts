export interface Subscription {
    id: string;
    organizationId: string;
    createdByUserId: string;
    plan: string;
    startDate: string;
    endDate: string;
    isActive: boolean;
    status: string;
    amount: number;
    currency: string;
  }

  export enum SubscriptionPlan {
    Free = 'Free',
    Basic = 'Basic',
    Professional = 'Professional',
    Enterprise = 'Enterprise'
  }
