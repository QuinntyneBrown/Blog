import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BASE_URL, HeadersInterceptor, JwtInterceptor } from '@core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { LayoutModule, WorkspaceLayoutModule } from '@shared';


@NgModule({
  declarations: [
    AppComponent,
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    LayoutModule,
    WorkspaceLayoutModule
  ],
  providers: [
    {
      provide: BASE_URL,
      useValue: 'https://localhost:5001/'
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HeadersInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: JwtInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
