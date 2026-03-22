# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build AgentExplorer.slnx
dotnet run --project src/AgentExplorer
```

Requires Ollama running locally with `qwen3:8b` model.

## Key Constraints

- **One commit per lesson** with message format: `Lesson N: <title>`
- **Never put package versions in .csproj** — versions are centrally managed in `Directory.Packages.props`
- **Terminal.Gui v2 is not thread-safe** — all UI updates from background threads must use `App.Invoke()`
- **Temperature must be 0** on all agents for deterministic responses
- **All examples must use the Vector Technologies plastics manufacturing domain** — see `docs/user-stories/` for real ERP user stories to draw from
- Write all code fully — no TODO(human) placeholders or "learn by doing" exercises
- Lesson docs go in `docs/lessons/NN-name.md` and must include a **Key Learnings** section with `file:line` references

## Non-Obvious Gotchas

- `AsAIAgent()` extension method requires `using Microsoft.Extensions.AI` (not `Microsoft.Agents.AI`)
- Terminal.Gui v2 `Tab` uses property initialisers (`new Tab { DisplayText = "...", View = ... }`), not constructor args
- `TabView.AddTab` requires the `andSelect` parameter explicitly — it's not optional
- Parent views must have `CanFocus = true` for child views to receive keyboard focus
