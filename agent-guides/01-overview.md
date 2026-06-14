# Overview

AiBrowserMediator supports two workflow modes. Load only the linked guide needed for the current task.

## IMPORTANT NOTE

Do **not** use any other app, browser, web request, external scraper, or side-channel to inspect target pages.

All page inspection must happen through AiBrowserMediator only:

- use `describePage`
- use `capturePageSource`
- use the WPF app bridge endpoints
- use the Selenium browser session managed by the app

The app is the source of truth. Do not scrape the page directly from another browser or HTTP client.

---

## Quick decision

```text
Need fastest execution, no WPF recorder UI update?
→ Use Backend Mode

Need to use the WPF app like a human and build XML visibly?
→ Use UI Mode

Need reusable XML?
→ Use Function IDs + XML Examples

Need to represent disabled/readonly/upload/submit controls safely?
→ Use Skipped Controls
```

---

## Guide navigation

| Need | Open this file |
|---|---|
| Direct backend/API execution | [02-backend-mode.md](./02-backend-mode.md) |
| Use WPF recorder like a human | [03-ui-mode.md](./03-ui-mode.md) |
| Discover and persist `functionId` | [04-function-ids.md](./04-function-ids.md) |
| Build reusable XML workflows | [05-xml-examples.md](./05-xml-examples.md) |
| Represent skipped/special controls safely | [06-skipped-controls.md](./06-skipped-controls.md) |
| Understand process visually | [07-flowcharts.md](./07-flowcharts.md) |

---

## Workflow modes

### Backend mode

Use when the agent wants fast execution without intentionally updating the WPF recorder UI.

Endpoint examples:

```http
POST http://localhost:5050/agent/execute
POST http://localhost:5050/workflow/execute
```

More details: [02-backend-mode.md](./02-backend-mode.md)

### UI mode

Use when the app must behave like a human using the WPF recorder.

Endpoint examples:

```http
GET  http://localhost:5050/app/ui-contract
POST http://localhost:5050/ui/workflow/execute
```

More details: [03-ui-mode.md](./03-ui-mode.md)

---

## Minimal agent sequence

1. `GET /capabilities`
2. Choose backend mode or UI mode
3. Discover strategy functions if creating reusable XML
4. Open browser if needed
5. Navigate through the app
6. Describe page through the app if locators are unknown
7. Execute workflow
8. Persist returned `functionId` values into XML

---

## Locator priority

1. Stable `id`
2. Stable `name`
3. Placeholder
4. Data attributes
5. Label mapping
6. CSS selector
7. Relative XPath
8. JavaScript fallback only if explicitly allowed

Avoid dynamic IDs such as:

```text
ctl00_123
a1b2c3
random_9999
```
