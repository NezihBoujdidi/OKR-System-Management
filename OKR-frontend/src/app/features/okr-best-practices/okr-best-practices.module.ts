import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { OkrBestPracticesComponent } from './components/okr-best-practices.component';

const routes: Routes = [
  {
    path: '',
    component: OkrBestPracticesComponent
  }
];

@NgModule({
  declarations: [OkrBestPracticesComponent],
  imports: [
    CommonModule,
    RouterModule.forChild(routes)
  ]
})
export class OkrBestPracticesModule { } 