namespace DinoLife.Core.Entities;

public struct Entity
{
    public Guid Id;
    public EntityType Type;    
    public ComponentFlags Flags;    // Which components this entity has
    public bool IsAlive;

    public Entity(EntityType type, ComponentFlags flags)
    {
        Id = Guid.NewGuid();
        Type = type;
        Flags = flags;
        IsAlive = true;
    }

    public bool Has(ComponentFlags flag)
        => Flags.HasFlag(flag);

    public void Kill()
    {
        IsAlive = false;
        Flags = ComponentFlags.None;
    }
}