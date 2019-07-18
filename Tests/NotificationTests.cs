

using FHIRcastSandbox.Model;
using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace FHIRcastSandbox
{
    public class NotificationTests
    {
        #region Unit Tests
        [Fact]
        public void EqualNotifications_ConfirmEquality_Test()
        {
            Notification notification1 = CreateNotification(1);
            Notification notification2 = CreateNotification(1);
            notification2.Id = notification1.Id;

            Assert.True(notification1.Equals(notification2));
        }

        [Fact]
        public void UnequalNotifications_ConfirmInequality_Test()
        {
            Notification notification1 = CreateNotification(1);
            Notification notification2 = CreateNotification(1); // Create notification uses a new GUID each time

            Assert.False(notification1.Equals(notification2));
        }

        [Fact]
        public void Notification_SingleResource_ConvertToJSONString_Test()
        {
            Notification notification = CreateNotification(1);
            string jsonBody = notification.ToJson();

            string error;

            Assert.True(ValidJson(jsonBody, out error), "Error validating JSON format: " + error);
            Assert.True(ValidTimestamp(jsonBody, out error), "Error validating timestamp: " + error);
            Assert.True(ValidId(jsonBody, out error), "Error validating id: " + error);
            Assert.True(ValidEventObject(jsonBody, out error), "Error validating event: " + error);
        }

        [Fact]
        public void Notification_MultipleResources_ConvertToJSONString_Test()
        {
            Notification notification = CreateNotification(2);
            string jsonBody = notification.ToJson();

            string error;

            Assert.True(ValidJson(jsonBody, out error), "Error validating JSON format: " + error);
            Assert.True(ValidTimestamp(jsonBody, out error), "Error validating timestamp: " + error);
            Assert.True(ValidId(jsonBody, out error), "Error validating id: " + error);
            Assert.True(ValidEventObject(jsonBody, out error), "Error validating event: " + error);

        } 

        [Fact]
        public void Notification_SingleResource_ConvertFromJSONString_Test()
        {
            Notification notification = CreateNotification(1);
            string json = notification.ToJson();

            Notification notificationFromJson = Notification.FromJson(json);


            Assert.True(notification.Equals(notificationFromJson, true));
        }

        #endregion



        private bool ValidJson(string jsonBody, out string error)
        {
            error = "";
            jsonBody = jsonBody.Trim();
            if ((jsonBody.StartsWith("{") && jsonBody.EndsWith("}")) || //For object
                (jsonBody.StartsWith("[") && jsonBody.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(jsonBody);
                    return true;
                }
                catch (Exception ex) //some exception
                {
                    error = ex.Message;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        #region Property Validator Functions
        private bool ValidTimestamp(string jsonBody, out string error)
        {
            error = "";
            JObject notificationObject = JObject.Parse(jsonBody);

            // Body SHALL contain a timestamp
            if (!JObjectHasKeyWithValue(notificationObject, "timestamp"))
            {
                error = "json does not include timestamp property";
                return false;
            }

            // Value needs to be a valid date time string
            DateTime time;
            if (!DateTime.TryParse(notificationObject.Property("timestamp").Value.ToString(), out time))
            {
                error = "timestamp does not conform to ISO 8601 format";
                return false;
            }

            return true;
        }

        private bool ValidId(string jsonBody, out string error)
        {
            error = "";
            JObject notificationObject = JObject.Parse(jsonBody);

            // Body SHALL contain an id
            if (!JObjectHasKeyWithValue(notificationObject, "id"))
            {
                error = "json does not include id property";
                return false;
            }

            return true;
        }

        private bool ValidEventObject(string jsonBody, out string error)
        {
            error = "";
            JObject notificationObject = JObject.Parse(jsonBody);

            // Body SHALL contain an event
            if (!JObjectHasKeyWithValue(notificationObject, "event"))
            {
                error = "json does not include event property";
                return false;
            }

            JObject eventObj = notificationObject["event"].ToObject<JObject>();

            if (!ValidHubTopic(eventObj, out error)) return false;
            if (!ValidHubEvent(eventObj, out error)) return false;
            if (!ValidContext(eventObj, out error)) return false;

            return true;
        }

        private bool ValidHubTopic(JObject eventObj, out string error)
        {
            error = "";

            // event SHALL contain a hub.topic
            if (!JObjectHasKeyWithValue(eventObj, "hub.topic"))
            {
                error = "event does not include a valid hub.topic property";
                return false;
            }

            // add any topic validation here if there is any

            return true;
        }

        private bool ValidHubEvent(JObject eventObj, out string error)
        {
            error = "";

            // event SHALL contain a hub.event
            if (!JObjectHasKeyWithValue(eventObj, "hub.event"))
            {
                error = "event does not include a valid hub.event property";
                return false;
            }

            // add any event validation here if there is any

            return true;
        }

        private bool ValidContext(JObject eventObj, out string error)
        {
            error = "";

            // event SHALL contain a context
            if (!JObjectHasKeyWithValue(eventObj, "context"))
            {
                error = "event does not include a valid context property";
                return false;
            }

            JToken context = eventObj["context"];
            if (context.Type != JTokenType.Array)
            {
                error = "context is not an array type";
                return false;
            }

            foreach (JObject resource in context.Children<JObject>())
            {
                if (!ValidResource(resource, out error)) return false;
            }

            return true;
        }

        private bool ValidResource(JObject resourceObj, out string error)
        {
            error = "";

            // resource SHALL have a key property
            if (!JObjectHasKeyWithValue(resourceObj, "key"))
            {
                error = "resource does not have a valid key property";
                return false;
            }

            if (!JObjectHasKeyWithValue(resourceObj, "resource"))
            {
                error = "resource does not have a valid resource property";
                return false;
            }

            try
            {
                string key = resourceObj["key"].ToString();
                switch (key)
                {
                    case "patient":
                        Hl7.Fhir.Model.Patient patient = resourceObj.ToObject<Hl7.Fhir.Model.Patient>();
                        break;
                    case "imagingstudy":
                        Hl7.Fhir.Model.ImagingStudy study = resourceObj.ToObject<Hl7.Fhir.Model.ImagingStudy>();
                        break;
                    case "encounter":
                        break;
                }
            }
            catch (Exception ex)
            {
                error = "Error parsing resource object: " + ex.Message;
                return false;
            }

            return true;
        }

        private bool JObjectHasKeyWithValue(JObject jObject, string key)
        {
            if (!jObject.ContainsKey(key)) return false;
            if (jObject[key].ToString() == String.Empty) return false;

            return true;
        }

        #endregion

        private Notification CreateNotification(int numberOfResources)
        {
            Notification notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
            };

            Hl7.Fhir.Model.Resource[] resources = new Hl7.Fhir.Model.Resource[numberOfResources];            

            Hl7.Fhir.Model.Patient patient = new Hl7.Fhir.Model.Patient();
            patient.Id = "abc1234";
            resources[0] = patient;

            if (numberOfResources >= 2)
            {
                Hl7.Fhir.Model.ImagingStudy imagingStudy = new Hl7.Fhir.Model.ImagingStudy();
                imagingStudy.Accession = new Hl7.Fhir.Model.Identifier("accession", "acc123");
                resources[1] = imagingStudy;
            }

            if (numberOfResources >= 3)
            {
                Hl7.Fhir.Model.Encounter encounter = new Hl7.Fhir.Model.Encounter();
                encounter.Id = "enc1234";
                resources[2] = encounter;
            }

            NotificationEvent notificationEvent = new NotificationEvent()
            {
                Topic = "topic1",
                Event = "open-patient-chart",
                Context = resources
            };

            notification.Event = notificationEvent;
            return notification;
        }

        private string SingleResourceNotificationJSONString()
        {
            return @"{
                      ""timestamp"": ""2018-01-08T01:37:05.14"",
                      ""id"": ""q9v3jubddqt63n1"",
                      ""event"": {
                        ""hub.topic"": ""https://hub.example.com/7jaa86kgdudewiaq0wtu"",
                        ""hub.event"": ""open-patient-chart"",
                        ""context"": [
                          {
                            ""key"": ""patient"",
                            ""resource"": {
                              ""resourceType"": ""Patient"",
                              ""id"": ""ewUbXT9RWEbSj5wPEdgRaBw3"",
                              ""identifier"": [
                                 {
                                   ""type"": {
                                        ""coding"": [
                                            {
                                                ""system"": ""http://terminology.hl7.org/CodeSystem/v2-0203"",
                                                ""value"": ""MR"",
                                                ""display"": ""Medication Record Number""
                                             }                                            
                                          ],
                                          ""text"": ""MRN""
                                      }
                                  }
                              ]
                            }
                          }
                        ]
                      }
                    }";
        }
    }
}
