# Project Structure

## Directory Layout

```
DinoLife/
├── .github/
│   └── workflows/
│       └── ci.yml                      # GitHub Actions CI/CD
├── src/
│   ├── DinoLife.Core/
│   │   ├── DinoLife.Core.csproj
│   │   ├── Entities/
│   │   │   ├── Entity.cs
│   │   │   ├── EntityType.cs
│   │   │   └── ComponentFlags.cs
│   │   ├── Components/
│   │   │   ├── Transform.cs
│   │   │   ├── Metabolism.cs
│   │   │   ├── Movement.cs
│   │   │   ├── Diet.cs
│   │   │   └── Reproduction.cs
│   │   ├── Systems/
│   │   │   ├── ISystem.cs
│   │   │   ├── MovementSystem.cs
│   │   │   ├── MetabolismSystem.cs
│   │   │   ├── HuntingSystem.cs
│   │   │   ├── ReproductionSystem.cs
│   │   │   ├── DeathSystem.cs
│   │   │   └── PlantGrowthSystem.cs
│   │   ├── World/
│   │   │   ├── World.cs
│   │   │   ├── WorldConfig.cs
│   │   │   ├── WorldState.cs
│   │   │   ├── SpatialGrid.cs
│   │   │   └── EntityFactory.cs
│   │   ├── Simulation/
│   │   │   ├── SimulationEngine.cs
│   │   │   ├── FixedTimeStep.cs
│   │   │   └── ISimulationListener.cs
│   │   └── Utils/
│   │       ├── RandomProvider.cs
│   │       ├── Vector2.cs
│   │       └── MathUtils.cs
│   │
│   ├── DinoLife.Rendering/
│   │   ├── DinoLife.Rendering.csproj
│   │   ├── IRenderer.cs
│   │   ├── RenderData.cs
│   │   ├── WorldSnapshot.cs
│   │   └── Terminal/
│   │       ├── TerminalRenderer.cs
│   │       ├── DoubleBuffer.cs
│   │       ├── ColorScheme.cs
│   │       └── ConsoleHelper.cs
│   │
│   ├── DinoLife.Persistence/
│   │   ├── DinoLife.Persistence.csproj
│   │   ├── IWorldSerializer.cs
│   │   ├── JsonWorldSerializer.cs
│   │   ├── SaveFile.cs
│   │   └── SaveMetadata.cs
│   │
│   └── DinoLife.Console/
│       ├── DinoLife.Console.csproj
│       ├── Program.cs
│       ├── Application.cs
│       ├── InputHandler.cs
│       ├── UI/
│       │   ├── HUD.cs
│       │   ├── PerformanceOverlay.cs
│       │   ├── HelpScreen.cs
│       │   └── ParameterEditor.cs
│       └── Config/
│           ├── appsettings.json
│           └── world-config.json
│
├── tests/
│   ├── DinoLife.Core.Tests/
│   │   ├── DinoLife.Core.Tests.csproj
│   │   ├── Systems/
│   │   │   ├── MovementSystemTests.cs
│   │   │   ├── MetabolismSystemTests.cs
│   │   │   └── ReproductionSystemTests.cs
│   │   ├── World/
│   │   │   ├── WorldTests.cs
│   │   │   └── SpatialGridTests.cs
│   │   ├── Integration/
│   │   │   ├── SimulationTests.cs
│   │   │   └── EmergentBehaviorTests.cs
│   │   └── TestHelpers/
│   │       ├── WorldBuilder.cs
│   │       └── EntityBuilder.cs
│   │
│   ├── DinoLife.Rendering.Tests/
│   │   ├── DinoLife.Rendering.Tests.csproj
│   │   └── Terminal/
│   │       └── DoubleBufferTests.cs
│   │
│   └── DinoLife.Benchmarks/
│       ├── DinoLife.Benchmarks.csproj
│       ├── SimulationBenchmarks.cs
│       ├── SystemBenchmarks.cs
│       └── SpatialGridBenchmarks.cs
│
├── docs/                               # Obsidian vault
│   ├── README.md
│   ├── Architecture.md
│   ├── Milestones.md
│   └── ...
│
├── .editorconfig                       # Code style rules
├── .gitignore
├── .gitattributes
├── Directory.Build.props               # Shared MSBuild properties
├── DinoLife.sln                        # Solution file
├── LICENSE
└── README.md
```

## Project Configurations

### DinoLife.Core.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>12.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks> <!-- For future SIMD optimizations -->
  </PropertyGroup>

  <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Missing XML comments -->
  </PropertyGroup>

  <ItemGroup>
    <!-- NO DEPENDENCIES - Core is dependency-free -->
  </ItemGroup>

</Project>
```

### DinoLife.Rendering.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\DinoLife.Core\DinoLife.Core.csproj" />
  </ItemGroup>

</Project>
```

### DinoLife.Persistence.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\DinoLife.Core\DinoLife.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- System.Text.Json is built into .NET 8, no package needed -->
  </ItemGroup>

</Project>
```

### DinoLife.Console.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\DinoLife.Core\DinoLife.Core.csproj" />
    <ProjectReference Include="..\DinoLife.Rendering\DinoLife.Rendering.csproj" />
    <ProjectReference Include="..\DinoLife.Persistence\DinoLife.Persistence.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Config\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

### DinoLife.Core.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\DinoLife.Core\DinoLife.Core.csproj" />
  </ItemGroup>

</Project>
```

### DinoLife.Benchmarks.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\DinoLife.Core\DinoLife.Core.csproj" />
  </ItemGroup>

</Project>
```

## Directory.Build.props

```xml
<Project>

  <PropertyGroup>
    <!-- Global properties for all projects -->
    <Authors>YourName</Authors>
    <Company>YourCompany</Company>
    <Product>DinoLife</Product>
    <Copyright>Copyright © 2026</Copyright>
    <Version>1.0.0</Version>
    <FileVersion>1.0.0.0</FileVersion>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Code analysis -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <Optimize>true</Optimize>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <Optimize>false</Optimize>
    <DebugType>full</DebugType>
    <DebugSymbols>true</DebugSymbols>
  </PropertyGroup>

</Project>
```

## .editorconfig

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csproj}]
indent_style = space
indent_size = 4

[*.{json,yml,yaml}]
indent_style = space
indent_size = 2

# C# Code Style Rules
[*.cs]

# Namespace preferences
csharp_style_namespace_declarations = file_scoped:warning

# var preferences
csharp_style_var_for_built_in_types = false:warning
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:warning

# Expression-bodied members
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion

# Pattern matching
csharp_style_pattern_matching_over_is_with_cast_check = true:warning
csharp_style_pattern_matching_over_as_with_null_check = true:warning

# Null checking
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:warning

# Code block preferences
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:suggestion

# Naming conventions
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = *

dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# Private fields should start with underscore
dotnet_naming_rule.private_fields_should_be_prefixed_with_underscore.severity = warning
dotnet_naming_rule.private_fields_should_be_prefixed_with_underscore.symbols = private_field
dotnet_naming_rule.private_fields_should_be_prefixed_with_underscore.style = underscore_prefix

dotnet_naming_symbols.private_field.applicable_kinds = field
dotnet_naming_symbols.private_field.applicable_accessibilities = private

dotnet_naming_style.underscore_prefix.required_prefix = _
dotnet_naming_style.underscore_prefix.capitalization = camel_case
```

## .gitignore

```gitignore
# Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/

# Visual Studio cache/options
.vs/
.vscode/
*.suo
*.user
*.userosscache
*.sln.docstates

# Test results
TestResults/
*.trx
*.coverage
*.coveragexml

# BenchmarkDotNet artifacts
BenchmarkDotNet.Artifacts/

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# Rider
.idea/

# Build outputs
*.dll
*.exe
*.pdb

# NuGet
*.nupkg
*.snupkg
packages/
.nuget/

# Save files (optional - might want to version control examples)
saves/
autosave.json

# OS files
.DS_Store
Thumbs.db

# Logs
*.log
```

## DinoLife.sln

```
Microsoft Visual Studio Solution File, Format Version 12.00
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DinoLife.Core", "src\DinoLife.Core\DinoLife.Core.csproj", "{GUID1}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DinoLife.Rendering", "src\DinoLife.Rendering\DinoLife.Rendering.csproj", "{GUID2}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DinoLife.Persistence", "src\DinoLife.Persistence\DinoLife.Persistence.csproj", "{GUID3}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DinoLife.Console", "src\DinoLife.Console\DinoLife.Console.csproj", "{GUID4}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DinoLife.Core.Tests", "tests\DinoLife.Core.Tests\DinoLife.Core.Tests.csproj", "{GUID5}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DinoLife.Rendering.Tests", "tests\DinoLife.Rendering.Tests\DinoLife.Rendering.Tests.csproj", "{GUID6}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DinoLife.Benchmarks", "tests\DinoLife.Benchmarks\DinoLife.Benchmarks.csproj", "{GUID7}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{GUID1}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{GUID1}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{GUID1}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{GUID1}.Release|Any CPU.Build.0 = Release|Any CPU
		{GUID2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{GUID2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{GUID2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{GUID2}.Release|Any CPU.Build.0 = Release|Any CPU
		{GUID3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{GUID3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{GUID3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{GUID3}.Release|Any CPU.Build.0 = Release|Any CPU
		{GUID4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{GUID4}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{GUID4}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{GUID4}.Release|Any CPU.Build.0 = Release|Any CPU
		{GUID5}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{GUID5}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{GUID5}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{GUID5}.Release|Any CPU.Build.0 = Release|Any CPU
		{GUID6}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{GUID6}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{GUID6}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{GUID6}.Release|Any CPU.Build.0 = Release|Any CPU
		{GUID7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{GUID7}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{GUID7}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{GUID7}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
```

## Build Commands

### Development
```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Build specific project
dotnet build src/DinoLife.Core/DinoLife.Core.csproj

# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run benchmarks
dotnet run --project tests/DinoLife.Benchmarks/DinoLife.Benchmarks.csproj -c Release
```

### Release
```bash
# Build release
dotnet build -c Release

# Publish self-contained executable (Windows)
dotnet publish src/DinoLife.Console/DinoLife.Console.csproj -c Release -r win-x64 --self-contained

# Publish self-contained executable (Linux)
dotnet publish src/DinoLife.Console/DinoLife.Console.csproj -c Release -r linux-x64 --self-contained

# Publish self-contained executable (macOS)
dotnet publish src/DinoLife.Console/DinoLife.Console.csproj -c Release -r osx-x64 --self-contained
```

## Dependency Graph

```
DinoLife.Console
    ├── DinoLife.Core (no dependencies)
    ├── DinoLife.Rendering
    │   └── DinoLife.Core
    └── DinoLife.Persistence
        └── DinoLife.Core

Tests depend on respective src projects
```

---

*Last updated: 2026-02-04*
