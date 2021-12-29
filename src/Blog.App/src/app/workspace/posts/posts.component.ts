import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Post, PostService } from '@api';
import { Destroyable, NavigationService } from '@core';
import { BehaviorSubject, combineLatest, of } from 'rxjs';
import { map, switchMap, takeUntil, tap } from 'rxjs/operators';


@Component({
  selector: 'b-posts',
  templateUrl: './posts.component.html',
  styleUrls: ['./posts.component.scss']
})
export class PostsComponent extends Destroyable {

  private readonly _refreshSubject: BehaviorSubject<null> = new BehaviorSubject(null);

  readonly vm$ = this._refreshSubject
  .pipe(
    switchMap(_ => combineLatest([
      this._postService.get(),
      this._activatedRoute
      .paramMap
      .pipe(
        map(p => p.get("postId")),
        switchMap(postId => postId ? this._postService.getById({ postId }) : of({ }))
        )
    ])),
    map(([posts, selected]) => ({ posts, selected }))
  );

  constructor(
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _router: Router,
    private readonly _postService: PostService,
    private readonly _navigationService: NavigationService
  ) {
    super();
  }

  public handleSelect(post: Post) {
    if(post.postId) {
      this._router.navigate(["/","workspace","posts","edit", post.postId]);
    } else {
      this._router.navigate(["/","workspace","posts","create"]);
    }
  }

  public handleSave(post: Post) {
    const obs$  = post.postId ? this._postService.update({ post }) : this._postService.create({ post });
    obs$
    .pipe(
      takeUntil(this._destroyed$),
      tap(_ => {
        this._refreshSubject.next(null);
        this._router.navigate(["/","workspace","posts"]);
      }))
    .subscribe();
  }
}
