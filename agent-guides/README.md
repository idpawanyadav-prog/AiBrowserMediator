# AI Flow Guide Index

Load only the file needed for the current task.

## Files

1. `01-overview.md`
   - Use first. Explains the two workflow modes and minimal agent sequence.

2. `02-backend-mode.md`
   - Use when executing directly through backend APIs without updating WPF recorder UI.

3. `03-ui-mode.md`
   - Use when the agent must operate the app like a human through the WPF recorder.

4. `04-function-ids.md`
   - Use when choosing or persisting `functionId` values.

5. `05-xml-examples.md`
   - Use when creating reusable XML workflows.

6. `06-skipped-controls.md`
   - Use when disabled, readonly, upload, submit, redirect, or destructive controls must be represented safely.

7. `07-flowcharts.md`
   - Use when the agent needs visual process understanding.

## Minimal rule

Prefer stable locators and persist successful `functionId` values into XML for faster reuse.
