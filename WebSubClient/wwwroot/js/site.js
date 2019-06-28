// Write your JavaScript code.
//#region Classes
class ContextResource {
    name = "";
    properties = [];

    constructor(resourceJson) {
        this.name = resourceJson['name'];
        for (var p in resourceJson.properties) {
            var property = new Array();
            property[p] = resourceJson.properties[p];
            this.addProperty(property);
        }
    }

    addProperty(property) {
        this.properties.push(property);
    }
}

class ClientContext {
    resources = [];

    addResource(resourceJson) {
        var resource = new ContextResource(resourceJson);
        this.resources.push(resource);
    }

    updateClientUI() {
        var contextFields = document.getElementById("contextFields");
        contextFields.innerHTML = "";

        // Parse JSON
        var htmlString = "";
        for (var i = 0; i < this.resources.length; i++) {
            var resource = this.resources[i];

            htmlString += startRow();
            htmlString += startCol();

            htmlString = addToHTML(htmlString, '<h3>' + resource.name + '</h3>');

            for (var j = 0; j < resource.properties.length; j++) {
                var property = resource.properties[j];
                for (var key in property) {
                    htmlString = addToHTML(htmlString, '<label>' + property[key] + '</label>');
                    htmlString = addToHTML(htmlString, '<input type="text" id="' + key + '" class="form-control" /></br >');
                }
            }

            htmlString += endDiv();
            htmlString += endDiv();
        }

        htmlString += startRow();
        htmlString += startCol();
        htmlString += '<label>Topic</label>';
        htmlString += '<input type="text" id="topic" class="form-control" readonly/>';
        htmlString += endDiv();
        htmlString += endDiv();

        htmlString += startRow();
        htmlString += startCol();
        htmlString += '<label>Event</label>';
        htmlString += '<input type="text" id="event" class="form-control" />';
        htmlString += endDiv();
        htmlString += endDiv();

        contextFields.innerHTML = htmlString;
    }

    callbackFunction(clientContext, httpRequest) {
        var clientDefinition = JSON.parse(httpRequest.responseText);

        for (var i = 0; i < clientDefinition.Resources.length; i++) {
            clientContext.addResource(clientDefinition.Resources[i]);
        }

        clientContext.updateClientUI();
    }

    constructor() {
        var request = new XMLHttpRequest();
        var callback = this.callbackFunction;
        var clientContext = this;
        request.open("GET", "../data/clientContextDefinition.json");
        request.send(null);
        request.onreadystatechange = function () {
            if (this.readyState == 4 && this.status == 200) {
                callback(clientContext, this);
            }
        };
    }
}
//#endregion

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/websubclienthub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection
    .start()
    .catch(err => console.error(err.toString()));

var clientTopic = "";

// Initiate session context fields
var context = new ClientContext();

//#region SignalR Connection Functions
connection.on("notification", (message) => {
    console.debug(message);

    $(".topic").val(message.event["hub.topic"]);
    $(".event").val(message.event["hub.event"]);

    for (var i = 0; i < message.event.context.length; i++) {
        var context = message.event.context[i];
        if (context.key === "patient") {
            $(".patientId").val(context.resource.id);
        }
        if (context.key === "study") {
            $(".accession").val(context.resource.id);
        }
    }
});

connection.on("error", (errorMsg) => {
    alert("Error on server: /n" + errorMsg);
});

connection.on("updatedSubscriptions", (subscriptions) => {
    console.debug("updated subscriptions called with " + subscriptions);
    var subTable = getSubscriptionTable(false).getElementsByTagName('tbody')[0];
    subTable.innerHTML = "";

    for (var i = 0; i < subscriptions.length; i++) {
        console.debug(subscriptions[i]);
        // Continue if subscription is unsubscribed
        if (subscriptions[i].mode == 1) {
            continue;
        }
        addSubscriptionToTable(subTable, subscriptions[i]);
    }
});

connection.on("updatedSubscribers", (subscriptions) => {
    console.debug("updated subscriptions called with " + subscriptions);
    var subTable = getSubscriptionTable(true).getElementsByTagName('tbody')[0];
    subTable.innerHTML = "";

    for (var i = 0; i < subscriptions.length; i++) {
        console.debug(subscriptions[i]);
        // Continue if subscription is unsubscribed
        if (subscriptions[i].mode == 1) {
            continue;
        }
        addSubscriptionToTable(subTable, subscriptions[i]);
    }
});
//#endregion

//#region HTML Functions
function startRow() {
    return '<div class="row">';
}

function startCol() {
    return '<div class="col">';
}

function endDiv() {
    return '</div>';
}

function addToHTML(htmlString, contentToAdd) {
    return htmlString + contentToAdd;
}
//#endregion


function getSubscriptionTable(subscribers) {
    var tableID = "";
    if (subscribers) {
        tableID = "clientsSubscribersTable";
    } else {
        tableID = "clientsSubscriptionTable";
    }
    return document.getElementById(tableID);
}

function unsubscribe(topic) {
    console.debug("unsubscribing from " + topic);

    connection
        .invoke(
            "unsubscribe", topic)
        .catch(e => console.error(e));
}

function addSubscriptionToTable(table, subscription) {
    var newRow = table.insertRow(table.rows.length);

    var urlCell = newRow.insertCell(0);
    var topicCell = newRow.insertCell(1);
    var eventCell = newRow.insertCell(2);
    var unsubscribeCell = newRow.insertCell(3);

    var urlText = document.createTextNode(subscription.hubURL.url);
    var topicText = document.createTextNode(subscription.topic);
    var eventsText = document.createTextNode(subscription.events.join(","));
    var unsubscribeBtn = document.createElement('input');
    unsubscribeBtn.type = "button";
    unsubscribeBtn.className = "btn btn-secondary";
    unsubscribeBtn.value = "Unsubscribe";
    unsubscribeBtn.id = "unsub";
    unsubscribeBtn.onclick = (function (topic) {
        return function () {
            unsubscribe(subscription.topic);
        };
    })(subscription.topic);

    urlCell.appendChild(urlText);
    topicCell.appendChild(topicText);
    eventCell.appendChild(eventsText);
    unsubscribeCell.appendChild(unsubscribeBtn);
}

function addHttpHeader() {
    var tbl = document.getElementById("tblHttpHeaders");

    var newRow = tbl.insertRow(tbl.rows.length);

    var nameCell = newRow.insertCell();
    var valueCell = newRow.insertCell();

    var nameText = document.createElement('input');
    nameText.type = "text";

    var valueText = document.createElement('input');
    valueText.type = "text";

    nameCell.appendChild(nameText);
    valueCell.appendChild(valueText);
}

//#region Button events
$("#subscribe").submit(function (e) {
    let form = $(this);
    let url = form.attr("action");

    console.debug("subscribing to " + this["subscriptionUrl"].value);

    var tbl = document.getElementById("tblHttpHeaders");
    let headers = [];
    for (var i = 0; i < tbl.rows.length; i++) {
        if (tbl.rows[i].cells[0].children[0].value === "") {
            continue;
        }
        headers[i] = tbl.rows[i].cells[0].children[0].value + ":" + tbl.rows[i].cells[1].children[0].value;
    }

    let eventChkBoxes = ["chkOpenPatient", "chkClosePatient", "chkOpenStudy", "chkCloseStudy"];
    let events = "";
    for (var i = 0; i < eventChkBoxes.length; i++) {
        if (this[eventChkBoxes[i]].checked) {
            if (events === "") {
                events = this[eventChkBoxes[i]].value;
            } else {
                events += "," + this[eventChkBoxes[i]].value;
            }
        }
    }
    if (this["events"].value !== "") {
        if (events === "") {
            events = this["events"].value;
        } else {
            events += "," + this["events"].value;
        }
    }

    connection
        .invoke(
            "subscribe",
            this["subscriptionUrl"].value,
            this["topic"].value,
            events,
            headers)
        .catch(e => console.error(e));

    e.preventDefault();
});

$("#update").submit(function (e) {
    let sessionContext = {

        'topic': clientTopic,
        'event': this["event"].value,
    };
    var resources = [];
    var count = 0;
    for (var i = 0; i < context.resources.length; i++) {
        var resource = context.resources[i];
        var resourceContext = {};
        var hasData = false;
        for (var j = 0; j < resource.properties.length; j++) {
            var property = resource.properties[j];
            for (var key in property) {
                if (this[key].value !== "") {
                    resourceContext[key] = this[key].value;
                    hasData = true;
                }                
            }
        }
        if (hasData) {
            var temp = {};
            temp[resource.name] = resourceContext;
            //resourceContext['name'] = resource.name;
            resources[count++] = temp; //resourceContext;
        }       
    }
    sessionContext['resources'] = resources;
    var data = {
        'action': 'event',
        'context': sessionContext
    };


    e.preventDefault();
});
//#endregion

function getCurrentContext() {

}

//#region Alert Functions
function popupNotification(message) {
    document.getElementById("alertText").innerHTML = message;
    $('#alertPlaceholder').fadeIn(1);
    setTimeout(notificationTimeout, 3000);
}

function notificationTimeout() {
    $('#alertPlaceholder').fadeOut(3000, "swing", clearNotification);
}

function clearNotification() {
    document.getElementById("alertText").innerHTML = "";
}
//#endregion

