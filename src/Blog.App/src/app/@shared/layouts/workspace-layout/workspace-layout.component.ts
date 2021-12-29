import { CommonModule } from '@angular/common';
import { Component, NgModule, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService, NavigationService } from '@core';
import { LeftRailModule } from '@shared';

@Component({
  selector: 'b-workspace-layout',
  templateUrl: './workspace-layout.component.html',
  styleUrls: ['./workspace-layout.component.scss']
})
export class WorkspaceLayoutComponent {

  constructor(
    private readonly _authService: AuthService,
    private readonly _navigationService: NavigationService
  ) {

  }

  handleLogout() {
    this._authService.logout();
    this._navigationService.redirectToPublicDefault();
  }

}

@NgModule({
  declarations: [
    WorkspaceLayoutComponent
  ],
  exports: [
    WorkspaceLayoutComponent
  ],
  imports: [
    CommonModule,
    RouterModule,
    LeftRailModule
  ]
})
export class WorkspaceLayoutModule { }
