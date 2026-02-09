# Design Guidelines

Design system reference for the YieldRaccoon WPF desktop application.
Based on **Microsoft Fluent Design v2**, adapted for **MahApps.Metro 2.4.11** on **.NET 9.0**.

---

## Design Philosophy

YieldRaccoon follows Fluent v2 principles adapted for a **data-dense desktop tool**:

- **Calm and focused** — neutral surfaces, accent color reserved for interactive elements and status
- **System-native** — respects the user's Windows accent color via `RuntimeThemeGenerator`
- **Hierarchical** — two-tier elevation system separates top-level panels from nested controls
- **Consistent** — all spacing on a 4px grid, all typography from the type ramp, all colors from semantic tokens
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

---

## Do's and Don'ts

### Do

- Use `yr.*` tokens for all spacing, typography, colors, and styles
- Use sentence casing for all UI text including section labels
- Keep all spacing values on the 4px grid
- Use `DynamicResource` for brush references
- Use `StaticResource` for styles and thickness tokens
- Test with different Windows accent colors (the RuntimeThemeGenerator adapts)

### Don't

- Hardcode hex color strings in XAML or ViewModels (use `yr.*Brush` tokens)
- Use ALL CAPS for section labels (Fluent v2 mandates sentence casing)
- Use odd Margin values like 3, 5, 7, 10, 15 (align to 4px grid: 4, 8, 12, 16, 24)
- Apply raw MahApps button styles directly — always use `yr.Button*Style` wrappers
- Use unstyled default WPF buttons (no implicit Button style in this app)
- Convert Segoe Fluent Icons FontSize to typography style tokens (they're icon sizing)

---

## Theme Initialization

The app's theme is initialized in `App.xaml.cs` → `ApplyYieldRaccoonTheme()`:

1. Reads Windows system accent color via `SystemParameters.WindowGlassColor`
2. Falls back to `#0078D4` (Windows 11 default blue) if transparent/black
3. Generates all 200+ MahApps theme resources via `RuntimeThemeGenerator`
4. Applies as a Light theme via `ThemeManager.Current.ChangeTheme()`

`Light.Blue.xaml` remains in `App.xaml` as a XAML designer fallback only — it is fully overridden at runtime.

### References

- [MahApps.Metro ThemeManager](https://mahapps.com/docs/themes/thememanager) — official docs for theme management
- [Code samples for MahApps.Metro themes](https://github.com/punker76/code-samples#mahappsmetro-themes) — examples by the MahApps maintainer

---

## Color

### Semantic Status Brushes

| Token | Color | Usage |
| ------- | ------- | ------- |
| `yr.SuccessBrush` | `#2ECC71` | Completed batches, green checkmarks, today's NAV date |
| `yr.WarningBrush` | `#F39C12` | Pending items, client errors, cancelled sessions |
| `yr.ErrorBrush` | `#E74C3C` | Failed batches, server errors, navigation failures |
| `yr.InfoBrush` | `#3498DB` | Informational events, navigation started |
| `yr.MutedBrush` | `#95A5A6` | Unknown/fallback events, disabled items |
| `yr.StreamingBrush` | `#FF4444` | Streaming mode overlay text |
| `yr.OverlayBrush` | `#44000000` | Semi-transparent overlay (25% black) |

### MahApps Theme Brushes (from RuntimeThemeGenerator)

| Brush | Usage |
| ------- | ------- |
| `MahApps.Brushes.Accent` | Interactive highlights: NAV values, current fund, primary buttons, delay countdown, splitter hover |
| `MahApps.Brushes.ThemeBackground` | Window/panel backgrounds |
| `MahApps.Brushes.ThemeForeground` | Default text color |
| `MahApps.Brushes.Gray1` | Darker gray — subtitle count text |
| `MahApps.Brushes.Gray2` | Medium-dark gray — ISIN, currency, descriptions |
| `MahApps.Brushes.Gray3` | Medium gray — subheader text, timestamps, completed fund names |
| `MahApps.Brushes.Gray7` | Light gray — panel borders, toolbar borders |
| `MahApps.Brushes.Gray8` | Very light gray — splitter line (rest state) |
| `MahApps.Brushes.Gray9` | Near-white gray — toolbar/control backgrounds |
| `MahApps.Brushes.Gray10` | Lightest gray — panel backgrounds |

### Color Rules

- **Accent color** is reserved for interactive elements and state indication only
- Status colors should be used via `yr.*` brush tokens, never hardcoded hex in XAML
- Use `DynamicResource` for all brush references (theme can change at runtime)

---

## Typography

Aligned to the Fluent v2 type ramp. Font: **Segoe UI Variable** (via MahApps default).

| Token | Size | Weight | Fluent v2 Mapping | Usage |
| ------- | ------ | -------- | ------------------- | ------- |
| `yr.TitleStyle` | 20px | SemiBold | Subtitle (20/28) | Section headings: "Extracted Funds", "Crawl Control" |
| `yr.SubheaderStyle` | 12px | SemiBold | Custom (Caption size + SemiBold) | Section labels in sentence case: "Manual control", "Event log" |
| `yr.BodyLargeStyle` | 18px | Regular | Body Large (18/24) | Large body text, streaming overlay subtitle |
| `yr.BodyStyle` | 14px | Regular | Body (14/20) | Standard readable text, delay countdown |
| `yr.BodyStrongStyle` | 14px | SemiBold | Body Strong (14/20) | Fund names, emphasized text |
| `yr.CaptionStyle` | 12px | Regular | Caption (12/16) | ISIN codes, progress counts, secondary info |
| `yr.SmallStyle` | 11px | Regular | Below minimum* | Timestamps, descriptions, tertiary info |

*\* Fluent v2 minimum legible size is 12px. 11px is a pragmatic compromise for very dense list data.*

### Typography Rules

- **Sentence casing** for all UI text, including section labels (Fluent v2 requirement)
- **Left-aligned** by default
- **50-60 characters** per line for ease of reading
- Segoe Fluent Icons `FontSize` is **icon sizing**, not typography — leave as inline values

---

## Spacing

All spacing values are multiples of **4px** (the Fluent v2 base unit).

### Double Tokens (for programmatic use)

| Token | Value | Semantic |
| ------- | ------- | ---------- |
| `yr.SpacingXS` | 4px | Extra small — tight internal spacing |
| `yr.SpacingS` | 8px | Small — between related items, button gaps |
| `yr.SpacingM` | 12px | Medium — between content areas |
| `yr.SpacingL` | 16px | Large — panel padding, surface-to-text |
| `yr.SpacingXL` | 24px | Extra large — between major sections |
| `yr.PanelPadding` | 16px | Panel inner padding (Fluent v2: 16px for large windows) |

### Thickness Tokens (for XAML Margin bindings)

| Token | Value | Usage |
| ------- | ------- | ------- |
| `yr.SectionSpacing` | `0,24,0,0` | Between major sections (e.g., Manual Control → Session Control) |
| `yr.SubsectionSpacing` | `0,16,0,0` | Between subsections, after headers |
| `yr.ItemSpacing` | `0,8,0,0` | Between related items within a section |
| `yr.LabelSpacing` | `0,0,0,8` | Below section labels (subheaders) |
| `yr.CompactSpacing` | `0,4,0,0` | Tight spacing in dense lists (batch rows, event rows) |

### Usage

```xml
<!-- Preferred: use Thickness tokens -->
<StackPanel Margin="{StaticResource yr.SectionSpacing}">

<!-- Acceptable: literal values on the 4px grid for one-off spacing -->
<TextBlock Margin="0,12,0,0" />

<!-- Avoid: off-grid values -->
<TextBlock Margin="0,5,0,0" />  <!-- BAD: 5 is not a multiple of 4 -->
```

---

## Geometry

Per Fluent v2, three levels of corner rounding:

| Corner Radius | Usage | Token/Style |
| --------------- | ------- | ------------- |
| **8px** | Top-level containers: panels, dialogs, standalone cards | `yr.PanelStyle` |
| **4px** | In-page elements: buttons, nested containers, toolbars | `yr.PanelControlStyle`, `yr.ButtonStyle` |
| **0px** | Edges intersecting other edges (e.g., toolbar inside a panel) | Local `CornerRadius="0"` override |

### Abutting Edge Rule

When a control-level container (like a toolbar) sits flush against the edge of a panel, set `CornerRadius="0"` as a local override. WPF style property precedence ensures local values override style setters.

```xml
<Border Style="{StaticResource yr.PanelControlStyle}"
        CornerRadius="0"       <!-- Overrides the 4px from style -->
        BorderThickness="0,0,0,1">
```

---

## Elevation

Two-tier hierarchy based on Fluent v2 elevation values:

### Card Level (`yr.PanelStyle`)

- **Background:** `MahApps.Brushes.Gray10`
- **Border:** `MahApps.Brushes.Gray7`, 1px
- **Corner radius:** 8px
- **Shadow:** `DropShadowEffect` — BlurRadius=8, ShadowDepth=2, Opacity=0.08, Direction=270

Use for: Main 3-column panels, standalone containers.

### Control Level (`yr.PanelControlStyle`)

- **Background:** `MahApps.Brushes.Gray9`
- **Border:** `MahApps.Brushes.Gray7`, 1px
- **Corner radius:** 4px
- **Shadow:** `DropShadowEffect` — BlurRadius=4, ShadowDepth=1, Opacity=0.04, Direction=270

Use for: Toolbars, response preview containers, nested cards within panels.

---

## Button Styles

All button styles add Fluent v2's 4px corner radius via `mah:ControlsHelper.CornerRadius`.

| Token | Base Style | Usage |
| ------- | ----------- | ------- |
| `yr.ButtonAccentStyle` | `MahApps.Styles.Button.Square.Accent` | Primary actions: "Start Crawling", "Save" |
| `yr.ButtonStyle` | `MahApps.Styles.Button.Square` | Secondary actions: "Stop", "Browse...", "Cancel", "Load Next Batch" |
| `yr.ButtonFlatStyle` | `MahApps.Styles.Button.Flat` | Tertiary/chromeless: "Reload", "Clear", "Copy URL", "Refresh" |

### Button Rules

- Every button in every view must use a `yr.Button*Style` (not raw MahApps styles or unstyled defaults)
- Primary action gets `yr.ButtonAccentStyle` — one per section maximum
- Button spacer between paired buttons: use `ColumnDefinition Width="8"` (not 5)

---

## Iconography

All icons use **Segoe Fluent Icons** font. Complete glyph catalog:

| Unicode | Glyph | Context |
| --------- | ------- | --------- |
| `\uEBE8` | Globe/Network | AboutFund button, InterceptorView header, navigation events |
| `\uE713` | Settings gear | Settings title bar button |
| `\uE73E` | Checkmark | Completed batch/fund, success events |
| `\uE895` | Sync arrows | Batch in progress |
| `\uE823` | Clock | Batch pending |
| `\uE783` | Error circle | Batch failed, navigation failure |
| `\uE76C` | Right chevron | Current fund indicator |
| `\uE768` | Play | Session started event |
| `\uE8AD` | Globe | Navigation started event |
| `\uE930` | Done | Session completed event |
| `\uE711` | Cancel/X | Session cancelled event |
| `\uE946` | Info circle | Unknown event fallback |

### Icon Rules

- Icon `FontSize` is **icon sizing** — keep as inline values, don't convert to typography styles
- Standard sizes: 12px (list items), 14px (title bar), 18px (section headers)
- Color icons via `DynamicResource yr.*Brush` tokens in DataTriggers

---

## Splitter

Custom `yr.SplitterStyle` provides a refined Fluent v2 splitter:

- **Hit target:** 6px (accessible grab area)
- **Visual:** 1px line in `MahApps.Brushes.Gray8`
- **Hover:** Expands to 2px, changes to `MahApps.Brushes.Accent`
- Width=3 GridSplitter is a good balance between refined look and usability

---

## Micro-Interactions

### CompletionFlashBehavior

Attached behavior that flashes an element's background when status changes to "Completed":

- Color: LightGreen
- Fade: 2s with `QuadraticEase.EaseOut`
- Hold: 5s before starting fade

### ListBoxAutoScrollBehavior

Smooth auto-scroll for new items:

- Debounce: 200ms
- Animation: 3s with `CubicEase.EaseOut`

---

## WPF Gotchas

Collected learnings to save future debugging:

1. **Style property precedence:** Local value > style triggers > style setters. Safe to override individual properties (e.g., `CornerRadius`) on elements using yr styles.

2. **ThemeManager overrides merged dictionaries:** Don't rely on adding ResourceDictionaries after the theme — use `RuntimeThemeGenerator` to create proper custom themes.

3. **MahApps brushes use frozen StaticResource:** Overriding Color keys alone won't change frozen brushes. Always use `DynamicResource` for yr.* brushes.

4. **`Application.Current` namespace conflict:** Use `System.Windows.Application.Current` because `YieldRaccoon.Application` exists in the namespace.

5. **TextBlock style on TextBox:** A style with `TargetType="TextBlock"` CANNOT be applied to a TextBox — causes `InvalidOperationException`.

6. **XAML Border tag edits:** Watch for duplicate attributes (e.g., `Padding` appearing twice) after edit operations.

7. **`sys:Double` in XAML:** Requires `xmlns:sys="clr-namespace:System;assembly=mscorlib"` (works in .NET 9).

8. **`ColumnDefinition Width="8"`** between buttons is a spacer, not a splitter — don't convert to SplitterStyle.
