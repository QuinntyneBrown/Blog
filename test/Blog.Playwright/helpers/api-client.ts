import { APIRequestContext } from '@playwright/test';

export class ApiClient {
  constructor(
    private readonly request: APIRequestContext,
    private readonly baseUrl: string = process.env.API_URL || 'http://localhost:5001'
  ) {}

  async login(email: string, password: string): Promise<string> {
    const response = await this.request.post(`${this.baseUrl}/api/auth/login`, {
      data: { email, password },
    });
    const body = await response.json();
    return body.data.token as string;
  }

  async createArticle(token: string, data: {
    title: string;
    abstract: string;
    body: string;
  }) {
    const response = await this.request.post(`${this.baseUrl}/api/articles`, {
      data,
      headers: { Authorization: `Bearer ${token}` },
    });
    const body = await response.json();
    return body.data;
  }

  async publishArticle(token: string, articleId: string, version: number) {
    const etag = `W/"article-${articleId}-v${version}"`;
    return this.request.patch(`${this.baseUrl}/api/articles/${articleId}/publish`, {
      data: { published: true },
      headers: { Authorization: `Bearer ${token}`, 'If-Match': etag },
    });
  }

  async deleteArticle(token: string, articleId: string, version: number) {
    const etag = `W/"article-${articleId}-v${version}"`;
    return this.request.delete(`${this.baseUrl}/api/articles/${articleId}`, {
      headers: { Authorization: `Bearer ${token}`, 'If-Match': etag },
    });
  }
}
