import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { OKRsComponent } from './components/okrs.component';
import { SessionDetailComponent } from './components/session-detail/session-detail.component';
import { AuthGuard } from '../../guards/auth.guard';
import { OkrSessionGuard } from '../../guards/okr-session.guard';

const routes: Routes = [
  { path: '', component: OKRsComponent },
  // { path: ':id', component: SessionDetailComponent },
  { path: ':orgId', component: OKRsComponent },
  { 
    path: ':id', 
    component: SessionDetailComponent,
    canActivate: [AuthGuard, OkrSessionGuard]
  },
  { 
    path: ':orgId/okrs/:id', 
    component: SessionDetailComponent,
    canActivate: [AuthGuard, OkrSessionGuard]
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class OKRsRoutingModule { }
