import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AboutContentDetailComponent } from './about-content-detail.component';

describe('AboutContentDetailComponent', () => {
  let component: AboutContentDetailComponent;
  let fixture: ComponentFixture<AboutContentDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AboutContentDetailComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AboutContentDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
