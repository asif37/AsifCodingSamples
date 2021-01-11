import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ToastrModule } from 'ngx-toastr';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { MsalModule, MsalInterceptor, MsalService } from '@azure/msal-angular';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';

import { appRoutingModule } from './app.routing';
import { ErrorInterceptor, JwtInterceptor } from './_helpers';
import { AppComponent } from './app.component';
import { LoginComponent } from './login';
import { AlertComponent } from './_components';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AppHeaderComponent } from './header/header.component';
import { AppSidebarComponent } from './sidebar/sidebar.component';
import { CoursesComponent } from './courses/courses.component';
import { CategoriesComponent} from './categories/categories.component';
import { UsersComponent} from './users/users.component';
import { ResetPasswordComponent } from './resetpassword/resetpassword.component';
import { ForgotPasswordComponent } from './forgotpassword/forgotpassword.component';
import { CreateCourseComponent } from './courses/createcourse/createcourse.component';
// import { SettingsComponent} from './settings/settings.component.ts.bak';
import { MarketPlaceComponent} from './marketplace/marketplace.component';
import { LearningplansComponent } from './learningplans/learningplans.component';
import { API_BASE_URL } from './_gen/swagger.gen';
import { environment } from 'src/environments/environment';
import { LessonsComponent } from './lessons/lessons.component';
import { TopicsComponent } from './topics/topics.component';
import { TopicDetails } from './topicdetails/topicdetails.component';
import { QuizComponent } from './quiz/quiz.component';
import { PlayerComponent } from './player/player.component';
import { TestComponent } from './test/test.component';
import { CoursesGrid } from './courses/courses-grid.component';
import { EmployeesComponent } from './employees/employees.component';
import { EmployeesGrid } from './employees/employees-grid.component';
import { CourseLearningPlanComponent } from './learningplans/courseLearningPlan.component';
import { LearningPlanGrid } from './learningplans/learningplan-grid.component';
import { LessonsGrid } from './lessons/lessons-grid.component';
import { TopicsGrid } from './topics/topics-grid.component';
import { CourseDetailedView } from './player/coursedetailedview.component';
import { ClassRoomComponent } from './player/classroom.component';
import { QuizPlayerComponent } from './player/quizplayer.component';

const isIE = window.navigator.userAgent.indexOf("MSIE ") > -1 || window.navigator.userAgent.indexOf("Trident/") > -1;
let api = environment.production ? 'https://8learning.azurewebsites.net/api' : 'http://localhost:58077/api';
const api_access = 'api://2d921948-80f7-47e4-b7a1-17116ef7fc09/api-access';

const msalModule = MsalModule.forRoot({
    auth: {
      clientId: 'bdc68698-7a93-4555-a3b0-1e550355d265',
      authority: "https://login.microsoftonline.com/organizations",
      redirectUri: environment.redirect
    },
    cache: {
      cacheLocation: "sessionStorage",
      storeAuthStateInCookie: isIE, // set to true for IE 11
    },
  },
  {
    popUp: false,
    consentScopes: [ "user.read", api_access ],
    protectedResourceMap: [
      ['https://graph.microsoft.com/v1.0/me', ['user.read']],
      [`${api}/Auth/login`, [api_access]],
      [`${api}/Auth/getLoggedInUsername`, [api_access]],
      [`${api}/Auth/getall`, [api_access]],
      [`${api}/Auth/deactivate`, [api_access]],
      [`${api}/Auth/delete`, [api_access]],
      [`${api}/Auth/picture`, [api_access]],
      [`${api}/Auth/save`, [api_access]],
      [`${api}/Auth/createNew`, [api_access]],
      [`${api}/Course/getbylearningplan`, [api_access]],
      [`${api}/Course/createNew`, [api_access]],
      [`${api}/Course/getById`, [api_access]],
      [`${api}/Course/save`, [api_access]],
      [`${api}/Course/delete`, [api_access]],
      [`${api}/Course/getall`, [api_access]],
      [`${api}/Course/getalllight`, [api_access]],
      [`${api}/Course/picture`, [api_access]],
      [`${api}/Employee/getall`, [api_access]],
      [`${api}/Employee/getalllight`, [api_access]],
      [`${api}/Employee/save`, [api_access]],
      [`${api}/Employee/delete`, [api_access]],
      [`${api}/Employee/picture`, [api_access]],
      [`${api}/Employee/createNew`, [api_access]],
      // [`${api}/Employee/course_blob`, [api_access]],
      // [`${api}/Employee/course`, [api_access]],
      // [`${api}/Employee/getemployeecoursemagiclink`, [api_access]],
      // [`${api}/Employee/postquizanswers`, [api_access]],
      [`${api}/LearningPlan/all`, [api_access]],
      [`${api}/LearningPlan/createNew`, [api_access]],
      [`${api}/LearningPlan/getById`, [api_access]],
      [`${api}/LearningPlan/save`, [api_access]],
      [`${api}/LearningPlan/delete`, [api_access]],
      [`${api}/LearningPlan/getemployees`, [api_access]],
      [`${api}/LearningPlan/assigntoemployees`, [api_access]],
      [`${api}/LearningPlan/getall`, [api_access]],
      [`${api}/LearningPlan/courseLearningPlan`, [api_access]],
      [`${api}/LearningPlan/courseLearningPlanSave`, [api_access]],
      [`${api}/Lesson/getLessons`, [api_access]],
      [`${api}/Lesson/createNew`, [api_access]],
      [`${api}/Lesson/getById`, [api_access]],
      [`${api}/Lesson/save`, [api_access]],
      [`${api}/Lesson/delete`, [api_access]],
      [`${api}/Test/SeedDatabase`, [api_access]],
      [`${api}/Test/handshake`, [api_access]],
      [`${api}/Topic/getTopics`, [api_access]],
      [`${api}/Topic/createNew`, [api_access]],
      [`${api}/Topic/save`, [api_access]],
      [`${api}/Topic/getById`, [api_access]],
      [`${api}/Topic/getBlobById`, [api_access]],
      [`${api}/Topic/getBlob`, [api_access]],
      [`${api}/Topic/deleteTopic`, [api_access]],
      [`${api}/Topic/uploadBlob`, [api_access]],
      [`${api}/Topic/getQuizQuestions`, [api_access]],
      [`${api}/Topic/addQuizQuestion`, [api_access]],
      [`${api}/Topic/saveQuizQuestion`, [api_access]],
      [`${api}/Topic/deleteQuizQuestion`, [api_access]],
    ],
    unprotectedResources:["https://www.microsoft.com/en-us/"],
  });

@NgModule({
    imports: [
        BrowserModule,
        ReactiveFormsModule,
        BrowserAnimationsModule,
        HttpClientModule,
        FormsModule,
        ToastrModule.forRoot(),
        appRoutingModule,
        msalModule,
    ],
    declarations: [
        AppComponent,
        LoginComponent,
        DashboardComponent,
        AlertComponent,
        AppHeaderComponent,
        AppSidebarComponent,
        UsersComponent,
        CategoriesComponent,
        CoursesComponent,
        // SettingsComponent,
        MarketPlaceComponent,
        LearningplansComponent,
        LearningPlanGrid,
        CourseLearningPlanComponent,
        LessonsComponent,
        LessonsGrid,
        TopicsComponent,
        TopicsGrid,
        TopicDetails,
        QuizComponent,
        PlayerComponent,
        ClassRoomComponent,
        TestComponent,
        ResetPasswordComponent,
        ForgotPasswordComponent,
        CreateCourseComponent,
        CoursesGrid,
        EmployeesComponent,
        EmployeesGrid,
        CourseDetailedView,
        QuizPlayerComponent,
    ],
    providers: [
        // { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
        { provide: API_BASE_URL, useValue: environment.API_URL }
    ],
    bootstrap: [AppComponent]
})
export class AppModule { 
  // constructor(private msalService: MsalService) {
  //   this.msalService.handleRedirectCallback(_ => {})
  // }
}
