import { Component, ComponentFactoryResolver, ComponentRef, EventEmitter, Input, NgModule, OnInit, Output, TemplateRef, ViewChild, ViewContainerRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { Content } from '@api';
import { ReactiveFormsModule } from '@angular/forms';
import { ShellContentDetailComponent, ShellContentDetailModule } from './shell-content-detail';
import { LandingContentDetailComponent, LandingContentDetailModule } from './landing-content-detail';
import { AboutContentDetailComponent, AboutContentDetailModule } from './about-content-detail';
import { takeUntil, tap } from 'rxjs/operators';
import { Destroyable } from '@core';


@Component({
  selector: 'b-content-detail',
  templateUrl: './content-detail.component.html',
  styleUrls: ['./content-detail.component.scss']
})
export class ContentDetailComponent extends Destroyable {

  private _lookUp = {
    "shell": ShellContentDetailComponent,
    "landing": LandingContentDetailComponent,
    "about": AboutContentDetailComponent
  }
  @ViewChild(TemplateRef, { read: ViewContainerRef, static: true }) viewContainerRef: ViewContainerRef

  @Input("content") set content(content: Content) {
    this.viewContainerRef.clear();

    if(content && content.contentId) {
      const factory = this._componentFactoryResolver.resolveComponentFactory(this._lookUp[content.slug]);
      const component = this.viewContainerRef.createComponent(factory) as ComponentRef<any>;
      component.instance.content = content;

      component.instance.save
      .pipe(
        takeUntil(this._destroyed$),
        tap((content: Content) => this.save.emit(content))
      ).subscribe();

      component.instance.backButtonClick
      .pipe(
        takeUntil(this._destroyed$),
        tap(_ => this.backButtonClick.emit())
      ).subscribe();
    }
  }

  @Output() save: EventEmitter<Content> = new EventEmitter();

  @Output() backButtonClick: EventEmitter<void> = new EventEmitter();

  constructor(
    private readonly _componentFactoryResolver: ComponentFactoryResolver
  ) {
    super();
  }

}

@NgModule({
  declarations: [
    ContentDetailComponent
  ],
  exports: [
    ContentDetailComponent
  ],
  imports: [
    CommonModule,
    MatIconModule,
    ShellContentDetailModule,
    LandingContentDetailModule,
    AboutContentDetailModule,
    ReactiveFormsModule
  ]
})
export class ContentDetailModule { }
