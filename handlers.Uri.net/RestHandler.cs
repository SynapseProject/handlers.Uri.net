using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Synapse.Core;

public class RestHandler : HandlerRuntimeBase
{
    private RestHandlerConfig _config;
    private readonly ExecuteResult _result = new ExecuteResult()
    {
        Status = StatusType.None,
        BranchStatus = StatusType.None,
        Sequence = int.MaxValue
    };
    private string _mainProgressMsg = "";
    // private string _subProgressMsg = "";

    public override object GetConfigInstance()
    {
        return new RestHandlerConfig()
        {
            Authorization = "basic",
            MaxAllowedResponseLength = 500000,
            Username = "xxxxxx",
            Password = "xxxxxx"
        };
    }

    public override object GetParametersInstance()
    {
        return new ClientRequest()
        {
            Authorization = "anonymous",
            Body = "",
            Headers = null,
            Password = "",
            Method = "get",
            Url = "http://xxx.com",
            Username = "xxxx"
        };
    }

    public override ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        int sequenceNumber = 0;
        string context = "Execute";

        try
        {
            _mainProgressMsg = "Processing client request...";
            OnProgress( context, _mainProgressMsg, _result.Status, sequenceNumber );
            string inputParameters = RemoveParameterSingleQuote( startInfo.Parameters );
            ClientRequest parms = DeserializeOrNew<ClientRequest>( inputParameters );

            _mainProgressMsg = "Validating request...";
            _result.Status = StatusType.Running;
            ++sequenceNumber;
            OnProgress( context, _mainProgressMsg, _result.Status, sequenceNumber );

            ValidateClientRequest( parms );

            if (!startInfo.IsDryRun)
            {
                RestClient client = new RestClient( parms.Url );
                if (!string.IsNullOrWhiteSpace(parms.Authorization))
                {
                    switch (parms.Authorization)
                    {
                        case "basic":
                            client.Authenticator = new HttpBasicAuthenticator(parms.Username, parms.Password);
                            break;
                        case "digest":
                            client.Authenticator = new DigestAuthenticator( parms.Username, parms.Password );
                            break;
                        case "ntlm":
                            client.Authenticator = new NtlmAuthenticator(parms.Username, parms.Password);
                            break;
                        case "oauth1":
                            client.Authenticator = new OAuth1Authenticator();
                            break;
                        case "oauth2":
                            client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator("xxx", "yyy");
                            break;
                        default:
                            throw new Exception("Authorization specified is invalid or not supported.");
                    }
                }
                bool isValidMethod = Enum.TryParse( parms.Method, out Method method );
                if (isValidMethod)
                {
                    RestRequest request = new RestRequest(method);
                    if (method == Method.POST || method == Method.PUT )
                    {
                        request.AddParameter("application/json", parms.Body, ParameterType.RequestBody);
                    }
                    IRestResponse response = client.Execute(request);
                    if (response.IsSuccessful)
                    {
                        _result.ExitData = response.Content;
                        _result.ExitCode = 0;
                    }
                    else
                    {
                        _result.ExitCode = -1;
                    }
                    _mainProgressMsg = response.StatusDescription;
                }
                else
                {
                    throw new Exception($"'{parms.Method}' is not a valid HTTP method.");
                }
            }
            
            _mainProgressMsg = startInfo.IsDryRun ? "Dry run execution is completed." : $"Execution is completed. Server Status: {_mainProgressMsg}";
        }
        catch ( Exception ex )
        {
            _mainProgressMsg = $"Execution has been aborted due to: {ex.Message}";
            OnLogMessage( context, _mainProgressMsg, LogLevel.Error );
            _result.Status = StatusType.Failed;
            _result.ExitCode = -1;
        }

        _result.Message = _mainProgressMsg;
        OnProgress( context, _mainProgressMsg, _result.Status, int.MaxValue );

        return _result;
    }

    private void ValidateClientRequest(ClientRequest request)
    {
        if ( request == null )
            throw new Exception( "Client request cannot be null." );

        if ( string.IsNullOrWhiteSpace( request.Url ) )
            throw new Exception( "Url cannot be null or empty." );

        if ( !IsValidHttpMethod( request.Method ) )
            throw new Exception( "Http method specified is not alid." );

        if ( !IsValidAuthorization( request.Authorization ) )
            throw new Exception( "Authorization specified is not valid." );

        if ( !IsValidUrl( request.Url ) )
            throw new Exception( "Url specified is not valid." );
    }

    private bool IsValidAuthorization(string authorization = "none")
    {
        if ( string.IsNullOrWhiteSpace( authorization ) )
            authorization = "none";

        List<string> validAuthorization = new List<string>()
        {
            "basic",
            "digest",
            "none",
            "ntlm",
            "oauth1",
            "oauth2"
        };
        return validAuthorization.Contains( authorization );
    }

    private bool IsValidUrl(string url)
    {
        if ( string.IsNullOrWhiteSpace( url ) )
            return false;

        List<string> schemes = new List<string>()
        {
            "http",
            "https"
        };

        string urlScheme = "";

        try
        {
            Uri newUri = new Uri( url );
            urlScheme = newUri.Scheme;
        }
        catch ( Exception )
        {
            // ignored
        }
        return schemes.Contains( urlScheme );
    }

    private bool IsValidHttpMethod(string method)
    {
        if ( string.IsNullOrWhiteSpace( method ) )
            return false;

        List<string> validMethods = new List<string>()
        {
            "get",
            "post",
            "put",
            "delete"
        };
        return validMethods.Contains( method.ToLower() );

    }

    private static string RemoveParameterSingleQuote(string input)
    {
        string output = "";
        if ( !string.IsNullOrWhiteSpace( input ) )
        {
            Regex pattern = new Regex( ":\\s*'" );
            output = pattern.Replace( input, ": " );
            pattern = new Regex( "'\\s*(\r\n|\r|\n|$)" );
            output = pattern.Replace( output, Environment.NewLine );
        }
        return output;
    }
}

public class HttpHeaders
{
    public List<HttpHeader> Headers { get; set; }
}

public class HttpHeader
{
    public string Key { get; set; }
    public string Value { get; set; }
}

public class FormData
{
    public List<FormDatum> Data { get; set; }
}

public class FormDatum
{
    public string Key { get; set; }
    public string Value { get; set; }
}