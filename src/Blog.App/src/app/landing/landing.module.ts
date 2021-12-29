import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LandingRoutingModule } from './landing-routing.module';
import { LandingComponent } from './landing.component';
import { PostModule } from '@shared';


@NgModule({
  declarations: [
    LandingComponent
  ],
  imports: [
    CommonModule,
    PostModule,
    LandingRoutingModule
  ]
})
export class LandingModule { }
