import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const expectedRoles = route.data['roles'] as UserRole[];
    if (!expectedRoles || expectedRoles.length === 0) {
      return true;
    }

    if (this.authService.hasRole(expectedRoles)) {
      return true;
    }

    // Role unauthorized: redirect to main courses view
    this.router.navigate(['/courses']);
    return false;
  }
}
