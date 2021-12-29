import { Component } from '@angular/core';
import { PostService } from '@api';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'b-post',
  templateUrl: './post.component.html',
  styleUrls: ['./post.component.scss']
})
export class PostComponent {

  readonly vm$ = of({ })
  .pipe(
    map(model => ({ model }))
  );

  constructor(
    private readonly _postService: PostService
  ) {

  }
}
