import { AfterContentChecked, AfterViewChecked, AfterViewInit, ChangeDetectionStrategy, Component, EventEmitter, Input, NgModule, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { Post } from '@api';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { pageSizeOptions } from '@core';
import { of } from 'rxjs';

@Component({
  selector: 'b-post-list',
  templateUrl: './post-list.component.html',
  styleUrls: ['./post-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PostListComponent implements AfterViewInit {

  private _dataSource: MatTableDataSource<Post>;

  readonly pageSizeOptions: typeof pageSizeOptions = pageSizeOptions;

  readonly displayedColumns: string[] = ["title"];

  @ViewChild(MatPaginator, { static: false }) private _paginator: MatPaginator;

  @Input("posts") set posts(value: Post[]) {
    this._dataSource = new MatTableDataSource(value);
  }

  get dataSource() { return this._dataSource; }

  @Output() select: EventEmitter<Post> = new EventEmitter();

  ngAfterViewInit(): void {
    this.dataSource.paginator = this._paginator;
  }
}

@NgModule({
  declarations: [
    PostListComponent
  ],
  exports: [
    PostListComponent
  ],
  imports: [
    CommonModule,
    MatTableModule,
    MatIconModule,
    MatPaginatorModule
  ]
})
export class PostListModule { }
