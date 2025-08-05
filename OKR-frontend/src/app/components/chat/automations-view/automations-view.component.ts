import { Component, EventEmitter, Input, Output } from '@angular/core';

interface Automation {
  id: string;
  name: string;
  description: string;
  icon: string;
  enabled: boolean;
}

@Component({
  selector: 'app-automations-view',
  templateUrl: './automations-view.component.html',
  styleUrls: ['./automations-view.component.scss']
})
export class AutomationsViewComponent {
  @Input() automations: Automation[] = [];
  @Output() automationToggled = new EventEmitter<string>();
  @Output() backToChat = new EventEmitter<void>();

  toggleAutomation(automationId: string): void {
    this.automationToggled.emit(automationId);
  }
} 