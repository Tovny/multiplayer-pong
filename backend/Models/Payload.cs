namespace backend.Models;

class IPayload
{
    public string action { get; set; } = string.Empty;
    public string payload { get; set; } = string.Empty;
    public double paddle { get; set; }
}