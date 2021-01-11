import { Component, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { LearningPlanClient, LearningPlanVM } from '../_gen/swagger.gen';
import { AlertService } from '../_services';
import { BaseComponent } from '../_services/base.component';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({ templateUrl: './learningplans.component.html' })
export class LearningplansComponent 
  extends BaseComponent<LearningPlanVM, LearningPlanClient> 
  implements OnInit {

  constructor(
    formBuilder: FormBuilder,
    router: Router,
    alertService: AlertService,
    toastr: ToastrService,
    private learningPlanClient: LearningPlanClient) { 
      super(alertService, router, formBuilder, learningPlanClient, toastr)
    }

  ngOnInit() {
    this.bindLearningPlans();
  }

  bindLearningPlans() {
    this.learningPlanClient.getall().subscribe(data => this.modelCollection = data, error => this.errorHandler(error));
    this.closeEditForm();
  }

  searched(value) {
    this.search = value.trim().toLowerCase();
  }

  openAddLearningPlanForm() {
    this.router.navigate(["learningplans", 0]);
  }

  deleteLearningPlan(learningPlan: LearningPlanVM) {
    if(confirm(`Are you sure you want to remove ${learningPlan.employee} from course ${learningPlan.course}`)) {
      this.client.delete(learningPlan.id)
        .subscribe(data => {
          this.info("Removed learning plan");
          this.bindLearningPlans();
        }, error => this.errorHandler(error));
    }
  }

}
