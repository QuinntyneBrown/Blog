import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LeftRailComponent } from './left-rail.component';

describe('LeftRailComponent', () => {
  let component: LeftRailComponent;
  let fixture: ComponentFixture<LeftRailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LeftRailComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LeftRailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
