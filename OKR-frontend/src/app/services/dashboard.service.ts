import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { KeyMetric, Activity, ChartData, TeamPerformance } from '../models/dashboard.interface';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  constructor() {}

  getKeyMetrics(): Observable<KeyMetric[]> {
    return of([
      { label: 'Total Employees', value: 150, trend: '+15%', isPositive: true },
      { label: 'Active Teams', value: 5, trend: '+2', isPositive: true },
      { label: 'Avg Performance', value: '87.6%', trend: '+5%', isPositive: true },
      { label: 'Active Projects', value: 12, trend: '+3', isPositive: true }
    ]);
  }

  getRecentActivities(): Observable<Activity[]> {
    return of([
      {
        title: 'New employee joined',
        description: 'Sarah Wilson joined Design team',
        time: '2 hours ago',
        type: 'new_employee'
      },
      {
        title: 'Project completed',
        description: 'Mobile app redesign project completed',
        time: '5 hours ago',
        type: 'project'
      },
      {
        title: 'Team meeting scheduled',
        description: 'Product review meeting at 3:00 PM',
        time: '1 day ago',
        type: 'meeting'
      }
    ]);
  }

  getTeamPerformance(): Observable<TeamPerformance[]> {
    return of([
      { team: 'Engineering', progress: 85, total: 100, status: 'on-track' },
      { team: 'Design', progress: 65, total: 100, status: 'at-risk' },
      { team: 'Marketing', progress: 92, total: 100, status: 'on-track' },
      { team: 'Sales', progress: 78, total: 100, status: 'on-track' },
      { team: 'Operations', progress: 45, total: 100, status: 'behind' }
    ]);
  }

  getEmployeeGrowthData(): Observable<ChartData> {
    return of({
      labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
      datasets: [{
        label: 'Employee Growth',
        data: [65, 80, 95, 110, 130, 150],
        backgroundColor: '#FFD700',
        borderColor: '#FFD700',
        borderWidth: 2,
        fill: false
      }]
    });
  }

  getTeamPerformanceChart(): Observable<ChartData> {
    return of({
      labels: ['Team A', 'Team B', 'Team C', 'Team D', 'Team E'],
      datasets: [{
        label: 'Performance Score',
        data: [85, 92, 78, 95, 88],
        backgroundColor: '#FFD700',
        borderWidth: 0
      }]
    });
  }
}