# GitFlow Workflow

## Branch Structure

```
main (production)
  │
  ├── develop (integration)
  │     │
  │     ├── feature/milestone-1-core-architecture
  │     ├── feature/milestone-2-entity-herbivore
  │     ├── feature/milestone-2-entity-carnivore
  │     ├── feature/milestone-2-spatial-grid
  │     ├── feature/milestone-3-terminal-renderer
  │     ├── feature/milestone-4-input-handling
  │     └── ...
  │
  ├── release/1.0.0 (pre-release)
  │
  └── hotfix/critical-bug (emergency fixes)
```

## Branch Types

### Main Branches

#### `main`
- **Purpose:** Production-ready code only
- **Protection:** No direct commits, PR required
- **Merges From:** `release/*` or `hotfix/*`
- **Tagged:** Every merge is a release (v1.0.0, v1.0.1, etc.)

#### `develop`
- **Purpose:** Integration branch for ongoing development
- **Protection:** PR required, CI must pass
- **Merges From:** `feature/*` branches
- **Merges To:** `release/*` branches

### Supporting Branches

#### `feature/*`
- **Purpose:** New features or major changes
- **Naming:** `feature/milestone-X-description`
  - Examples:
    - `feature/milestone-1-simulation-loop`
    - `feature/milestone-2-herbivore-entity`
    - `feature/milestone-3-double-buffering`
- **Created From:** `develop`
- **Merges To:** `develop`
- **Lifetime:** Until feature complete and merged
- **Delete After Merge:** Yes

#### `release/*`
- **Purpose:** Prepare release (testing, bug fixes, versioning)
- **Naming:** `release/X.Y.Z`
  - Examples: `release/1.0.0`, `release/1.1.0`
- **Created From:** `develop`
- **Merges To:** `main` AND back to `develop`
- **Lifetime:** Until release published
- **Delete After Merge:** Yes

#### `hotfix/*`
- **Purpose:** Critical production bug fixes
- **Naming:** `hotfix/description`
  - Examples: `hotfix/crash-on-save`, `hotfix/memory-leak`
- **Created From:** `main`
- **Merges To:** `main` AND `develop`
- **Lifetime:** Until hotfix deployed
- **Delete After Merge:** Yes

---

## Workflow Scenarios

### 1. Starting a New Feature

```bash
# Ensure develop is up to date
git checkout develop
git pull origin develop

# Create feature branch
git checkout -b feature/milestone-2-herbivore-entity

# Work on feature
# ... make commits ...

# Push to remote
git push -u origin feature/milestone-2-herbivore-entity

# Create PR to develop when ready
```

**PR Requirements:**
- ✅ All tests passing
- ✅ Code coverage maintained (>80%)
- ✅ No compiler warnings
- ✅ Code review approved
- ✅ Conflicts resolved

### 2. Merging Feature to Develop

```bash
# On feature branch, ensure up to date with develop
git checkout feature/milestone-2-herbivore-entity
git fetch origin
git rebase origin/develop

# Resolve any conflicts
# Run tests locally

# Push (force if rebased)
git push --force-with-lease

# Merge via PR (GitHub/GitLab UI)
# Delete feature branch after merge
```

### 3. Creating a Release

```bash
# Create release branch from develop
git checkout develop
git pull origin develop
git checkout -b release/1.0.0

# Update version numbers
# DinoLife.Core/DinoLife.Core.csproj: <Version>1.0.0</Version>
# Directory.Build.props: <Version>1.0.0</Version>

# Update CHANGELOG.md
# Add release notes

git add .
git commit -m "chore: Bump version to 1.0.0"
git push -u origin release/1.0.0

# Run full test suite, integration tests
# Fix any release-blocking bugs on this branch
```

### 4. Finalizing Release

```bash
# Merge to main
git checkout main
git merge --no-ff release/1.0.0
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin main --tags

# Merge back to develop
git checkout develop
git merge --no-ff release/1.0.0
git push origin develop

# Delete release branch
git branch -d release/1.0.0
git push origin --delete release/1.0.0
```

### 5. Hotfix Workflow

```bash
# Critical bug in production!
git checkout main
git pull origin main
git checkout -b hotfix/memory-leak-in-renderer

# Fix the bug
# ... commits ...

# Bump patch version (1.0.0 → 1.0.1)
git commit -m "fix: Memory leak in TerminalRenderer"
git push -u origin hotfix/memory-leak-in-renderer

# Merge to main
git checkout main
git merge --no-ff hotfix/memory-leak-in-renderer
git tag -a v1.0.1 -m "Hotfix: Memory leak in renderer"
git push origin main --tags

# Merge to develop
git checkout develop
git merge --no-ff hotfix/memory-leak-in-renderer
git push origin develop

# Delete hotfix branch
git branch -d hotfix/memory-leak-in-renderer
git push origin --delete hotfix/memory-leak-in-renderer
```

---

## Commit Message Convention

### Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, no logic change)
- `refactor`: Code refactor (no feature/bug change)
- `perf`: Performance improvement
- `test`: Adding/updating tests
- `chore`: Build process, tooling, dependencies

### Examples

```
feat(entities): Add Herbivore entity with plant feeding behavior

Implements Herbivore entity with:
- Movement towards nearest plant
- Flee from carnivores
- Energy metabolism
- Reproduction when energy > 80

Closes #42
```

```
fix(renderer): Fix flickering in terminal double buffer

Buffer swap was not clearing old content properly,
causing visual artifacts during rapid entity movement.

Fixes #128
```

```
perf(spatial-grid): Optimize radius queries with early exit

Reduced query time from 3ms to 0.8ms for 5000 entities
by adding distance-squared checks before full calculations.
```

```
test(systems): Add integration tests for predator-prey cycles

Verifies that herbivore-carnivore populations oscillate
over 10,000 tick simulation with expected lag phase.
```

---

## PR Template

### Feature PR Template

```markdown
## Description
Brief description of the feature

## Related Issue
Closes #XX

## Changes
- Change 1
- Change 2

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] Manual testing performed

## Checklist
- [ ] Code compiles without warnings
- [ ] Tests pass locally
- [ ] Documentation updated
- [ ] CHANGELOG.md updated (if user-facing)
```

### Release PR Template

```markdown
## Release: vX.Y.Z

### Milestone
Closes Milestone X

### Changes
See CHANGELOG.md

### Pre-Merge Checklist
- [ ] All milestone features merged
- [ ] Version bumped in all projects
- [ ] CHANGELOG.md updated
- [ ] Release notes written
- [ ] Full integration test suite passing
- [ ] Performance benchmarks run (no regressions)
- [ ] Documentation reviewed

### Post-Merge Actions
- [ ] Tag main with vX.Y.Z
- [ ] Create GitHub release
- [ ] Merge back to develop
- [ ] Close milestone
```

---

## Branch Protection Rules

### `main` Branch
- ✅ Require PR before merging
- ✅ Require status checks (CI/CD)
- ✅ Require up-to-date before merge
- ✅ No force push
- ✅ No deletion
- ✅ Require signed commits (optional)

### `develop` Branch
- ✅ Require PR before merging
- ✅ Require status checks (CI/CD)
- ✅ Require up-to-date before merge
- ✅ No force push
- ❌ Allow deletion (only during complete resets)

### `feature/*` Branches
- ❌ No protection (developer freedom)
- ✅ Delete after merge

---

## CI/CD Pipeline (.github/workflows/ci.yml)

```yaml
name: CI

on:
  push:
    branches: [ develop, main ]
  pull_request:
    branches: [ develop, main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
  
  benchmark:
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
    
    - name: Run Benchmarks
      run: dotnet run --project tests/DinoLife.Benchmarks/DinoLife.Benchmarks.csproj -c Release
    
    - name: Upload Results
      uses: actions/upload-artifact@v3
      with:
        name: benchmark-results
        path: BenchmarkDotNet.Artifacts/
```

---

## Version Numbering

**Format:** `MAJOR.MINOR.PATCH`

- **MAJOR:** Breaking changes, incompatible API changes
- **MINOR:** New features, backwards compatible
- **PATCH:** Bug fixes, backwards compatible

### v1.x Roadmap
- `v1.0.0` - Initial release (all milestones 1-5)
- `v1.1.0` - Genetic evolution system
- `v1.2.0` - Unity renderer
- `v1.3.0` - Advanced behaviors

---

## Tags

### Release Tags
```bash
git tag -a v1.0.0 -m "Release 1.0.0 - Initial DinoLife simulation"
git push origin v1.0.0
```

### Milestone Tags (Optional)
```bash
git tag -a m1-complete -m "Milestone 1: Core Architecture Complete"
git push origin m1-complete
```

---

## Best Practices

### Do
- ✅ Keep feature branches short-lived (<1 week)
- ✅ Rebase frequently from develop
- ✅ Write descriptive commit messages
- ✅ Squash commits when merging features (if needed)
- ✅ Delete branches after merge
- ✅ Run tests before pushing
- ✅ Update documentation with features

### Don't
- ❌ Commit directly to main or develop
- ❌ Merge without PR review
- ❌ Push broken code to develop
- ❌ Leave feature branches stale
- ❌ Force push to protected branches
- ❌ Skip writing tests

---

*Last updated: 2026-02-19*
