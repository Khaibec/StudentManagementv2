import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { MainLayoutComponent } from './shared/layout/main-layout.component';
import { CourseListComponent } from './features/courses/course-list/course-list.component';
import { CourseDetailComponent } from './features/courses/course-detail/course-detail.component';
import { CourseLearningComponent } from './features/learning/course-learning/course-learning.component';
import { AssignmentDetailComponent } from './features/assignments/assignment-detail/assignment-detail.component';
import { AssignmentGradingComponent } from './features/assignments/assignment-grading/assignment-grading.component';
import { ForbiddenComponent } from './shared/components/forbidden.component';
import { NotFoundComponent } from './shared/components/not-found.component';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        redirectTo: 'courses',
        pathMatch: 'full'
      },
      {
        path: 'courses',
        component: CourseListComponent
      },
      {
        path: 'courses/:id',
        component: CourseDetailComponent
      },
      {
        path: 'courses/:id/learn',
        component: CourseLearningComponent
      },
      {
        path: 'courses/:courseId/assignments/:id',
        component: AssignmentDetailComponent
      },
      {
        path: 'courses/:courseId/assignments/:id/grading',
        component: AssignmentGradingComponent
      },
      {
        path: 'forbidden',
        component: ForbiddenComponent
      }
    ]
  },
  {
    path: '**',
    component: NotFoundComponent
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
