export const VIEWPORTS = {
  XS: { width: 375, height: 812 },
  SM: { width: 576, height: 1024 },
  MD: { width: 768, height: 1024 },
  LG: { width: 992, height: 768 },
  XL: { width: 1200, height: 900 },
} as const;

export type ViewportName = keyof typeof VIEWPORTS;
