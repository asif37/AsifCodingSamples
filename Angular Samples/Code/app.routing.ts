import { Routes, RouterModule } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';

import { LoginComponent } from './login';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { CategoriesComponent} from './categories/categories.component';
import { UsersComponent} from './users/users.component';
// import { SettingsComponent} from './settings/settings.component.ts.bak';
import { MarketPlaceComponent} from './marketplace/marketplace.component';
import { LearningplansComponent } from './learningplans/learningplans.component';
import { LessonsComponent } from './lessons/lessons.component';
import { TopicsComponent } from './topics/topics.component';
import { TopicDetails } from './topicdetails/topicdetails.component';
import { QuizComponent } from './quiz/quiz.component';
import { PlayerComponent } from './player/player.component';
import { TestComponent } from './test/test.component';
import { ResetPasswordComponent } from './resetpassword/resetpassword.component';
import { ForgotPasswordComponent } from './forgotpassword/forgotpassword.component';
import { CreateCourseComponent } from './courses/createcourse/createcourse.component';
import { EmployeesComponent } from './employees/employees.component';
import { CourseLearningPlanComponent } from './learningplans/courseLearningPlan.component';
import { environment } from 'src/environments/environment';
import { CourseDetailedView } from './player/coursedetailedview.component';
import { ClassRoomComponent } from './player/classroom.component';
import { QuizPlayerComponent } from './player/quizplayer.component';

const canActivate = (environment.production ? [MsalGuard] : [])

const routes: Routes = [
    { path: 'player/quiz/:id/:magiclink', component: QuizPlayerComponent },
    { path: 'player/:magiclink', component: ClassRoomComponent },
    { path: 'player/course/:magiclink', component: CourseDetailedView },
    { path: 'test', component: TestComponent },
    { path: 'login', component: LoginComponent },
    { path: 'resetpassword', component: ResetPasswordComponent },
    { path: 'forgotpassword', component: ForgotPasswordComponent },
    { path: '', component: CoursesComponent, canActivate },
    { path: 'createcourse', component: CreateCourseComponent, canActivate },
    { path: 'learningplan', component: LearningplansComponent, canActivate },
    { path: 'courses', component: CoursesComponent, canActivate },
    { path: 'categories', component: CategoriesComponent, canActivate },
    { path: 'users', component: UsersComponent, canActivate },
    { path: 'marketplace', component: MarketPlaceComponent, canActivate },
    // { path: 'settings', component: SettingsComponent, canActivate },
    { path: 'learningplans', component: LearningplansComponent, canActivate },
    { path: 'learningplans/:learningPlanId', component: CourseLearningPlanComponent, canActivate },
    { path: 'lessons/:courseId', component: LessonsComponent, canActivate },
    { path: 'topics/:lessonId', component: TopicsComponent, canActivate },
    { path: 'topicdetails/:topicId', component: TopicDetails, canActivate },
    { path: 'quiz/:topicId', component: QuizComponent, canActivate },
    { path: 'employees', component: EmployeesComponent, canActivate },

    // otherwise redirect to home
    { path: '**', redirectTo: 'courses' }
];

export const appRoutingModule = RouterModule.forRoot(routes);