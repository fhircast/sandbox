// Write your JavaScript code.
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/websubclienthub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("notification", (message) => {
    console.debug(message);

    var context = message.event.context[0];
    $(".user-identifier").val(context.UserIdentifier);
    $(".patient-identifier").val(context.PatientIdentifier);
    $(".patient-id-issuer").val(context.PatientIdIssuer);
    $(".accession-number").val(context.AccessionNumber);
    $(".accession-number-group").val(context.AccessionNumberGroup);
    $(".study-id").val(context.StudyId);
});

connection
    .start()
    .catch(err => console.error(err.toString()));

$("#subscribe").submit(function (e) {
    let form = $(this);
    let url = form.attr("action");

    connection
        .invoke(
            "subscribe",
            this["subscriptionUrl"].value,
            this["topic"].value,
            this["events"].value)
        .catch(e => console.error(e));

    e.preventDefault();
});

$("#unsubscribe").submit(function (e) {
    let form = $(this);
    let url = form.attr("action");

    $.ajax({
        type: "POST",
        url: url,
        data: form.serialize(),
        success: function (response) { },
        failure: function (response) {
            console.error(`Failed to unsubscribe: ${JSON.stringify(response)}`);
        },
        error: function (response) {
            console.error(`Error when unsubscribing: ${JSON.stringify(response)}`);
        }
    });

    e.preventDefault();
});