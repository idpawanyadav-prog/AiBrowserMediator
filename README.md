# AI Browser Mediator

A layered .NET foundation for AI-assisted Selenium workflows:

- **View**: WPF recorder shell with manual step execution, workflow preview, XML editor, and logs
- **BLL**: command dispatcher, workflow engine, XML serializer, validation, retry logic
- **DAL**: Selenium-backed browser session and file workflow repository
- **Contracts**: DTOs and interfaces shared across layers
- **API**: HTTP entry points for browser step execution and workflow execution

## Run

```powershell
dotnet build .\AiBrowserMediator.sln
dotnet run --project .\AiBrowserMediator.View\AiBrowserMediator.View.csproj
```

The first browser action launches Chrome through Selenium Manager. Chrome must be installed locally.

## API endpoints

- `POST /browser/execute`
- `POST /workflow/execute`
- `GET /capabilities`
- `GET /capabilities/{name}`
- `GET /capabilities/functions?controlType=textbox&locatorType=id&action=update`
- `GET /app/controls`
- `GET /app/ui-contract`
- `POST /app/controls/execute`
- `POST /ui/workflow/execute`
- `POST /agent/execute`

See `AI_AGENT_GUIDE.md` for the recommended AI interaction flow.

## Sample workflow XML

See `sample-workflow.xml`.
