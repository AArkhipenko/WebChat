let webSocket = null;

$(document).ready(function () {

    webSocket = new ChatWebSocket();

    $(webSocket).on(ChatWebSocket.Events.SignalClose, function (event) {
        $("#message_buffer").append("Connection lost \r\n");
        $("#users_list tbody").html("");
    });

    $(webSocket).on(ChatWebSocket.Events.SignalError, function (event) {
        $("#message_buffer").append("Error \r\n");
    });

    $(webSocket).on(ChatWebSocket.Events.SignalList, function (event, data) {
        $("#users_list tbody").html("");
        for (var i = 0; i < data.clientList.length; i++) {
            var td = $("<td>", {
                html: data.clientList[i],
                click: function () {
                    $("#text_message").val(`/user \"${$(this).html()}\": `)
                }
            });
            var tr = $("<tr>");
            tr.append(td);
            $("#users_list tbody").append(tr);
        }
    });

    $(webSocket).on(ChatWebSocket.Events.SignalMessage, function (event, data) {
        let log = "[" + data.src_user;
        if (data.dst_user != null)
            log += " -> " + data.dst_user;
        log += "] " + data.message;
        $("#message_buffer").append(log + "\r\n");
    });

    $(webSocket).on(ChatWebSocket.Events.SignalOpen, function (event) {
        $("#message_buffer").append("Connection create \r\n");
    });

    $(webSocket).on(ChatWebSocket.Events.SignalInit, function (event, data) {
        $("#message_buffer").append(`You are "${data.socketId}" \r\n`);
    });

    $("#text_message").on("keypress", function (event) {
        if (event.keyCode == 13) {
            var str = $(this).val().trim();
            if (str == null ||
                str == '')
                return;
            let dst_user = null;
            let message = null;
            const match = $(this).val().match(/^(\/user "(?<dst_user>.*)":\s*)?(?<message>.*)$/)
            if (match.groups != null) {
                if (match.groups.dst_user != null)
                    dst_user = match.groups.dst_user;
                if (match.groups.message != null)
                    message = match.groups.message;
                webSocket.SendMessage(message, dst_user);
            }
            $(this).val("");
        }
    })
});