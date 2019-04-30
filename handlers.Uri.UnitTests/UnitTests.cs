using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Synapse.Core;
using Synapse.Core.Utilities;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace handlers.Uri.UnitTests
{
    [TestFixture]
    public class UnitTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            Directory.SetCurrentDirectory( Path.Combine( System.Reflection.Assembly.GetExecutingAssembly().Location, @"..\..\..\Files" ) );
        }

        [Test]
        [Category( "File" )]
        [TestCase( "yaml_in.yaml", ReturnFormat.Yaml )]
        [TestCase( "json_in.json", ReturnFormat.Json )]
        [TestCase( "appConfig.xml", ReturnFormat.Xml )]
        public void FileAsLocalPath(string file, ReturnFormat format)
        {
            SimpleUriHandler handler = new SimpleUriHandler();
            string uri = Path.Combine( Environment.CurrentDirectory, file );

            SimpleUriHandlerParameters parms = new SimpleUriHandlerParameters() { Uri = uri, Format = format };
            string result = handler.GetFileUri( parms.ParsedUri.ToString() );

            string expectedResult = File.ReadAllText( uri );
            Assert.AreEqual( expectedResult, result );

            object o = handler.FormatData( result, parms.Format );
        }

        [Test]
        [Category( "File" )]
        [TestCase( "yaml_in.yaml", ReturnFormat.Yaml )]
        [TestCase( "json_in.json", ReturnFormat.Json )]
        [TestCase( "xml_in.xml", ReturnFormat.Xml )]
        // ensure Server service is started or the share name will not be available
        public void FileAsUncPath(string file, ReturnFormat format)
        {
            SimpleUriHandler handler = new SimpleUriHandler();
            string uri = $@"\\{Environment.MachineName}\" + Path.Combine( Environment.CurrentDirectory, file ).ToLower().Replace( "c:", "c$" );

            SimpleUriHandlerParameters parms = new SimpleUriHandlerParameters() { Uri = uri, Format = format };
            string result = handler.GetFileUri( parms.ParsedUri.ToString() );

            string expectedResult = File.ReadAllText( uri );
            Assert.AreEqual( expectedResult, result );

            object o = handler.FormatData( result, parms.Format );
        }

        [Test]
        [Category( "File" )]
        [TestCase( "yaml_in.yaml", ReturnFormat.Yaml )]
        [TestCase( "json_in.json", ReturnFormat.Json )]
        [TestCase( "xml_in.xml", ReturnFormat.Xml )]
        public void FileAsUriPath(string file, ReturnFormat format)
        {
            SimpleUriHandler handler = new SimpleUriHandler();
            string uri = $"file://" + Path.Combine( Environment.CurrentDirectory, file );

            SimpleUriHandlerParameters parms = new SimpleUriHandlerParameters() { Uri = uri, Format = format };
            string result = handler.GetFileUri( parms.ParsedUri.ToString() );

            string expectedResult = File.ReadAllText( Path.Combine( Environment.CurrentDirectory, file ) );
            Assert.AreEqual( expectedResult, result );

            object o = handler.FormatData( result, parms.Format );
        }
        [Test]
        [Category( "File" )]
        [TestCase( "yaml_in.yaml", ReturnFormat.Yaml )]
        [TestCase( "json_in.json", ReturnFormat.Json )]
        [TestCase( "xml_in.xml", ReturnFormat.Xml )]
        public void MergeData(string file, ReturnFormat format)
        {
            SimpleUriHandler handler = new SimpleUriHandler();
            string uri = Path.Combine( Environment.CurrentDirectory, file );

            SimpleUriHandlerParameters parms = new SimpleUriHandlerParameters() { Uri = uri, Format = format };
            string uriContent = handler.GetFileUri( parms.ParsedUri.ToString() );

            object inputData = handler.FormatData( uriContent, parms.Format );

            if( format == ReturnFormat.Xml )
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml( $@"<PXmlDoc><PNode1>PValue1_file_newvalue</PNode1></PXmlDoc>" );
                handler.MergeData( ref inputData, xmlDoc, format );
            }
            else
            {
                Dictionary<object, object> merge = new Dictionary<object, object>();
                merge.Add( "PNode1", "PValue1_file_newvalue" );
                handler.MergeData( ref inputData, merge, format );
            }

            string result = null;
            string expectedResult = null;
            switch( format )
            {
                case ReturnFormat.Json:
                    {
                        result = JsonConvert.SerializeObject( inputData, Newtonsoft.Json.Formatting.Indented );
                        expectedResult = File.ReadAllText( Path.Combine( Environment.CurrentDirectory, $"{format}_out_mergeData.{format}" ) );
                        object o = JsonConvert.DeserializeObject( expectedResult );
                        expectedResult = JsonConvert.SerializeObject( o, Newtonsoft.Json.Formatting.Indented );
                        break;
                    }
                case ReturnFormat.Yaml:
                    {
                        Serializer serializer = new Serializer();
                        result = serializer.Serialize( inputData );
                        expectedResult = File.ReadAllText( Path.Combine( Environment.CurrentDirectory, $"{format}_out_mergeData.{format}" ) );
                        Dictionary<object, object> o = YamlHelpers.Deserialize( expectedResult );
                        expectedResult = YamlHelpers.Serialize( o );
                        break;
                    }
                case ReturnFormat.Xml:
                    { 
                        result = XmlHelpers.Serialize<string>( inputData );
                        XmlDocument expectedResultXml = new XmlDocument();
                        expectedResultXml.Load( Path.Combine( Environment.CurrentDirectory, $"{format}_out_mergeData.{format}" ) );
                        expectedResult = XmlHelpers.Serialize<string>( expectedResultXml );
                        break;
                    }
            };
            Assert.AreEqual( expectedResult, result );
        }
    }
}