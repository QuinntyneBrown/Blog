import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PostDetailModule, PostListModule, ListDetailModule } from '@shared';
import { PostsRoutingModule } from './posts-routing.module';
import { PostsComponent } from './posts.component';



@NgModule({
  declarations: [
    PostsComponent
  ],
  imports: [
    CommonModule,
    PostsRoutingModule,
    PostListModule,
    PostDetailModule,
    ListDetailModule
  ]
})
export class PostsModule { }
