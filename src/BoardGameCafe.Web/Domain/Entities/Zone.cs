namespace BoardGameCafe.Web.Domain.Entities;

public class Zone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Table> Tables { get; set; } = new List<Table>();
}
