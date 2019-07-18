using FHIRcastSandbox.Model;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        #region Properties
        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "event")]
        public NotificationEvent Event { get; set; } = new NotificationEvent(); 
        #endregion

        #region JSON Conversions
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

        public static Notification FromJson(string jsonString)
        {
            Notification notification = new Notification();

            try
            {
                JObject jObject = JObject.Parse(jsonString);

                notification.Timestamp = DateTime.Parse(jObject["timestamp"].ToString());
                notification.Id = jObject["id"].ToString();

                JObject eventObj = jObject["event"].ToObject<JObject>();
                notification.Event = NotificationEvent.FromJson(eventObj);
            }
            catch (Exception ex)
            {
                return null;
            }

            return notification;
        } 
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            try
            {
                Notification that = (Notification)obj;

                if (this.Id != that.Id) return false;
                // For now just check Id but maybe add more checks later
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool Equals(object obj, bool deepEquals)
        {
            if (!deepEquals)
            {
                return this.Equals(obj);
            }

            // Check id to start
            if (!this.Equals(obj))
            {
                return false;
            }

            try
            {
                Notification that = (Notification)obj;

                if (this.Timestamp.Equals(that.Timestamp)) return false;
                if (!this.Event.Equals(that.Event)) return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            string newline = Environment.NewLine;
            return $"timestamp: {Timestamp} {newline}" +
                $"id: {Id} {newline}" +
                $"event: {Event.ToString()}";
        } 
        #endregion      
    }

    /// <summary>
    /// Represents the event object within the overall notification object.
    /// This contains the hub.topic, hub.event, and context array of FHIR resources
    /// </summary>
    public class NotificationEvent
    {
        #region Properties
        [ModelBinder(Name = "hub.topic")]
        [JsonProperty(PropertyName = "hub.topic")]
        public string Topic { get; set; }

        [ModelBinder(Name = "hub.event")]
        [JsonProperty(PropertyName = "hub.event")]
        public string Event { get; set; }

        [JsonProperty(PropertyName = "context")]
        public Resource[] Context { get; set; } 
        #endregion

        #region JSON Conversions
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

        internal static NotificationEvent FromJson(JObject eventObj)
        {
            NotificationEvent notificationEvent = new NotificationEvent();

            try
            {
                notificationEvent.Topic = eventObj["hub.topic"].ToString();
                notificationEvent.Event = eventObj["hub.event"].ToString();

                List<Resource> resources = new List<Resource>();
                JArray context = JArray.FromObject(eventObj["context"]);
                foreach (JObject resource in context)
                {
                    JObject fhirResource = resource["resource"].ToObject<JObject>();
                    if (resource["key"].ToString() == "patient")
                    {
                        Patient patient = new Patient();
                        patient.Id = fhirResource["id"].ToString();
                        resources.Add(patient);
                    }
                    else if (resource["key"].ToString() == "imagingstudy")
                    {
                        ImagingStudy study = new ImagingStudy();
                        study.Id = fhirResource["id"].ToString();
                        resources.Add(study);
                    }
                }

                notificationEvent.Context = resources.ToArray();
            }
            catch (Exception)
            {
                return null;
            }

            return notificationEvent;
        }
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            try
            {
                NotificationEvent that = (NotificationEvent)obj;

                if (this.Topic != that.Topic) return false;
                if (this.Event != that.Event) return false;

                // Verify context equality
                if (this.Context.Length != that.Context.Length) return false;
                foreach (Resource thisResource in this.Context)
                {
                    bool included = false;
                    foreach (Resource thatResource in that.Context)
                    {
                        if (thisResource.ResourceType == thatResource.ResourceType)
                        {
                            if (thisResource.Id == thatResource.Id)
                            {
                                included = true;
                                break;
                            }
                        }
                    }
                    if (!included) return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            string newline = Environment.NewLine;
            string context = "";
            foreach (Resource resource in Context)
            {
                context += $"{resource.ResourceType}: {resource.Id} {newline}";
            }
            return $"hub.topic: {Topic} {newline}" +
                $"hub.event: {Event} {newline}" +
                $"context: {context}";
        } 
        #endregion
    }
}
