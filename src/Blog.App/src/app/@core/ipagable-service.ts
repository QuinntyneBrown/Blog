import { Observable } from "rxjs";
import { Page } from "./page";

export interface IPagableService<T> {
    getPage(options: { pageIndex: number, pageSize: number }): Observable<Page<T>>;
    uniqueIdentifierName: string;
}
