# Skipped Controls

Use skipped steps when a control must be represented in XML but should not execute.

## XML format

```xml
<Step id="10" action="click" timeout="20" controlType="button" skipExecution="true" skipReason="submit control intentionally not clicked">
  <Locator type="id" value="submitButton" strategy="Button.IdStrategy" functionId="Button.Id.ClickByFindElement" />
</Step>
```

## Use skipExecution for

- disabled controls
- readonly controls
- submit buttons not explicitly requested
- file upload without a real local path
- redirecting dropdowns
- destructive buttons

## Rule

Do not silently omit special controls. Represent them and explain why they are skipped.
