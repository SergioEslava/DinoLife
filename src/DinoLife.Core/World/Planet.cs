using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;

namespace DinoLife.Core.World;

/// <summary>
/// Centralized component storage using parallel arrays indexed by entity slot.
/// Provides allocation and freeing of entity slots and exposes tick counter.
/// </summary>
public class Planet
{
    private const int MAX_ENTITIES = 5000;
    
    // Parallel arrays indexed by entity slot
    private Entity[] _entities = new Entity[MAX_ENTITIES];
    private Transform[] _transforms = new Transform[MAX_ENTITIES];
    private Metabolism[] _metabolisms = new Metabolism[MAX_ENTITIES];
    private Movement[] _movements = new Movement[MAX_ENTITIES];
    private Diet[] _diets = new Diet[MAX_ENTITIES];
    private Plant[] _plants = new Plant[MAX_ENTITIES];
    private Reproduction[] _reproductions = new Reproduction[MAX_ENTITIES];
    private Stack<int> _freeSlots = new Stack<int>();
    private int _entityCount = 0;
    private int _tick;

    /// <summary>
    /// World boundaries used by movement wrapping.
    /// </summary>
    public Vector2 WorldSize { get; set; } = new Vector2(1000f, 1000f);
    
    /// <summary>
    /// Allocate and return an available entity slot index. Throws when the world is full.
    /// </summary>
    public int AllocateEntitySlot()
    {
        if (_freeSlots.Count > 0) {return _freeSlots.Pop();}
        
        if (_entityCount >= MAX_ENTITIES){ throw new InvalidOperationException("World full");}
        
        return _entityCount++;
    }
    
    /// <summary>
    /// Free the slot at <paramref name="slot"/>, marking the entity dead and making the slot reusable.
    /// </summary>
    /// <param name="slot">Index of the entity slot to free.</param>
    public void FreeEntitySlot(int slot)
    {
        _entities[slot].IsAlive = false;
        _freeSlots.Push(slot);
    }

    /// <summary>
    /// Total number of allocated entity slots.
    /// </summary>
    public int EntityCount => _entityCount;

    /// <summary>
    /// Raw entity component storage arrays.
    /// </summary>
    public Entity[] Entities => _entities;
    public Transform[] Transforms => _transforms;
    public Movement[] Movements => _movements;
    public Metabolism[] Metabolisms => _metabolisms;
    public Diet[] Diets => _diets;
    public Plant[] Plants => _plants;
    public Reproduction[] Reproductions => _reproductions;

    /// <summary>
    /// Simulation tick counter incremented by the engine.
    /// </summary>
    public int Tick {get => _tick; set => _tick = value;}
}
