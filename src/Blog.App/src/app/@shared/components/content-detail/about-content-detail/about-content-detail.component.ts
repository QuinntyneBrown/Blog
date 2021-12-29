import { Component, EventEmitter, Input, NgModule, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ckEditorConfig } from '@core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Content } from '@api';
import { CKEditorModule } from 'ckeditor4-angular';
import { MatIconModule } from '@angular/material/icon';


@Component({
  selector: 'b-about-content-detail',
  templateUrl: './about-content-detail.component.html',
  styleUrls: ['./about-content-detail.component.scss']
})
export class AboutContentDetailComponent {

  ckEditorConfig: typeof ckEditorConfig = ckEditorConfig;

  readonly form: FormGroup = new FormGroup({
    contentId: new FormControl(null, []),
    name: new FormControl(null,[Validators.required]),
    json: new FormGroup({
      body: new FormControl(null, [])
    })
  });

  @Input("content") set content(value: Content) {
    if(!value?.contentId) {
      this.form.reset({
        name: null,
        json: {
          body: null
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
    AboutContentDetailComponent
  ],
  exports: [
    AboutContentDetailComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CKEditorModule,
    MatIconModule
  ]
})
export class AboutContentDetailModule { }
