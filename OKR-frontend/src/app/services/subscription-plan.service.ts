import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Plan } from '../models/plan.interface';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionPlanService {
  private apiUrl = `${environment.apiUrl}/api/subscription-plans`;

  constructor(private http: HttpClient) {}

  getAllPlans(includeInactive: boolean = false): Observable<Plan[]> {
    return this.http.get<Plan[]>(`${this.apiUrl}?includeInactive=${includeInactive}`);
  }

  getPlanById(id: string): Observable<Plan> {
    return this.http.get<Plan>(`${this.apiUrl}/${id}`);
  }

  createPlan(plan: any): Observable<Plan> {
    return this.http.post<Plan>(this.apiUrl, plan);
  }

  updatePlan(id: string, plan: any): Observable<Plan> {
    return this.http.put<Plan>(`${this.apiUrl}/${id}`, plan);
  }

  deletePlan(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  activatePlan(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/activate`, {});
  }

  deactivatePlan(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }
} 