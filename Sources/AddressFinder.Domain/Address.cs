namespace AddressFinder.Domain;

public class Address
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}