import { Component, Input, OnInit, Output, EventEmitter } from "@angular/core";
import FilteredPagedCollection from '../_services/FilteredPagedCollection';
import { Employee } from '../_gen/swagger.gen';
import { environment } from 'src/environments/environment';

@Component({
    selector: "employees-grid",
    templateUrl: "./employees-grid.component.html",
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
export class EmployeesGrid 
    extends FilteredPagedCollection<Employee>
    implements OnInit {

    @Output() editEmployee = new EventEmitter<Employee>();
    @Output() deleteEmployee = new EventEmitter<Employee>();

    @Input() set collection(value: Employee[]) {
        this._collection = value;
        this.refreshCollections();
    }

    @Input() set pageSize(value: number){
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

    image(id) {
        return `${environment.API_URL}/api/Employee/picture/${id}.jpg`;
    }

    ngOnInit(): void {
        this.filterFunction = employee => employee.name.toLowerCase().startsWith(this._search.trim().toLowerCase()) || 
            employee.employeeNumber.toLowerCase().indexOf(this._search.trim().toLowerCase()) >= 0;
        this.refreshCollections();
    }

    editEmployeeClicked(employee) {
        this.editEmployee.emit(employee);
    }

    deleteEmployeeClicked(employee) {
        this.deleteEmployee.emit(employee);
    }

}