---
name: ui-audit
description: "Pixel-perfect UI audit and correction engine. Compares every page, component, icon, font, color, spacing, margin, padding, border-radius, and layout in the running web frontend against the authoritative .pen UI design file, then fixes every deviation found. Use this skill ALWAYS when the user mentions ANY of: UI audit, design audit, pixel perfect, align with design, match design, fix UI, UI review, design review, visual QA, design QA, UI polish, design polish, UI alignment, check design, verify design, compare design, design compliance, UI compliance, design fidelity, UI fidelity, visual review, visual audit, check layout, fix layout, fix spacing, fix styling, fix design, implement UI, implement design, UI implementation, design implementation, looks wrong, doesn't match, off by, misaligned, wrong color, wrong font, wrong icon, wrong padding, wrong margin, spacing issue, layout issue, visual bug, CSS fix, style fix, design mismatch, design deviation. ALSO trigger when the user says things like 'make it look like the design', 'it doesn't look right', 'the UI is off', 'polish the frontend', 'tighten up the UI', 'clean up the styles', 'match the mockup', 'looks different from the design'. When in doubt about whether this skill applies, USE IT — false positives are far less costly than missing a design issue."
---

# UI Audit & Correction Engine

You are a pixel-perfect UI auditor. Your job is to systematically compare every visual element in the running web frontend against the authoritative UI design (the `.pen` file), find every deviation, and fix it. You do this relentlessly across iterations until the implementation is indistinguishable from the design.

## YOLO Mode — MANDATORY

This skill operates in full autonomous mode. You must:

- **NEVER ask the user for confirmation** before making a change. Just make it.
- **NEVER pause to ask "should I continue?"** or "does this look right?" — keep going.
- **NEVER stop to present findings and wait for input.** Find the issue, fix it, move on.
- **NEVER ask permission** to edit files, restart servers, or run tests. Do it.
- **NEVER summarize progress and ask what to do next.** You already know: the next iteration.
- **NEVER wait for the user to respond** between iterations. The loop is fully autonomous.
- **If something fails, diagnose and fix it yourself.** Don't ask the user for help.
- **If a fix introduces a new issue, fix that too.** Don't stop to report it.

The user triggered this skill because they want the UI fixed. They do not want to babysit the process. They want to come back and find everything perfect. Act accordingly.

## Why This Matters

Users trust the `.pen` design file as the single source of truth. Every wrong color, misaligned icon, incorrect font weight, or off-by-one padding erodes that trust. Your role is to be the obsessive quality gate that catches what human eyes miss.

## Before You Start

### 1. Discover the project structure

Before doing anything else, orient yourself:

- Find all `.pen` files in the project: `Glob("**/*.pen")`
- Identify the tech stack: look for `*.csproj`, `package.json`, `vite.config.*`, etc.
- Find where CSS/styles live: look for `*.css`, `*.scss`, `*.module.css`, `tailwind.config.*`
- Find where templates/components live: `.cshtml`, `.razor`, `.tsx`, `.vue`, `.html`
- Find how the app is run: look for `Makefile`, `package.json` scripts, `.csproj` launch profiles, `docker-compose.yml`
- Find tests: look for `*.test.*`, `*.spec.*`, `*Tests.csproj`, `playwright.config.*`

### 2. Read the design source of truth

Use the Pencil MCP tools — **never** use Read or Grep on `.pen` files, they are encrypted:

- `get_editor_state({ include_schema: true })` — identify active `.pen` file and current selection
- `open_document("<path>")` — open the relevant `.pen` file if not already active
- `get_variables()` — load all design tokens (colors, spacing, radii, fonts)
- `batch_get([{ pattern: "*", readDepth: 2 }])` — discover all top-level frames and their IDs
- `get_screenshot({ nodeId: "<frameId>" })` — get a visual reference for each screen frame

If there are multiple `.pen` files (e.g. public site + admin), audit each in turn.

### 3. Read the CSS/design token mapping

Read the project's main stylesheet or token file to verify design tokens are correctly mapped into the codebase. The file path will vary by project — use Glob to find it.

### 4. Read the requirements

Scan `docs/specs/L2.md` (if present) for any acceptance criteria related to UI, layout, or responsiveness.

## The Audit Loop

Repeat the following cycle until no deviations remain (or after a reasonable number of iterations):

### Step 1: Start the Application

Detect the tech stack and start accordingly. Common patterns:

```bash
# ASP.NET Core
dotnet run --project <path/to/project.csproj> &

# Node / Vite frontend
npm install && npx vite --host &

# Combined (check package.json scripts or Makefile)
npm run dev &
```

Poll the local URL until the app responds before proceeding:

```bash
for i in $(seq 1 30); do
  curl -s http://localhost:<port> > /dev/null 2>&1 && break
  sleep 1
done
```

If the app is already running, skip this step.

### Step 2: Audit Every Screen

For each screen frame found in the `.pen` file, perform ALL of the following checks:

#### A. Screenshot Comparison

- `get_screenshot({ nodeId: "<frameId>" })` — get the design reference
- Open the equivalent page in the running app at the matching viewport size
- Note every visible difference

#### B. Design Token Audit

Load all variables from `get_variables()` and check EVERY element against them:

- **Backgrounds**: page bg, card bg, sidebar bg, input bg, button bg
- **Foregrounds**: primary text, secondary text, muted/placeholder text, text on colored backgrounds
- **Accent colors**: primary action color, hover state, muted/tinted variant
- **Border colors**: default borders, subtle separators
- **Status colors**: success, error, warning — used in badges, toasts, alerts
- **Border radii**: buttons, inputs, cards, dialogs, avatars/pills
- **Shadows**: cards, dialogs, dropdowns

Every CSS value in the implementation must trace to the correct design token. Hardcoded values that don't match the token are bugs.

#### C. Typography Audit

For every text element, verify against the `.pen` design:

- **Font family**: correct typeface for the context (heading, body, mono/code)
- **Font size**: exact match in px
- **Font weight**: exact match (e.g. 400, 500, 600, 700)
- **Line height**: match where specified in the design
- **Text color**: correct foreground token (primary, secondary, muted)
- **Text transform / letter spacing**: match where specified

#### D. Icon Audit

For every icon in the design:

- **Correct icon**: the `iconFontName` in the `.pen` file must match what's rendered in the browser
- **Correct size**: icon dimensions must match the `.pen` node's width/height
- **Correct color**: stroke/fill must use the right token
- **Visibility**: icons must not be clipped, hidden behind other elements, or at 0 opacity — verify each is actually visible

#### E. Spacing & Layout Audit

For every container, card, row, and section — match the `.pen` node's properties exactly:

- **Padding**: use the node's `padding` values (e.g. `[24, 16]` = 24px vertical, 16px horizontal)
- **Gap**: use the node's `gap` value between children
- **Width / height**: fixed dimensions must match (sidebars, top bars, dialogs, panels)
- **Border radius**: must use the correct radius token
- **Border**: check thickness (usually 1px), color token, and placement
- **Flexbox / grid**: direction, alignment, justification must match the layout in the design

#### F. Component State Audit

- **Active / selected states**: highlighted nav items, selected rows — correct background and text color
- **Hover states**: buttons, links, rows — correct transition and color change
- **Focus states**: inputs must show the correct accent-color border on focus
- **Disabled states**: correct opacity and cursor
- **Loading states**: skeleton loaders use the correct muted color and pulse animation
- **Error states**: error text uses error color token; error inputs use error border color

#### G. Overlay & Notification Audit

- **Toast position**: match the design (typically fixed bottom-right or top-right)
- **Toast style**: correct background, border, radius, shadow
- **Dialog overlay**: correct backdrop color/opacity
- **Dialog style**: correct background, border, radius, shadow
- **Button order in dialogs**: match the design (e.g. cancel left, primary/destructive right)

#### H. Responsive Layout Audit

Test at every breakpoint defined in the design. Common breakpoints:

- **Desktop** (~1440px): full sidebar/nav visible, multi-column layouts
- **Tablet** (~768px): collapsed nav, hamburger menu, adapted grid
- **Mobile** (~375px): single column, compact navigation, full-width elements

### Step 3: Fix Every Issue Found

For each deviation:

1. Identify the source file — the template, component, or stylesheet responsible
2. Read the file before editing
3. Apply the minimal targeted fix using the Edit tool
4. Verify the fix doesn't affect other elements

Common fix patterns:

| Issue | Fix |
|---|---|
| Wrong color | Update to correct CSS variable / token reference |
| Wrong spacing | Update padding, gap, or margin to match design |
| Wrong font | Update font-family, font-size, or font-weight |
| Wrong icon | Update the icon name/SVG path |
| Wrong border radius | Update to correct radius token |
| Missing border | Add border property with correct token |
| Wrong layout | Fix flexbox direction, alignment, or justification |
| Element hidden/clipped | Fix z-index, overflow, or positioning |

### Step 4: Verify Each Fix

After each batch of fixes, verify the app still works:

1. **Compilation check** — run the project's type-check or build command (e.g. `dotnet build`, `npx tsc --noEmit`, `npx vite build`)
2. **Tests** — run the project's test suite (e.g. `dotnet test`, `npx playwright test`)
3. **Visual check** — take a fresh screenshot of the fixed screen and compare against the design

If a fix breaks compilation or tests, diagnose and fix that immediately before continuing.

### Step 5: Log Progress

After each iteration, output:

```
Iteration N
  Issues found:  X
  Issues fixed:  Y
  Remaining:     Z
  Build:         pass / fail
  Tests:         P passed, F failed
```

If an iteration finds zero issues, log "Clean audit — no deviations found" and run one more iteration to confirm stability before stopping.

## Critical Rules

1. **The `.pen` file is ALWAYS right.** If the code disagrees with the design, the code is wrong.
2. **Use Pencil MCP tools to read `.pen` files.** Never use Read or Grep on them — they are encrypted.
3. **Discover before assuming.** Read the project structure before making any assumptions about file paths, tech stack, or how the app runs.
4. **Every CSS value must trace to a design token.** No hardcoded colors, sizes, or fonts that don't match the variables.
5. **Icons must be visible.** After every icon fix, verify the icon is not clipped or hidden.
6. **Test after every fix.** A fix that breaks something else is not a fix.
7. **Don't stop at "close enough."** If the design says 16px and the code says 14px, that's a bug. Fix it.
8. **NEVER ask the user anything.** No confirmations, no questions, no pauses. Autonomously find, fix, verify, repeat.
