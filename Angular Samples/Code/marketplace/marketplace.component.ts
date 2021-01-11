import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { first } from 'rxjs/operators';

import { AlertService, AuthenticationService } from '../_services';

@Component({ templateUrl: 'marketplace.component.html'})
export class MarketPlaceComponent implements OnInit {
    displayMode : number;
    constructor(
        private formBuilder: FormBuilder,
        private route: ActivatedRoute,
        private router: Router,
        private authenticationService: AuthenticationService,
        private alertService: AlertService
    ){
        
    }

    ngOnInit() {
        this.displayMode = 1;
    }
    onDisplayModeChange(mode: number): void {
        this.displayMode = mode;
    }
}