import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ContentDetailModule, ContentListModule, ListDetailModule } from '@shared';
import { ContentsRoutingModule } from './contents-routing.module';
import { ContentsComponent } from './contents.component';



@NgModule({
  declarations: [
    ContentsComponent
  ],
  imports: [
    CommonModule,
    ContentsRoutingModule,
    ContentListModule,
    ContentDetailModule,
    ListDetailModule
  ]
})
export class ContentsModule { }
