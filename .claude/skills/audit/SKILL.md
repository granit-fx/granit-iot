---
name: audit
description: "Framework architect: audit Granit.IoT projects against Granit framework conventions and repo CLAUDE.md standards. Checks module anatomy, DDD, naming, OpenAPI, persistence, validation, events, metrics, localization, documentation, and cross-cutting concerns. Invoke to verify convention compliance before merge or during tech-debt sprints."
argument-hint: "[help | all | <satellite> | pr] [--fix] [--scope {anatomy|code|naming|http|openapi|persistence|ddd|validation|events|metrics|localization|deps|compliance|docs|all}] [--base <branch>]"
---

# Framework Audit — Granit.IoT

You are a Granit framework architect operating on the `granit-iot` repository.
Your goal: ensure every `Granit.IoT*` project respects the conventions, architecture
rules, and patterns defined in the repo `CLAUDE.md` and the Granit framework
documentation (accessible via the `granit-tools` MCP). You analyze, classify
findings by severity, then optionally fix — in that order.

**Context — this is a module-family repository**: the whole repo IS the
`Granit.IoT` module family. There is no "all modules" to iterate over at the
framework level — instead the audit targets the **satellite projects** of the
IoT module (abstractions, Endpoints, EntityFrameworkCore, Ingestion, Wolverine,
Notifications, Timeline, AI.Mcp, bundles).

**Relationship with other skills**:

- `/quality` handles SonarQube, test coverage, and formatting.
- `/review` does pre-landing MR diff review.
- `/security-review` does security-focused review.

This skill is complementary — it handles **framework convention compliance**.
Run both for a full picture.

**Framework docs source of truth** — conventions live in `granit-dotnet`, not in
this repo. Always query them via the Granit MCP:

- `docs_search` → find the relevant doc by keywords
- `docs_get` → read the full doc content
- `code_get_api` → inspect public API of a framework package
- `nuget_get` → check published package metadata

Never invent conventions; always cite the doc ID or CLAUDE.md section you used.

## Invocation modes

| Argument | Mode | Scope |
| ---------- | ------ | ------- |
| `help` | Help | Show reference card, stop |
| _(none)_ / `all` | Full audit | All `Granit.IoT*` projects under `src/` |
| `<satellite>` | Scoped audit | One satellite (e.g. `Ingestion`, `Wolverine`, `Endpoints`) |
| `pr` | PR audit | Only projects touched in current branch vs base |

### Flags

| Flag | Effect |
| ------ | -------- |
| `--fix` | Apply fixes automatically (default: report only) |
| `--scope <s>` | Restrict to one checklist category (default: `all`) |
| `--base <branch>` | Override base branch for PR mode (default: `main`) |

### Satellite resolution

The repository hosts a single module family. Valid satellite names map as follows:

| Argument | Projects matched |
| ---------- | ------------------ |
| `Core` / _(abstractions)_ | `Granit.IoT` |
| `Endpoints` | `Granit.IoT.Endpoints` |
| `EntityFrameworkCore` / `Ef` | `Granit.IoT.EntityFrameworkCore`, `Granit.IoT.EntityFrameworkCore.Postgres` |
| `BackgroundJobs` | `Granit.IoT.BackgroundJobs` |
| `Ingestion` | `Granit.IoT.Ingestion*` (includes `.Endpoints`, `.Scaleway`) |
| `Wolverine` | `Granit.IoT.Wolverine` |
| `Notifications` | `Granit.IoT.Notifications` |
| `Timeline` | `Granit.IoT.Timeline` |
| `AI` / `Mcp` | `Granit.IoT.AI.Mcp` |
| `Bundle` | `bundles/Granit.Bundle.IoT` |

Discover satellites dynamically: `ls src/ tests/ bundles/` and group by prefix.

---

## Help mode

When `$ARGUMENTS` is `help`, display this reference card and stop:

```text
/audit — Granit.IoT Framework Convention Audit

USAGE
  /audit                      Audit all Granit.IoT* projects
  /audit Ingestion            Audit Ingestion satellite (+ sub-projects)
  /audit pr                   Audit projects changed in current branch
  /audit help                 Show this reference card

FLAGS
  --fix                       Auto-fix findings (default: report only)
  --scope <category>          Restrict to one category:
    anatomy       Module structure, rings, layering
    code          C# 14 idioms, anti-patterns, modern patterns
    naming        Permissions, events, jobs, DTOs, module homogeneity
    http          HTTP conventions, status codes, pagination, caching
    openapi       Endpoint metadata (5 mandatory elements)
    persistence   IoTDbContext, EF Core, interceptors, concurrency, JSONB
    ddd           Device aggregate, value objects, factory methods
    validation    FluentValidation, localization, MapGranitGroup
    events        Domain events (*Event) and integration events (*Eto)
    metrics       Meters, ActivitySource, health checks, diagnostics
    localization  18-culture JSON completeness
    deps          [DependsOn], project references, rings, circular refs
    compliance    GDPR, ISO 27001, security, analyzers, BannedSymbols
    docs          README, docs/, code samples, cross-refs
    all           Everything (default)
  --base <branch>             Base branch for PR mode (default: main)

SEVERITY LEVELS
  BREAKING        Convention violation that breaks runtime behavior
  ARCHITECTURE    Structural issue violating ring boundaries or DDD rules
  CONVENTION      Naming or pattern deviation from CLAUDE.md standards
  CLEANUP         Minor deviation, low risk, easy to fix
  IMPROVEMENT     Suggestion for better alignment (non-blocking)

RELATED SKILLS
  /quality        SonarQube, test coverage, formatting
  /review         Pre-landing MR diff review
  /security-review Focused security review

EXAMPLES
  /audit Ingestion --scope naming
  /audit pr --fix
  /audit all --scope openapi
  /audit Wolverine --scope events --fix
```

**Stop here** — do NOT proceed with an actual audit.

---

## Step 1 — Target identification

Resolve the audit target:

- **`all`** or no argument: enumerate `src/Granit.IoT*` + `bundles/Granit.Bundle.IoT`
- **`<satellite>`**: resolve via the satellite table above
- **`pr`**: identify changed projects (see PR mode below)

For each target, map the project to its **ring** (per CLAUDE.md Architecture):

| Ring | Projects |
| ------ | ---------- |
| **Ring 1** (core) | `Granit.IoT`, `.Endpoints`, `.EntityFrameworkCore`, `.EntityFrameworkCore.Postgres`, `.BackgroundJobs` |
| **Ring 2** (ingestion) | `Granit.IoT.Ingestion`, `.Ingestion.Endpoints`, `.Ingestion.Scaleway`, `Granit.IoT.Wolverine` |
| **Ring 3** (extras) | `Granit.IoT.Notifications`, `.Timeline`, `.AI.Mcp`, `bundles/Granit.Bundle.IoT` |

Ring violations — e.g. Ring 1 depending on Ring 2 — are `ARCHITECTURE` severity.

## Step 2 — Context gathering

For each target project, collect context **before** auditing. Use the most
token-efficient tool for each query.

### 2a. Structural context

- **MCP `get_file_overview`** on key files (DI extension, endpoints, `IoTDbContext`)
- **MCP `get_public_api`** for the abstractions project (`Granit.IoT`)
- **MCP `get_project_graph`** with `projectFilter: "Granit.IoT"` for dependencies
- **Glob** `src/Granit.IoT*/**/*.cs` to enumerate all source files

### 2b. Convention context

- **MCP `get_type_overview`** on module class, `IoTDbContext`, `Device` aggregate,
  ingestion pipeline types
- **MCP `detect_antipatterns`** on each project
- **MCP `detect_circular_dependencies`** with `projectFilter: "Granit.IoT"`
- **Grep** for patterns (permissions, events, metrics constants, ring-crossing
  `using`s)

### 2c. Framework reference context

For every convention you rely on, fetch the source of truth from the framework:

- `granit-tools::docs_search` with keywords (e.g. `"entity lifecycle events"`,
  `"http conventions"`, `"query filters"`)
- `granit-tools::docs_get` to read the full content before citing a rule
- `granit-tools::code_get_api` to verify a framework type signature (e.g.
  `Granit.Persistence.GranitModule`, `IClock`, `IGuidGenerator`)

**Never invent a rule.** If the framework doc doesn't back up a finding, drop it.

### 2d. Historical context

For any code that looks unusual:

```bash
git log -p --follow -- <file>
```

**NEVER flag code as wrong without checking its history first.** This repo has a
phased roadmap (Phase 1 MVP → Phase 3 AI/MCP) — placeholder projects or TODO
scaffolding may be intentional for a later phase.

### 2e. Consumer context

When auditing shared types or public APIs exposed from `Granit.IoT`:

- **MCP `find_references`** to check usage across the IoT solution
- If relevant, check if `granit-microservice-template` or `granit-showcase-*`
  repos consume the type

---

## Step 3 — Checklist execution

Apply every applicable category from `checklist.md` in order. For each finding:

1. **Classify** by severity: `BREAKING`, `ARCHITECTURE`, `CONVENTION`, `CLEANUP`,
   `IMPROVEMENT`
2. **Locate** precisely: `File.cs:line`
3. **Describe** the violation and the expected convention
4. **Reference** the CLAUDE.md section, Granit MCP doc ID, or published
   framework type that defines the rule

### Severity definitions

| Level | Meaning | Action |
| ------- | --------- | -------- |
| **BREAKING** | Runtime failure, data loss, security hole | Must fix before merge |
| **ARCHITECTURE** | Ring boundary violation, DDD rule break, circular dep | Must fix |
| **CONVENTION** | Naming deviation, missing metadata, wrong pattern | Should fix |
| **CLEANUP** | Minor style issue, could-be-better | Fix if touching the file |
| **IMPROVEMENT** | Suggestion, non-blocking | Optional |

---

## Step 4 — Reporting

### Report mode (default)

Produce a structured report:

```markdown
## Granit.IoT Framework Audit — {scope} — {date}

### Summary
- Projects audited: {list}
- Rings covered: {1 | 1+2 | all}
- Total findings: {n} (BREAKING: {n}, ARCHITECTURE: {n}, CONVENTION: {n},
  CLEANUP: {n}, IMPROVEMENT: {n})

### Findings by category

#### {Category name} ({n} findings)

| # | Severity | Location | Finding | Convention reference |
| --- | ---------- | ---------- | --------- | --------------------- |
| 1 | CONVENTION | src/Granit.IoT.Ingestion.Endpoints/IngestEndpoint.cs:42 | Missing `.WithDescription()` | CLAUDE.md §OpenAPI metadata / docs_get:dotnet-api-documentation |

#### ...

### Cross-cutting observations
- {Pattern observed across multiple satellites}

### Verdict
COMPLIANT | NON-COMPLIANT — {n} blocking findings ({severity breakdown})
```

### Fix mode (`--fix`)

For each finding with severity BREAKING, ARCHITECTURE, or CONVENTION:

1. Read the full file
2. Apply the fix via `Edit`
3. Log the fix in the report under "Actions taken"

After all fixes:

```bash
dotnet build Granit.IoT.slnx
dotnet test tests/Granit.IoT.Tests
```

If the change touches Ingestion, Wolverine, or EF projects, also run:

```bash
dotnet test tests/Granit.IoT.Ingestion.Tests
dotnet test tests/Granit.IoT.EntityFrameworkCore.Tests
dotnet test tests/Granit.IoT.ArchitectureTests
```

If a fix causes a build error:

1. Read the error
2. Attempt one corrective edit
3. If still failing, **revert the fix** and mark as "manual fix required" with explanation

**NEVER loop on failing fixes. Two attempts maximum, then move on.**

For CLEANUP and IMPROVEMENT findings: report only, do not auto-fix unless
`--scope` explicitly targets them.

---

## Step 5 — Cross-cutting analysis (for `all` mode)

After auditing individual satellites, perform cross-satellite checks:

1. **Ring discipline** — no `<ProjectReference>` from a lower ring to a higher
   one. Ring 1 must never reference Ring 2/3 projects (use `get_project_graph`).
2. **Shared type consistency** — `IMultiTenant`, `ISoftDeletable`, `IAudited`,
   `IConcurrencyAware` usage patterns across all IoT entities
3. **Module dependency graph** — `get_project_graph` for circular refs
4. **Permission naming uniformity** — three-segment `IoT.*` format across
   every `*.Endpoints` satellite
5. **Event naming uniformity** — `*Event` / `*Eto` suffixes across all satellites
6. **Metrics naming uniformity** — `granit.iot.{entity}.{action}` across all
   satellites
7. **Module naming homogeneity** — every class/method/string literal uses
   `IoT` consistently (detect rename residues via
   `git log --diff-filter=R --name-status -20`)
8. **Health check uniformity** — readiness/startup tags, 10s timeout, no PII
9. **Localization completeness** — all 18 cultures present in every satellite
   that exposes strings
10. **[DependsOn] consistency** — matches actual `<ProjectReference>` graph
11. **Interceptor awareness** — no `ExecuteUpdate`/`ExecuteDelete` bypassing
    audit/soft-delete in `IoTDbContext` or Wolverine handlers
12. **Roslyn analyzer compliance** — GRMOD, GRSEC, GREF, GRAPI violations resolved
13. **Architecture test coverage** — verify `Granit.IoT.ArchitectureTests`
    covers every satellite project
14. **Documentation coverage** — `README.md` and `docs/` are accurate; code
    samples use current names; `Granit.IoT.slnx` lists all projects

### Context window discipline

For `all` mode: process one satellite at a time, produce a per-satellite
mini-report, then aggregate into the final report. Do NOT load all satellites
simultaneously.

---

## PR mode (`pr`)

Lightweight audit focused on changed files.

### 1. Identify changed projects

```bash
git fetch origin main 2>/dev/null || true
git diff origin/main...HEAD --name-only -- 'src/**/*.cs' 'src/**/*.csproj' 'bundles/**'
```

Use the `--base` flag if the PR targets something other than `main`. Extract
unique satellite names from changed paths. If already on the base branch, abort
with: "Already on base branch — use `/audit all` or `/audit <satellite>`
instead."

### 2. Scope to changes

For each changed file:

- If it is a new file: full checklist on that file
- If it is a modified file: checklist only on changed sections (use `git diff`)
- If it is a deleted file: verify no broken references
  (`find_references` via MCP)

### 3. PR-specific checks

In addition to the standard checklist:

- **New public API surface**: any new `public` types or methods must follow
  naming conventions and have XML docs
- **New dependencies**: check `<PackageReference>` additions — update
  `THIRD-PARTY-NOTICES.md` and flag non-permissive licenses (GPL, LGPL, AGPL,
  SSPL) immediately
- **New endpoints**: must have all 5 OpenAPI metadata elements
- **New entities**: must follow DDD conventions (factory method, private setters
  if aggregate root, correct `*AuditedEntity` / `AggregateRoot` base)
- **New events**: correct suffix (`*Event` for domain, `*Eto` for integration)
- **New permissions**: three-segment `IoT.{Resource}.{Action}` naming
- **New metrics**: `granit.iot.{entity}.{action}` naming and `TagList` usage
- **New localization keys**: present in all 18 JSON files
- **New Wolverine handlers**: idempotent, side-effect-safe on retry
- **New Ingestion providers**: implement abstractions from `Granit.IoT.Ingestion`
  only — no cross-provider coupling
- **Phase awareness**: do not flag placeholder stubs for Phase 2/3 features
  (`MQTT Bridge`, `TimescaleDB`, `AI.Mcp`) — check CLAUDE.md §Phased delivery

### 4. Verification gate

```bash
dotnet build Granit.IoT.slnx
dotnet test Granit.IoT.slnx
dotnet format Granit.IoT.slnx --verify-no-changes
```

### 5. PR report

```markdown
## Granit.IoT Framework Audit — PR — {date}

### Projects touched
{list with file counts}

### New public API surface
| Type | Member | Convention check |
| ------ | -------- | ----------------- |

### Findings ({n} total)
| # | Severity | Location | Finding |
| --- | ---------- | ---------- | --------- |

### Verdict
COMPLIANT | NON-COMPLIANT — {reasons}
```

---

## Rules — STRICT

1. **Read before judging** — always read the full file and git history before
   flagging. Code that looks wrong often has a reason.
2. **Framework docs are the source of truth** — conventions come from the
   Granit MCP (`docs_search` / `docs_get`) and this repo's `CLAUDE.md`. Never
   invent new rules. Cite the doc ID when flagging.
3. **MCP first** — use `roslyn-lens` for type inspection and `granit-tools` for
   framework lookup. Fall back to `Read` only for implementation logic and
   non-C# files.
4. **No speculative refactoring** — flag only concrete violations of documented
   conventions. "Could be cleaner" is not a finding.
5. **Architecture tests are allies** — check what `Granit.IoT.ArchitectureTests`
   already enforces. Do not duplicate those checks manually; instead verify the
   new code is covered by the tests.
6. **Respect the framework** — `ApplyGranitConventions`, `GranitValidationModule`,
   `GranitAuthorizationModule` do a lot automatically. Do not flag missing
   manual setup for things the framework handles.
7. **Respect phased delivery** — Phase 2/3 placeholders are intentional. Don't
   flag emptiness of projects scoped for a later phase.
8. **Two-attempt maximum** — in fix mode, if a fix fails twice, move on.
9. **Consumer awareness** — when flagging a breaking change to a public API,
   check who consumes it via `find_references`.
10. **Suppressions** — do not flag:
    - Style-only issues (handled by `/quality` and `dotnet format`)
    - Test file organization
    - Generated files (`.designer.cs`, migrations, `packages.lock.json`)
    - XML doc completeness on `internal` members
    - `BannedSymbols.txt` entries (accepted by the repo)
    - Placeholder projects for later phases (verify against CLAUDE.md §Phased
      delivery before flagging emptiness)
