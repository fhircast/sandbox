# FHIRcast Sandbox 

The FHIRcast Sandbox is a tool that allows users to simulate the workflow of the [FHIRcast](http://fhircast.org/) standard. It acts as a sort of "mock"-EHR or PACS that can be used as a demonstration and testing tool for showing how FHIRcast would work with subscribers.

## How it Works


## Local development

You can develop on and run this project locally by using the following steps below. 

### Development without Docker (recommended)

#### Setup

First, install the [.NET Core SDK](http://dot.net).

#### Run it

In order to run the webserver locally, run:

```sh
dotnet run
```

You can then start issuing HTTP requests to the server. Here's an example using curl that will go through a request-notify workflow:

```sh
events='switch-patient-chart'
event='switch-patient-chart'
topic='some_topic'
# Make a subscription towards the hub.
curl http://localhost:5000/api/hub -d "hub.callback=http://localhost:5000/api/echo&hub.mode=subscribe&hub.topic=${topic}&hub.events=${events}&hub.secret=very-secret" \
  `# Since the subscription is validated asynchronously, we wait for one second.` \
  && sleep 1 \
  `# List the subscriptions of teh hub.` \
  && curl http://localhost:5000/api/hub \
  `# Notify the hub that something happened using teh non-standard client API of` \
  `# the sandbox web client. This will be converted by the hub to a FHIRcast` \
  `# notification and sent to all subscribers that match the topic/event.` \
  && curl -i -d "{ \"topic\": \"${topic}\", \"event\": \"${event}\", \"patientIdentifier\": \"abc123\" }" http://localhost:5000/api/hub/notify -H 'Content-Type:application/json'
```

## Build and Contribution

We welcome any contributions to help further enhance this tool for the FHIRcast community! To contribute to this project, please see instructions above for running the application locally and testing the app to make sure the tool works as expected with your incorporated changes. Then follow the steps below.

1. Issue a pull request on the `fhircast/sandbox` repository with your changes for review.
