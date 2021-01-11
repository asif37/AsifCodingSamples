import { Component, OnInit, } from '@angular/core';
import { AuthClient, AdminVM } from '../_gen/swagger.gen';
import { BaseComponent } from '../_services/base.component';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AlertService } from '../_services/alert.service';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';

declare let $: any;

@Component({
    templateUrl: 'users.component.html'
})
export class UsersComponent extends BaseComponent<AdminVM, AuthClient> implements OnInit {

    constructor(
        formBuilder: FormBuilder,
        router: Router,
        alertService: AlertService,
        toastr: ToastrService,
        private http: HttpClient,
        private authClient: AuthClient) {
        super(alertService, router, formBuilder, authClient, toastr)
    }

    ngOnInit() {
        this.form = this.formBuilder.group({
            userName: ['', Validators.required],
            lastName: [''],
            firstName: [''],
            email: [''],
            jobTitle: [''],
            aboutMe: [''],
            mobilePhone: [''],
            address: [''],
            city: [''],
            zipCode: [''],
            state: [''],
        })

        this.bindAdmins();
    }

    userNameCaption(userName: string) {
        if (userName !== undefined) {
            let inits: string[] = [];
            userName.split(" ").forEach(element => {
                inits.push(element[0].toUpperCase());
            });
            return inits.join("");
        }
        return "";
    }

    bindAdmins() {
        this.authClient.getall().subscribe(data => this.modelCollection = data, error => this.errorHandler(error));
        this.ajax_inprogress = false;
    }

    deactivate(user: AdminVM) {
        this.authClient.deactivate(user.id)
            .subscribe(() => {
                user.active = !user.active;
                this.info(`Admin user ${!user.active ? "de" : ""}activated`);
            }, error => this.errorHandler(error))
    }

    delete(user: AdminVM) {
        if (confirm("Are you sure you want to delete this Admin user?")) {
            this.authClient.delete(user.id)
                .subscribe((message: any) => {
                    if (!message) {
                        this.bindAdmins();
                    }
                    else {
                        this.errorHandler(message.o);
                    }
                }, error => this.errorHandler(error))
        }
    }

    image(id) {
        return `${environment.API_URL}/api/Auth/picture/${id}.jpg`;
    }

    addClicked() {
        this.model = null;
        this.form.reset();
        $("#exampleModal").modal();
    }

    edit(user: AdminVM) {
        this.model = user;
        this.f.userName.setValue(user.userName);
        this.f.lastName.setValue(user.lastName);
        this.f.firstName.setValue(user.firstName);
        this.f.email.setValue(user.email);
        this.f.jobTitle.setValue(user.jobTitle);
        this.f.aboutMe.setValue(user.aboutMe);
        this.f.mobilePhone.setValue(user.mobilePhone);
        this.f.address.setValue(user.address);
        this.f.city.setValue(user.city);
        this.f.zipCode.setValue(user.zipCode);
        this.f.state.setValue(user.state);
        $("#exampleModal").modal();
    }

    file: File
    fileSelected(target): void {
        if (target.files.length > 0) {
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
                .post(`${environment.API_URL}/api/Auth/createNew`, formData)
                .subscribe(
                    (data: any) => {
                        if (data && data.o) {
                            this.info(data.o);
                            this.ajax_inprogress = false;
                        }
                        else {
                            this.bindAdmins();
                            this.file = null;
                            $("#exampleModal").modal("hide");
                            this.info("User added successfully");
                        }
                    },
                    (error) => this.errorHandler(error)
                );
        } else {
            const formData: FormData = this.getFormData();
            formData.set("id", this.model.id.toString());
            formData.set("File", this.file);
            this.http
                .put(`${environment.API_URL}/api/Auth/save`, formData)
                .subscribe(
                    () => {
                        this.file = null;
                        this.model = null;
                        this.form.reset();
                        this.bindAdmins();
                        $("#exampleModal").modal("hide");
                        this.info("User saved successfully");
                    },
                    (error) => this.errorHandler(error)
                );
        }
        return false;
    }
}