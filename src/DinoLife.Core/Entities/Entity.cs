public struct Entity
{
    public Guid Id;
    public EntityType Type;    
    public ComponentFlags Flags;    // Which components this entity has
    public bool IsAlive;
}