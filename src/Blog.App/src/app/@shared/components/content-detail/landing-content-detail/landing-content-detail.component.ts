import { Component, EventEmitter, Input, NgModule, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';
import { ckEditorConfig } from '@core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Content } from '@api';
import { CKEditorModule } from 'ckeditor4-angular';
import { MatIconModule } from '@angular/material/icon';
import { ChooseAFileOrDragItHereModule } from '@shared/components/choose-a-file-or-drag-it-here';

@Component({
  selector: 'b-landing-content-detail',
  templateUrl: './landing-content-detail.component.html',
  styleUrls: ['./landing-content-detail.component.scss']
})
export class LandingContentDetailComponent {

  ckEditorConfig: typeof ckEditorConfig = ckEditorConfig;

  readonly form: FormGroup = new FormGroup({
    contentId: new FormControl(null, []),
    name: new FormControl(null,[Validators.required]),
    json: new FormGroup({
      title: new FormControl(null,[]),
      subTitle: new FormControl(null,[]),
      digitalAsset: new FormControl(null,[])
    })
  });

  get content(): Content { return this.form.value as Content; }

  @Input("content") set content(value: Content) {
    if(!value?.contentId) {
      this.form.reset({
        name: null,
        json: {

        }
      })
    } else {
      this.form.patchValue(value);
    }
  }

  @Output() save: EventEmitter<Content> = new EventEmitter();

  @Output() backButtonClick: EventEmitter<void> = new EventEmitter();
}

@NgModule({
  declarations: [
    LandingContentDetailComponent
  ],
  exports: [
    LandingContentDetailComponent
  ],
  imports: [
    ChooseAFileOrDragItHereModule,
    CommonModule,
    ReactiveFormsModule,
    CKEditorModule,
    MatIconModule
  ]
})
export class LandingContentDetailModule { }
