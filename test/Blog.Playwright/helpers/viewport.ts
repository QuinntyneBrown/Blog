export const viewports = {
  xl: { width: 1440, height: 900 },
  lg: { width: 992, height: 768 },
  md: { width: 768, height: 1024 },
  sm: { width: 576, height: 812 },
  xs: { width: 375, height: 812 },
} as const;

export type ViewportName = keyof typeof viewports;

export const VIEWPORTS = {
  XL: viewports.xl,
  LG: viewports.lg,
  MD: viewports.md,
  SM: viewports.sm,
  XS: viewports.xs,
} as const;
