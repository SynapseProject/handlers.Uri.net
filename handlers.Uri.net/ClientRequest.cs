public class ClientRequest
{
    public string Authorization { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public string Url { get; set; }

    public string Body { get; set; }

    public HttpHeaders Headers { get; set;}

    public string Method { get; set; } // GET, POST, PUT, DELETE
}