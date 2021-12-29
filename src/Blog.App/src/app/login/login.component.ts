import { Component } from '@angular/core';
import { AuthService, Destroyable, NavigationService } from '@core';
import { takeUntil, tap } from 'rxjs/operators';


@Component({
  selector: 'b-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent extends Destroyable {
  constructor(
    private readonly _authService: AuthService,
    private readonly _navigationService: NavigationService
  ) {
    super();
  }

  handleLoginAttempt(credentials: { username: string, password: string }) {
    this._authService.tryToLogin(credentials)
    .pipe(
      takeUntil(this._destroyed$),
      tap(_ => this._navigationService.redirectPreLogin())
    )
    .subscribe();
  }
}
