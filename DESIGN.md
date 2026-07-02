---
version: alpha
name: TSMS
description: "A warm, human-centered academic scheduling interface built on cream (#FFF7F0) canvases and a friendly coral accent (#F45D48), where rounded forms and generous spacing turn the anxiety of grades, schedules, and enrollment into something calm and legible. Software that feels like a helpful academic advisor, not a bureaucratic portal."

colors:
  primary: "#F45D48"
  on-primary: "#FFFFFF"
  primary-hover: "#E04A36"
  accent-green: "#1E875F"
  accent-green-soft: "#D7F0E4"
  ink: "#1C1B1A"
  ink-muted: "#5C5854"
  ink-subtle: "#8A847E"
  canvas: "#FFF7F0"
  surface-1: "#FFFFFF"
  surface-2: "#FBEDE4"
  border: "#E8DDD3"
  border-strong: "#D8C9BB"
  success: "#1E875F"
  warning: "#E5A20B"
  error: "#D7372C"
  info: "#2E73C4"

typography:
  display:
    fontFamily: "GT Walsheim, Inter, -apple-system, sans-serif"
    fontSize: 44px
    fontWeight: 700
    lineHeight: 1.1
    letterSpacing: -0.02em
  heading:
    fontFamily: "GT Walsheim, Inter, -apple-system, sans-serif"
    fontSize: 22px
    fontWeight: 600
    lineHeight: 1.25
    letterSpacing: -0.01em
  body:
    fontFamily: "Inter, -apple-system, BlinkMacSystemFont, sans-serif"
    fontSize: 16px
    fontWeight: 400
    lineHeight: 1.6
    letterSpacing: 0em
  mono:
    fontFamily: "Roboto Mono, SF Mono, monospace"
    fontSize: 15px
    fontWeight: 500
    lineHeight: 1.5
    letterSpacing: 0em

spacing:
  base: 8px
  scale: [4, 8, 12, 16, 24, 32, 48, 64, 96, 128]

radius:
  sm: 8px
  md: 12px
  lg: 20px
  pill: 9999px

shadows:
  card: "0 1px 3px rgba(28,27,26,0.08), 0 1px 2px rgba(28,27,26,0.04)"
  elevated: "0 8px 24px rgba(28,27,26,0.12)"
  focus: "0 0 0 3px rgba(244,93,72,0.30)"

motion:
  duration-fast: 120ms
  duration-base: 220ms
  easing: cubic-bezier(0.34, 1.56, 0.64, 1)
---

## Rationale

**Disarming a category built on quiet anxiety** — Checking a grade, confirming an enrollment went through, wondering if a schedule conflict was caught in time: these are small, recurring moments of low-grade academic anxiety, felt most by Students and most acutely by Admins juggling dozens of overlapping course schedules. TSMS's design philosophy is to remove that friction without pretending the underlying stakes aren't real. The warm cream canvas, the coral that reads as encouraging rather than institutional, and the rounded geometry all say the same thing: this system is on your side, not testing you. The aesthetic deliberately counter-positions against the cold, table-dense academic portals (student information systems, LMS backends) that TSMS was built to feel nothing like.

**Coral as forward motion, not alarm** — Coral (#F45D48) sits at the tonal point where red becomes friendly rather than urgent — important in a system that already has a real error red (#D7372C) for validation and conflicts. Coral anchors primary actions that move something forward: enrolling in a course, submitting a grade, saving a class session. It should feel like an invitation to act, never a warning to heed.

**Green as the language of "confirmed" and "on track"** — In payroll software, green means money landed. In TSMS, green means a Course is Active, an Enrollment succeeded, an Attendance mark is Present, a score falls in the top band. Green is reserved for state that a Student or Admin can stop worrying about — it is the visual "you're good" across every status pill in the system, and it must mean the same thing everywhere it appears.

**Roundness as approachability** — Every radius is larger than convention demands: 8px on inputs, 12px on cards, 20px on modals and report tiles, full pills on status badges. Sharp corners read as institutional; TSMS's rounded geometry, paired with a gentle spring easing, keeps a schedule-and-grades system from feeling like a bureaucratic form.

## 1. Visual Theme & Atmosphere
TSMS feels like a well-organized advisor's office, not a records department. The cream background (#FFF7F0) warms every screen and signals from the first pixel that this isn't gray-on-gray enterprise software. The atmosphere is calm and legible; illustration is used sparingly and only where it genuinely helps — empty states ("No courses yet", "No grades assigned"), not as decoration throughout the product, since every Admin, Lecturer, and Student here is a required user, not one being courted.

Layouts are spacious but data stays dense where it needs to be — a Course Grid or Grading table is still a working tool, so rows get soft dividers (#E8DDD3) and comfortable height rather than illustration-heavy cards. The product resists the visual noise of legacy academic portals, trading nothing on comprehension: an Admin scanning 40 courses for a schedule conflict needs to scan fast, not admire whitespace.

## 2. Color System
**Warm canvas system**:
- Canvas: #FFF7F0 — the signature cream page background
- Surface 1: #FFFFFF — cards, modals, tables, input fields that float above the cream
- Surface 2: #FBEDE4 — subtle warm fill for secondary panels, hover rows, info blocks
- Border: #E8DDD3 — soft dividers and card edges
- Border strong: #D8C9BB — input outlines, emphasized separators

**Coral (primary action)**:
- Coral: #F45D48 — primary buttons (Enroll, Save Grade, Submit Attendance), active nav item, key links
- Hover: #E04A36 — deepened on interaction

**Green (confirmed & on-track)**:
- Green: #1E875F — Course status "Active", Attendance "Present", Enrollment success, top score band
- Soft green: #D7F0E4 — success banner backgrounds, "Active"/"Present" pill fills

**Text**:
- Primary ink: #1C1B1A — warm near-black, all primary reading text
- Muted: #5C5854 — secondary labels, helper text, metadata
- Subtle: #8A847E — placeholders, timestamps, tertiary info

**Semantic — reused across 3 real domains instead of left generic**:
- **Success (#1E875F)** — Course "Active" · Attendance "Present" · Enrollment confirmed · Score band "Xuất sắc"
- **Info (#2E73C4)** — Course "Upcoming" · Score band "Giỏi" · informational banners
- **Warning (#E5A20B)** — Attendance "Excused" · Score band "Trung bình" · action-needed banners
- **Error (#D7372C)** — Attendance "Absent" · Score band "Yếu" · validation errors, schedule conflicts

Course "Completed" is deliberately **not** a semantic color — it uses neutral `ink-subtle` on `surface-2`, since finishing a course is neither good nor bad news, just closed. Reusing coral for "Completed" would blur the line between brand-action and status, which is why it's kept out of the status vocabulary entirely.

## 3. Typography
TSMS pairs the rounded geometric display face (GT Walsheim, Inter fallback) for headings with Inter for all body and UI text — the same pairing as the source system, since the goal (friendly headlines, crisply legible dense tables) applies just as much to a Course Grid as to a payroll table.

Display scale (page titles): 36–44px, weight 700. Headings within flows: 22px weight 600. Body text: 16px / 1.6 — deliberately larger than typical SaaS, which matters here because a meaningful share of TSMS's reading is a Student parsing their own grade or attendance breakdown under real stress.

**Monospace (Roboto Mono) is reserved for figures that must be read precisely and compared across rows**: grades (0–10, one decimal), attendance rates (%), Student/Lecturer IDs, and session dates in schedule tables. Tabular numerals keep a column of scores or attendance percentages perfectly aligned — critical when a Lecturer is scanning a Grading table for outliers, or an Admin is comparing enrollment counts across a Course Statistics table.

## 4. Components & Patterns
**Course Grid (Dashboard)**:
- Table with soft row dividers, 56px row height
- Status pill (Upcoming / Active / Completed) using the semantic mapping above
- Search bar + status filter above the table, coral "Create Course" button top-right (Admin only)

**Course Detail**:
- White card on cream: course info, capacity shown as `enrolledCount / maxCapacity` in mono
- ClassSession list below, each session a soft row with day/type and edit/delete affordances (Admin only, disabled once the session has occurred)

**Grading table**:
- Student list with inline grade input (0–10), mono numerals, right-aligned
- Unsaved edits show a subtle coral dot until saved; saved rows settle back to plain ink
- No celebratory animation on save — grading is a routine action, not a milestone

**Attendance marking**:
- Per-student row with a 3-state segmented control: Present (green) / Absent (red) / Excused (amber)
- Status is never color-only — each state also shows as a short text label inside the control, not just a color chip
- Bulk-safe: each row saves independently with its own small loading state (no page-level save button)

**Enrollment modal**:
- Two-column session picker (available ClassSessions for the course), coral border + check when selected
- Capacity shown as a soft progress bar (`enrolledCount / maxCapacity`), turning amber past 80% full
- Friendly inline error, never a raw error code, when a session conflicts with an existing enrollment

**Course Report (Statistics + Grid)**:
- Bar chart (enrollment count, average score per course) in coral/green on cream, axis labels in muted ink
- Pie chart for Score Distribution using the 4-band semantic mapping (success → info → warning → error), each slice labeled directly rather than relying on a separate legend
- Empty state ("Chưa có dữ liệu") when a course has no graded students yet — text only, no illustration; this is a working report, not a marketing empty state

**Schedule timeline**:
- Weekly calendar view, coral-highlighted "today" column
- Each session block shows course name, time, and — for Students — attendance status as a small colored dot (green/red/amber) once marked

**Real-time grade notification**:
- Soft-fill toast, green accent, slides in from bottom with the spring easing when a grade updates live (SignalR) while a Student has My Courses or Personal Summary open
- Auto-dismisses after a few seconds, dismissible early — this is the one moment in TSMS that's allowed to feel like a small reward, since it's the direct payoff of the real-time feature, not decoration bolted onto a routine save

**User Management table (Admin)**:
- Same table pattern as Course Grid, Role and Active/Inactive shown as pills
- CSV import result shown as a small table of row-level errors, not a single error banner — an Admin needs to see exactly which rows failed and why

## 5. Spacing & Layout
TSMS uses the same 8px base grid and generous scale as the source system: card padding 24px, sections separated by 32–48px, centered content column for flows (~960px), wider for tables and reports. Form fields stay tall (48px) for comfortable entry — this matters as much for an Admin doing rapid data entry as for a Student on a phone checking grades between classes.

**App shell navigation reflects the actual TSMS Menu structure**, grouped by Role rather than generic job-categories:
- **Admin**: Dashboard (Course Grid), Users, Report
- **Lecturer**: Dashboard (their Courses), Grading, Attendance, Schedule, Report (Attendance tab only)
- **Student**: Available Courses, My Courses, Schedule, Personal Summary

Modals use 20px radius and 32px internal padding, centered over a softly darkened cream scrim. Tables use 56px row heights with hover rows in #FBEDE4.

## 6. Motion & Interaction
**Spring-eased interactions**: Buttons and tiles use the same gentle overshoot easing (`cubic-bezier(0.34, 1.56, 0.64, 1)`) so presses feel alive rather than mechanical.

**Real-time grade toast**: The one celebratory-feeling moment in the system (see Components) — deliberately singular, so it stays meaningful instead of becoming background noise. No confetti; TSMS is used daily and by obligation, not opted into for delight.

**Hover lift**: Table rows and cards lift subtly (2px translate + softened shadow) at 220ms on hover, reinforcing that rows are clickable (opens Course Detail, Student Detail, etc).

**Toast notifications**: Slide in from bottom with the spring easing, auto-dismiss after a few seconds — used for save confirmations, errors, and the real-time grade update.

**Deliberately removed from the source system**: confetti bursts and full-screen celebration states. An internal academic tool used under obligation should never simulate excitement its users don't feel — it reads as tone-deaf rather than warm.

## Accessibility

### Contrast Ratios
- **#1C1B1A ink on #FFF7F0 canvas**: 15.8:1 — passes AAA
- **#1C1B1A ink on #FFFFFF surface**: 17.4:1 — passes AAA
- **#5C5854 muted on #FFF7F0**: 6.6:1 — passes AA
- **#8A847E subtle on #FFF7F0**: 3.6:1 — fails AA for body text; use only for large/non-essential text (timestamps, placeholders)
- **#FFFFFF on #F45D48 coral**: 3.3:1 — fails AA for small text; coral buttons must use 16px+ semibold white labels, or the #E04A36 hover tone
- **#FFFFFF on #1E875F green**: 4.9:1 — passes AA
- **#1E875F green on #FFF7F0**: 4.6:1 — passes AA (acceptable for text and icons)
- **#D7372C error on #FFF7F0**: 4.8:1 — passes AA

### Minimum Requirements
- **Touch target**: 44×44px minimum; form fields run 48px tall — Students frequently check TSMS on mobile between classes
- **Focus indicator**: 3px coral focus ring (`rgba(244,93,72,0.30)`) with sufficient offset — visible on both cream and white surfaces
- **Status never color-only**: Course status, Attendance status, and Score bands always pair color with a text label — this is non-negotiable given how much of TSMS's information is a status pill
- **Grade and attendance-rate fields**: monospace tabular numerals so a Lecturer scanning a column of scores isn't relying on visual alignment alone to catch an outlier

### Motion
- Respects `prefers-reduced-motion`: yes — spring overshoot and hover lifts reduce to simple fades; the real-time grade toast still appears, just without the spring entrance
- State changes (grade saved, attendance marked) still update visibly under reduced motion, without relying on animation to communicate success

### Notes
- White text on coral (#F45D48) is borderline (3.3:1) — never use coral as a background for small text
- The cream canvas (#FFF7F0) reduces overall contrast headroom versus pure white — verify any new muted grays against cream, not white
- Green (#1E875F) is the only color permitted to signal "good status" (Active / Present / confirmed / top score band) — do not substitute coral, since coral is reserved for actions, not outcomes. Mixing the two would make a Student unable to tell, at a glance, whether a coral element is something to click or something to feel good about.
- Plain-language microcopy matters here too: a Student reading "Bạn chưa có điểm cho môn này" is calmer than reading a raw null or an empty cell — treat empty and zero states as design material, not an afterthought
