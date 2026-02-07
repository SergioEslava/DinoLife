using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class MovementIntegrationTests
{
    [Fact]
    public void MovementSystems_UpdatesAndWraps_For100Entities()
    {
        var world = new Planet { WorldSize = new Vector2(50f, 30f) };
        var behavior = new BehaviorSystem();
        var movement = new MovementSystem();

        var initialPositions = new Vector2[100];
        for (int i = 0; i < 100; i++)
        {
            EntityType type = i % 2 == 0 ? EntityType.Herbivore : EntityType.Scavenger;
            float x = i % 10;
            float y = i / 10;
            initialPositions[i] = new Vector2(x, y);

            CreateEntity(world, type, initialPositions[i], speed: 5f);
        }

        for (int tick = 0; tick < 10; tick++)
        {
            behavior.Update(world, 1.0 / 60.0);
            movement.Update(world, 1.0 / 60.0);
            world.Tick++;
        }

        bool anyMoved = false;
        for (int i = 0; i < 100; i++)
        {
            Vector2 pos = world.Transforms[i].Position;
            if (pos != initialPositions[i]) { anyMoved = true; }

            pos.X.Should().BeInRange(0f, world.WorldSize.X);
            pos.Y.Should().BeInRange(0f, world.WorldSize.Y);
        }

        anyMoved.Should().BeTrue();
    }

    [Fact]
    public void BehaviorSystem_SetsFleeDirection_ForHerbivoreWhenCarnivoreNearby()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        var behavior = new BehaviorSystem();

        int herbivore = CreateEntity(world, EntityType.Herbivore, new Vector2(50f, 50f), speed: 4f);
        CreateEntity(world, EntityType.Carnivore, new Vector2(52f, 50f), speed: 4f);

        behavior.Update(world, 1.0 / 60.0);

        Vector2 velocity = world.Movements[herbivore].Velocity;
        velocity.X.Should().BeLessThan(0f);
    }

    [Fact]
    public void BehaviorSystem_SetsChaseDirection_ForCarnivoreWhenHerbivoreNearby()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        var behavior = new BehaviorSystem();

        int carnivore = CreateEntity(world, EntityType.Carnivore, new Vector2(10f, 10f), speed: 4f);
        CreateEntity(world, EntityType.Herbivore, new Vector2(12f, 10f), speed: 4f);

        behavior.Update(world, 1.0 / 60.0);

        Vector2 velocity = world.Movements[carnivore].Velocity;
        velocity.X.Should().BeGreaterThan(0f);
    }

    private static int CreateEntity(Planet world, EntityType type, Vector2 position, float speed)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = type,
            Flags = ComponentFlags.Transform | ComponentFlags.Movement,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);
        world.Movements[slot] = new Movement
        {
            Velocity = Vector2.Zero,
            Speed = speed,
            Acceleration = speed
        };

        return slot;
    }
}
