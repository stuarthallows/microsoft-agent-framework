# Learning Microsoft Agent Framework

> **Microsoft Agent Framework (MAF)** is an open-source framework for building, orchestrating, and deploying AI agents and multi-agent workflows. It combines AutoGen's simple agent abstractions with Semantic Kernel's enterprise features — session-based state management, type safety, middleware, telemetry — and adds graph-based workflows for explicit multi-agent orchestration. MAF supports both .NET and Python with consistent APIs.
>
> [GitHub](https://github.com/microsoft/agent-framework) | [Documentation](https://learn.microsoft.com/en-us/agent-framework/overview/) | [NuGet](https://www.nuget.org/profiles/MicrosoftAgentFramework)

## What is this repo?

A **10-lesson progressive course** for learning MAF through a C# Terminal UI (TUI) application. All examples are themed around a real-world domain: an ERP system for a plastics manufacturing company.

The TUI — built with [Terminal.Gui v2](https://gui-cs.github.io/Terminal.Gui/) — acts as a visual sandbox where you can watch agents chat, call tools, hand off to each other, and collaborate in group conversations, all from your terminal.

## Quick start

```bash
# Prerequisites
brew install ollama
ollama pull qwen3:8b
ollama serve

# Run
dotnet run --project src/AgentExplorer
```

## Tech stack

| Component | Version |
|-----------|---------|
| .NET | 10 |
| Microsoft Agent Framework | RC4 (`Microsoft.Agents.AI`) |
| Terminal.Gui | v2 (beta) |
| LLM | Ollama locally (`qwen3:8b`), upgradable to Azure OpenAI |

## Course outline

### Tier 1 — Foundation
| Lesson | Title | MAF Features | Scenario |
|--------|-------|--------------|----------|
| 1 | Single Agent + TUI Shell | Agent creation, streaming, sessions | Production floor assistant |
| 2 | Tool-Using Agent | Function calling, structured output | Query stock levels, resin specs, machine schedules |

### Tier 2 — Enterprise Features
| Lesson | Title | MAF Features | Scenario |
|--------|-------|--------------|----------|
| 3 | MCP Integration | MCP tool servers | Supplier feeds, MSDS, document retrieval |
| 4 | Middleware & Context Providers | Middleware pipeline, context injection | Audit logging, role-based data filtering |

### Tier 3 — Multi-Agent Orchestration
| Lesson | Title | MAF Features | Scenario |
|--------|-------|--------------|----------|
| 5 | Sequential Workflow | Sequential orchestration | Order -> Costing -> Planning -> Material Check |
| 6 | Concurrent Workflows | Fan-out/fan-in | Check inventory + machines + suppliers in parallel |
| 7 | Handoff Pattern | Conditional routing | Inquiry triage: Sales / Production / QA / Purchasing |
| 8 | Group Chat | Multi-agent conversation, custom managers | Production planning meeting |

### Tier 4 — Advanced & Production
| Lesson | Title | MAF Features | Scenario |
|--------|-------|--------------|----------|
| 9 | Graph-Based Workflows | DAG workflows, checkpointing, human-in-the-loop | Order-to-delivery pipeline with approval gates |
| 10 | Observability & Production | OpenTelemetry, Aspire, retries, LLM provider swap | Production hardening, chaos scenarios |

## Lesson docs

Each lesson has detailed documentation in [`docs/lessons/`](docs/lessons/), including architecture diagrams, key concepts, code walkthroughs with file references, and a **Key Learnings** section.

## Project structure

```
src/AgentExplorer/
  Program.cs                        # Entry point, theme config
  Views/                            # Terminal.Gui TUI screens
  Agents/
    L01_Foundation/                  # Lesson 1: single agent
    L02_ToolAgent/                   # Lesson 2: function calling
    ...                              # One folder per lesson
  MockData/                          # Simulated ERP data
  Shared/                            # Common utilities
docs/
  lessons/                           # Course documentation
  user-stories/                      # Vector Technologies ERP user stories
```

## Model selection

- **Lessons 1-6:** `qwen3:8b` — fast, good tool calling, sufficient for structured workflows
- **Lesson 7+:** Review before starting — handoff routing and group chat may need `qwen2.5:32b` for stronger instruction following
- Swap via `OLLAMA_MODEL` env var or one-line code change

## MAF resources

- [MAF GitHub Repository](https://github.com/microsoft/agent-framework)
- [MAF Overview](https://learn.microsoft.com/en-us/agent-framework/overview/)
- [MAF Release Candidate Blog Post](https://devblogs.microsoft.com/foundry/microsoft-agent-framework-reaches-release-candidate/)
- [Your First Agent (Quick Start)](https://learn.microsoft.com/en-us/agent-framework/get-started/your-first-agent)
- [Using Ollama as a Provider](https://learn.microsoft.com/en-us/agent-framework/agents/providers/ollama)
- [Workflow Orchestrations](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/orchestrations/overview)
- [Handoff Orchestration](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/orchestrations/handoff)
- [Group Chat Orchestration](https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/group-chat)
- [MCP Tool Integration](https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools)
- [Observability for Multi-Agent Systems](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/observability-for-multi-agent-systems-with-microsoft-agent-framework-and-azure-a/4469090)
- [Real-world Example with MCP + Aspire](https://developer.microsoft.com/blog/build-a-real-world-example-with-microsoft-agent-framework-microsoft-foundry-mcp-and-aspire)
