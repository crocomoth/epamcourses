namespace BlazorShared;

public class BaseUrlConfiguration
{
    public const string CONFIG_NAME = "baseUrls";

    public string ApiBase { get; set; }
    public string WebBase { get; set; }
    public string StorageBase { get; set; }
    public string FunctionBase { get; set; }
    public string[] WebBases { get; set; }
    public string AzureBusConnectionString { get; set; }
}
