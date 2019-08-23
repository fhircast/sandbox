using Common.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FHIRcastSandbox.Core
{
    /// <summary>
    /// SignalR hub for communication with WebSubClient clients. The main communication will be around
    /// new subscriptions to those clients (which come in through the Hub) and notifications of client
    /// updates that affect those subscriptions.
    /// 
    /// TODO: Add notification for unsubscriber
    /// TODO: Move notifications from client to go through here instead of using the post mechanism
    /// TODO: Unit tests for the SignalR connection. No updated unit test documentation for
    ///       SignalR in ASP.NET Core that I could find
    /// </summary>
    public class InternalHub : Hub<IInternalHubClient>
    {
        /// <summary>
        /// The topic for the client(which is currently the signalr connectionid between the websubclienthub and
        /// the javascript client) is different than this signalr connectionid so we need a mapping between the two.
        /// 
        /// TODO: Should the topic for the client be assigned by this hub? so when the client first starts up it asks 
        ///       for its topic id which could then be this signalr hub's connectionid meaning we don't need this collection?
        /// </summary>
        Dictionary<string, string> topicConnectionIdMapping = new Dictionary<string, string>();

        private readonly ILogger<InternalHub> logger;

        public InternalHub(ILogger<InternalHub> logger)
        {
            this.logger = logger;
        }

        #region Calls from Client
        /// <summary>
        /// Called by the client (WebSubClient) to register their topic so the InternalHub knows which client
        /// to talk to given a topic received from an outside caller (i.e. subscription request).
        /// </summary>
        /// <param name="topic"></param>
        public void RegisterTopic(string topic)
        {
            logger.LogDebug($"Registering topic {topic} with InternalHub");
            if (!topicConnectionIdMapping.ContainsKey(topic))
            {
                topicConnectionIdMapping.Add(topic, Context.ConnectionId);
            }
        }
        #endregion

        #region Calls to Client
        /// <summary>
        /// Called when we have validated a subscription request. Informs the client of their new subscriber
        /// </summary>
        /// <param name="topic">Topic being subscribed to</param>
        /// <param name="subscription">Subscription object</param>
        /// <returns></returns>
        public Task NotifyClientOfSubscriber(string topic, SubscriptionRequest subscription)
        {
            if (!topicConnectionIdMapping.ContainsKey(topic))
            {
                logger.LogError($"Could not find a client connection associated with topic {topic}");
                return null;
            }

            logger.LogDebug($"Notifying {topic} of new subscriber {subscription.ToString()}");
            return Clients.Client(topicConnectionIdMapping[topic]).AddSubscriber(subscription);
        }
        #endregion
    }
}
