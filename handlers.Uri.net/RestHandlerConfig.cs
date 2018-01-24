public class RestHandlerConfig
{
    public string Authentication { get; set; } // basic, ntlm etc

    public string Username { get; set; }

    public string Password { get; set; }

    public ulong MaxAllowedResponseLength { get; set; }
}