import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LocalStorageService } from './local-storage.service';
import { switchMap, tap } from 'rxjs/operators';
import { accessTokenKey, BASE_URL, usernameKey } from './constants';
import { User, UserService } from '@api';
import { Observable, ReplaySubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly _currentUserSubject: ReplaySubject<User> = new ReplaySubject(1);

  readonly currentUser$: Observable<User> = this._currentUserSubject.asObservable();

  constructor(
    @Inject(BASE_URL) private _baseUrl: string,
    private _httpClient: HttpClient,
    private _localStorageService: LocalStorageService,
    private _userService: UserService

  ) {}

  public logout() {
    this._localStorageService.put({ name: accessTokenKey, value: null});
    this._localStorageService.put({ name: usernameKey, value: null });
  }

  public tryToLogin(options: { username: string; password: string }) {
    return this._httpClient.post<any>(`${this._baseUrl}api/user/token`, options)
    .pipe(
      tap(response => {
        this._localStorageService.put({ name: accessTokenKey, value: response.accessToken });
        this._localStorageService.put({ name: usernameKey, value: options.username});
      }),
      switchMap(_ => this._userService.getCurrent()),
      tap(x => this._currentUserSubject.next(x))
    );
  }
}
