export function makeArticle(overrides: Partial<{
  title: string;
  abstract: string;
  body: string;
}> = {}) {
  const id = Math.random().toString(36).slice(2, 8);
  return {
    title: `Test Article ${id}`,
    abstract: `This is a test article abstract for ${id}.`,
    body: `# Test Article ${id}\n\nThis is the body of the test article. It has enough content to be meaningful.\n\n## Section Two\n\nMore content here.`,
    ...overrides,
  };
}

export const testUser = {
  email: process.env.ADMIN_EMAIL || 'admin@blog.dev',
  password: process.env.ADMIN_PASSWORD || 'Admin1234!',
};
