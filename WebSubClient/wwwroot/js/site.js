// Write your JavaScript code.
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/websubclienthub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

var clientTopic = "";

connection.AlertMessage = function (message) {
    console.debug("Alert message " + message);
    popupNotification(message);
};

connection.start()
    .then(function () {
        connection.invoke("getTopic")
            .then((topic) => {
                clientTopic = topic;
                document.getElementById("topic").value = clientTopic;
            })
            .catch(e => handleError(e));
    })
    .catch(err => handleError(err));

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
        //console.debug(subscriptions[i]);
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

// Handles receiving a notification from one of our subscriptions
connection.on("ReceivedNotification", (notification) => {
    popupNotification(notification);
});

// Handles adding a verified subscription we created
connection.on("AddSubscription", (subscription) => {
    popupNotification(subscription);

    var subTable = getSubscriptionTable(false).getElementsByTagName('tbody')[0];
    addSubscriptionToTable(subTable, subscription);
});

// Handles adding a verified subscription to this client
connection.on("AddSubscriber", (subscription) => {
    popupNotification(subscription);
});

// Handles receiving a message from the hub to be displayed to the user
connection.on("AlertMessage", (message) => {
    popupNotification(message);
});
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
    var clientModel = {
        PatientID: this["patientID"].value,
        AccessionNumber: this["accessionNumber"].value
    };

    connection
        .invoke("update",
            clientTopic,
            this["event"].value,
            clientModel)
        .catch(e => handleError(e));

    e.preventDefault();
});
//#endregion

//#region Alert Functions
function handleError(error) {
    console.log(error);
    popupNotification(error.message);
}

function popupNotification(message) {
    if (!alertDivExists()) {
        addAlertDiv();
    }

    var alertTextSpan = document.getElementById("alertText");

    alertTextSpan.innerHTML = message;
    $('#alertPlaceholder').fadeIn(1);
    setTimeout(notificationTimeout, 3000);
}

function notificationTimeout() {
    $('#alertPlaceholder').fadeOut(3000, "swing", clearNotification);
}

function clearNotification() {
    if (alertDivExists()) {
        document.getElementById("alertText").innerHTML = "";
    }
}

function alertDivExists() {
    return (document.getElementById("alertMessageDiv") != null);
}

function addAlertDiv() {
    var closeButton = document.createElement("a");
    closeButton.setAttribute("class", "close");
    closeButton.setAttribute("data-dismiss", "alert");
    closeButton.innerHTML = "x";

    var textSpan = document.createElement("span");
    textSpan.setAttribute("id", "alertText");

    var element = document.createElement("div");
    element.setAttribute("id", "alertMessageDiv");
    element.setAttribute("class", "alert alert-warning alert-dismissable");
    element.appendChild(closeButton);
    element.appendChild(textSpan);

    document.getElementById("alertPlaceholder").appendChild(element);
    return textSpan;
}
//#endregion

