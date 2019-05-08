// Write your JavaScript code.
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/websubclienthub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection
    .start()
    .catch(err => console.error(err.toString()));

connection.on("notification", (message) => {
    console.debug(message);

    $(".topic").val(message.event["hub.topic"]);
    $(".event").val(message.event["hub.event"]);

    for (var i = 0; i < message.event.context.length; i++) {
        var context = message.event.context[i];
        if (context.key === "patient") {
            $(".patient-identifier").val(context.resource.id);
        }
        if (context.key === "study") {
            $(".study-id").val(context.resource.id);
        }
    }
});

connection.on("error", (errorMsg) => {
    alert("Error on server: /n" + errorMsg);
});

connection.on("updatedSubscriptions", (subscriptions) => {
    console.debug("updated subscriptions called with " + subscriptions);
    var subTable = getSubscriptionTable().getElementsByTagName('tbody')[0];
    subTable.innerHTML = "";

    for (var i = 0; i < subscriptions.length; i++) {
        console.debug(subscriptions[i]);
        addSubscriptionToTable(subTable, subscriptions[i]);
    }
});

function getSubscriptionTable() {
    return document.getElementById("clientSubTable");
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

$("#unsubscribe").submit(function (e) {
    console.debug("unsubscribe param: " + e);
    let form = $(this);
    let topic = form[0].attributes("action");

    console.debug("unsubscribing from " + topic);

    connection
        .invoke(
            "unsubscribe", topic)
        .catch(e => console.error(e));

    e.preventDefault();
});