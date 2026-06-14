# Strategy Function IDs

Use `functionId` to skip discovery and execute a known strategy function directly.

## Rule

If `functionId` exists:

```text
Run exactly that function. No fallback.
```

If `functionId` is missing:

```text
Try matching functions by priority. Return successful functionId.
```

## Discover functions

```http
GET http://localhost:5050/capabilities/functions?controlType=textbox&locatorType=id&action=update
```

## Common function IDs

### TextBox

```text
TextBox.Id.SetValueByClearAndSendKeys
TextBox.Name.SetValueByClearAndSendKeys
TextBox.Css.SetValueByClearAndSendKeys
TextBox.XPath.SetValueByClearAndSendKeys
TextBox.Id.GetValueByAttribute
```

### ComboBox

```text
ComboBox.Id.SelectByTextOrValue
ComboBox.Name.SelectByTextOrValue
ComboBox.Css.SelectByTextOrValue
ComboBox.XPath.SelectByTextOrValue
```

### CheckBox

```text
CheckBox.Id.SetCheckedByClickWhenNeeded
CheckBox.Name.SetCheckedByClickWhenNeeded
CheckBox.Css.SetCheckedByClickWhenNeeded
CheckBox.XPath.SetCheckedByClickWhenNeeded
```

### Button

```text
Button.Id.ClickByFindElement
Button.Name.ClickByFindElement
Button.Css.ClickByFindElement
Button.XPath.ClickByFindElement
```

## XML locator with functionId

```xml
<Locator
  type="id"
  value="my-text-id"
  strategy="TextBox.IdStrategy"
  functionId="TextBox.Id.SetValueByClearAndSendKeys" />
```
