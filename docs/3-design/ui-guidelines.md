# SILA UI/UX Guidelines

**Task:** T1.3 · **Status:** design baseline · **Audience:** T2.3, T3.1, T3.4, T4.3

## Product feel

SILA is a calm, trustworthy B2B workspace for Egypt’s fashion supply chain. Use clear language, visible next steps, and useful record details. The shared UI supports English first, with labels kept short so an Arabic localization can be added without changing the page structure.

## Color tokens

| Token | Value | Use |
|---|---|---|
| Forest | `#153D35` | Primary action, brand, active navigation |
| Forest dark | `#102E29` | Hover and pressed primary actions |
| Clay | `#C66B4E` | Small attention accents and unread notification dot |
| Sand | `#F5F1E9` | Subtle surfaces and neutral status background |
| Paper | `#FFFDFA` | Page background |
| Ink | `#202824` | Main text |
| Muted | `#68736E` | Supporting text |
| Line | `#E1E6E2` | Borders and dividers |
| Focus | `#1769AA` | Keyboard focus ring |

Do not communicate state by color alone. Every status badge displays the exact enum member name (for example `PendingReview`); pass the enum value to `_StatusBadge` rather than duplicating status strings in views.

## Type and spacing

- Use the system sans-serif stack: Segoe UI, Tahoma, Arial, sans-serif.
- Body text is 16px with 1.55 line-height. Use 12–14px only for metadata and helper text.
- Use a 4px base spacing rhythm; common gaps are 8, 12, 16, 24, 32 and 48px.
- Content width follows Bootstrap’s responsive container. Keep forms readable and usually no wider than 48rem.
- Cards use a 16px radius, a fine border and a soft shadow. Prefer whitespace over extra divider lines.

## Components and actions

- Primary button: one main action per page/form, forest fill, white text. Name the action (“Send request”, “Confirm order”).
- Secondary action: outlined forest. Destructive actions use a clearly named danger style and confirmation when irreversible.
- Inputs: visible labels above controls; show units and examples in helper text. Never rely on placeholder text as a label.
- Status badges: neutral sand surface by default, readable text, exact enum name. Keep Manufacturing Request and Material Order statuses in their own context.
- Empty states explain why the list is empty and offer a relevant next step when the role can act.
- Lists show the record name, counterparty, date, total/quantity where relevant, status and a clear details link.
- Validation summary appears at the top of the form; field-level errors sit next to the relevant input and state what to correct.

## Accessibility and responsive behavior

- Use semantic landmarks, one page-level heading, visible labels, table headers, and descriptive link/button names.
- All functionality must work by keyboard. Keep a visible 3px focus ring and a skip link to main content.
- Maintain readable contrast; pair color with text or an icon/label.
- At 1920×1080 and 1366×768, use a centered content container and avoid excessive line length. At 768×1024, let navigation collapse and stack form columns/cards. Keep ordinary forms free from horizontal scrolling.
- Respect `prefers-reduced-motion`; do not rely on animation to communicate a change.

## Shared partial contracts

- `_StatusBadge`: model is the enum value; its rendered text must match the enum member exactly.
- `_Pagination`: `ViewData["CurrentPage"]`, `ViewData["TotalPages"]`, optional `PageController`, `PageAction`, and `PageRouteValues`.
- `_ValidationSummary`: render inside a form; field messages remain beside their fields.
- `_NotificationBell`: placeholder in T1.3; TODO(T2.4) to connect the unread count.

## Navigation map

- Visitor: Home, Browse materials, Sign in, Create account. Home links to Browse Suppliers and Browse Factories.
- Brand Owner: Dashboard, Materials, Requests, Notifications, Profile, Sign out.
- Factory Owner (`Factory` role): Dashboard, Materials, Requests, Notifications, Profile, Sign out.
- Supplier: Dashboard, My listings, Requests, Notifications, Profile, Sign out.
- Admin: Dashboard, Requests/Notifications if authorized by the implementation, Verification review, Profile, Sign out. Admin is for verification review only; no Admin registration is offered.

Route/controller names in the shared layout are integration targets for T2.3 and T3.x. Add corresponding endpoints in the owning task; do not move business rules into the UI.
