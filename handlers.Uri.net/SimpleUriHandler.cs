using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;

using Synapse.Core;
using Synapse.Core.Utilities;

using YamlDotNet.Serialization;

/// <summary>
/// This is a stub handler and will be delete/replaced with fully-featured implmentation.
/// </summary>
public class SimpleUriHandler : HandlerRuntimeBase
{
    public override object GetConfigInstance() { return null; }
    public override object GetParametersInstance() { return new SimpleUriHandlerParameters() { Uri = "http://sample/uri", Format = ReturnFormat.Json }; }

    override public ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        ExecuteResult result = new ExecuteResult() { Status = StatusType.Complete };
        string msg = string.Empty;
        Exception exception = null;
        object exitData = null;

        SimpleUriHandlerParameters parms = DeserializeOrNew<SimpleUriHandlerParameters>( startInfo.Parameters );

        OnProgress( "Execute", $"Beginning fetch for {parms.Uri}.", StatusType.Running, startInfo.InstanceId, 1 );

        try
        {
            switch( parms.ParsedUri.Scheme )
            {
                case "http":
                case "https":
                {
                    exitData = GetHttpUri( parms.Uri ).Result;
                    break;
                }
                case "file":
                {
                    exitData = GetFileUri( parms.Uri );
                    break;
                }
                case "s3":
                case "blob":
                default:
                {
                    throw new NotSupportedException( $"URI scheme {parms.ParsedUri.Scheme} is not supported." );
                }
            }

            exitData = FormatData( exitData, parms.Format );

            if( parms.HasMerge )
                MergeData( ref exitData, parms.Merge, parms.Format );

            result.ExitData = exitData;

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

    public void MergeData(ref object data, object merge, ReturnFormat format)
    {
        switch( format )
        {
            case ReturnFormat.Yaml:
            case ReturnFormat.Json:
            {
                Dictionary<object, object> exitData = data as Dictionary<object, object>;
                YamlHelpers.Merge( ref exitData, merge as Dictionary<object, object> );

                data = exitData;
                break;
            }
            case ReturnFormat.Xml:
            {
                XmlDocument exitData = data as XmlDocument;

                if( merge is XmlNode[] && ((XmlNode[])merge).Length > 0 )
                {
                    foreach( XmlNode node in (XmlNode[])merge )
                    {
                        XmlDocument mergeDoc = new XmlDocument();
                        mergeDoc.LoadXml( $@"<root>{node.OuterXml}</root>" );
                        XmlHelpers.Merge( ref exitData, mergeDoc );
                    }
                }
                else
                {
                    XmlHelpers.Merge( ref exitData, merge as XmlDocument );
                }

                data = exitData;
                break;
            }
            //default:
            //{
            //    return data;
            //}
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

public class SimpleUriHandlerParameters
{
    Uri _uri = null;

    public SimpleUriHandlerParameters()
    {
    }

    public string Uri { get; set; }
    [YamlIgnore]
    public Uri ParsedUri { get { return _uri ?? new System.Uri( this.Uri ); } }

    public object Merge { get; set; }
    [YamlIgnore]
    public bool HasMerge { get { return Merge != null; } }

    public ReturnFormat Format { get; set; }
}