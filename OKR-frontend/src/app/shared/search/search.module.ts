import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SearchComponent } from './components/search.component';

@NgModule({
  declarations: [SearchComponent],
  imports: [
    CommonModule,
    FormsModule
  ],
  exports: [SearchComponent]
})
export class SearchModule { } 