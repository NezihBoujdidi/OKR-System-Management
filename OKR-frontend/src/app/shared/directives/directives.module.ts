import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClickOutsideDirective } from './click-outside.directive';
import { NgVarDirective } from './ng-var.directive';

@NgModule({
  declarations: [
    ClickOutsideDirective,
    NgVarDirective
  ],
  imports: [
    CommonModule
  ],
  exports: [
    ClickOutsideDirective,
    NgVarDirective
  ]
})
export class DirectivesModule { } 