import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, tap } from 'rxjs';
import { Subscription } from '../models/subscription.interface'
import { environment } from '../../environments/environment';

export interface SubscriptionPlan {
  id: string;
  name: string;
  description: string;
  price: number;
  interval: 'month' | 'year';
  features: string[];
}

export interface StripeConfig {
  publishableKey: string;
}

export interface SubscriptionRequest {
  planId: string;
  paymentMethodId: string;
  organizationId?: string;
}

export interface BillingHistoryItem {
  invoiceId: string;
  paidAt: Date;
  amount: number;
  currency: string;
  status: string;
  description: string;
  invoicePdfUrl: string;
}

export interface SuperAdminDashboard {
  activeSubscriptions: number;
  mrr: number;
  arr: number;
  arpu: number;
  churnRate: number;
  planDistribution: PlanDistributionItem[];
}

export interface PlanDistributionItem {
  plan: string;
  count: number;
}

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  private apiUrl = `${environment.apiUrl}/api/subscriptions`;

  constructor(private http: HttpClient) { }

  /**
   * Get Stripe configuration from the server
   */
  getStripeConfig(): Observable<StripeConfig> {
    return this.http.get<StripeConfig>(`${this.apiUrl}/config`);
  }

  /**
   * Get available subscription plans
   */
  getPlans(): Observable<SubscriptionPlan[]> {
  
    
    ubscriptionPlan[]>(`${this.apiUrl}/plans`);
  }

  /**
   * Create a new subscription
   */
  createSubscription(subscriptionRequest: SubscriptionRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, subscriptionRequest);
  }

  /**
   * Get current user's subscription
   */
  getCurrentSubscription(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/organization-subscription`);
  }

  /**
   * Cancel current subscription
   */
  cancelSubscription(): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/cancel`, {});
  }

  isOrganizationSubscribed(organizationId: string): Observable<Subscription | null> {
    return this.http.get<Subscription | null>(`${this.apiUrl}/is-subscribed/${organizationId}`);}
  /**
   * Get billing history for organization
   */
  getBillingHistory(): Observable<BillingHistoryItem[]> {
    console.log('Subscription service: Requesting billing history from', `${this.apiUrl}/billing-history`);
    return this.http.get<BillingHistoryItem[]>(`${this.apiUrl}/billing-history`)
      .pipe(
        tap(data => console.log('Subscription service: Received billing history data:', data)),
        catchError(err => {
          console.error('Subscription service: Error fetching billing history:', err);
          throw err;
        })
      );
  }

  /**
   * Get dashboard stats for superadmin
   */
  getSuperAdminDashboard(): Observable<SuperAdminDashboard> {
    return this.http.get<SuperAdminDashboard>(`${this.apiUrl}/admin/dashboard`);
  }
} 