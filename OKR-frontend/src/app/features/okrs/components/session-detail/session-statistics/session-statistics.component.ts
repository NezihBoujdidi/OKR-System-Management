import { Component, Input } from '@angular/core';
import { OKRSession } from '../../../../../models/okr-session.interface';

@Component({
  selector: 'app-session-statistics',
  templateUrl: './session-statistics.component.html'
})
export class SessionStatisticsComponent {
  @Input() session!: OKRSession;
} 