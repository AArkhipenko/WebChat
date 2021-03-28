function ChatWebSocket() {
    const protocol = location.protocol === "https:" ? "wss:" : "ws:";
    const wsUri = protocol + "//" + window.location.host;

    this.socketId = null;

    /**
     * Отправка сообщения
     * @param {any} message - текст сообщения
     * @param {any} dst_user - ид сокет назначения
     */
    this.SendMessage = function (message, dst_user = null) {
        if (this.socketId == null) {
            console.error("Connection wasn't create");
            return;
        }

        var packet = {
            packetType: ChatWebSocket.PacketTypes.Message,
            src_user: this.socketId,
            message: message,
            dst_user: dst_user,
        }
        socket.send(JSON.stringify(packet));
    }

    /**
     * Реакция на создание соединения
     * @param {any} event
     */
    let OnOpen = function (event) {
        console.log("socket opened", event);
        $(this.parent).trigger(ChatWebSocket.Events.SignalOpen);
    };

    /**
     * Реакция на закрытие соединения
     * @param {any} event
     */
    let OnClose = function (event) {
        console.log("socket closed", event);

        this.parent.socketId = null;
        $(this.parent).trigger(ChatWebSocket.Events.SignalClose);
    };

    /**
     * Реакция на событие получения сообщения от сервера
     * @param {any} obj
     */
    let OnRecieveMessage = function (obj) {
        console.log(obj);

        let packet = JSON.parse(obj.data);
        if (packet.packetType == null)
            return;

        switch (packet.packetType) {
            case ChatWebSocket.PacketTypes.Init:
                this.parent.socketId = packet.socketId;
                $(this.parent).trigger(ChatWebSocket.Events.SignalInit, [{ socketId: packet.socketId }]);
                break;
            case ChatWebSocket.PacketTypes.List:
                $(this.parent).trigger(ChatWebSocket.Events.SignalList, [{ clientList: packet.clientList }]);
                break;
            case ChatWebSocket.PacketTypes.Message:
                $(this.parent).trigger(ChatWebSocket.Events.SignalMessage, [{
                    src_user: packet.src_user,
                    message: packet.message,
                    dst_user: packet.dst_user
                }]);
                break;
            default:
                return;
        }
    }

    /**
     * Реакция на возникновение ошибки
     * @param {any} obj
     */
    let OnError = function (obj) {
        console.error(obj.data);
        $(this.parent).trigger(ChatWebSocket.Events.SignalError, obj);
    };


    let socket = new WebSocket(wsUri);
    socket.parent = this;
    socket.onopen = OnOpen;
    socket.onclose = OnClose;
    socket.onmessage = OnRecieveMessage;
    socket.onerror = OnError;
}

ChatWebSocket.Events = {
    SignalOpen: "SignalOpen",
    SignalClose: "SignalClose",
    SignalMessage: "SignalMessage",
    SignalList: "SignalList",
    SignalError: "SignalError",
    SignalInit: "SignalInit",
};

ChatWebSocket.PacketTypes = {
    Init: 1,
    Message: 2,
    List: 3
}