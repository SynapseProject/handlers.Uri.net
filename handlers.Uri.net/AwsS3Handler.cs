using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Synapse.Core;


public class AwsS3Handler : HandlerRuntimeBase
{
    private int _sequenceNumber = 0;
    private string _mainProgressMsg = "";
    private string _context = "Execute";
    // private bool _encounteredFailure = false;

    public override object GetConfigInstance()
    {
        return null;
    }

    public override object GetParametersInstance()
    {
        return new AwsS3FileRequest()
        {
            AccessKeyId = "xxxxxx",
            SecretAccessKey = "xxxxxx",
            SessionToken = "xxxxxx",
            BucketName = "xxxxxx",
            ObjectKey = "xxxxxx",
            Uri = "s3://xxxxxx/xxxxxx"
        };
    }

    public override ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        string message;

        try
        {
            message = "Deserializing incoming request...";
            UpdateProgress( message, StatusType.Initializing );
            string inputParameters = startInfo.Parameters;
            AwsS3FileRequest parms = DeserializeOrNew<AwsS3FileRequest>( inputParameters );

            message = "Processing request...";
            UpdateProgress( message, StatusType.Running );

            if (parms != null)
            {
                if (string.IsNullOrWhiteSpace(parms.Uri))
                {
                    
                }
            }
            //            ValidateRequest( parms );
            //            xslt = parms.Xslt;
            //            GetFilteredInstances( parms );
            //
            //            message = "Querying against AWS has been processed" + (_encounteredFailure ? " with error" : "") + ".";
            //            UpdateProgress( message, _encounteredFailure ? StatusType.CompletedWithErrors : StatusType.Success );
            //            _response.Summary = message;
            //            _response.ExitCode = _encounteredFailure ? -1 : 0;

            // AmazonS3Client s3Client = new AmazonS3Client(parms.AccessKey, parms.SecretKey, RegionEndpoint.USEast2 );
            //            var fileStream = s3Client.GetObjectStream("wu2-p3-0392-synapse-001", "Active Directory Role Based Management.pdf");
            //            s3Client.CopyObjectToLocal( "wu2-p3-0392-synapse-001", "Active Directory Role Based Management.pdf", "c:\\temp");
        }
        catch ( Exception )
        {
//            UpdateProgress( ex.Message, StatusType.Failed );
//            _encounteredFailure = true;
//            _response.Summary = ex.Message;
//            _response.ExitCode = -1;
        }

        return null;
    }



    private void UpdateProgress(string message, StatusType status = StatusType.Any, int seqNum = -1)
    {
        _mainProgressMsg = _mainProgressMsg + Environment.NewLine + message;
        if ( status != StatusType.Any )
        {
//            _result.Status = status;
        }
        if ( seqNum == 0 )
        {
            _sequenceNumber = int.MaxValue;
        }
        else
        {
            _sequenceNumber++;
        }
        OnProgress( _context, _mainProgressMsg, status, _sequenceNumber );
    }
}
