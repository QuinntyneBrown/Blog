import { Component, Input, NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';
import { Post } from '@api';

@Component({
  selector: 'b-post',
  templateUrl: './post.component.html',
  styleUrls: ['./post.component.scss']
})
export class PostComponent {

  @Input() post!:Post;

  constructor(

  ) {

  }
}

@NgModule({
  declarations: [
    PostComponent
  ],
  exports: [
    PostComponent
  ],
  imports: [
    CommonModule,
  ]
})
export class PostModule { }
