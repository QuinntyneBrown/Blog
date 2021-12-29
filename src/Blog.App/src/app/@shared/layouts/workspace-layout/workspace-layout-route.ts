import { Route, Routes } from '@angular/router';
import { AuthGuard } from '@core';
import { WorkspaceLayoutComponent } from './workspace-layout.component';


export class WorkspaceLayoutRoute {
  static withLayout(routes: Routes): Route {
    return {
      path: '',
      component: WorkspaceLayoutComponent,
      children: routes,
      canActivate: [AuthGuard]
    };
  }
};
