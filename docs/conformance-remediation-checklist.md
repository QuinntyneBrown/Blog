# Conformance Remediation Checklist

This checklist breaks the identified implementation gaps into two independent workstreams so they can be fixed concurrently with minimal file overlap.

## Workstream A — Admin Auth And Asset Plumbing

- [ ] A1. Hydrate `HttpContext.User` from the session-stored admin JWT before Razor Pages authorization runs.
  Required changes: validate the session token, set the current principal, and preserve bearer-header auth for API requests.
  Primary files: `src/Blog.Api/Program.cs`, `src/Blog.Api/Middleware/*`

- [ ] A2. Stop using placeholder authentication state inside admin page models.
  Required changes: read the authenticated admin identity from claims/session instead of ad-hoc placeholder logic and make admin access checks consistent.
  Primary files: `src/Blog.Api/Pages/Admin/*.cshtml.cs`

- [ ] A3. Make the digital-assets list query use the real logged-in user id.
  Required changes: resolve the current user id from the authenticated principal and pass it through the query path.
  Primary files: `src/Blog.Api/Pages/Admin/DigitalAssets/Index.cshtml.cs`, `src/Blog.Api/Features/DigitalAssets/*`

- [ ] A4. Make digital-asset upload work from the back-office flow.
  Required changes: ensure uploads no longer short-circuit on `Guid.Empty`, keep success/error feedback intact, and leave uploaded assets associated to the current admin user.
  Primary files: `src/Blog.Api/Pages/Admin/DigitalAssets/Index.cshtml.cs`, `src/Blog.Api/Features/DigitalAssets/*`

- [ ] A5. Make digital-asset deletion work from the back-office flow.
  Required changes: wire the delete action to the correct handler path and preserve the conflict/error behavior when an asset is still referenced.
  Primary files: `src/Blog.Api/Pages/Admin/DigitalAssets/Index.cshtml`, `src/Blog.Api/Pages/Admin/DigitalAssets/Index.cshtml.cs`

## Workstream B — Admin UI Conformance

- [ ] B1. Add a responsive admin top bar shell that matches the design intent at MD/SM/XS.
  Required changes: expose a top bar with heading, hamburger control, avatar/identity affordance, and stable test hooks.
  Primary files: `src/Blog.Api/Pages/Admin/Shared/_AdminLayout.cshtml`

- [ ] B2. Add a small-screen admin navigation drawer/panel.
  Required changes: preserve access to Articles, Media, Settings, and Sign out when the sidebar is hidden.
  Primary files: `src/Blog.Api/Pages/Admin/Shared/_AdminLayout.cshtml`

- [ ] B3. Bring the desktop/tablet articles list closer to the design.
  Required changes: add the missing search affordance, show abstract preview below the title, and add stable row/test selectors.
  Primary files: `src/Blog.Api/Pages/Admin/Articles/Index.cshtml`

- [ ] B4. Replace the SM/XS article table presentation with card-based rows.
  Required changes: render card layout on small screens, keep date/status/actions visible, and hide desktop rows at those breakpoints.
  Primary files: `src/Blog.Api/Pages/Admin/Articles/Index.cshtml`, `src/Blog.Api/Pages/Admin/Shared/_AdminLayout.cshtml`

- [ ] B5. Improve editor metadata and structure to match the design more closely.
  Required changes: add slug visibility, richer article metadata, stronger featured-image controls, and stable test hooks for toolbar/modal/toast surfaces.
  Primary files: `src/Blog.Api/Pages/Admin/Articles/Create.cshtml`, `src/Blog.Api/Pages/Admin/Articles/Edit.cshtml`, `src/Blog.Api/Pages/Admin/Shared/_AdminLayout.cshtml`

- [ ] B6. Add an in-editor featured-image chooser flow.
  Required changes: provide a modal-style image chooser/upload surface, preview the selected image, and support clearing it.
  Primary files: `src/Blog.Api/Pages/Admin/Articles/Create.cshtml`, `src/Blog.Api/Pages/Admin/Articles/Edit.cshtml`, `src/Blog.Api/Pages/Admin/Shared/_AdminLayout.cshtml`

## Completion

- [ ] C1. Re-run available validation and update this checklist to completed state.
  Required changes: execute the available local verification steps, record blockers if the environment prevents full validation, and mark the completed items.
