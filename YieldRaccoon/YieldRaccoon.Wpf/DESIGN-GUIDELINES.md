# Design Guidelines

UX/UI design philosophy for the YieldRaccoon desktop application.
Based on **Microsoft Fluent Design v2**, adapted for a **data-dense desktop tool**.

> **Implementation details** (token names, XAML patterns, MahApps brushes, WPF gotchas) live in the `wpf-fluent-design` Claude skill.
> This document covers the platform-agnostic *what* and *why*.

---

## Design Philosophy

- **Calm and focused** — neutral surfaces, accent color reserved for interactive elements and status
- **System-native** — respects the user's OS accent color; adapts dynamically
- **Hierarchical** — two-tier elevation system separates top-level panels from nested controls
- **Consistent** — all spacing on a base grid, all typography from the type ramp, all colors from semantic tokens
- **Light mode** — single theme with dynamic accent color (dark mode not yet implemented)

---

## UI/UX Design Principles

Universal design principles that guide aesthetic decisions across platforms.

### Core Mindset

**Be intentional, avoid generic, commit to a cohesive aesthetic.** Every visual choice — font, color, spacing, motion — should reinforce the app's identity and purpose, not just "look fine."

### Type Hierarchy

- A well-structured **type ramp** with clear hierarchy is the single highest-impact design choice
- Body text prioritizes **readability**; display/title text is where personality lives
- Consider a **distinctive display font** for titles to elevate the aesthetic beyond system defaults
- Consistent weight pairing (e.g., SemiBold headers + Regular body) creates rhythm

### Color Discipline

- **Dominant neutrals with sharp accents** outperform timid, evenly-distributed palettes
- Accent color reserved for **interactive elements and status** keeps the UI calm and scannable
- Semantic color tokens (success, warning, error) should be the only way to express status — never hardcoded values
- Test with different accent colors to ensure the palette holds together

### Spatial Composition

- All spacing on a **consistent grid** (4px base unit) creates visual harmony even in dense layouts
- **Generous negative space** between major sections prevents cognitive overload
- **Controlled density** within data areas is fine — the grid keeps it orderly
- Named spacing tokens beat magic numbers for maintainability and consistency

### Elevation & Depth

- A **two-tier elevation system** (container level vs. control level) is enough for most apps — more layers add noise
- Subtle shadows and borders create hierarchy without competing for attention
- Backgrounds with slight tonal variation separate layers without harsh lines

### Motion & Micro-Interactions

- Motion should be **purposeful**: confirm actions, guide attention, show state changes
- Focus on **high-impact moments**: panel reveals on load, status change flashes, smooth scrolling
- Subtle hover feedback (color shift, slight scale) makes interactive elements feel alive
- Avoid gratuitous animation — every transition should communicate something

### Visual Consistency

- **One accent style per section** for primary actions — multiple competing highlights confuse priority
- **Sentence casing** everywhere creates a calm, modern tone (ALL CAPS reads as shouting)
- Consistent corner radius tiers (large for containers, small for controls, zero for abutting edges) build subconscious order
- Icon style should be uniform — one icon family, consistent sizing per context

### Selected Item Contrast

- When items are selectable (lists, grids, tabs), **all text must remain legible** on the selection highlight background
- Accent-colored selection backgrounds require switching all foreground colors — including status colors and secondary text — to a high-contrast foreground
- This applies to every text element in the item template, not just the primary label
- The selection state should override all other color states (status colors, muted text) to guarantee readability

---

## Do's and Don'ts

### Do

- Use design tokens for all spacing, typography, colors, and styles
- Use sentence casing for all UI text including section labels
- Keep all spacing values on the base grid (multiples of 4px)
- Ensure selected/highlighted items have sufficient text contrast against accent backgrounds
- Test with different OS accent colors to verify the palette holds together

### Don't

- Hardcode color values in views or view models — always use semantic tokens
- Use ALL CAPS for section labels (Fluent v2 mandates sentence casing)
- Use off-grid spacing values (3, 5, 7, 10, 15) — align to the 4px grid
- Leave text elements with default foreground colors inside selectable item templates — they'll be invisible on accent backgrounds
- Use unstyled default platform buttons — always apply a design system button style

---

## References

- [Microsoft Fluent Design v2](https://fluent2.microsoft.design/) — design language reference
- [MahApps.Metro ThemeManager](https://mahapps.com/docs/themes/thememanager) — official docs for theme management
- [MahApps.Metro code samples](https://github.com/punker76/code-samples#mahappsmetro-themes) — examples by the MahApps maintainer
