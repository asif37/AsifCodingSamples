import { Component, OnInit } from '@angular/core'
import { ActivatedRoute, Router } from '@angular/router';
import { TopicClient, Topic } from '../_gen/swagger.gen';
import { FormBuilder, FormGroup } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { BaseComponent } from '../_services/base.component';
import { AlertService, AuthenticationService } from '../_services';
import { ToastrService } from 'ngx-toastr';


@Component({ templateUrl: './topicdetails.component.html' })
export class TopicDetails extends BaseComponent<Topic, TopicClient> implements OnInit {
    
    types = {
        '1': 'Video',
        '2': 'Document',
        '3': 'Picture',
        '4': 'Quiz'
    }

    files: FileList
    uploadProgress: number
    uploadProgressMessage: string

    constructor(
        private activateRoute: ActivatedRoute,
        private topicClient: TopicClient,
        router: Router,
        private httpClient: HttpClient,
        alertService: AlertService,
        toastr: ToastrService,
        private sanitizer: DomSanitizer,
        private authenticationService: AuthenticationService,
        formBuilder: FormBuilder) {
            super(alertService, router, formBuilder, topicClient, toastr)
    }
    
    ngOnInit(): void {
        let topicId = +this.activateRoute.snapshot.params["topicId"]
        this.form = this.formBuilder.group({
            fileUpload: ['']
        })
        // this.topicClient.getBlobById(topicId).subscribe(data => this.model = data )
    }

    get url() {
        return this.sanitizer.bypassSecurityTrustResourceUrl(`${environment.API_URL}/api/Topic/getBlob/${this.model.id}/${encodeURI(this.authenticationService.accessToken)}`)
    }

    onSubmit() {

        if(this.files == null || this.files.length == 0) {
            return
        }

        let formData = new FormData()
        formData.set(this.files[0].name, this.files[0])
        let url = `${environment.API_URL}/api/Topic/uploadBlob/${this.model.id}`
        this.uploadProgressMessage = 'Uploading ...'
        this.httpClient.post<any>(url, formData, { reportProgress: true, observe: 'events' })
            .subscribe(event => {
                if(event.type === HttpEventType.UploadProgress){
                    this.uploadProgress = Math.round(100 * event.loaded / event.total)
                }
                else if (event.type === HttpEventType.Response) {
                    this.uploadProgressMessage = "Upload success."
                }
            })
        return false
    }

    handleFileChange(files: FileList) {
        this.uploadProgress = 0
        this.uploadProgressMessage = null
        this.files = files
    }

}