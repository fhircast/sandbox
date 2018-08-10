// Write your JavaScript code.
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/websubclienthub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("notification", (message) => {
    console.log(message);
});

connection
    .start()
    .catch(err => console.error(err.toString()));

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

$("#subscribe").submit(function (e) {

  let form = $(this);
  let url = form.attr("action");

  let data = form.serialize();
  connectionId = connection.id;
  data += `&connectionId=${connectionId}`;

  console.log(`Subscribe data: ${data}`);

  $.ajax({
    type: "POST",
    url: url,
    data: data,
    success: function (response) { },
    failure: function (response) {
      console.error(`Failed to subscribe: ${JSON.stringify(response)}`);
    },
    error: function (response) {
      console.error(`Error when subscribing: ${JSON.stringify(response)}`);
    }
  });

  e.preventDefault();
});
