import { Component, EventEmitter, NgModule, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';


@Component({
  selector: 'b-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.scss']
})
export class LoginFormComponent {

  readonly form: FormGroup = new FormGroup({
    username: new FormControl('user',[Validators.required]),
    password: new FormControl('password',[Validators.required])
  });

  @Output() readonly tryToLogin: EventEmitter<{ username: string, password: string }> = new EventEmitter();
}

@NgModule({
  declarations: [
    LoginFormComponent
  ],
  exports: [
    LoginFormComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule
  ]
})
export class LoginFormModule { }
