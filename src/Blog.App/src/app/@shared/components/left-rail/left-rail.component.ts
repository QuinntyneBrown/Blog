import { Component, EventEmitter, NgModule, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'b-left-rail',
  templateUrl: './left-rail.component.html',
  styleUrls: ['./left-rail.component.scss']
})
export class LeftRailComponent {

  @Output() logoutClick: EventEmitter<void> = new EventEmitter();
}

@NgModule({
  declarations: [
    LeftRailComponent
  ],
  exports: [
    LeftRailComponent
  ],
  imports: [
    CommonModule,
    RouterModule
  ]
})
export class LeftRailModule { }
