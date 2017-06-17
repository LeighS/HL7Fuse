using System;
using System.IO;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NHapi.Base.Model;
using NHapi.Base.Parser;

namespace HL7Fuse.Hub.EndPoints
{
    internal class ServiceBusEndPoint : BaseEndPoint
    {
        
        
        #region Public methods
        public override bool Send(IMessage msg)
        {
            try
            {
                // Create management credentials
                TokenProvider credentials = TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey", "0cH6A9wTngFOWGpg3jfI8OYd+4FUGSFJWAsid9EHnKE=");
                // Create namespace client
                //NamespaceManager namespaceClient = new NamespaceManager(ServiceBusEnvironment.CreateServiceUri("sb", "hl7test", string.Empty), credentials);

                //QueueDescription myQueue;
                //myQueue = namespaceClient.CreateQueue("NewMessageQueue");
                MessagingFactory factory = MessagingFactory.Create(ServiceBusEnvironment.CreateServiceUri("sb", "hl7test", string.Empty), credentials);
                QueueClient myQueueClient = factory.CreateQueueClient("NewMessageQueue");

                //{NHapi.Model.V251.Message.MDM_T01}
                var msgDetail = (msg as NHapi.Model.V251.Message.MDM_T01);
                if (msgDetail!= null)
                {
                    var name = msgDetail.PID.GetPatientName();
                    var content =
                        $"<html><body><p>A new appointment has been scheduled for your patient: {msgDetail.PID.PatientID.IDNumber.Value} <b>{name[0].GivenName} {name[0].FamilyName.Surname}</b><p></body></html>";
                    myQueueClient.Send(new BrokeredMessage(GenerateStreamFromString(content)) { CorrelationId = Guid.NewGuid().ToString() });
                }
                
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }
        #endregion
        
    public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

    }
}
