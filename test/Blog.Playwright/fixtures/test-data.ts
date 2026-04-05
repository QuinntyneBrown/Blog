export const TEST_ADMIN = {
  email: process.env.TEST_ADMIN_EMAIL || 'admin@blog.local',
  password: process.env.TEST_ADMIN_PASSWORD || 'Admin123!',
  displayName: 'Admin User',
};

export function createArticleData(overrides: Partial<{
  title: string;
  body: string;
  abstract: string;
}> = {}) {
  const id = Math.random().toString(36).substring(2, 8);
  return {
    title: overrides.title ?? `Test Article ${id}`,
    body: overrides.body ?? `<p>This is the body content of test article ${id}. It contains enough words to produce a meaningful reading time estimate for the blog platform.</p>`,
    abstract: overrides.abstract ?? `Abstract for test article ${id}`,
  };
}

export function createLongArticleData() {
  const words = Array.from({ length: 1190 }, (_, i) => `word${i}`).join(' ');
  return createArticleData({
    title: 'Long Article for Reading Time',
    body: `<p>${words}</p>`,
    abstract: 'An article with exactly 1190 words for reading time calculation testing.',
  });
}

export function createArticleWithXss() {
  return createArticleData({
    title: 'XSS Test Article',
    body: `<p>Safe content</p><script>alert('xss')</script><p>More content</p>`,
    abstract: 'Article to test XSS sanitization',
  });
}

export function createArticleWithLongTitle() {
  const longTitle = 'A'.repeat(201);
  return createArticleData({
    title: longTitle,
    abstract: 'Article with title exceeding 200 characters',
  });
}

export function createArticleWithLongAbstract() {
  const longAbstract = 'B'.repeat(501);
  return createArticleData({
    abstract: longAbstract,
  });
}
