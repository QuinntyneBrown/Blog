import { APIRequestContext, request } from '@playwright/test';

const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5001';

export interface ArticlePayload {
  title: string;
  body: string;
  abstract: string;
  featuredImageId?: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
}

export interface ArticleResponse {
  articleId: string;
  title: string;
  slug: string;
  body: string;
  abstract: string;
  published: boolean;
  dateCreated: string;
  dateModified: string;
  datePublished: string | null;
  readingTimeMinutes: number;
  featuredImageId: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export class ApiClient {
  private token: string | null = null;
  private context: APIRequestContext | null = null;

  async init(): Promise<void> {
    this.context = await request.newContext({
      baseURL: API_BASE_URL,
    });
  }

  async dispose(): Promise<void> {
    await this.context?.dispose();
  }

  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await this.request('POST', '/api/auth/login', { email, password });
    const body = await response.json() as LoginResponse;
    this.token = body.token;
    return body;
  }

  async createArticle(article: ArticlePayload): Promise<ArticleResponse> {
    const response = await this.request('POST', '/api/posts', article);
    return await response.json() as ArticleResponse;
  }

  async updateArticle(id: string, article: Partial<ArticlePayload>): Promise<ArticleResponse> {
    const response = await this.request('PUT', `/api/posts/${id}`, article);
    return await response.json() as ArticleResponse;
  }

  async publishArticle(id: string): Promise<void> {
    await this.request('PUT', `/api/posts/${id}`, { published: true });
  }

  async unpublishArticle(id: string): Promise<void> {
    await this.request('PUT', `/api/posts/${id}`, { published: false });
  }

  async deleteArticle(id: string): Promise<void> {
    await this.request('DELETE', `/api/posts/${id}`);
  }

  async getArticles(page = 1, pageSize = 20): Promise<PagedResponse<ArticleResponse>> {
    const response = await this.request('GET', `/api/posts?page=${page}&pageSize=${pageSize}`);
    return await response.json() as PagedResponse<ArticleResponse>;
  }

  async uploadImage(filePath: string, fileName: string): Promise<{ digitalAssetId: string; url: string }> {
    const context = this.getContext();
    const response = await context.post('/api/digital-assets/upload', {
      headers: this.authHeaders(),
      multipart: {
        file: {
          name: fileName,
          mimeType: 'image/jpeg',
          buffer: Buffer.from('fake-image-content'),
        },
      },
    });
    return await response.json();
  }

  private async request(method: string, url: string, data?: unknown) {
    const context = this.getContext();
    const options = {
      headers: this.authHeaders(),
      data,
    };

    switch (method) {
      case 'GET': return context.get(url, options);
      case 'POST': return context.post(url, options);
      case 'PUT': return context.put(url, options);
      case 'DELETE': return context.delete(url, options);
      default: throw new Error(`Unsupported method: ${method}`);
    }
  }

  private getContext(): APIRequestContext {
    if (!this.context) throw new Error('ApiClient not initialized. Call init() first.');
    return this.context;
  }

  private authHeaders(): Record<string, string> {
    if (!this.token) return {};
    return { Authorization: `Bearer ${this.token}` };
  }
}
