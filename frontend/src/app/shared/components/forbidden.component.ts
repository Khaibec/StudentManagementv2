import { Component } from '@angular/core';

@Component({
  selector: 'app-forbidden',
  template: `
    <div class="container" style="text-align: center; padding: 60px 20px;">
      <div style="font-size: 56px; margin-bottom: 16px;">🚫</div>
      <h1 style="font-size: 24px; font-weight: 700; color: var(--danger); margin-bottom: 8px;">
        403 - Bạn không có quyền truy cập
      </h1>
      <p style="color: var(--text-secondary); max-width: 480px; margin: 0 auto 24px auto;">
        Trang hoặc tài nguyên bạn đang cố truy cập nằm ngoài phạm vi phân quyền của vai trò hiện tại.
      </p>
      <a routerLink="/courses" class="btn btn-primary">
        Quay lại Khóa học của tôi
      </a>
    </div>
  `
})
export class ForbiddenComponent {}
