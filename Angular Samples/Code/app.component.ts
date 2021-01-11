import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import './_content/app.less';
import { LoginVM } from './_gen/swagger.gen';

@Component({ selector: 'app', templateUrl: 'app.component.html',styleUrls:['app.component.css'] })
export class AppComponent implements OnInit {
    currentUser: LoginVM;

    constructor(private router: Router) { }
    
    ngOnInit(): void {
        //this.router.navigate(['/']);
    }

}