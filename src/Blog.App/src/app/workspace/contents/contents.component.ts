import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Content, ContentService } from '@api';
import { Destroyable } from '@core';
import { BehaviorSubject, combineLatest, of } from 'rxjs';
import { map, switchMap, takeUntil, tap } from 'rxjs/operators';


@Component({
  selector: 'b-contents',
  templateUrl: './contents.component.html',
  styleUrls: ['./contents.component.scss']
})
export class ContentsComponent extends Destroyable {

  private readonly _refreshSubject: BehaviorSubject<null> = new BehaviorSubject(null);

  readonly vm$ = this._refreshSubject
  .pipe(
    switchMap(_ => combineLatest([
      this._contentService.get(),
      this._activatedRoute
      .paramMap
      .pipe(
        map(x => x.get("contentId")),
        switchMap(contentId => contentId ? this._contentService.getById({ contentId }) : of({ }))
        )
    ])),
    map(([contents, selected]) => {
      if((selected as any).contentId == undefined) {
        this._router.navigate(["/","workspace","contents","edit", contents[0].contentId]);
      }

      return { contents, selected };
    })
  );

  constructor(
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _router: Router,
    private readonly _contentService: ContentService
  ) {
    super();
  }

  public handleSelect(content: Content) {
    if(content.contentId) {
      this._router.navigate(["/","workspace","contents","edit", content.contentId]);
    } else {
      this._router.navigate(["/","workspace","contents","create"]);
    }
  }

  public handleSave(content: Content) {
    const obs$  = content.contentId ? this._contentService.update({ content }) : this._contentService.create({ content });
    obs$
    .pipe(
      takeUntil(this._destroyed$),
      tap(_ => {
        this._refreshSubject.next(null);
        this._router.navigate(["/","workspace","contents"]);
      }))
    .subscribe();
  }
}
