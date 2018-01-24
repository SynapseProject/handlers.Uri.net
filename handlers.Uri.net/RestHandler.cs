using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            Authentication = "basic",
            MaxAllowedResponseLength = 500000,
            Username = "xxxxxx",
            Password = "xxxxxx"
        };
    }

    public override object GetParametersInstance()
    {
        return new ClientRequest()
        {
            Authentication = "NONE",
            Body = "",
            Headers = null,
            Method = "GET",
            Url = "http://xxx.com",
            Username = "XXX",
            Password = "XXX"
        };
    }

    public override ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        int sequenceNumber = 0;
        const string context = "Execute";

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

            RestClient client = new RestClient( parms.Url );
            //                client.Proxy = new WebProxy("http://127.0.0.1:8888");

            switch ( parms.Authentication.ToUpper() )
            {
                case "BASIC":
                    client.Authenticator = new HttpBasicAuthenticator( parms.Username, parms.Password );
                    break;
                case "DIGEST":
                    client.Authenticator = new DigestAuthenticator( parms.Username, parms.Password );
                    break;
                case "NTLM":
                    if ( string.IsNullOrWhiteSpace( parms.Username ) && string.IsNullOrWhiteSpace( parms.Password ) )
                        client.Authenticator = new WindowsAuthenticator();
                    else
                        client.Authenticator = new NtlmAuthenticator( parms.Username, parms.Password );
                    break;
                case "OAUTH1":
                    client.Authenticator = new OAuth1Authenticator();
                    break;
                case "OAUTH2":
                    client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator( "xxx", "yyy" );
                    break;
                case "NONE":
                    break;
                default:
                    throw new Exception( "Authentication specified is invalid or not supported." );
            }

            bool isValidMethod = Enum.TryParse( parms.Method.ToUpper(), out Method method );
            if ( isValidMethod )
            {
                RestRequest request = new RestRequest( method );
                if ( method == Method.POST || method == Method.PUT )
                {
                    request.AddParameter( "application/json", parms.Body, ParameterType.RequestBody );
                }
                if (!startInfo.IsDryRun)
                {
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
            }
            else
            {
                throw new Exception( $"'{parms.Method}' is not a valid HTTP method." );
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
            throw new Exception( "Http method specified is not valid." );

        if ( !IsValidAuthentication( request.Authentication ) )
            throw new Exception( "Authentication specified is not valid." );

        if ( !IsValidProtocol( request.Url ) )
            throw new Exception( "Protocol specified is not valid." );
    }

    private static bool IsValidAuthentication(string authentication = "NONE")
    {
        if ( string.IsNullOrWhiteSpace( authentication ) )
            authentication = "NONE";

        List<string> validAuthentications = new List<string>()
        {
            "BASIC",
            "DIGEST",
            "NONE",
            "NTLM",
            "OAUTH1",
            "OAUTH2"
        };
        return validAuthentications.Contains( authentication.ToUpper() );
    }

    private static bool IsValidProtocol(string url)
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
            urlScheme = newUri.Scheme; // Always in lower-case
        }
        catch ( Exception )
        {
            // ignored
        }
        return schemes.Contains( urlScheme );
    }

    private static bool IsValidHttpMethod(string method)
    {
        if ( string.IsNullOrWhiteSpace( method ) )
            return false;

        List<string> validMethods = new List<string>()
        {
            "GET",
            "POST",
            "PUT",
            "DELETE"
        };
        return validMethods.Contains( method.ToUpper() );
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