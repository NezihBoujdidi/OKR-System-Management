namespace NXM.Tensai.Back.OKR.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
