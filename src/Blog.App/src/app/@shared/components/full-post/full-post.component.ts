import { Component, NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'b-full-post',
  templateUrl: './full-post.component.html',
  styleUrls: ['./full-post.component.scss']
})
export class FullPostComponent {

  readonly vm$ = of({ })
  .pipe(
    map(model => ({ model }))
  );

  constructor(

  ) {

  }
}

@NgModule({
  declarations: [
    FullPostComponent
  ],
  exports: [
    FullPostComponent
  ],
  imports: [
    CommonModule,
  ]
})
export class FullPostModule { }
