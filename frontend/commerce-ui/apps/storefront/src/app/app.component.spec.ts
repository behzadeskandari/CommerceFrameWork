import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';

describe('Storefront AppComponent', () => {
  it('creates the root component', () => {
    TestBed.configureTestingModule({
      imports: [AppComponent]
    });
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
