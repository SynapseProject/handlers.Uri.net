using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synapse.Core;

public class AzureBlobHandler: HandlerRuntimeBase
{
    public override object GetConfigInstance()
    {
        return null;
    }

    public override object GetParametersInstance()
    {
        return new AzureBlobFileRequest()
        {
            StorageAccountName = "xxxxxx",
            StorageAccountKey = "xxxxxx",
            ContainerName = "xxxxxx",
            ObjectKey = "xxxxxx",
            Uri = "xxxxxx"
        };
    }

    public override ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        throw new NotImplementedException();
    }
}
