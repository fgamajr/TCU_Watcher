namespace TCUWatcher.Domain.Entities;

public class User
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public List<string> Roles { get; set; } = new();
}
