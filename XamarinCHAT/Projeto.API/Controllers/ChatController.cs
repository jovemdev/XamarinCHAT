using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Projeto.API.Controllers
{
    [RoutePrefix("api/chat")]
    public class ChatController : ApiController
    {
        private NotificationHubClient _hubClient;

        public ChatController()
        {
            _hubClient = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://pushmaratona02.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=Qbm7rcQSSFEpf3ss7x/+EmCE2NQ9qeJ9eih6YRN+uP0=", "push-maratona02");
        }

        [HttpGet]
        [Route("listar")]
        public async Task<IHttpActionResult> ObterRegistrados()
        {
            List<RegistroViewModel> listaDeRetorno = new List<RegistroViewModel>();
            var listaDeRegistrados = await _hubClient.GetAllRegistrationsAsync(100);

            foreach (var registrationDescription in listaDeRegistrados)
            {
                string registrationId = registrationDescription.RegistrationId;
                List<string> tags = registrationDescription.Tags.ToList();
                
                if (!tags.Any(p => p.Contains("$InstallationId")))
                {
                    listaDeRetorno.Add(new RegistroViewModel()
                    {
                        RegistratioId = registrationId,
                        Tags = tags
                    });
                }
            }

            var result = new { sucess = true, dados = listaDeRetorno };
            return Ok(result);
        }

        [HttpPost]
        [Route("enviar")]
        public async Task<IHttpActionResult> Enviar(ChatViewModel messageInfo)
        {
            string[] userTag = new string[2];
            userTag[0] = "username:" + messageInfo.To;
            userTag[1] = "from:" + messageInfo.From;

            Microsoft.Azure.NotificationHubs.NotificationOutcome outcome = null;
            HttpStatusCode ret = HttpStatusCode.InternalServerError;

            var notif = "{ \"data\" : {\"message\":\"" + "De " + messageInfo.From + ": " + messageInfo.Message + "\"}}";
            outcome = await _hubClient.SendGcmNativeNotificationAsync(notif, userTag);

            if (outcome != null)
            {
                if (!((outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Abandoned) ||
                    (outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Unknown)))
                {
                    ret = HttpStatusCode.OK;
                }
            }

            var result = new { sucess = true, message = ret == HttpStatusCode.OK ? "Mensagem enviada com sucesso." : "Ocorreu algum problema ao tentar enviar a mensagem." };
            return Ok(result);
        }       
    }

    public class RegistroViewModel
    {
        public string RegistratioId { get; set; }
        public List<string> Tags { get; set; }
    }
    public class ChatViewModel
    {
        public string Message { get; set; }
        public string To { get; set; }
        public string From { get; set; }
    }
}
