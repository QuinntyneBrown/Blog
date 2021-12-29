import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Post, PostService } from '@api';
import { map } from 'rxjs/operators';

@Component({
  selector: 'b-landing',
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent {

  readonly vm$ = this._postService
  .getPage({ pageIndex: 0, pageSize: 1 })
  .pipe(
    map(page => ({ posts: page.entities }))
  );

  constructor(
    private readonly _postService: PostService,
    private readonly _router: Router
  ) {

  }

  handleClick(post:Post) {
    this._router.navigate(["/","posts",post.slug])
  }
}
