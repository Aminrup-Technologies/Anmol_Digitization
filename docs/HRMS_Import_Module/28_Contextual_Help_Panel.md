# 28 — Contextual Synchronization Information Panel

**Status:** Delivered.  
**Scope:** UI/UX only — `HrmsBatchManager` help panel.  
**Unchanged:** Stored procedures, synchronization logic, database schema, validation rules, code-behind business flow.

---

## 1. Design

A single right-side slide-out panel explains Synchronization Configuration for operators.

| Element | Implementation |
|---|---|
| Triggers | Font Awesome `fa-info-circle` buttons beside section labels |
| Panel | One reusable `UserControl` — `Controls/HrmsSyncInfoPanel.ascx` |
| Backdrop | Semi-transparent overlay (`rgba(20,28,40,0.45)`) |
| Motion | Slide / fade **250ms** |
| Stack | Bootstrap + Font Awesome + jQuery (already on `Dristi.Master`) |
| Theme | Navy / steel-blue admin accent aligned with Gentelella — not a purple or cream AI-default palette |

### Assets

| File | Role |
|---|---|
| `AnmolDristi/Controls/HrmsSyncInfoPanel.ascx` (+ `.cs` / `.designer.cs`) | Reusable panel markup + static content |
| `AnmolDristi/css/HrmsBatchManager.css` | Page + panel styles |
| `AnmolDristi/Scripts/HrmsBatchManager.js` | Panel behaviour + existing row-select / confirm helpers |
| `AnmolDristi/HrmsBatchManager.aspx` | Info icons + register/include UserControl |

Inline style block and inline script functions were moved out of the ASPX (ClientID config object remains — required for Web Forms).

---

## 2. Behaviour

| Action | Result |
|---|---|
| Click any info icon | Opens the **same** panel; scrolls to the related section |
| Click Close (X) | Closes panel |
| Press `Esc` | Closes panel |
| Click backdrop | Closes panel |
| Open | Body scroll locked; focus moves to Close |
| Close | Focus returns to the trigger that opened the panel |

Animation:

1. Backdrop fades in/out (250ms)  
2. Panel translates from `translateX(100%)` → `0` (250ms)

Only **one** panel instance exists (outside UpdatePanels). Triggers use delegated jQuery handlers so they survive async postbacks.

Public API for other pages:

```javascript
window.HrmsSyncInfoPanel.open('hrms-help-section-modes');
window.HrmsSyncInfoPanel.close();
window.HrmsSyncInfoPanel.isOpen();
```

---

## 3. Sections (static v1.0 content)

| ID | Title |
|---|---|
| `hrms-help-section-modes` | Processing Modes |
| `hrms-help-section-modules` | Synchronization Modules (masters, employees, hierarchy) |
| `hrms-help-section-overwrite` | Fields to Overwrite |
| `hrms-help-section-flow` | Execution Flow diagram |
| `hrms-help-section-validation` | Validation behaviour |
| `hrms-help-section-warnings` | Warnings vs Errors |
| `hrms-help-section-status` | Batch Status Lifecycle |

### Trigger mapping on BatchManager

| UI label | Opens section |
|---|---|
| Synchronization Configuration | Execution Flow |
| Processing Mode | Processing Modes |
| Synchronization Modules | Modules |
| Fields to Overwrite | Overwrite fields |

Content is **hardcoded HTML** in the UserControl for v1.0 (no DB / resource file).

---

## 4. Accessibility

| Feature | Detail |
|---|---|
| Dialog semantics | `role="dialog"`, `aria-modal="true"`, `aria-labelledby` |
| Hidden state | `aria-hidden` toggled with open/close |
| Triggers | Native `<button>` with `aria-label` / `title` |
| Keyboard | `Esc` closes; basic Tab cycle within the panel |
| Focus | Restored to opener on close |
| Icons | `aria-hidden="true"` on decorative Font Awesome icons |

---

## 5. Future extensibility

1. **Reuse on other admin pages**  
   Register `HrmsSyncInfoPanel.ascx`, include once, add `data-hrms-help-trigger` + `data-hrms-help-section` buttons, reference the same CSS/JS (or extract a shared `HrmsSyncInfoPanel.js`).

2. **Content localization / CMS**  
   Replace static sections with resource keys or a small JSON feed without changing open/close behaviour.

3. **Deep links**  
   Support `?help=modes` by calling `HrmsSyncInfoPanel.open(...)` on load.

4. **Per-section analytics**  
   Hook `openPanel(sectionId)` to log which topic operators open most.

---

## 6. Non-goals (explicit)

- No SQL / stored procedure changes  
- No synchronization or validation logic changes  
- No schema changes  
- No UI redesign of batch grid / Run Import flow beyond info icons + panel  

---

## 7. Acceptance checklist

- [x] Info icons on Configuration, Mode, Modules, Overwrite  
- [x] Single reusable slide-out panel from the right  
- [x] Backdrop + Close / Esc / outside click  
- [x] 250ms slide and fade  
- [x] Responsive full-width on small screens  
- [x] JS in `HrmsBatchManager.js`, CSS in `HrmsBatchManager.css`  
- [x] Static, extendable help content  
- [x] Documentation (this file)  
