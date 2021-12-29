import { Component, EventEmitter, Input, NgModule, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { Post } from '@api';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CKEditorModule } from 'ckeditor4-angular';
import { ckEditorConfig } from '@core';
import { BreakpointObserver } from '@angular/cdk/layout';

@Component({
  selector: 'b-post-detail',
  templateUrl: './post-detail.component.html',
  styleUrls: ['./post-detail.component.scss']
})
export class PostDetailComponent {

  ckEditorConfig: typeof ckEditorConfig = ckEditorConfig;

  readonly form: FormGroup = new FormGroup({
    postId: new FormControl(null, []),
    title: new FormControl(null, [Validators.required]),
    body: new FormControl(null,[]),
    abstract: new FormControl(null,[])
  });

  public get post(): Post { return this.form.value as Post; }

  @Input("post") public set post(value: Post) {
    if(!value?.postId) {
      this.form.reset({
        name: null
      })
    } else {
      this.form.patchValue(value);
    }
  }

  @Output() save: EventEmitter<Post> = new EventEmitter();

  @Output() backButtonClick: EventEmitter<void> = new EventEmitter();

}

@NgModule({
  declarations: [
    PostDetailComponent
  ],
  exports: [
    PostDetailComponent
  ],
  imports: [
    CommonModule,
    MatIconModule,
    ReactiveFormsModule,
    CKEditorModule
  ]
})
export class PostDetailModule { }
