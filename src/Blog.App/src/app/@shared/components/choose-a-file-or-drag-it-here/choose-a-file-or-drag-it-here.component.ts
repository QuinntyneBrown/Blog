import { AfterViewInit, Component, ElementRef, forwardRef, Input, NgModule, ViewChild } from '@angular/core';
import { NG_VALUE_ACCESSOR } from '@angular/forms';
import { switchMap, takeUntil, tap } from 'rxjs/operators';
import { BehaviorSubject, fromEvent, Subject } from 'rxjs';
import { BaseControlValueAccessor } from '@core';
import { DigitalAsset, DigitalAssetService } from '@api';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

const packageFiles = function (fileList: FileList): FormData {
  const formData = new FormData();
  for (var i = 0; i < fileList.length; i++) {
    formData.append(fileList[i].name, fileList[i]);
  }
  return formData;
}

@Component({
  selector: 'b-choose-a-file-or-drag-it-here',
  templateUrl: './choose-a-file-or-drag-it-here.component.html',
  styleUrls: ['./choose-a-file-or-drag-it-here.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ChooseAFileOrDragItHereComponent),
      multi: true
    }
  ]
})
export class ChooseAFileOrDragItHereComponent extends BaseControlValueAccessor implements AfterViewInit  {

  private readonly _digitalAssetSubject: BehaviorSubject<DigitalAsset | null> = new BehaviorSubject(null);

  readonly digitalAsset$ = this._digitalAssetSubject.asObservable();

  @ViewChild("input", { static: true }) fileInput: ElementRef<HTMLInputElement>;

  constructor(
    private readonly _digitalAssetService: DigitalAssetService,
    private readonly _elementRef: ElementRef<HTMLElement>
  ) {
    super()
  }

  writeValue(digitalAsset: DigitalAsset): void {
    this._digitalAssetSubject.next(digitalAsset);
  }

  ngAfterViewInit(): void {
    fromEvent(this._elementRef.nativeElement,"dragover")
    .pipe(
      tap((x: DragEvent) => this.onDragOver(x)),
      takeUntil(this._destroyed$)
    ).subscribe();

    fromEvent(this._elementRef.nativeElement,"drop")
    .pipe(
      tap((x: DragEvent) => this.onDrop(x)),
      takeUntil(this._destroyed$)
    ).subscribe();
  }

  async onDrop(e: DragEvent): Promise<any> {
    e.stopPropagation();
    e.preventDefault();

    if (e.dataTransfer && e.dataTransfer.files) {
      const data = packageFiles(e.dataTransfer.files);
      this._upload(data);
    }
  }

  registerOnChange(fn: any): void {
    this._digitalAssetSubject
    .pipe(takeUntil(this._destroyed$))
    .subscribe(fn);
  }

  onDragOver(e: DragEvent): void {
    e.stopPropagation();
    e.preventDefault();
  }

  handleFileInput(files: FileList) {
    const data = packageFiles(files);
    this._upload(data);
  }

  private _upload(data: FormData) {
    this._digitalAssetService.upload({ data })
    .pipe(
      takeUntil(this._destroyed$),
      tap(response => this._digitalAssetSubject.next(response.digitalAsset))
    )
    .subscribe();
  }

  handleChooseAFileClick() { this.fileInput.nativeElement.click(); }
}

@NgModule({
  declarations: [
    ChooseAFileOrDragItHereComponent
  ],
  exports: [
    ChooseAFileOrDragItHereComponent
  ],
  imports: [
    CommonModule,
    MatIconModule,
    MatMenuModule
  ]
})
export class ChooseAFileOrDragItHereModule { }
