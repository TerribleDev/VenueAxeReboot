namespace VenueAxe.Services;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "mail.tommyparnell.com";
    public int Port { get; set; } = 465;
    public string Username { get; set; } = "bot@tommyparnell.com";
    public string Password { get; set; } = "J5dHmgc14y6Cq7B2puhjHf";
    public string FromEmail { get; set; } = "bot@tommyparnell.com";
    public string FromName { get; set; } = "VenueAxe";
    public bool EnableSsl { get; set; } = true;
}
