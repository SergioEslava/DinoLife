# Project Overview

## Vision

DinoLife simulates a simple ecosystem where 4 entity types interact to produce emergent behavior - predator-prey cycles, population dynamics, and resource competition without explicit programming of these high-level patterns.

## Goals

### Primary (v1.0)
1. **Functional Simulation** - 5000 entities running at 60 TPS consistently
2. **Emergent Behavior** - Observable population cycles and ecosystem dynamics
3. **Real-time Visualization** - Terminal rendering with performance metrics
4. **State Persistence** - Save/load complete simulation state
5. **Parameter Control** - Runtime tuning of entity properties

### Secondary (Future)
- Unity integration (renderer swap)
- Genetic evolution system
- Advanced spatial behaviors
- Multi-biome environments
- Statistical analysis tools

## Non-Goals (v1.0)

- ❌ 3D rendering
- ❌ Multiplayer/networking
- ❌ Complex genetics (genes/mutations)
- ❌ Machine learning integration
- ❌ Audio/sound effects
- ❌ Advanced graphics (sprites, animations)

## Success Criteria

### Technical
- ✅ 5000+ entities at stable 60 TPS
- ✅ <16ms per simulation tick
- ✅ Zero memory leaks over 1-hour runs
- ✅ Save/load roundtrip in <1 second

### Behavioral
- ✅ Herbivore-Carnivore predator-prey oscillations
- ✅ Plant growth sustains herbivore population
- ✅ Scavenger population correlates with death rate
- ✅ System self-regulates (no entity extinction in first 10,000 ticks)

### Usability
- ✅ Clear visual distinction between entity types
- ✅ Readable population statistics
- ✅ Responsive controls (pause, speed, save)
- ✅ Documented parameter effects

## Target Audience

**Primary:** Myself (learning project, portfolio piece)  
**Secondary:** Developers interested in simulation architecture

## Timeline

**Total Duration:** 6 sprints (~8 weeks)  
**First Milestone:** 2 weeks from start  
**Release Target:** End of Sprint 6

See [[Milestones]] for detailed breakdown.

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Performance bottleneck | High | Early profiling, spatial partitioning |
| Behavior not emerging | High | Iterative parameter tuning, testing |
| Terminal rendering limits | Medium | Abstract renderer, Unity fallback |
| Scope creep | Medium | Strict v1.0 feature lock |

## Links

- [[Technical-Decisions|Technical Decisions]]
- [[Architecture|Architecture Overview]]
- [[Milestones|Project Milestones]]

---

*Last updated: 2026-02-04*
