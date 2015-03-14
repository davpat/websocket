using System;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace SignalRChat
{
    public class ChatHub : Hub
    {
        public void NewContosoChatMessage(string name, string message)
        {
            // Call the addNewMessageToPage method to update clients.
            Clients.All.addContosoChatMessageToPage(name, message);
        }
    }
}