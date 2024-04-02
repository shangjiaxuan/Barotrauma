using Barotrauma.Networking;
using System.Collections.Generic;
using System.Linq;

namespace Barotrauma
{
    partial class LuaCsNetworking
    {
        private const int MaxRegisterPerClient = 1000;

        private Dictionary<string, int> clientRegisterCount = new Dictionary<string, int>();

        private ushort currentId = 0;

        public void NetMessageReceived(IReadMessage netMessage, ClientPacketHeader header, Client client = null)
        {
            if (header != ClientPacketHeader.LUA_NET_MESSAGE)
            {
                GameMain.LuaCs.Hook.Call("netMessageReceived", netMessage, header, client);
                return;
            }

            LuaCsClientToServer luaCsHeader = (LuaCsClientToServer)netMessage.ReadByte();

            switch (luaCsHeader)
            {
                case LuaCsClientToServer.NetMessageString:
                    HandleNetMessageString(netMessage, client);
                    break;

                case LuaCsClientToServer.NetMessageId:
                    HandleNetMessageId(netMessage, client);
                    break;

                case LuaCsClientToServer.RequestAllIds:
                    WriteAllIds(client);
                    break;

                case LuaCsClientToServer.RequestSingleId:
                    RequestIdSingle(netMessage, client);
                    break;
            }
        }

        private void HandleNetMessageId(IReadMessage netMessage, Client client = null)
        {
            ushort id = netMessage.ReadUInt16();

            if (idToString.ContainsKey(id))
            {
                string name = idToString[id];

                HandleNetMessage(netMessage, name, client);
            }
            else
            {
                if (GameSettings.CurrentConfig.VerboseLogging)
                {
                    LuaCsLogger.LogError($"Received NetMessage for unknown id {id} from {GameServer.ClientLogName(client)}.");
                }
            }
        }

        public IWriteMessage Start(string netMessageName)
        {
            var message = new WriteOnlyMessage();

            message.WriteByte((byte)ServerPacketHeader.LUA_NET_MESSAGE);

            if (stringToId.ContainsKey(netMessageName))
            {
                message.WriteByte((byte)LuaCsServerToClient.NetMessageId);
                message.WriteUInt16(stringToId[netMessageName]);
            }
            else
            {
                message.WriteByte((byte)LuaCsServerToClient.NetMessageString);
                message.WriteString(netMessageName);
            }

            return message;
        }

        public void Receive(string netMessageName, LuaCsAction callback)
        {
            RegisterId(netMessageName);

            netReceives[netMessageName] = callback;
        }

        public ushort RegisterId(string name)
        {
            if (stringToId.ContainsKey(name))
            {
                return stringToId[name];
            }

            if (currentId >= ushort.MaxValue)
            {
                LuaCsLogger.LogError($"Tried to register more than {ushort.MaxValue} network ids!");
                return 0;
            }

            currentId++;

            idToString[currentId] = name;
            stringToId[name] = currentId;

            WriteIdToAll(currentId, name);

            return currentId;
        }

        private void RequestIdSingle(IReadMessage netMessage, Client client)
        {
            string name = netMessage.ReadString();

            if (!stringToId.ContainsKey(name) && client.AccountId.TryUnwrap(out AccountId id))
            {
                if (!clientRegisterCount.ContainsKey(id.StringRepresentation)) 
                { 
                    clientRegisterCount[id.StringRepresentation] = 0; 
                }

                clientRegisterCount[id.StringRepresentation]++;

                if (clientRegisterCount[id.StringRepresentation] > MaxRegisterPerClient)
                {
                    LuaCsLogger.Log($"{GameServer.ClientLogName(client)} Tried to register more than {MaxRegisterPerClient} Ids!");
                    return;
                }
            }

            RegisterId(name);
        }

        private void WriteIdToAll(ushort id, string name)
        {
            WriteOnlyMessage message = new WriteOnlyMessage();
            message.WriteByte((byte)ServerPacketHeader.LUA_NET_MESSAGE);
            message.WriteByte((byte)LuaCsServerToClient.ReceiveIds);

            message.WriteUInt16(1);
            message.WriteUInt16(id);
            message.WriteString(name);

            Send(message, null, DeliveryMethod.Reliable);
        }

        private void WriteAllIds(Client client)
        {
            WriteOnlyMessage message = new WriteOnlyMessage();
            message.WriteByte((byte)ServerPacketHeader.LUA_NET_MESSAGE);
            message.WriteByte((byte)LuaCsServerToClient.ReceiveIds);

            message.WriteUInt16((ushort)idToString.Count());
            foreach ((ushort id, string name) in idToString)
            {
                message.WriteUInt16(id);
                message.WriteString(name);
            }

            Send(message, client.Connection, DeliveryMethod.Reliable);
        }

        public void ClientWriteLobby(Client client) => GameMain.Server.ClientWriteLobby(client);

        public void Send(IWriteMessage netMessage, NetworkConnection connection = null, DeliveryMethod deliveryMethod = DeliveryMethod.Reliable)
        {
            if (connection == null)
            {
                foreach (NetworkConnection conn in Client.ClientList.Select(c => c.Connection))
                {
                    GameMain.Server.ServerPeer.Send(netMessage, conn, deliveryMethod);
                }
            }
            else
            {
                GameMain.Server.ServerPeer.Send(netMessage, connection, deliveryMethod);
            }
        }

        public void UpdateClientPermissions(Client client)
        {
            GameMain.Server.UpdateClientPermissions(client);
        }

        public int FileSenderMaxPacketsPerUpdate
        {
            get { return FileSender.FileTransferOut.MaxPacketsPerUpdate; }
            set { FileSender.FileTransferOut.MaxPacketsPerUpdate = value; }
        }
    }
}
