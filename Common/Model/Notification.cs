using FHIRcastSandbox.Model;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace FHIRcastSandbox.Model
{
    /// <summary>
    /// Represents a notification object that is either sent out to other clients or 
    /// received from them in response to a subscribed event
    /// </summary>
    public class Notification : ModelBase
    {
        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "event")]
        public NotificationEvent Event { get; set; } = new NotificationEvent();

        /// <summary>
        /// Creates the JSON string for this notification object as specified by the FHIRcast specs
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();  // overall start object

                // Write timestamp
                writer.WritePropertyName("timestamp");
                writer.WriteValue(Timestamp.ToString());

                // Write id
                writer.WritePropertyName("id");
                writer.WriteValue(Id);

                // Write event
                writer.WritePropertyName("event");
                writer.WriteRawValue(Event.ToJson());

                writer.WriteEndObject();    // overall end object

                return sb.ToString();
            }
        }

        public override string ToString()
        {
            string newline = Environment.NewLine;
            return $"timestamp: {Timestamp} {newline}" +
                $"id: {Id} {newline}" +
                $"event: {Event.ToString()}";
        }
    }

    /// <summary>
    /// Represents the event object within the overall notification object.
    /// This contains the hub.topic, hub.event, and context array of FHIR resources
    /// </summary>
    public class NotificationEvent
    {
        [ModelBinder(Name = "hub.topic")]
        [JsonProperty(PropertyName = "hub.topic")]
        public string Topic { get; set; }

        [ModelBinder(Name = "hub.event")]
        [JsonProperty(PropertyName = "hub.event")]
        public string Event { get; set; }

        [JsonProperty(PropertyName = "context")]
        public Resource[] Context { get; set; }

        /// <summary>
        /// Creates the JSON string for this notification event object as specified by the FHIRcast specs
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                // Write hub.topic
                writer.WritePropertyName("hub.topic");
                writer.WriteValue(Topic);

                // Write hub.event
                writer.WritePropertyName("hub.event");
                writer.WriteValue(Event);

                // Write context
                writer.WritePropertyName("context");
                writer.WriteStartArray();

                foreach (Resource resource in Context)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("key");
                    writer.WriteValue(resource.TypeName.ToLower());

                    writer.WritePropertyName("resource");
                    FhirJsonSerializationSettings settings = new FhirJsonSerializationSettings();
                    settings.Pretty = true;
                    writer.WriteRawValue(resource.ToJson(settings));

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();

                return sb.ToString();
            }
        }

        public override string ToString()
        {
            string newline = Environment.NewLine;
            return $"hub.topic: {Topic} {newline}" +
                $"hub.event: {Event} {newline}" +
                $"context: {Context.ToString()}";
        }
    }
}
