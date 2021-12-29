import { InjectionToken } from "@angular/core";
import { environment } from "src/environments/environment";

export const BASE_URL: InjectionToken<unknown> = new InjectionToken("BASE_URL");
export const accessTokenKey = "accessTokenKey";
export const usernameKey = "usernameKey";
export const storageKey = "storageKey";
export const ckEditorConfig = {
  removeDialogTabs :'image:advanced;image:Link;link:advanced;link:upload',
  filebrowserImageUploadUrl: `${environment.baseUrl}api/digitalasset/upload?single=true`
};
