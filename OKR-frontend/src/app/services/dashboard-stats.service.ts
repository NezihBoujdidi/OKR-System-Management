import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { 
  CollaboratorPerformanceRangeDto,
  EmployeeGrowthStatsDto, 
  TeamPerformanceBarDto,
  ActiveOKRSessionsDto,
  CollaboratorTaskStatusStatsDto,
  ActiveTeamsDto,
  ManagerSessionStatsDto,
  CollaboratorMonthlyPerformanceDto,
  GlobalOrgOkrStatsDto,
  UserGrowthStatsDto,
  UserRolesCountDto,
  OrgPaidPlanCountDto,
  KeyResultTaskDto,
  CollaboratorTaskDetailsDto
} from '../models/dashboard-stats.interface';
import { Objective } from '../models/objective.interface';

@Injectable({
  providedIn: 'root'
})
export class DashboardStatsService {
  private apiUrl = `${environment.apiUrl}/api/dashboard-stats`;

  constructor(private http: HttpClient) { }

  /**
   * Get count of active OKR sessions for an organization
   * @param organizationId Organization ID
   * @returns Observable with the count of current and last month's active OKR sessions
   */
  getActiveOKRs(organizationId: string): Observable<ActiveOKRSessionsDto> {
    return this.http.get<{ activeOKRSessionCount: number, activeOKRSessionCountLastMonth: number }>(`${this.apiUrl}/active-okrs/${organizationId}`)
      .pipe(
        map(response => ({
          activeOKRSessionCount: response.activeOKRSessionCount,
          activeOKRSessionCountLastMonth: response.activeOKRSessionCountLastMonth
        })),
        catchError(error => {
          console.error('Error fetching active OKRs:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get collaborator performance metrics for an organization
   * @param organizationId Organization ID
   * @returns Observable with list of collaborator performance data including ranges
   */
  getCollaboratorPerformance(organizationId: string): Observable<CollaboratorPerformanceRangeDto[]> {
    return this.http.get<CollaboratorPerformanceRangeDto[]>(`${this.apiUrl}/collaborator-performance/${organizationId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching collaborator performance:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get employee growth statistics for an organization
   * @param organizationId Organization ID
   * @returns Observable with employee growth statistics
   */
  getEmployeeGrowthStats(organizationId: string): Observable<EmployeeGrowthStatsDto> {
    return this.http.get<EmployeeGrowthStatsDto>(`${this.apiUrl}/employee-growth/${organizationId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching employee growth stats:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get team performance bar chart data for an organization
   * @param organizationId Organization ID
   * @returns Observable with team performance bar chart data
   */
  getTeamPerformanceBarChart(organizationId: string): Observable<TeamPerformanceBarDto[]> {
    return this.http.get<TeamPerformanceBarDto[]>(`${this.apiUrl}/team-performance/${organizationId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching team performance bar chart:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get collaborator task status statistics for an organization
   * @param organizationId Organization ID
   * @returns Observable with list of collaborator task status statistics
   */
  getCollaboratorTaskStatusStats(organizationId: string): Observable<CollaboratorTaskStatusStatsDto[]> {
    return this.http.get<CollaboratorTaskStatusStatsDto[]>(`${this.apiUrl}/collaborator-task-status-stats/${organizationId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching collaborator task status stats:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get active teams statistics for an organization
   * @param organizationId Organization ID
   * @returns Observable with active teams count for current and last month
   */
  getActiveTeams(organizationId: string): Observable<ActiveTeamsDto> {
    return this.http.get<{ activeTeamsCount: number, activeTeamsCountLastMonth: number }>(`${this.apiUrl}/active-teams/${organizationId}`)
      .pipe(
        map(response => ({
          activeTeamsCount: response.activeTeamsCount,
          activeTeamsCountLastMonth: response.activeTeamsCountLastMonth
        })),
        catchError(error => {
          console.error('Error fetching active teams stats:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get session statistics for a team manager
   * @param managerId Manager ID
   * @returns Observable with manager session statistics
   */
  getManagerSessionStats(managerId: string): Observable<ManagerSessionStatsDto> {
    return this.http.get<ManagerSessionStatsDto>(`${this.apiUrl}/manager-session-stats/${managerId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching manager session stats:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get monthly performance data for a specific collaborator
   * @param organizationId Organization ID
   * @param collaboratorId Collaborator ID
   * @returns Observable with list of monthly performance data
   */
  getCollaboratorMonthlyPerformance(organizationId: string, collaboratorId: string): Observable<CollaboratorMonthlyPerformanceDto[]> {
    return this.http.get<CollaboratorMonthlyPerformanceDto[]>(
      `${this.apiUrl}/collaborator-monthly-performance/${organizationId}/${collaboratorId}`
    ).pipe(
      catchError(error => {
        console.error('Error fetching collaborator monthly performance:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Get global organization OKR statistics
   * @returns Observable with global organization and OKR session counts
   */
  getGlobalOrgOkrStats(): Observable<GlobalOrgOkrStatsDto> {
    return this.http.get<GlobalOrgOkrStatsDto>(`${this.apiUrl}/global-org-okr-stats`)
      .pipe(
        catchError(error => {
          console.error('Error fetching global organization OKR stats:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get user growth statistics
   * @returns Observable with yearly and monthly user growth data
   */
  getUserGrowthStats(): Observable<UserGrowthStatsDto> {
    return this.http.get<UserGrowthStatsDto>(`${this.apiUrl}/user-growth-stats`)
      .pipe(
        catchError(error => {
          console.error('Error fetching user growth stats:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get user roles count statistics
   * @returns Observable with counts of users in each role
   */
  getUserRolesCount(): Observable<UserRolesCountDto> {
    return this.http.get<UserRolesCountDto>(`${this.apiUrl}/user-roles-count`)
      .pipe(
        catchError(error => {
          console.error('Error fetching user roles count:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get paid organization count
   * @returns Observable with count of organizations on paid plans
   */
  getPaidOrgCount(): Observable<OrgPaidPlanCountDto> {
    return this.http.get<OrgPaidPlanCountDto>(`${this.apiUrl}/paid-org-count`)
      .pipe(
        catchError(error => {
          console.error('Error fetching paid organization count:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get task details for a specific collaborator (recent completed, in progress, overdue)
   * @param collaboratorId Collaborator ID
   * @returns Observable with lists of tasks categorized by status
   */
  getCollaboratorTaskDetails(collaboratorId: string): Observable<CollaboratorTaskDetailsDto> {
    return this.http.get<CollaboratorTaskDetailsDto>(
      `${this.apiUrl}/collaborator-task-details/${collaboratorId}`
    ).pipe(
      catchError(error => {
        console.error('Error fetching collaborator task details:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Get objectives for a specific manager
   * @param managerId Manager ID
   * @returns Observable with list of objectives managed by this manager
   */
  getObjectivesByManagerId(managerId: string): Observable<Objective[]> {
    return this.http.get<Objective[]>(`${this.apiUrl}/manager-objectives/${managerId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching manager objectives:', error);
          return throwError(() => error);
        })
      );
  }
} 