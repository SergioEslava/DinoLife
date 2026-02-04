using DinoLife.Core.Entities;
using DinoLife.Core.Components;

namespace DinoLife.Core.World;

/// <summary>
/// Component Storage
/// </summary>
public class World
{
    private const int MAX_ENTITIES = 5000;
    
    // Parallel arrays indexed by entity slot
    private Entity[] _entities = new Entity[MAX_ENTITIES];
    private Transform[] _transforms = new Transform[MAX_ENTITIES];
    private Metabolism[] _metabolisms = new Metabolism[MAX_ENTITIES];
    private Movement[] _movements = new Movement[MAX_ENTITIES];
    private Diet[] _diets = new Diet[MAX_ENTITIES];
    private Stack<int> _freeSlots = new Stack<int>();
    private int _entityCount = 0;
    
    public int AllocateEntitySlot()
    {
        if (_freeSlots.Count > 0) {return _freeSlots.Pop();}
        
        if (_entityCount >= MAX_ENTITIES){ throw new InvalidOperationException("World full");}
        
        return _entityCount++;
    }
    
    public void FreeEntitySlot(int slot)
    {
        _entities[slot].IsAlive = false;
        _freeSlots.Push(slot);
    }
}