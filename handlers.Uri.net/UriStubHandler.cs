using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using Synapse.Core;
using Synapse.Core.Utilities;

/// <summary>
/// This is a stub handler and will be delete/replaced with fully-featured implmentation.
/// </summary>
public class UriStubHandler : HandlerRuntimeBase
{
    public override object GetConfigInstance() { return null; }
    public override object GetParametersInstance() { return new UriStubHandlerParameters() { Uri = "http://sample/uri" }; }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        ExecuteResult result = new ExecuteResult() { Status = StatusType.Complete };
        string msg = string.Empty;
        Exception exception = null;

        UriStubHandlerParameters parms = DeserializeOrNew<UriStubHandlerParameters>( startInfo.Parameters );

        OnProgress( "Execute", $"Beginning fetch for {parms.Uri}.", StatusType.Running, startInfo.InstanceId, 1 );

        try
        {
            switch( parms.ParsedUri.Scheme )
            {
                case "http":
                case "https":
                {
                    result.ExitData = GetHttpUri( parms.Uri ).Result;
                    break;
                }
                case "file":
                {
                    result.ExitData = GetFileUri( parms.Uri );
                    break;
                }
                case "s3":
                case "blob":
                default:
                {
                    throw new NotSupportedException( $"URI scheme {parms.ParsedUri.Scheme} is not supported." );
                }
            }

            result.ExitData = FormatData( result.ExitData, parms.Format );

            msg = $"Successfully executed HttpClient.Get( {parms.Uri} ).";
        }
        catch( Exception ex )
        {
            result.Status = StatusType.Failed;
            result.ExitData = msg = ex.Message;
            exception = ex;
        }

        OnProgress( "Execute", msg, result.Status, startInfo.InstanceId, Int32.MaxValue, false, exception );

        return result;
    }

    public async Task<string> GetHttpUri(string uri)
    {
        HttpClient client = new HttpClient();
        return await client.GetStringAsync( uri );
    }

    public string GetFileUri(string uri)
    {
        return WebRequestClient.GetString( uri );
    }

    async Task<string> GetUri(string uri)
    {
        string result = null;

        using( HttpClient client = new HttpClient() )
        using( HttpResponseMessage response = await client.GetAsync( uri ) )
        using( HttpContent content = response.Content )
        {
            result = await content.ReadAsStringAsync();
        }

        return result;
    }

    public object FormatData(object data, ReturnFormat format)
    {
        switch( format )
        {
            case ReturnFormat.Yaml:
            {
                return YamlHelpers.Deserialize( data.ToString() );
            }
            case ReturnFormat.Json:
            {
                return YamlHelpers.Deserialize( data.ToString() );
            }
            case ReturnFormat.Xml:
            {
                return XmlHelpers.Deserialize<XmlDocument>( data.ToString() );
            }
            default:
            {
                return data;
            }
        }
    }
}

public enum ReturnFormat
{
    Native,
    Yaml,
    Json,
    Xml
}

public class UriStubHandlerParameters
{
    Uri _uri = null;

    public UriStubHandlerParameters()
    {
    }

    public string Uri { get; set; }
    public Uri ParsedUri { get { return _uri ?? new System.Uri( this.Uri ); } }

    public ReturnFormat Format { get; set; }
}