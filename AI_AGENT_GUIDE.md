# AI Agent Guide

## Goal

Use the app as a self-describing browser automation system. Do not guess the command contract when the app can describe it.

## Required sequence

1. Call `GET /capabilities`
2. Choose the smallest capability that satisfies the user request
3. Call `GET /capabilities/{name}`
4. Validate all required parameters from the returned descriptor
5. If locator choice is uncertain, call `describePage` or `capturePageSource`
6. Execute through `POST /agent/execute`
7. Use the structured response to decide the next step

The desktop app hosts the bridge at `http://localhost:5050/` while it is running.

## Two workflow execution modes

### 1. Backend mode

Use backend mode when the task should execute quickly without intentionally driving the WPF recorder UI.

```http
POST /agent/execute
POST /workflow/execute
```

### 2. UI mode

Use UI mode when the agent must operate the app like a human: populate recorder fields, run `Execute Step`, learn `functionId`, then add successful steps to XML.

```http
GET /app/ui-contract
POST /ui/workflow/execute
```

The `/app/ui-contract` endpoint explains the purpose of each recorder control and how XML maps into it.

Example:

```http
POST /ui/workflow/execute
Content-Type: application/xml
```

```xml
<Workflow sessionId="S1">
  <Step id="1" action="navigate" url="https://www.selenium.dev/selenium/web/web-form.html" timeout="20" controlType="generic" />
  <Step id="2" action="update" timeout="20" controlType="textbox">
    <Locator type="id" value="my-text-id" />
    <Value>test1</Value>
  </Step>
</Workflow>
```

If the locator does not include `functionId`, UI mode discovers it during `Execute Step`, selects it in the Function ID dropdown, and stores it when adding the step to XML.

## Locator preference

1. `id`
2. `name`
3. `css`
4. `xpath` only as fallback

## Safety rules

- Do not click submit, delete, upload, or redirecting controls unless explicitly requested
- When a control should be represented but not executed, set `skipExecution=true` and explain `skipReason`
- When a known strategy function has already worked, include `Locator.functionId` so the engine executes that exact function and skips discovery.
- Prefer `describePage` before `capturePageSource` when a compact control inventory is enough
- Prefer explicit waits after navigation or async changes
- Validate updates using the returned result

## Example flow

### 1. Discover capabilities

```http
GET /capabilities
```

### 2. Inspect navigate

```http
GET /capabilities/navigate
```

### 3. Execute navigate

```json
POST /agent/execute
{
  "id": 1,
  "action": "navigate",
  "url": "https://example.com",
  "timeoutSeconds": 20
}
```

### 3a. Open the browser first when needed

```json
POST /agent/execute
{
  "id": 0,
  "action": "openBrowser"
}
```

### 4. Describe page

```json
POST /agent/execute
{
  "id": 2,
  "action": "describePage"
}
```

### 5. Update a textbox

```json
POST /agent/execute
{
  "id": 3,
  "action": "update",
  "controlType": "textbox",
  "locator": {
    "type": "id",
    "value": "username",
    "strategy": "TextBox.IdStrategy",
    "functionId": "TextBox.Id.SetValueByClearAndSendKeys"
  },
  "value": "admin"
}
```

If `functionId` is omitted, the app tries matching strategy functions in priority order. The response includes the successful `functionId`, which should be persisted into future XML for faster replay.

### 6. Discover available strategy functions

```http
GET /capabilities/functions?controlType=textbox&locatorType=id&action=update
```

Example XML using a known function:

```xml
<Step id="1" action="update" timeout="20" controlType="textbox">
  <Locator
    type="id"
    value="my-text-id"
    strategy="TextBox.IdStrategy"
    functionId="TextBox.Id.SetValueByClearAndSendKeys" />
  <Value>test1</Value>
</Step>
```

## Representing special controls safely

```xml
<Step id="20"
      action="update"
      timeout="20"
      controlType="textbox"
      skipExecution="true"
      skipReason="readonly control cannot be edited">
  <Locator type="name" value="readonly" />
  <Value>sample</Value>
</Step>
```
