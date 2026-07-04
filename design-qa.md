# WordCollector 1.2.1 Design QA

## Evidence

- Source visual truth: `E:\AIGC Test\Word_Collector\design-qa\source-main-dark-720x650.png`
- Dark implementation: `E:\AIGC Test\Word_Collector\design-qa\compact-main-dark.png`
- Light implementation: `E:\AIGC Test\Word_Collector\design-qa\input-fix-main-light.png`
- Final 1.2.1 input smoke test: `E:\AIGC Test\Word_Collector\design-qa\input-fix-release-1.2.1.png`
- Settings input verification: `E:\AIGC Test\Word_Collector\design-qa\input-fix-settings.png`
- Review input verification: `E:\AIGC Test\Word_Collector\design-qa\input-fix-review-selected.png`
- Full-view comparison: `E:\AIGC Test\Word_Collector\design-qa\compact-comparison-full.png`
- Focused header comparison: `E:\AIGC Test\Word_Collector\design-qa\compact-comparison-header.png`
- Native EXE icon check: `E:\AIGC Test\Word_Collector\design-qa\exe-icon-final.png`
- Viewport: source 720 × 650 and implementation 520 × 400 device-independent pixels, both captured at 150% Windows scaling.
- State: blue accent with a realistic “break the ice” result. The source has the theme popover open; the implementation comparison keeps it closed to expose the compact query and result layout.

The source and implementation were combined at the same physical scale for both full-view and focused-region comparison. The smaller frame is the requested product change, not unintended design drift.

## Findings

No actionable P0, P1, or P2 findings remain.

- Fonts and typography: Segoe UI Variable / Microsoft YaHei UI fallbacks, ClearType rendering, explicit selection text color, and compact display/body sizing remain crisp in light and dark modes.
- Spacing and layout rhythm: the default frame is reduced from 720 × 650 to 520 × 400 (about 44% of the former area). Header, query row, result hierarchy, example surface, and status bar retain clear grouping. Overflow is isolated to the result scroll area.
- Colors and visual tokens: the restrained light/dark surfaces and all five accent palettes continue to use runtime theme tokens with strong foreground contrast.
- Image quality and asset fidelity: the real W raster identity is retained in the title bar and is now embedded as a native multi-size ICO in the executable. No placeholder, CSS-drawn, or text-glyph brand asset is used.
- Copy and content: visible Chinese labels remain concise and coherent. Query status now reports the source (`本地` / `词典` / `AI`) and elapsed time.
- Interaction and accessibility: title actions remain reachable at the 480 × 340 minimum; typed text, caret, focus border, password masking and selected text are visible; minimize enters the native Windows `Minimized` state while `ShowInTaskbar` remains enabled.

## Focused Region Comparison

The focused comparison covers the title bar and query controls because these are the densest parts of the compact redesign. It confirms that all six title actions and the full query/clear row fit without overlap or truncation. The full-view comparison is sufficient for the result typography and example card because those elements remain readable at native capture resolution.

## Patches Made During QA

1. Reduced the default window to 520 × 400 and added a 480 × 340 safe minimum with migration from the prior 720 × 650 default.
2. Converted the main content area to a compact result-only scroll layout.
3. Changed the minimize action from tray hiding to the native taskbar minimized state.
4. Added explicit selected-text contrast and removed automatic select-all on every activation.
5. Embedded the WordCollector ICO in the executable and all application windows.
6. Preserved the existing light/dark and five-accent theme system.
7. Removed duplicate padding from the shared TextBox and PasswordBox content hosts, restoring visible text in the main window, settings and review screens.

## Follow-up Polish

- P3: a future iteration could add an optional “ultra-compact” collapsed result state for users who want a dictionary bar rather than the current small utility window.

final result: passed
