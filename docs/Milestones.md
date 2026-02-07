# Project Milestones

## Overview

**Total Duration:** 6 sprints (~8 weeks)  
**Sprint Length:** 1-2 weeks per milestone  
**Velocity:** Senior developer, solo project

## Milestone Roadmap

```
M1 ──────→ M2 ──────→ M3 ────→ M4 ────→ M5 ──→ Release
2 weeks    3 weeks    2 weeks  1.5 wk   0.5 wk
```

---

## Milestone 1: Core Architecture & Foundation

**Duration:** 2 weeks (Sprint 1)  
**Branch:** `develop`  
**Tag:** `v0.1.0-alpha`

### Goals
Establish solid technical foundation with zero functional features but production-quality infrastructure.

### Deliverables

#### 1.1 Project Scaffold
- [x] Solution structure with 4 projects
- [x] .csproj configurations (NET8, nullable, warnings as errors)
- [x] .editorconfig with team code style
- [x] Directory.Build.props for shared properties
- [x] .gitignore and .gitattributes

#### 1.2 Core Data Structures
- [x] `Entity` struct with Id, Type, Flags
- [x] `EntityType` enum (Herbivore, Carnivore, Plant, Scavenger)
- [x] `ComponentFlags` enum
- [x] Component structs: `Transform`, `Metabolism`, `Movement`, `Diet`, `Reproduction`
- [x] `World` class with component arrays (SoA layout)

#### 1.3 Simulation Loop Infrastructure
- [x] `SimulationEngine` with fixed timestep
- [x] `ISystem` interface definition
- [x] Empty system implementations (stubs)
- [x] Performance timer abstraction
- [x] Main loop with tick counter

#### 1.4 Testing Infrastructure
- [x] xUnit test projects configured
- [x] FluentAssertions integration
- [x] BenchmarkDotNet setup
- [x] Test helpers and builders
- [x] CI/CD pipeline (GitHub Actions or equivalent)

#### 1.5 Documentation
- [x] README with build instructions
- [x] Architecture diagram (ASCII or Mermaid)
- [x] Code documentation standards
- [x] ADR template

### Definition of Done
- ✅ Empty simulation runs at locked 60 TPS
- ✅ CI pipeline green (build + test)
- ✅ Code coverage >80% on infrastructure code
- ✅ Zero compiler warnings
- ✅ Memory profiling shows no leaks over 10-minute run

### Exit Criteria
- Can create World with 5000 empty entities
- Simulation loop maintains 16.67ms tick time
- All tests passing
- Documentation up to date

---

## Milestone 2: Entity Implementation

**Duration:** 3 weeks (Sprint 2-3)  
**Branch:** `develop` (feature branches merge here)  
**Tag:** `v0.2.0-alpha`

### Goals
Implement all 4 entity types with complete behavior systems producing emergent population dynamics.

### Deliverables

#### 2.1 Movement System
- [x] `MovementSystem` with velocity integration
- [x] Boundary handling (wrap-around)
- [x] Random walk behavior for herbivores/scavengers
- [x] Chase/flee behaviors for carnivores/herbivores
- [x] Integration tests with 100 entities

#### 2.2 Metabolism System
- [x] `MetabolismSystem` with energy depletion
- [x] Energy gain from eating
- [x] Starvation mechanics
- [x] Energy thresholds for behaviors
- [x] Tests for energy balance

#### 2.3 Plant Entity
- [x] Plant growth over time
- [x] Energy provision when eaten
- [x] Respawn mechanics
- [x] Spatial distribution algorithm
- [x] Tests for growth curves

#### 2.4 Herbivore Entity
- [x] Plant detection in radius
- [x] Movement towards nearest plant
- [x] Eating behavior (energy transfer)
- [x] Flee from carnivores
- [x] Reproduction when energy > threshold

#### 2.5 Carnivore Entity
- [x] Herbivore detection in radius
- [x] Chase behavior
- [x] Hunting mechanics (catch + kill)
- [x] Energy from hunting
- [x] Reproduction when energy > threshold

#### 2.6 Scavenger Entity
- [ ] Corpse detection
- [ ] Movement towards corpses
- [ ] Scavenging mechanics
- [ ] Reproduction when energy > threshold
- [ ] Lower metabolism than carnivores

#### 2.7 Death System
- [ ] Death conditions (starvation, old age)
- [ ] Corpse creation
- [ ] Corpse decay over time
- [ ] Entity removal from world

#### 2.8 Reproduction System
- [ ] Reproduction conditions (energy, cooldown)
- [ ] Offspring creation with stat variation (±10%)
- [ ] Energy cost to parent
- [ ] Spawn positioning (near parent)
- [ ] Tests for population growth

#### 2.9 Spatial Partitioning
- [ ] `SpatialGrid` implementation
- [ ] Entity insertion/removal
- [ ] Radius query optimization
- [ ] Performance tests (5000 entities, 100 queries/tick)
- [ ] Grid visualization (debug mode)

### Definition of Done
- ✅ 5000 entities simulation at stable 60 TPS
- ✅ Observable predator-prey oscillations in population graph
- ✅ All 4 entity types interacting correctly
- ✅ No entity extinction in first 10,000 ticks
- ✅ Spatial queries complete in <1ms per query
- ✅ All systems unit tested (>90% coverage)
- ✅ Integration test suite passing

### Behavioral Validation
Run 10,000 tick simulation and verify:
- [ ] Herbivore population oscillates (not flat line)
- [ ] Carnivore peaks lag herbivore peaks
- [ ] Plants regrow maintaining ecosystem
- [ ] Scavengers correlate with death events
- [ ] System self-regulates (no runaway growth)

### Exit Criteria
- Emergent behavior documented with screenshots
- Performance benchmarks recorded
- Parameter tuning guide created

---

## Milestone 3: Visualization Layer

**Duration:** 2 weeks (Sprint 4)  
**Branch:** `develop`  
**Tag:** `v0.3.0-beta`

### Goals
Create terminal-based visualization with real-time statistics and debugging tools.

### Deliverables

#### 3.1 Renderer Architecture
- [ ] `IRenderer` interface
- [ ] `RenderData` / `WorldSnapshot` struct (read-only view)
- [ ] `TerminalRenderer` skeleton
- [ ] Color scheme configuration
- [ ] Entity symbol mapping

#### 3.2 Double Buffering
- [ ] `DoubleBuffer` class (char[,] arrays)
- [ ] Buffer swap logic
- [ ] Diff-based console updates (only changed cells)
- [ ] Flicker testing

#### 3.3 Entity Rendering
- [ ] Symbol per entity type:
  - Herbivore: `H` (Green)
  - Carnivore: `C` (Red)
  - Plant: `*` (Light Green)
  - Scavenger: `S` (Yellow)
- [ ] Position scaling (world → screen coords)
- [ ] Culling for off-screen entities
- [ ] Layering (plants → herbivores → carnivores)

#### 3.4 HUD Display
- [ ] Top bar: Tick count, FPS, TPS
- [ ] Population counts per entity type
- [ ] World statistics (total energy, avg lifespan)
- [ ] Color-coded health indicators

#### 3.5 Performance Overlay
- [ ] Real-time FPS counter
- [ ] Tick time graph (last 60 ticks)
- [ ] Memory usage
- [ ] Entity count
- [ ] Toggle on/off with keyboard

#### 3.6 Camera Controls
- [ ] Pan (arrow keys or WASD)
- [ ] Zoom (+ / -)
- [ ] Reset view (Home key)
- [ ] Follow entity mode (F key + select)

### Definition of Done
- ✅ 500+ entities render smoothly (>30 FPS display)
- ✅ HUD updates every frame without tearing
- ✅ No visible flickering
- ✅ All controls responsive (<50ms input latency)
- ✅ Performance overlay shows accurate metrics
- ✅ Rendering decoupled from simulation (sim at 60 TPS, render at display rate)

### Visual Validation
- [ ] Screenshot with 5000 entities showing clear distinction
- [ ] Video of 1-minute simulation showing smooth rendering
- [ ] Performance test report (FPS under load)

### Exit Criteria
- Renderer can visualize full 5000-entity world
- Color scheme is readable on dark and light terminals
- Documentation includes key bindings reference

---

## Milestone 4: Simulation Control & Polish

**Duration:** 1.5 weeks (Sprint 5)  
**Branch:** `develop`  
**Tag:** `v0.9.0-rc`

### Goals
Add user controls, persistence, and parameter tuning for full interactivity.

### Deliverables

#### 4.1 Input Handling
- [ ] `InputHandler` class with key mapping
- [ ] Non-blocking keyboard input
- [ ] Command queue pattern
- [ ] Key bindings:
  - `Space`: Play/Pause
  - `→`: Step one tick (when paused)
  - `+/-`: Speed control (0.25x, 0.5x, 1x, 2x, 4x)
  - `S`: Save state
  - `L`: Load state
  - `R`: Reset simulation
  - `Q`: Quit
  - `P`: Toggle performance overlay
  - `H`: Show help

#### 4.2 Simulation Controls
- [ ] Pause/Resume without state corruption
- [ ] Single-step mode for debugging
- [ ] Speed multiplier (affects tick rate)
- [ ] Reset to initial state
- [ ] Frame advance visualization

#### 4.3 Persistence System
- [ ] `IWorldSerializer` interface
- [ ] `JsonWorldSerializer` implementation
- [ ] Save format v1.0 schema
- [ ] Incremental saves (autosave every N ticks)
- [ ] Save browser (list/load from saves/)
- [ ] Versioning support
- [ ] Corruption detection

#### 4.4 Parameter Tuning UI
- [ ] In-game menu for parameter editing
- [ ] Live parameter updates (no restart)
- [ ] Parameter categories:
  - Movement speeds
  - Metabolism rates
  - Reproduction thresholds
  - Detection radii
  - Growth rates
- [ ] Preset configurations (Balanced, Chaotic, Stable)
- [ ] Export/Import parameter files

#### 4.5 Help System
- [ ] In-game help screen (key: `H`)
- [ ] Command reference
- [ ] Entity behavior summary
- [ ] Tips for interesting scenarios

#### 4.6 Configuration Files
- [ ] `appsettings.json` for global config
- [ ] `world-config.json` for simulation parameters
- [ ] JSON schema validation
- [ ] Hot-reload support

### Definition of Done
- ✅ Pause/resume works flawlessly (no desyncs)
- ✅ Save/load roundtrip in <1 second
- ✅ Saved state reproduces exactly on load
- ✅ Parameter changes visible within 1 tick
- ✅ All controls documented in help screen
- ✅ Configuration validated on startup

### User Experience Validation
- [ ] New user can start simulation in <30 seconds
- [ ] Saving and loading feels instant
- [ ] Parameter tweaking is intuitive
- [ ] Help screen answers common questions

### Exit Criteria
- Complete user manual written
- Example scenarios documented
- Edge cases tested (pause during save, etc.)

---

## Milestone 5: Release Preparation

**Duration:** 0.5 weeks (Sprint 6)  
**Branch:** `release/1.0.0`  
**Tag:** `v1.0.0`

### Goals
Final polish, testing, and documentation for public release.

### Deliverables

#### 5.1 Integration Testing
- [ ] Full simulation run (100k ticks) with monitoring
- [ ] Memory leak detection (Valgrind/.NET profiler)
- [ ] Edge case testing:
  - All entities dead
  - Single entity type
  - Parameter extremes
  - Rapid save/load cycles
- [ ] Cross-platform testing (Windows, Linux, macOS)

#### 5.2 Performance Optimization
- [ ] Profiling with BenchmarkDotNet
- [ ] Hot path optimization (systems)
- [ ] Memory allocation reduction
- [ ] Rendering optimizations
- [ ] Before/after benchmarks

#### 5.3 Bug Fixes
- [ ] Triage all open issues
- [ ] Fix critical/high priority bugs
- [ ] Regression testing
- [ ] Known issues documented

#### 5.4 Documentation Finalization
- [ ] User guide (getting started, controls, concepts)
- [ ] Developer documentation (architecture, extending)
- [ ] API documentation (XML comments)
- [ ] Troubleshooting guide
- [ ] FAQ

#### 5.5 Release Assets
- [ ] Build scripts for all platforms
- [ ] Packaged executables (ZIP/TAR)
- [ ] LICENSE file
- [ ] CHANGELOG.md
- [ ] Release notes

#### 5.6 Quality Gates
- [ ] All tests passing (unit + integration)
- [ ] Code coverage >85%
- [ ] Zero critical bugs
- [ ] Performance benchmarks met
- [ ] Documentation complete

### Definition of Done
- ✅ Release builds created for Win/Linux/macOS
- ✅ All quality gates passed
- ✅ Git tags and release notes published
- ✅ Documentation hosted (GitHub Pages or similar)
- ✅ Zero known critical bugs

### Release Checklist
- [ ] Version bumped to 1.0.0
- [ ] CHANGELOG updated
- [ ] Git tag created
- [ ] Merge to `main` branch
- [ ] GitHub release published
- [ ] Demo video recorded
- [ ] Social media announcement (if applicable)

---

## Post-Release (v1.1+ Planning)

### Potential Features
- Genetic evolution system
- Unity renderer
- Statistical analysis tools
- Advanced behaviors (herd/pack mechanics)
- Biome system
- Web-based viewer

### Lessons Learned
- Document what worked well
- Document what didn't
- Performance bottlenecks encountered
- Architecture decisions to revisit

---

## Milestone Tracking

| Milestone | Status | 
|-----------|--------|
| M1 - Foundation | 🟢 
| M2 - Entities | 🟡 
| M3 - Visualization | 🟡 
| M4 - Controls | 🔴 
| M5 - Release | 🔴 

**Status Legend:**
- 🔴 Not Started
- 🟡 In Progress
- 🟢 Completed
- ⚠️ Blocked

---

*Last updated: 2026-02-04*
