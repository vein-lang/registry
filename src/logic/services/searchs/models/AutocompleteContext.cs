namespace core.services.searchs.models;

using Newtonsoft.Json;

public class AutocompleteContext
{
    public static readonly AutocompleteContext Default = new()
    {
        Vocab = "http://schema.registry.vein-lang.org/schema#"
    };

    [JsonProperty("@vocab")]
    public string Vocab { get; set; }
}
