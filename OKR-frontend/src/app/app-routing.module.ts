import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout.component';
import { LoadingGuard } from './core/guards/loading.guard';
import { AuthGuard } from './guards/auth.guard';
import { RoleGuard } from './guards/role.guard';
import { RoleType } from './models/role-type.enum';
import { ChatComponent } from './components/chat/chat.component';
import { OkrSessionGuard } from './guards/okr-session.guard';
import { UnauthorizedComponent } from './shared/unauthorized/unauthorized.component';
import { SubscriptionGuard } from './guards/subscription.guard';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: '',
    component: DashboardLayoutComponent,
    children: [
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module')
          .then(m => m.DashboardModule)
      },
      {
        path: 'home',
        loadChildren: () => import('./features/home/home.module')
          .then(m => m.HomeModule)
      },
      {
        path: 'employees',
        loadChildren: () => import('./features/employees/employees.module')
          .then(m => m.EmployeesModule)
      },
      // New path for organization teams
      {
        path: 'OrgTeams',
        loadChildren: () => {
          console.log('📁 App Routing - Loading Teams module for organization teams...');
          return import('./features/teams/teams.module').then(m => {
            console.log('📁 App Routing - Teams module for organization teams loaded successfully');
            return m.TeamsModule;
          });
        }
      },
      {
        path: 'organizations/:id/employees',
        loadChildren: () => import('./features/employees/employees.module').then(m => m.EmployeesModule),
        canActivate: [RoleGuard],
        data: { roles: [RoleType.SuperAdmin] }
      },
      // General routes follow
      {
        path: 'teams',
        loadChildren: () => {
          console.log('📁 App Routing - Loading Teams module...');
          return import('./features/teams/teams.module').then(m => {
            console.log('📁 App Routing - Teams module loaded successfully');
            return m.TeamsModule;
          });
        }
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.module')
          .then(m => m.UsersModule)
      },
      {
        path: 'organization',
        loadChildren: () => import('./features/organization/organization.module')
          .then(m => m.OrganizationModule)
      },
      {
        path: 'organizations',
        loadChildren: () => import('./features/okrs/okrs.module').then(m => m.OKRsModule),
        canActivate: [RoleGuard],
        data: { roles: [RoleType.SuperAdmin, RoleType.OrganizationAdmin, RoleType.TeamManager, RoleType.Collaborator] }
      },  
      {
        path: 'subscription',
        loadChildren: () => import('./features/subscription/subscription.module').then(m => m.SubscriptionModule),
        canActivate: [AuthGuard, SubscriptionGuard]
      },
      {
        path: 'meetings',
        loadChildren: () => import('./features/meetings/meetings.module').then(m => m.MeetingsModule),
        canActivate: [AuthGuard]
      },
      {
        path: 'okr-best-practices',
        loadChildren: () => import('./features/okr-best-practices/okr-best-practices.module')
          .then(m => m.OkrBestPracticesModule)
      },
      {
        path: 'okrs',
        loadChildren: () => import('./features/okrs/okrs.module')
          .then(m => m.OKRsModule)
      },
      {
        path: 'okrs/:id',
        loadChildren: () => import('./features/okrs/okrs.module')
          .then(m => m.OKRsModule),
        canActivate: [AuthGuard, OkrSessionGuard]
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule)
      }
    ]
  },
  {
    path: 'login',
    loadChildren: () => import('./features/login/login.module')
      .then(m => m.LoginModule),
    canActivate: [LoadingGuard]
  },
  {
    path: 'signup',
    loadChildren: () => import('./features/signup/signup.module')
      .then(m => m.SignupModule)
  },
  {
    path: 'chat',
    loadChildren: () => import('./components/chat/chat.module').then(m => m.ChatModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'unauthorized',
    component: UnauthorizedComponent
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
