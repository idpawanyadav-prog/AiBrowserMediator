# UI Mode: Use App Like a Human

Use UI mode when the agent must drive the WPF recorder workflow visibly.

## Inspect UI contract

```http
GET http://localhost:5050/app/ui-contract
```

This explains recorder controls:

- Action
- Timeout Seconds
- URL
- Selector Type
- Control Type
- Selector
- Value / Expected Text
- Function ID
- Execute Step
- Add To XML

## Execute XML through UI

```http
POST http://localhost:5050/ui/workflow/execute
Content-Type: application/xml
```

Body:

```xml
<Workflow sessionId="S1">
  <Step id="1" action="navigate" url="https://www.selenium.dev/selenium/web/web-form.html" timeout="20" controlType="generic" />
  <Step id="2" action="update" timeout="20" controlType="textbox">
    <Locator type="id" value="my-text-id" />
    <Value>Text1</Value>
  </Step>
</Workflow>
```

## UI mode behavior

The app will:

1. Clear existing recorder flow
2. Apply each XML step to recorder fields
3. Execute step
4. Learn successful `functionId` if missing
5. Select that `functionId` in the dropdown
6. Add successful step to XML
7. Return execution history
