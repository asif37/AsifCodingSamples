import { Component, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { CourseClient, CourseLearningPlan, LearningPlanClient } from '../_gen/swagger.gen';
import { FormBuilder, Validators } from '@angular/forms';
import { AlertService } from '../_services';
import { BaseComponent } from '../_services/base.component';
import * as _ from 'lodash';
import { ToastrService } from 'ngx-toastr';

declare let $: any;

@Component({
    templateUrl: "./courseLearningPlan.component.html"
})
export class CourseLearningPlanComponent 
    extends BaseComponent<CourseLearningPlan, LearningPlanClient> 
    implements OnInit {

    learningPlanId: number;

    selectedCourse: any = null;
    filteredCourseSelectedIndex = -1;

    courseSearch: string = "";
    employeeSearch: string = "";

    constructor(
        formBuilder: FormBuilder,
        router: Router,
        alertService: AlertService,
        toastr: ToastrService,
        private activatedRoute: ActivatedRoute,
        private learningPlanClient: LearningPlanClient, private http: HttpClient) 
    {
        super(alertService, router, formBuilder, learningPlanClient, toastr);
        this.model = { allEmployees: [] };
    }

    ngOnInit(): void {
        this.learningPlanId = +this.activatedRoute.snapshot.params["learningPlanId"];
        this.learningPlanClient.courseLearningPlan(this.learningPlanId).subscribe(data => {
            this.model = data;
            if(data.completionDate) {
                $("#datetimepicker2 input").val(data.completionDate);
            }
            if (this.model.learningPlanEmployees == null) {
                this.model.learningPlanEmployees = [];
            }

            if (this.learningPlanId > 0) {
                this.selectedCourse = this.model.courses.find(o => o.id == this.model.courseId);
                this.courseSearch = this.selectedCourse.name;
            }
        }, error => this.errorHandler(error));

        $(document).ready(function(){
            $('#datetimepicker2').datetimepicker({ format: "MM/DD/YYYY" });
        });

    }

    submit() {
        if (this.selectedCourse == null) {
            this.errorHandler("Error: No course selected");
        }

        this.ajax_inprogress = true;
        
        let payload = _.cloneDeep(this.model);
        delete payload.allEmployees;
        delete payload.courses;
        payload.courseId = this.selectedCourse.id;
        payload.completionDate = $("#datetimepicker2 input").val();
        console.log(payload);
        this.client.courseLearningPlanSave(payload).subscribe(() => {
            this.ajax_inprogress = false;
            this.info("Learning plan saved successfully", this.back);
            // this.back();
        }, error => this.errorHandler(error));
    }

    get courseSearched() {
        return this.model.courses.filter(o => o.name.toLowerCase().startsWith(this.courseSearch.toLowerCase()));
    }

    get employeeSearched() {
        return this.model.allEmployees.filter(o => o.name.toLowerCase().startsWith(this.employeeSearch.toLowerCase()));
    }

    get selectedEmployeesList() {
        return this.model.learningPlanEmployees;
    }

    get displayCourseDropdown() {
        if (this.courseSearch.length >= 1 && this.selectedCourse == null) {
            return true;
        }
    }

    get currentSelectedCourse() {
        return this.courseSearched[this.filteredCourseSelectedIndex];
    }

    image(empId) {
        return `${environment.API_URL}/api/Employee/picture/${empId}.jpg`;
    }

    courseSelected(o) {
        this.selectedCourse = o;
        this.courseSearch = o.name;
    }

    searchChanged() {
        this.selectedCourse = null;
        this.filteredCourseSelectedIndex = -1;
    }

    searchKey(key: KeyboardEvent) {
        if(key.keyCode == 38) { // Up
            if(this.filteredCourseSelectedIndex > 0) {
                this.filteredCourseSelectedIndex--;
            }
        }
        else if(key.keyCode == 40) { // Down
            if(this.filteredCourseSelectedIndex < this.courseSearched.length - 1) {
                this.filteredCourseSelectedIndex++;
            }
        }
        else if(key.keyCode == 27) { // Esc
            this.filteredCourseSelectedIndex = -1;
            this.courseSearch = "";
        }
        else if(key.keyCode == 13) { // Enter
            this.selectedCourse = this.currentSelectedCourse;
            this.courseSearch = this.selectedCourse.name;
            this.filteredCourseSelectedIndex = -1;
        }
    }

    employeeClicked(e) {
        if (e.selected) {
            e.selected = false;
        }
        else {
            e.selected = true;
        }
    }

    moveSelected() {
        let selected = this.employeeSearched.filter(o => o.selected);
        if(selected.length == 0) {
            return;
        }
        selected.forEach(element => {
            delete element.selected;
            this.model.learningPlanEmployees.push(element);
        });

        _.remove(this.model.allEmployees, o => selected.findIndex(k => k.id == o.id) >= 0);
    }

    moveAll() {
        this.employeeSearched.forEach(element => {
            this.model.learningPlanEmployees.push(element);
        });

        _.remove(this.model.allEmployees, o => this.employeeSearched.findIndex(k => k.id == o.id) >= 0);
    }

    unmoveSelected() {
        let selected = this.model.learningPlanEmployees.filter(o => o.selected);
        if(selected.length == 0) {
            return;
        }
        selected.forEach(element => {
            delete element.selected;
            this.model.allEmployees.push(element);
        });

        _.remove(this.model.learningPlanEmployees, o => selected.findIndex(k => k.id == o.id) >= 0);
    }

    unmoveAll() {
        this.model.learningPlanEmployees.forEach(element => {
            this.model.allEmployees.push(element);
        });
        this.model.learningPlanEmployees = [];
    }
}