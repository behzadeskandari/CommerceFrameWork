import { TestBed } from '@angular/core/testing';
import { PermissionService } from '@commerce/auth';
import { AuthService } from '@commerce/auth';
import { signal } from '@angular/core';

describe('PermissionService', () => {
  it('checks permissions from session', () => {
    TestBed.configureTestingModule({
      providers: [
        PermissionService,
        {
          provide: AuthService,
          useValue: {
            session: signal({
              isAuthenticated: true,
              identityUserId: '1',
              email: 'admin@example.com',
              customerId: null,
              roles: ['Administrator'],
              permissions: ['Catalog.Products.View', 'Catalog.Products.Create']
            })
          }
        }
      ]
    });

    const permissions = TestBed.inject(PermissionService);
    expect(permissions.hasPermission('Catalog.Products.View')).toBeTrue();
    expect(permissions.hasPermission('Catalog.Products.Delete')).toBeFalse();
  });
});
