using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebChat
{
    public class ChatMiddleWare
    {
        private static ConcurrentDictionary<string, WebSocket> m_sockets = new ConcurrentDictionary<string, WebSocket>();

        private readonly RequestDelegate m_next;

        public ChatMiddleWare(RequestDelegate next)
        {
            m_next = next;
        }

        /// <summary>
        /// Вызов выполнения middleware
        /// </summary>
        /// <param name="context">запрос</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            //если не websocket, переходим на выполнение _next
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await m_next.Invoke(context);
                return;
            }

            //токен прерывания выполнения асинхронной перации
            CancellationToken ct = context.RequestAborted;

            WebSocket currentSocket = await context.WebSockets.AcceptWebSocketAsync();
            var socketId = Guid.NewGuid().ToString();
            m_sockets.TryAdd(socketId, currentSocket);
            {
                SendInitMessage(socketId, ct);
                SendClientListMessage(ct);
            }

            //крутитмся в цикле пока возможно
            while (true)
            {
                if (ct.IsCancellationRequested)
                    break;

                //получение сообщения от текущего сокета
                var response = await ReceivePacketAsync(currentSocket, ct);
                if (response == null)
                {
                    if (currentSocket.State != WebSocketState.Open)
                        break;
                    continue;
                }

                AnalizePacket(response, ct);
            }

            WebSocket dummy;
            m_sockets.TryRemove(socketId, out dummy);

            await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
            currentSocket.Dispose();
        }

        /// <summary>
        /// Получение пакета от клиента
        /// </summary>
        /// <param name="socket">сокет</param>
        /// <param name="ct">токен прерывания выполнения задачи</param>
        /// <returns></returns>
        private static async Task<Models.PacketModel> ReceivePacketAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    ct.ThrowIfCancellationRequested();

                    result = await socket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                if (result.MessageType != WebSocketMessageType.Text)
                    return null;

                string jsonStr = Encoding.UTF8.GetString(buffer);
                Models.PacketModel pack = JsonConvert.DeserializeObject<Models.PacketModel>(jsonStr);
                //от клиента можно получить только сообщение
                switch (pack.packetType)
                {
                    case Models.PacketType.Message:
                        return JsonConvert.DeserializeObject<Models.MessageModel>(jsonStr);
                    default:
                        return null;
                }
            }
        }

        private async void AnalizePacket(Models.PacketModel packet, CancellationToken ct = default(CancellationToken))
        {
            switch (packet.packetType)
            {
                case Models.PacketType.Message:
                    Models.MessageModel packetMessage = packet as Models.MessageModel;
                    if(string.IsNullOrEmpty(packetMessage.dst_user))
                    {
                        //отправка сообщений всем клиентам
                        foreach (var socket in m_sockets)
                        {
                            if (socket.Value.State != WebSocketState.Open)
                                continue;
                            await SendPacketAsync(socket.Value, packetMessage, ct);
                        }
                    }
                    else
                    {
                        WebSocket socket;
                        if (m_sockets.TryGetValue(packetMessage.dst_user, out socket))
                            await SendPacketAsync(socket, packet, ct);
                        if (packetMessage.src_user != packetMessage.dst_user &&
                            m_sockets.TryGetValue(packetMessage.src_user, out socket))
                            await SendPacketAsync(socket, packet, ct);
                    }
                    break;
                default:
                    return;
            }

        }

        private async void SendInitMessage(string socketId, CancellationToken ct = default(CancellationToken))
        {
            Models.InitModel packet = new Models.InitModel { socketId = socketId };
            WebSocket socket;
            if (m_sockets.TryGetValue(socketId, out socket))
                await SendPacketAsync(socket, packet, ct);
        }

        private async void SendClientListMessage(CancellationToken ct = default(CancellationToken))
        {
            Models.ClientListModel packet = new Models.ClientListModel 
            {
                clientList = m_sockets
                .Where(x=>x.Value.State == WebSocketState.Open)
                .Select(x=>x.Key)
                .ToList() 
            };

            foreach (var socket in m_sockets)
            {
                if (socket.Value.State != WebSocketState.Open)
                    continue;
                await SendPacketAsync(socket.Value, packet, ct);
            }
        }

        /// <summary>
        /// Отправка сообщения клиенту
        /// </summary>
        /// <param name="socket">сокет</param>
        /// <param name="packet">пакет данных</param>
        /// <param name="ct">токен прерывания задачи</param>
        /// <returns></returns>
        private static Task SendPacketAsync(WebSocket socket, Models.PacketModel packet, CancellationToken ct = default(CancellationToken))
        {
            var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));
            var segment = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }

    }
}
