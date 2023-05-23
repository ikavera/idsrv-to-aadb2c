import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { User } from 'src/app/models/user';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-users-list',
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss']
})
export class UsersListComponent implements OnInit {
  constructor(private userService: UserService) { }

  public usersTableData = new MatTableDataSource<User>();
  public displayedColumns: string[] = ['firstName', 'lastName', 'email'];

  @ViewChild(MatSort, { static: true })
  sort!: MatSort;

  ngOnInit(): void {
    this.userService.getUsersList()
      .subscribe(x => {
        this.usersTableData.data = x;
      });
  }
}
