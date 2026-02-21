---
name: wpf-fluent-design
description: WPF Fluent Design System implementation for MahApps.Metro desktop apps. Use when creating or editing XAML views, styling controls, adding ListBox/DataTemplate items, working with colors/brushes, spacing, typography, or any visual/UI appearance work in WPF projects. DO NOT use for web frameworks (Next.js, React, JavaScript, TypeScript, Node.js).
allowed-tools: Read, Edit, Write, Bash, Grep, Glob
---

# WPF Fluent Design System

Implementation rules and token reference for MahApps.Metro desktop apps with Fluent v2 styling.

> **UX/UI philosophy** lives in `YieldRaccoon/YieldRaccoon.Wpf/DESIGN-GUIDELINES.md` (platform-agnostic *what* and *why*).
> This skill covers the WPF-specific *how* — tokens, rules, and the agent verification checklist.

> **Theme file:** `YieldRaccoon/YieldRaccoon.Wpf/Themes/YieldRaccoonTheme.xaml`
> **Framework:** MahApps.Metro 2.4.11 on .NET 9.0

---

## Theme System

The app theme is initialized in `App.xaml.cs` via `RuntimeThemeGenerator`:

1. Reads Windows system accent color from `SystemParameters.WindowGlassColor`
2. Falls back to `#0078D4` (Windows 11 default blue) if transparent/black
3. Generates all 200+ MahApps theme resources from the accent color
4. Applies as Light theme via `ThemeManager.Current.ChangeTheme()`

`Light.Blue.xaml` in `App.xaml` is a **XAML designer fallback only** — fully overridden at runtime.

> **Key constraint:** `ThemeManager` overrides merged dictionaries — don't add ResourceDictionaries after the theme expecting them to override. Use `RuntimeThemeGenerator` for custom themes.

---

## Token Reference

### Semantic Status Brushes

| Token | Color | Usage |
| --- | --- | --- |
| `yr.SuccessBrush` | `#2ECC71` | Completed items, checkmarks, today's date |
| `yr.WarningBrush` | `#F39C12` | Pending items, client errors, cancelled |
| `yr.ErrorBrush` | `#E74C3C` | Failed items, server errors |
| `yr.InfoBrush` | `#3498DB` | Informational events |
| `yr.MutedBrush` | `#95A5A6` | Unknown/fallback, disabled |
| `yr.StreamingBrush` | `#FF4444` | Streaming mode overlay |
| `yr.OverlayBrush` | `#44000000` | Semi-transparent overlay (25% black) |

### MahApps Theme Brushes

| Brush | Usage |
| --- | --- |
| `MahApps.Brushes.Accent` | Interactive highlights, primary buttons, active indicators |
| `MahApps.Brushes.Selected.Foreground` | Text on accent-colored selected item backgrounds |
| `MahApps.Brushes.ThemeBackground` | Window/panel backgrounds |
| `MahApps.Brushes.ThemeForeground` | Default text color |
| `MahApps.Brushes.Gray1` | Darker gray — subtitle count text |
| `MahApps.Brushes.Gray2` | Medium-dark gray — ISIN, currency, descriptions |
| `MahApps.Brushes.Gray3` | Medium gray — subheaders, timestamps, completed items |
| `MahApps.Brushes.Gray7` | Light gray — panel/toolbar borders |
| `MahApps.Brushes.Gray8` | Very light gray — splitter rest state |
| `MahApps.Brushes.Gray9` | Near-white gray — toolbar/control backgrounds |
| `MahApps.Brushes.Gray10` | Lightest gray — panel backgrounds |

### Typography

Font: **Segoe UI Variable** (via MahApps default).

| Token | Size | Weight | Usage |
| --- | --- | --- | --- |
| `yr.TitleStyle` | 20px | SemiBold | Section headings |
| `yr.SubheaderStyle` | 12px | SemiBold | Section labels (sentence case) |
| `yr.BodyLargeStyle` | 18px | Regular | Large body text |
| `yr.BodyStyle` | 14px | Regular | Standard readable text |
| `yr.BodyStrongStyle` | 14px | SemiBold | Emphasized text |
| `yr.CaptionStyle` | 12px | Regular | Secondary info, progress counts |
| `yr.SmallStyle` | 11px | Regular | Timestamps, tertiary info |

### Spacing (4px grid)

**Double tokens:**

| Token | Value |
| --- | --- |
| `yr.SpacingXS` | 4 |
| `yr.SpacingS` | 8 |
| `yr.SpacingM` | 12 |
| `yr.SpacingL` | 16 |
| `yr.SpacingXL` | 24 |
| `yr.PanelPadding` | 16 |

**Thickness tokens:**

| Token | Value | Usage |
| --- | --- | --- |
| `yr.SectionSpacing` | `0,24,0,0` | Between major sections |
| `yr.SubsectionSpacing` | `0,16,0,0` | Between subsections |
| `yr.ItemSpacing` | `0,8,0,0` | Between related items |
| `yr.LabelSpacing` | `0,0,0,8` | Below section labels |
| `yr.CompactSpacing` | `0,4,0,0` | Dense lists |

### Elevation (two-tier)

| Style | Corners | Background | Border | Shadow |
| --- | --- | --- | --- | --- |
| `yr.PanelStyle` | 8px | Gray10 | Gray7, 1px | Blur=8, Depth=2, Opacity=0.08 |
| `yr.PanelControlStyle` | 4px | Gray9 | Gray7, 1px | Blur=4, Depth=1, Opacity=0.04 |

### Button Styles

| Token | Base | Usage |
| --- | --- | --- |
| `yr.ButtonAccentStyle` | `MahApps.Styles.Button.Square.Accent` | Primary actions (one per section max) |
| `yr.ButtonStyle` | `MahApps.Styles.Button.Square` | Secondary actions |
| `yr.ButtonFlatStyle` | `MahApps.Styles.Button.Flat` | Tertiary/chromeless actions |

All add 4px `CornerRadius` via `mah:ControlsHelper.CornerRadius`.

### Geometry (corner radius tiers)

| Radius | Usage |
| --- | --- |
| **8px** | Top-level containers (`yr.PanelStyle`) |
| **4px** | In-page elements (buttons, `yr.PanelControlStyle`) |
| **0px** | Abutting edges (local `CornerRadius="0"` override) |

### Iconography (Segoe Fluent Icons)

| Unicode | Glyph | Context |
| --- | --- | --- |
| `\uEBE8` | Globe/Network | AboutFund, navigation events |
| `\uE713` | Settings gear | Settings button |
| `\uE73E` | Checkmark | Completed, success |
| `\uE895` | Sync arrows | In progress |
| `\uE823` | Clock | Pending |
| `\uE783` | Error circle | Failed |
| `\uE76C` | Right chevron | Current item indicator |
| `\uE768` | Play | Session started |
| `\uE8AD` | Globe | Navigation started |
| `\uE930` | Done | Session completed |
| `\uE711` | Cancel/X | Session cancelled |
| `\uE946` | Info circle | Unknown/fallback |

Standard icon sizes: 12px (list items), 14px (title bar), 18px (section headers).

### Splitter

`yr.SplitterStyle`: Width=3, rest=Gray8, hover=Accent.

---

## Agent Verification Checklist

**MUST verify every item when creating or editing XAML:**

### Resources

- [ ] All resource references use `StaticResource` (no runtime theme switching — single light theme set once at startup via `RuntimeThemeGenerator`)
- [ ] Exception: `DynamicResource` is required inside `DataTrigger` `Setter.Value` (WPF limitation)
- [ ] Status colors use `yr.*Brush` tokens (no hardcoded hex)

### Spacing & Layout

- [ ] All spacing values on the 4px grid (4, 8, 12, 16, 24)
- [ ] Button spacer between paired buttons: `ColumnDefinition Width="8"` (not 5)

### Typography & Text

- [ ] Sentence casing for all UI text (no ALL CAPS)
- [ ] Icon `FontSize` is inline (not a typography style token — it's icon sizing)

### Buttons

- [ ] Every button uses `yr.Button*Style` or an appropriate MahApps style (no unstyled WPF defaults)
- [ ] One `yr.ButtonAccentStyle` max per section

### Geometry

- [ ] Corner radius follows tiers: 8px containers, 4px controls, 0px abutting edges

### ListBox & DataTemplate (CRITICAL)

- [ ] Every ListBox with a custom DataTemplate has an `IsSelected` DataTrigger
- [ ] `IsSelected` trigger switches ALL text elements to `MahApps.Brushes.Selected.Foreground`
- [ ] `IsSelected` trigger is the LAST trigger in `DataTemplate.Triggers` (last wins in WPF)
- [ ] All TextBlocks in DataTemplates have `x:Name` for trigger targeting

---

## WPF Gotchas

- **ThemeManager overrides merged dictionaries** — use `RuntimeThemeGenerator`, not extra ResourceDictionaries
- **MahApps brushes use frozen StaticResource** — overriding Color keys alone won't change frozen brushes; use `RuntimeThemeGenerator` instead
- **`StaticResource` for all references** — no runtime theme switching; `DynamicResource` only needed inside `DataTrigger` `Setter.Value`
- **`Application.Current` namespace conflict** — use `System.Windows.Application.Current` (YieldRaccoon.Application exists)
- **TextBlock style on TextBox** — `TargetType="TextBlock"` style on a TextBox causes `InvalidOperationException`
- **XAML Border edits** — watch for duplicate attributes (e.g., `Padding` twice) after edit operations
- **`sys:Double` in XAML** — requires `xmlns:sys="clr-namespace:System;assembly=mscorlib"`
- **Button spacer** — `ColumnDefinition Width="8"` is a spacer, not a splitter
- **Style property precedence** — local value > style triggers > style setters (safe to override individual properties)
- **GridSplitter Width=3** — good balance between refined look and grab usability (1px too hard to grab)

---

## Motion Guidelines

- Motion should be **purposeful**: confirm actions, guide attention, show state changes
- Focus on **high-impact moments**: status change flashes, smooth scrolling, panel reveals
- Subtle hover feedback (color shift, slight scale) makes interactive elements feel alive
- Avoid gratuitous animation — every transition should communicate something

---

## References

- [MahApps.Metro ThemeManager](https://mahapps.com/docs/themes/thememanager) — official theme management docs
- [MahApps.Metro code samples](https://github.com/punker76/code-samples#mahappsmetro-themes) — examples by the MahApps maintainer
- [DESIGN-GUIDELINES.md](../../YieldRaccoon/YieldRaccoon.Wpf/DESIGN-GUIDELINES.md) — UX/UI design philosophy

## Related Skills

- **`dotnet-wpf-mvvm`** — MVVM patterns, ViewModels, commands, DI, data binding
