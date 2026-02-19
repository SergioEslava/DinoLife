# Technical Decisions (ADRs)

Architecture Decision Records for DinoLife v1.0.

## ADR-001: Data-Oriented Hybrid Architecture

- Status: Accepted
- Date: 2026-02-04
- Decision: store simulation data in parallel arrays with stateless systems.
- Why:
  - high cache locality
  - simple, explicit memory layout
  - easier performance profiling than deep object graphs

## ADR-002: JSON Persistence

- Status: Accepted
- Date: 2026-02-04
- Decision: use `System.Text.Json` for world save/load.
- Why:
  - readable save files
  - zero extra runtime dependency
  - schema evolution is manageable in v1 scope

## ADR-003: Terminal Rendering with Internal TUI Layer

- Status: Accepted
- Date: 2026-02-04 (updated 2026-02-19)
- Decision:
  - keep direct console renderer (`TerminalRenderer`) with double-buffer diff
  - add internal overlay-based TUI layer (`TerminalGuiRenderer`)
- Why:
  - preserves rendering control and performance
  - enables richer in-game UI without external TUI dependency
  - supports gradual UI evolution through overlays

## ADR-004: Fixed 60 TPS Simulation

- Status: Accepted
- Date: 2026-02-04
- Decision: run simulation updates at fixed 60 ticks/sec.
- Why:
  - deterministic behavior
  - consistent cross-machine simulation progression
  - test-friendly timing model

## ADR-005: Lightweight Stat Variation

- Status: Accepted
- Date: 2026-02-04
- Decision: keep non-genetic stat variation in reproduction path.
- Why:
  - enough diversity for emergent behavior in v1
  - lower implementation complexity

## ADR-006: EntityType + ComponentFlags (No Inheritance Tree)

- Status: Accepted
- Date: 2026-02-04
- Decision: represent entity behavior via type enum and component flags.
- Why:
  - avoids polymorphic dispatch overhead
  - aligns with data-oriented systems

## ADR-007: Uniform Spatial Grid

- Status: Accepted
- Date: 2026-02-04
- Decision: use uniform grid for neighborhood queries.
- Why:
  - predictable performance and implementation simplicity
  - good fit for current world scale and entity density

## ADR-008: Dependency Policy

- Status: Accepted
- Date: 2026-02-04
- Decision:
  - keep simulation core dependency-light
  - allow standard test/benchmark tooling
- Why:
  - minimizes external breakage risk
  - keeps portability high

## ADR-009: File-Based Runtime Configuration + Hot Reload

- Status: Accepted
- Date: 2026-02-19
- Decision:
  - add `appsettings.json` for app-level behavior
  - add `world-config.json` for world defaults and tuning
  - validate with JSON schemas before applying
  - hot-reload changes at runtime (polling-based)
- Why:
  - faster balancing and experimentation loop
  - safer runtime config edits through schema guardrails
  - no restart required for most operational changes

## Repository Status (2026-02-19)

- ADR-001: Implemented
- ADR-002: Implemented
- ADR-003: Implemented (`TerminalRenderer` + `TerminalGuiRenderer` + overlays)
- ADR-004: Implemented
- ADR-005: Implemented
- ADR-006: Implemented
- ADR-007: Implemented
- ADR-008: Implemented
- ADR-009: Implemented

---

Last updated: 2026-02-19
