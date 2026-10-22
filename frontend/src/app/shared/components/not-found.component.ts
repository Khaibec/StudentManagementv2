import { Component } from '@angular/core';

@Component({
  selector: 'app-not-found',
  template: `
    <div class="container" style="text-align: center; padding: 60px 20px;">
      <div style="font-size: 56px; margin-bottom: 16px;">🔍</div>
      <h1 style="font-size: 24px; font-weight: 700; color: var(--text-primary); margin-bottom: 8px;">
        404 - Không tìm thấy trang
      </h1>
      <p style="color: var(--text-secondary); max-width: 480px; margin: 0 auto 24px auto;">
        Đường dẫn bạn yêu cầu không tồn tại hoặc đã bị xóa khỏi hệ thống.
      </p>
      <a routerLink="/courses" class="btn btn-primary">
        Về trang chủ LMS
      </a>
    </div>
  `
})
export class NotFoundComponent {}
