import { Component, OnInit } from '@angular/core';
import { DashboardService } from '../../../services/dashboard.service';
import { KeyMetric, Activity, ChartData, TeamPerformance } from '../../../models/dashboard.interface';
import { Chart, ChartConfiguration } from 'chart.js/auto';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  keyMetrics: KeyMetric[] = [];
  recentActivities: Activity[] = [];
  teamPerformance: TeamPerformance[] = [];

  // Employee Growth Chart
  employeeGrowthData: ChartConfiguration['data'] = {
    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
    datasets: [
      {
        label: 'Employee Growth',
        data: [65, 80, 95, 110, 130, 150],
        borderColor: '#FFD700',
        tension: 0.4,
        fill: false
      }
    ]
  };

  employeeGrowthOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          display: false
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    },
    plugins: {
      legend: {
        display: false
      }
    }
  };

  // Team Performance Chart
  teamPerformanceData: ChartConfiguration['data'] = {
    labels: ['Team A', 'Team B', 'Team C', 'Team D', 'Team E'],
    datasets: [
      {
        label: 'Performance Score',
        data: [85, 92, 78, 95, 88],
        backgroundColor: '#FFD700',
        borderRadius: 5
      }
    ]
  };

  teamPerformanceOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          display: false
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    },
    plugins: {
      legend: {
        display: false
      }
    }
  };

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.dashboardService.getKeyMetrics().subscribe(metrics => {
      this.keyMetrics = metrics;
    });

    this.dashboardService.getRecentActivities().subscribe(activities => {
      this.recentActivities = activities;
    });

    this.dashboardService.getTeamPerformance().subscribe(data => {
      this.teamPerformance = data;
    });

    this.dashboardService.getEmployeeGrowthData().subscribe(data => {
      this.employeeGrowthData = data;
    });

    this.dashboardService.getTeamPerformanceChart().subscribe(data => {
      this.teamPerformanceData = data;
    });
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'on-track': return 'text-green-600';
      case 'at-risk': return 'text-yellow-600';
      case 'behind': return 'text-red-600';
      default: return 'text-gray-600';
    }
  }

  getTrendIcon(trend: number): string {
    return trend > 0 ? '↑' : trend < 0 ? '↓' : '→';
  }

  getTrendClass(trend: number): string {
    return trend > 0 ? 'text-green-600' : trend < 0 ? 'text-red-600' : 'text-gray-600';
  }
}