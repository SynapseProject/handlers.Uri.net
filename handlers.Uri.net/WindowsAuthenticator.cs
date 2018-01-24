using RestSharp;
using RestSharp.Authenticators;

internal class WindowsAuthenticator : IAuthenticator
{
    public void Authenticate(IRestClient client, IRestRequest request)
    {
        request.UseDefaultCredentials = true;
    }
}