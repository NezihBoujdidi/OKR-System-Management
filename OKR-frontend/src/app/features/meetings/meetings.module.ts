import { NgModule } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { SharedModule } from '../../shared/shared.module';
import { MeetingsRoutingModule } from './meetings-routing.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MeetingsComponent } from './components/meetings.component';
import { MeetingNotificationComponent } from './components/meeting-notification/meeting-notification.component';
import { MeetingRoomComponent } from './components/meeting-room/meeting-room.component';
import { NotificationService } from './services/notification.service';
import { MeetingService } from './services/meeting.service';

@NgModule({
  declarations: [
    MeetingsComponent,
    MeetingNotificationComponent,
    MeetingRoomComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    FormsModule,
    ReactiveFormsModule,
    MeetingsRoutingModule
  ],
  providers: [
    DatePipe,
    NotificationService,
    MeetingService
  ],
  exports: [
    MeetingNotificationComponent
  ]
})
export class MeetingsModule { } 