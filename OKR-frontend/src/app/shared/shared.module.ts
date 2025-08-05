import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { NavigationSidebarComponent } from './navigation-sidebar/components/navigation-sidebar.component';
import { ContentSidebarComponent } from './content-sidebar/components/content-sidebar.component';
import { LoaderComponent } from './loader/components/loader.component';
import { TableComponent } from './table/components/table.component';
import { GridComponent } from './grid/components/grid.component';
import { EmptyStateComponent } from './empty-state/components/empty-state.component';
import { ProfileAvatarComponent } from './profile-avatar/components/profile-avatar.component';
import { DirectivesModule } from './directives/directives.module';
import { AppCardComponent } from './app-card/app-card.component';
import { LoadingOverlayModule } from './loading-overlay/loading-overlay.module';
import { ModalComponent } from './modal/modal.component';
import { UnauthorizedComponent } from './unauthorized/unauthorized.component';
import { ConfirmationDialogComponent } from './components/confirmation-dialog/confirmation-dialog.component';

@NgModule({
  declarations: [
    NavigationSidebarComponent,
    ContentSidebarComponent,
    LoaderComponent,
    TableComponent,
    GridComponent,
    EmptyStateComponent,
    ProfileAvatarComponent,
    UnauthorizedComponent,
    ConfirmationDialogComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ReactiveFormsModule,
    HttpClientModule,
    DirectivesModule,
    AppCardComponent,
    LoadingOverlayModule,
    ModalComponent
  ],
  exports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    NavigationSidebarComponent,
    ContentSidebarComponent,
    LoaderComponent,
    TableComponent,
    GridComponent,
    EmptyStateComponent,
    ProfileAvatarComponent,
    DirectivesModule,
    AppCardComponent,
    ModalComponent,
    UnauthorizedComponent,
    ConfirmationDialogComponent
  ]
})
export class SharedModule { } 