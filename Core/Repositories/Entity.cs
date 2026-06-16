namespace Core.Repositories;

public class Entity: IEntity,IEntityTimestamps
{
    public long Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }
    public DateTime? ArchivedDate { get; set; }

    public Entity() { }

    public Entity(long id)
        : this()
    {
        Id = id;
    }
}