import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeetingsComponent } from './components/meetings.component';
import { MeetingRoomComponent } from './components/meeting-room/meeting-room.component';

const routes: Routes = [
  {
    path: '',
    component: MeetingsComponent
  },
  {
    path: 'room/:id',
    component: MeetingRoomComponent
  },
  {
    path: 'join/:id',
    component: MeetingRoomComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MeetingsRoutingModule { } 