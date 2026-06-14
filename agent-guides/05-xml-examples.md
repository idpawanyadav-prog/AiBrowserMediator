# XML Examples

## Basic XML format

```xml
<?xml version="1.0" encoding="utf-8"?>
<Workflow sessionId="S1">
  <Step id="1" action="navigate" url="https://www.selenium.dev/selenium/web/web-form.html" timeout="20" controlType="generic" />

  <Step id="2" action="update" timeout="20" controlType="textbox">
    <Locator type="id" value="my-text-id" strategy="TextBox.IdStrategy" functionId="TextBox.Id.SetValueByClearAndSendKeys" />
    <Value>Text1</Value>
  </Step>
</Workflow>
```

## Selenium web form reusable XML

```xml
<?xml version="1.0" encoding="utf-8"?>
<Workflow sessionId="SeleniumWebForm">
  <Step id="1" action="navigate" url="https://www.selenium.dev/selenium/web/web-form.html" timeout="20" controlType="generic" />

  <Step id="2" action="update" timeout="20" controlType="textbox">
    <Locator type="id" value="my-text-id" strategy="TextBox.IdStrategy" functionId="TextBox.Id.SetValueByClearAndSendKeys" />
    <Value>Text1</Value>
  </Step>

  <Step id="3" action="update" timeout="20" controlType="textbox">
    <Locator type="name" value="my-password" strategy="TextBox.NameStrategy" functionId="TextBox.Name.SetValueByClearAndSendKeys" />
    <Value>Test2</Value>
  </Step>

  <Step id="4" action="update" timeout="20" controlType="textbox">
    <Locator type="name" value="my-textarea" strategy="TextBox.NameStrategy" functionId="TextBox.Name.SetValueByClearAndSendKeys" />
    <Value>This is test area</Value>
  </Step>

  <Step id="5" action="update" timeout="20" controlType="combobox">
    <Locator type="name" value="my-select" strategy="ComboBox.NameStrategy" functionId="ComboBox.Name.SelectByTextOrValue" />
    <Value>One</Value>
  </Step>

  <Step id="6" action="update" timeout="20" controlType="textbox">
    <Locator type="name" value="my-datalist" strategy="TextBox.NameStrategy" functionId="TextBox.Name.SetValueByClearAndSendKeys" />
    <Value>New York</Value>
  </Step>
</Workflow>
```
