import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ChatComponent } from './chat.component';
import { ChatViewComponent } from './chat-view/chat-view.component';
import { AgentsViewComponent } from './agents-view/agents-view.component';
import { PromptsViewComponent } from './prompts-view/prompts-view.component';
import { AutomationsViewComponent } from './automations-view/automations-view.component';
import { LoadingOverlayModule } from '../../shared/loading-overlay/loading-overlay.module';
import { DataTableModule } from './data-table/data-table.module';
import { SuccessMessageModule } from './success-message/success-message.module';
import { ConfirmationModalModule } from '../../shared/confirmation-modal/confirmation-modal.module';
import { ChatRoutingModule } from './chat-routing.module';
import { ModalComponent } from '../../shared/modal/modal.component';

@NgModule({
  declarations: [
    ChatComponent,
    ChatViewComponent,
    AgentsViewComponent,
    PromptsViewComponent,
    AutomationsViewComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ChatRoutingModule,
    LoadingOverlayModule,
    DataTableModule,
    SuccessMessageModule,
    ConfirmationModalModule,
    ModalComponent
  ],
  exports: [
    ChatComponent
  ]
})
export class ChatModule { }