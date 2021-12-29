import { Route, Routes } from '@angular/router';
import { LayoutComponent } from './layout.component';

export class DefaultLayoutRoute {
  static withLayout(routes: Routes): Route {
    return {
      path: '',
      component: LayoutComponent,
      children: routes
    };
  }
};
