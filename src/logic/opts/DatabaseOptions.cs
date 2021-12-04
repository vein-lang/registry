namespace core;

using System.ComponentModel.DataAnnotations;

public class DatabaseOptions
{
    public string Type { get; set; }

    [Required]
    public string ConnectionString { get; set; }
}