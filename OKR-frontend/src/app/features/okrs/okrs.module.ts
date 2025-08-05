import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { OKRsComponent } from './components/okrs.component';
import { SessionDetailComponent } from './components/session-detail/session-detail.component';
import { SessionObjectivesComponent } from './components/session-detail/session-objectives/session-objectives.component';
import { SessionAlignmentComponent } from './components/session-detail/session-alignment/session-alignment.component';
import { SessionStatisticsComponent } from './components/session-detail/session-statistics/session-statistics.component';
import { TimelineModule } from '../../shared/timeline/timeline.module';
import { OKRsRoutingModule } from './okrs-routing.module';
import { SearchModule } from '../../shared/search/search.module';
import { DrawerModule } from '../../shared/drawer/drawer.module';
import { NewSessionFormComponent } from './components/new-session-form/new-session-form.component';
import { SessionCardModule } from '../../shared/session-card/session-card.module';
import { PdfExportService } from '../../services/pdf-export.service';
import { NewObjectiveFormComponent } from './components/new-objective-form/new-objective-form.component';
import { NewKeyResultFormComponent } from './components/new-key-result-form/new-key-result-form.component';
import { NewTaskFormComponent } from './components/new-task-form/new-task-form.component';
import { NotificationModule } from '../../shared/notification/notification.module';
import { ConfirmationModalModule } from '../../shared/confirmation-modal/confirmation-modal.module';
import { AiSessionAssistantComponent } from './components/ai-session-assistant/ai-session-assistant.component';
import { PdfExportModalComponent } from './components/pdf-export-modal/pdf-export-modal.component';

@NgModule({
  declarations: [
    OKRsComponent,
    SessionDetailComponent,
    SessionObjectivesComponent,
    SessionAlignmentComponent,
    SessionStatisticsComponent,
    NewSessionFormComponent,
    NewObjectiveFormComponent,
    NewKeyResultFormComponent,
    NewTaskFormComponent,
    AiSessionAssistantComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    SharedModule,
    TimelineModule,
    OKRsRoutingModule,
    SearchModule,
    DrawerModule,
    SessionCardModule,
    NotificationModule,
    ConfirmationModalModule,
    PdfExportModalComponent
  ],
  providers: [
    PdfExportService
  ]
})
export class OKRsModule { }