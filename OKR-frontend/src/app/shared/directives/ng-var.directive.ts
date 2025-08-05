import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';

/**
 * NgVar directive - allows storing the result of an expression in a template variable
 * Usage: *ngVar="expression as variable"
 */
@Directive({
  selector: '[ngVar]'
})
export class NgVarDirective {
  private context: any = {};
  private hasView = false;

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef
  ) {}

  @Input()
  set ngVar(value: any) {
    this.context.$implicit = this.context.ngVar = value;
    
    if (!this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef, this.context);
      this.hasView = true;
    }
  }
} 