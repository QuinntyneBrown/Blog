import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DefaultLayoutRoute, WorkspaceLayoutRoute } from '@shared';

const routes: Routes = [

  { path: '', pathMatch: 'full', redirectTo: 'landing' },

  DefaultLayoutRoute.withLayout([
    { path: 'landing', loadChildren: () => import('./landing/landing.module').then(m => m.LandingModule) },

    { path: 'post', loadChildren: () => import('./post/post.module').then(m => m.PostModule) }
  ]),

  WorkspaceLayoutRoute.withLayout([
    {
      path: 'workspace',
      loadChildren: () => import('./workspace/workspace.module').then(m => m.WorkspaceModule)
    }
  ]),

  { path: 'login', loadChildren: () => import('./login/login.module').then(m => m.LoginModule) },


];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
