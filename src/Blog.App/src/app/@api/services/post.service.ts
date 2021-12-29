import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Post } from '@api';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BASE_URL, Page } from '@core';

@Injectable({
  providedIn: 'root'
})
export class PostService {

  constructor(
    @Inject(BASE_URL) private readonly _baseUrl: string,
    private readonly _client: HttpClient
  ) { }

  getPage(options: { pageIndex: number; pageSize: number; }): Observable<Page<Post>> {
    return this._client.get<Page<Post>>(`${this._baseUrl}api/post/page/${options.pageSize}/${options.pageIndex}`)
  }

  public get(): Observable<Post[]> {
    return this._client.get<{ posts: Post[] }>(`${this._baseUrl}api/post`)
      .pipe(
        map(x => x.posts)
      );
  }

  public getById(options: { postId: string }): Observable<Post> {
    return this._client.get<{ post: Post }>(`${this._baseUrl}api/post/${options.postId}`)
      .pipe(
        map(x => x.post)
      );
  }

  public remove(options: { post: Post }): Observable<void> {
    return this._client.delete<void>(`${this._baseUrl}api/post/${options.post.postId}`);
  }

  public create(options: { post: Post }): Observable<{ post: Post }> {
    return this._client.post<{ post: Post }>(`${this._baseUrl}api/post`, { post: options.post });
  }

  public update(options: { post: Post }): Observable<{ post: Post }> {
    return this._client.put<{ post: Post }>(`${this._baseUrl}api/post`, { post: options.post });
  }
}
