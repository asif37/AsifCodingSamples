import { Component, OnInit } from "@angular/core";
import { BaseComponent } from "../_services/base.component";
import { Employee, EmployeeClient } from "../_gen/swagger.gen";
import { FormBuilder, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { AlertService } from "../_services";
import { HttpClient } from "@angular/common/http";
import { environment } from "src/environments/environment";
import { ToastrService } from 'ngx-toastr';

declare let $: any;

@Component({
  templateUrl: "./employees.component.html",
})
export class EmployeesComponent extends BaseComponent<Employee, EmployeeClient>
  implements OnInit {
  constructor(
    formBuilder: FormBuilder,
    router: Router,
    alertService: AlertService,
    toastr: ToastrService,
    private http: HttpClient,
    private employeeClient: EmployeeClient
  ) {
    super(alertService, router, formBuilder, employeeClient, toastr);
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      name: ["", Validators.required],
      email: ["", Validators.email],
      description: [""],
      employeeNumber: [""],
    });
    this.bindEmployees();
  }

  searched(value) {
    this.search = value.trim().toLowerCase();
  }

  bindEmployees() {
    this.employeeClient.getall().subscribe(
      (data) => (this.modelCollection = data),
      (error) => this.errorHandler(error)
    );
    this.closeEditForm();
    this.ajax_inprogress = false;
  }

  file: File
  fileSelected(target): void {
    if(target.files.length > 0) {
      this.file = target.files[0];
      $(target).prev().text(target.files[0].name);
    }
  }

  onSubmit() {
    if (this.form.invalid) {
      this.errorHandler("Invalid form input");
      return false;
    }

    this.ajax_inprogress = true;

    if (this.model == null) {
      const formData: FormData = this.getFormData();
      formData.set("id", "0");
      formData.set("File", this.file);
      this.http
        .post(`${environment.API_URL}/api/Employee/createNew`, formData)
        .subscribe(
          () => {
            this.bindEmployees();
            this.file = null;
            $("#exampleModal").modal("hide");
            this.info("Employee added successfully");
          },
          (error) => this.errorHandler(error)
        );
    } else {
      const formData: FormData = this.getFormData();
      formData.set("id", this.model.id.toString());
      formData.set("File", this.file);
      this.http
        .put(`${environment.API_URL}/api/Employee/save`, formData)
        .subscribe(
          () => {
            this.file = null;
            this.model = null;
            this.form.reset();
            this.bindEmployees();
            $("#exampleModal").modal("hide");
            this.info("Employee saved successfully");
          },
          (error) => this.errorHandler(error)
        );
    }
    return false;
  }

  editEmployee(employee: Employee) {
    this.model = employee;
    this.f.name.setValue(employee.name);
    this.f.email.setValue(employee.email);
    this.f.employeeNumber.setValue(employee.employeeNumber);
    this.f.description.setValue(employee.description);
    $("#exampleModal").modal();
  }

  deleteEmployee(employee: Employee) {
    if(confirm(`Are you sure you want to remove this employee?`)) {
      this.client.delete(employee.id)
        .subscribe(data => {
          this.info("Removed employee record");
          this.bindEmployees();
        }, error => this.errorHandler(error));
    }
  }

  add() {
    this.model = null;
    this.form.reset();
    $("#exampleModal").modal();
  }
}
