import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { EntityCreationInfo } from '../../../models/chat.models.interface';

@Component({
  selector: 'app-success-message',
  templateUrl: './success-message.component.html',
  styleUrls: ['./success-message.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SuccessMessageComponent implements OnInit {
  @Input() entityInfo!: EntityCreationInfo;
  
  constructor() { }

  ngOnInit(): void {
    // Set default operation type if not specified (for backward compatibility)
    if (!this.entityInfo.operation) {
      this.entityInfo.operation = 'create';
    }
  }

  // Getter for operation type to ensure TypeScript type safety
  get operationType(): 'create' | 'update' | 'delete' {
    return this.entityInfo.operation as 'create' | 'update' | 'delete';
  }

  getIconClass(): string {
    switch (this.entityInfo.entityType.toLowerCase()) {
      case 'okr-session':
        return 'fas fa-calendar-alt';
      case 'objective':
        return 'fas fa-bullseye';
      case 'keyresult':
        return 'fas fa-key';
      case 'task':
        return 'fas fa-tasks';
      case 'team':
        return 'fas fa-users';
      default:
        return 'fas fa-check-circle';
    }
  }

  getSuccessMessage(): string {
    const isUpdate = this.entityInfo.operation === 'update';
    const isDelete = this.entityInfo.operation === 'delete';
    
    if (this.entityInfo.entityType === 'team') {
      if (isDelete) {
        return `Team "${this.entityInfo.title}" has been deleted.`;
      } else if (isUpdate) {
        if (this.entityInfo.description) {
          return `Team "${this.entityInfo.title}" has been updated with a new description.`;
        } else {
          return `Team "${this.entityInfo.title}" has been updated.`;
        }
      } else {
        if (this.entityInfo.description) {
          return `Team "${this.entityInfo.title}" has been created with the description: "${this.entityInfo.description}".`;
        } else {
          return `Team "${this.entityInfo.title}" has been created.`;
        }
      }
    }

    // Default message format for other entity types
    const entityTypeDisplay = this.getEntityTypeLabel();
    if (isDelete) {
      return `${entityTypeDisplay} "${this.entityInfo.title}" has been deleted.`;
    } else if (isUpdate) {
      return `${entityTypeDisplay} "${this.entityInfo.title}" has been updated.`;
    } else {
      return `${entityTypeDisplay} "${this.entityInfo.title}" has been created.`;
    }
  }

  getEntityTypeLabel(): string {
    switch (this.entityInfo.entityType.toLowerCase()) {
      case 'okr-session':
        return 'OKR session';
      case 'objective':
        return 'objective';
      case 'keyresult':
        return 'key result';
      case 'task':
        return 'task';
      case 'team':
        return 'team';
      default:
        return 'item';
    }
  }

  formatDateRange(startDate: string, endDate: string): string {
    if (!startDate || !endDate) return '';
    
    const start = new Date(startDate);
    const end = new Date(endDate);
    
    return `${start.toLocaleDateString()} - ${end.toLocaleDateString()}`;
  }
  
  getActionColor(): string {
    if (this.entityInfo.operation === 'delete') {
      return '#EF4444'; // Red color for deletion
    }
    return '#FFD700'; // Yellow accent color for both creation and update
  }
  
  getSuccessIcon(): string {
    if (this.entityInfo.operation === 'delete') {
      return 'fas fa-trash-alt';
    }
    return 'fas fa-check-circle';
  }
}
