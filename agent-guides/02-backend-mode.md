# Backend Mode

Use backend mode when no WPF recorder UI update is required.

## Open browser

```json
POST http://localhost:5050/agent/execute
{
  "id": 0,
  "action": "openBrowser"
}
```

## Navigate

```json
POST http://localhost:5050/agent/execute
{
  "id": 1,
  "action": "navigate",
  "url": "https://www.selenium.dev/selenium/web/web-form.html",
  "timeoutSeconds": 20,
  "controlType": "generic"
}
```

## Update textbox

```json
POST http://localhost:5050/agent/execute
{
  "id": 2,
  "action": "update",
  "controlType": "textbox",
  "locator": {
    "type": "id",
    "value": "my-text-id",
    "strategy": "TextBox.IdStrategy",
    "functionId": "TextBox.Id.SetValueByClearAndSendKeys"
  },
  "value": "Text1"
}
```

## Describe page

Use when locators are unknown.

```json
POST http://localhost:5050/agent/execute
{
  "id": 3,
  "action": "describePage"
}
```

## Expected response

```json
{
  "success": true,
  "strategy": "TextBox.IdStrategy",
  "functionId": "TextBox.Id.SetValueByClearAndSendKeys"
}
```
