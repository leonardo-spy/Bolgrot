using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bolgrot.Core.Ankama.Protocol.Types;

namespace Bolgrot.Core.Ankama.Protocol.Messages
{
    public class JobCrafterDirectorySettingsMessage : NetworkMessage
    {

	    public JobCrafterDirectorySettings[] craftersSettings;


        public JobCrafterDirectorySettingsMessage()
        {
        }

        public JobCrafterDirectorySettingsMessage(JobCrafterDirectorySettings[] craftersSettings)
        {
            this.craftersSettings = craftersSettings;

        }
    }
}