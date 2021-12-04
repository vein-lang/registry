namespace core.services.searchs.models;

using Newtonsoft.Json;

public class SearchContext
{
    public static SearchContext Default(string registrationBaseUrl) => new SearchContext
    {
        Vocab = "http://schema.registry.vein-lang.org/schema#",
        Base = registrationBaseUrl
    };

    [JsonProperty("@vocab")]
    public string Vocab { get; set; }

    [JsonProperty("@base")]
    public string Base { get; set; }
}
