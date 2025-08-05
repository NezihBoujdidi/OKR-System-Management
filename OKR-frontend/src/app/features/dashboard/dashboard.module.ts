import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardComponent } from './components/dashboard.component';
import { OrganizationAdminDashboardComponent } from './components/organization-admin-dashboard/organization-admin-dashboard.component';
import { TeamManagerDashboardComponent } from './components/team-manager-dashboard/team-manager-dashboard.component';
import { CollaboratorDashboardComponent } from './components/collaborator-dashboard/collaborator-dashboard.component';
import { DashboardIndexComponent } from './components/dashboard-index/dashboard-index.component';
import { SuperAdminDashboardComponent } from './components/super-admin-dashboard/super-admin-dashboard.component';
import { NgChartsModule } from 'ng2-charts';
import { SharedModule } from '../../shared/shared.module';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { SubscriptionModule } from '../subscription/subscription.module';

@NgModule({
  declarations: [
    DashboardComponent,
    OrganizationAdminDashboardComponent,
    TeamManagerDashboardComponent,
    CollaboratorDashboardComponent, 
    DashboardIndexComponent,
    SuperAdminDashboardComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    NgChartsModule,
    DashboardRoutingModule,
    SubscriptionModule
  ]
})
export class DashboardModule { }

// Add the indeterminate progress animation to global styles
import { Injector } from '@angular/core';

// Self-executing function to add styles once the module is loaded
(function() {
  const style = document.createElement('style');
  style.innerHTML = `
    @keyframes indeterminate-progress {
      0% {
        transform: translateX(-100%);
        width: 50%;
      }
      50% {
        transform: translateX(100%);
        width: 70%;
      }
      100% {
        transform: translateX(-100%);
        width: 50%;
      }
    }
    
    .animate-indeterminate-progress {
      animation: indeterminate-progress 1.5s infinite ease-in-out;
    }
    
    /* Slow spinning animation for decorative elements */
    @keyframes spin-slow {
      from {
        transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
      }
    }
    
    .animate-spin-slow {
      animation: spin-slow 6s linear infinite;
    }
    
    /* Animation for typing indicators if not already defined */
    @keyframes typing1 {
      0%, 100% { transform: translateY(0); opacity: 0.4; }
      50% { transform: translateY(-4px); opacity: 1; }
    }
    
    @keyframes typing2 {
      0%, 100% { transform: translateY(0); opacity: 0.4; }
      50% { transform: translateY(-4px); opacity: 1; }
    }
    
    @keyframes typing3 {
      0%, 100% { transform: translateY(0); opacity: 0.4; }
      50% { transform: translateY(-4px); opacity: 1; }
    }
    
    .animate-typing1 {
      animation: typing1 1s infinite 0s;
    }
    
    .animate-typing2 {
      animation: typing2 1s infinite 0.2s;
    }
    
    .animate-typing3 {
      animation: typing3 1s infinite 0.4s;
    }
  `;
  document.head.appendChild(style);
})();

