# DinoLife v1.0 Documentation

> Emergent life simulation system in C# .NET8

## Overview

DinoLife is a terminal-based life simulation featuring 4 entity types that create emergent ecosystems through simple interaction rules.

**Version:** 1.0.0  
**Target Platform:** Desktop (.NET8)  
**Rendering:** Terminal (Console)  
**Architecture:** Data-Oriented Design (Hybrid)

## Quick Navigation

### Planning & Architecture
- [Project Overview](docs/Project-Overview.md)
- [Technical Decisions (ADRs)](docs/Technical-Decisions.md)
- [System Architecture](docs/Architecture.md)
- [Project Milestones](docs/Milestones.md)

### Development
- [Project Structure](docs/Project-Structure.md)
- [Entity Design](docs/Entity-Design.md)
- [System Design](docs/System-Design.md)

### Workflow
- [GitFlow Workflow](docs/GitFlow-Workflow.md)
- [Development Guidelines](docs/Development-Guidelines.md)

## Core Specifications

| Aspect | Specification |
|--------|--------------|
| Language | C# .NET8 |
| Architecture | Data-Oriented Hybrid |
| Tick Rate | 60 updates/second |
| Target Entities | 5000 simultaneous |
| Persistence | JSON serialization |
| Rendering | Terminal (Console direct) |

## Entity Types

1. **Herbivore** - Grazes plants, reproduces, prey
2. **Carnivore** - Hunts herbivores, apex predator
3. **Plant** - Energy source, grows over time
4. **Scavenger** - Consumes corpses, cleanup role

## Key Features (v1.0)

- [x] Real-time simulation at 60 TPS
- [ ] 4 entity types with emergent behavior
- [ ] Terminal visualization with stats HUD
- [ ] Play/Pause/Speed controls
- [ ] Save/Load simulation state
- [ ] Parameter tuning interface
- [ ] Performance metrics overlay

## Status

**Current Phase:** Milestone 1: Core Architecture & Foundation
**Next Milestone:** Milestone 2: Entity Implementation

---

## Build Instructions

1. **Clone the repository:**

```bash
git clone https://github.com/SergioEslava/DinoLife.git
cd DinoLife
```

2. **Restore NuGet packages:**

```bash
dotnet restore
```

3. **Build the solution in Release mode:**

```bash
dotnet build -c Release
```

## Running Tests:
All unit tests use xUnit and FluentAssertions:

### Run all tests in Release mode

```bash
dotnet test -c Release
```

## Running Benchmarks
Benchmarks use **BenchmarkDotNet** and are located in **tests/DinoLife.Benchmarks**.

### Run all benchmarks in Release mode

```bash
dotnet run --project tests/DinoLife.Benchmarks/DinoLife.Benchmarks.csproj -c Release
```

**Notes:**
Always run in Release mode for reliable measurements.

**BenchmarkDotNet** outputs results in:

- Console logs (summary of mean execution times, memory usage)
- HTML reports in BenchmarkDotNet.Artifacts/results/

You can open the HTML files to visualize detailed performance metrics and compare baselines.

---
