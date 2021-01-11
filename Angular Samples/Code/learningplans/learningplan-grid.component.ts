import { Component, OnInit, Input, Output, EventEmitter } from "@angular/core";
import FilteredPagedCollection from '../_services/FilteredPagedCollection';
import { LearningPlanVM } from '../_gen/swagger.gen';
import { environment } from 'src/environments/environment';

@Component({
    selector: "learningplan-grid",
    templateUrl: "./learningplan-grid.component.html",
    styles: [
        `.grid_info_button {
            border: 1px solid #E5E9EC;
            background-color: #fff;
            border-radius: 4px;
            padding: 10px;
            color: #748494;
            text-transform: uppercase;
        }`
    ]
})
export class LearningPlanGrid
    extends FilteredPagedCollection<LearningPlanVM>
    implements OnInit {

    @Output() deleteLearningPlan = new EventEmitter<LearningPlanVM>();

    @Input() set collection(value: LearningPlanVM[]) {
        this._collection = value;
        this.refreshCollections();
    }

    @Input() set pageSize(value: number) {
        this._pageSize = value;
    }

    @Input() set search(value: string) {
        this._search = value;
        this.refreshCollections();
    }

    @Input() set currentPage(value: number) {
        this._currentPage = value;
        this.refreshPaged()
    }

    image(empId) {
        return `${environment.API_URL}/api/Employee/picture/${empId}.jpg`;
    }

    deleteClicked(model) {
        this.deleteLearningPlan.emit(model);
    }

    ngOnInit(): void {
        this.filterFunction = model =>
            model.employee.toLowerCase().startsWith(this._search.trim().toLowerCase()) ||
            model.course.toLowerCase().startsWith(this._search.trim().toLowerCase());
        this.refreshCollections();
    }
}