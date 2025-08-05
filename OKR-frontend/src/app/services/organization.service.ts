import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { Organization, CreateOrganizationCommand, CreateOrganizationResponse, PaginatedListResult, UpdateOrganizationCommand } from '../models/organization.interface';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { tap, catchError, map, switchMap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class OrganizationService {
  private apiUrl = `${environment.apiUrl}/api/organizations`;
  private organizations: Organization[] = [
    {
      id: '1',
      name: 'Tech Innovations',
      description: 'A leading tech company specializing in AI',
      country: 'USA',
      industry: 'Technology',
      size: 200,
      email: 'contact@techinnovations.com',
      phone: '+1234567890',
      isActive: true
    },
    {
      id: '2',
      name: 'Creative Design Co.',
      description: 'Award-winning design agency',
      country: 'UK',
      industry: 'Design',
      size: 50,
      email: 'info@creativedesign.co.uk',
      phone: '+441234567890',
      isActive: true
    },
    {
      id: '3',
      name: 'Marketing Growth',
      description: 'Global marketing agency',
      country: 'Canada',
      industry: 'Marketing',
      size: 120,
      email: 'hello@marketinggrowth.com',
      phone: '+14165567890',
      isActive: false
    }
  ];

  constructor(private http: HttpClient) {}

  createOrganization(organization: CreateOrganizationCommand): Observable<CreateOrganizationResponse> {
    return this.http.post<CreateOrganizationResponse>(this.apiUrl, organization)
      .pipe(
        tap(response => console.log('Organization creation raw response:', response))
      );
  }

  getOrganizations(): Observable<Organization[]> {
    // No parameters - just get all organizations
    return this.http.get<PaginatedListResult<Organization>>(`${this.apiUrl}`)
      .pipe(
        tap(response => console.log('Organizations API response:', response)),
        // Extract just the items array from the paginated response
        map(response => response.items || []),
        tap(organizations => console.log('Extracted organizations:', organizations)),
        catchError(error => {
          console.error('Error fetching organizations:', error);
          // No fallback to mock data, just propagate the error
          return throwError(() => error);
        })
      );
  }

  getOrganizationById(id: string): Observable<Organization> {
    return this.http.get<Organization>(`${this.apiUrl}/${id}`)
      .pipe(
        tap(organization => console.log(`Retrieved organization with ID: ${id}`, organization))
      );
  }

  updateOrganization(id: string, updates: UpdateOrganizationCommand): Observable<Organization> {
    return this.http.put<Organization>(`${this.apiUrl}/${id}`, updates, {
      responseType: 'text' as 'json' // Tell Angular to treat the response as text
    })
      .pipe(
        tap(response => console.log(`Updated organization with ID: ${id}`, response)),
        // After receiving text response, get the updated organization
        switchMap(() => this.getOrganizationById(id)),
        catchError(error => {
          console.error('Error updating organization:', error);
          // No fallback to mock data, just propagate the error
          return throwError(() => error);
        })
      );
  }
}
