# Microsoft Agent Framework — Course Outline

A 10-lesson progressive course learning MAF through a C# TUI application, with all examples themed around the Vector Technologies plastics manufacturing ERP.

## Tech Stack
- .NET 10 / C#
- Microsoft Agent Framework (RC4)
- Terminal.Gui v2 (TUI)
- Ollama (local LLM, upgradable to Azure OpenAI)

## Model Selection
- **Lessons 1-6:** `qwen3:8b` — fast, good tool calling support, sufficient for single-agent and structured workflows
- **Lesson 7+:** Review model selection before starting. Handoff routing and group chat require stronger instruction following. If agents misroute or talk past each other, upgrade to `qwen2.5:32b` (needs ~20GB RAM, comfortable on 48GB MacBook Pro)
- Swap is a one-line change in the agent constructor or via `OLLAMA_MODEL` env var

## Lessons

### Tier 1 — Foundation
| Lesson | Title | MAF Features | Vector Scenario |
|--------|-------|--------------|-----------------|
| 1 | Single Agent + TUI Shell | Agent creation, streaming, sessions | Production floor assistant |
| 2 | Tool-Using Agent | Function calling, structured output | Query stock levels, resin specs, machine schedules |

### Tier 2 — Enterprise Features
| Lesson | Title | MAF Features | Vector Scenario |
|--------|-------|--------------|-----------------|
| 3 | MCP Integration | MCP tool servers | Supplier feeds, MSDS, document retrieval |
| 4 | Middleware & Context Providers | Middleware pipeline, context injection | Audit logging, role-based data filtering |

### Tier 3 — Multi-Agent Orchestration
| Lesson | Title | MAF Features | Vector Scenario |
|--------|-------|--------------|-----------------|
| 5 | Sequential Workflow | Sequential orchestration | Order → Costing → Planning → Material Check |
| 6 | Concurrent Workflows | Fan-out/fan-in | Check inventory + machines + suppliers in parallel |
| 7 | Handoff Pattern | Conditional routing | Inquiry triage: Sales / Production / QA / Purchasing |
| 8 | Group Chat | Multi-agent conversation, custom managers | Production planning meeting |

### Tier 4 — Advanced & Production
| Lesson | Title | MAF Features | Vector Scenario |
|--------|-------|--------------|-----------------|
| 9 | Graph-Based Workflows | DAG workflows, checkpointing, human-in-the-loop | Order-to-delivery pipeline with approval gates |
| 10 | Observability & Production | OpenTelemetry, Aspire, retries, LLM provider swap | Production hardening, chaos scenarios |

## How to use this course
- Each lesson has its own doc in this folder (`01-foundation.md`, `02-tool-agent.md`, etc.)
- Each doc contains: architecture diagrams, key concepts, code walkthrough with file references, and a **Key Learnings** section summarising what to take away
- Code lives in `src/AgentExplorer/Agents/L01_Foundation/`, `L02_ToolAgent/`, etc.
- Run any lesson: `dotnet run` from `src/AgentExplorer/` — use the tab selector in the TUI
