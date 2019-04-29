using EspiaPerdido.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EspiaPerdido.HubConfig
{
    public class JuegoHub : Hub
    {
        private static Situacion[] situaciones = JsonConvert.DeserializeObject<Situacion[]>(File.ReadAllText(@"espia.json"));

        public static List<Client> ConnectedUsers { get; set; } = new List<Client>();
        public static List<Room> OnlineRooms { get; set; } = new List<Room>();

        public async void Ready()
        {
            var c = ConnectedUsers.FirstOrDefault(u => u.ID == Context.ConnectionId);

            c.Room.ReadyUsers.Add(c);
            c.Room.WaitingUsers.Remove(c.Room.WaitingUsers.FirstOrDefault(u => u.ID == Context.ConnectionId));
            await Clients.Group(c.Room.Name).SendAsync("updateUsers", c.Room.WaitingUsers.Count, c.Room.WaitingUsers.Select(u => u.Usuario));
            await Clients.Group(c.Room.Name).SendAsync("updateReadyUsers", c.Room.WaitingUsers.Count + c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Select(u => u.Usuario), c.Room.game);
            Game();
        }

        public async void Game()
        {
            var c = ConnectedUsers.FirstOrDefault(u => u.ID == Context.ConnectionId);

            if (c.Room.WaitingUsers.Count == 0 && c.Room.ReadyUsers.Count > 0 && c.Room.game==false)
            {
                c.Room.game = true;
                Random rnd = new Random();
                var situacion = situaciones[rnd.Next(0, situaciones.Length)];
                var roles = situacion.Roles.ToList();
                List<string> asignaciones = new List<string>();
                var randomIndex = 0;
                for (int i = 0; i < c.Room.ReadyUsers.Count; i++)
                {
                    if (roles.Count == 0)
                    {
                        asignaciones.Add("Espia");
                    }
                    else
                    {
                        randomIndex = rnd.Next(0, roles.Count); //Choose a random object in the list
                        asignaciones.Add(roles[randomIndex]); //add it to the new, random list
                        roles.RemoveAt(randomIndex); //remove to avoid duplicates
                    }
                }
                if (!asignaciones.Contains("Espia"))
                {
                    asignaciones[rnd.Next(0, asignaciones.Count)] = "Espia";
                }
                roles = asignaciones;
                for (int i = 0; i < c.Room.ReadyUsers.Count; i++)
                {
                    randomIndex = rnd.Next(0, roles.Count); //Choose a random object in the list
                    asignaciones.Add(roles[randomIndex]); //add it to the new, random list
                    roles.RemoveAt(randomIndex); //remove to avoid duplicates
                }
                await Clients.Group(c.Room.Name).SendAsync("empezando");
                Task.Delay(4000).Wait();
                int i2 = 0;
                foreach (var user in c.Room.ReadyUsers)
                {
                    string rol = null;
                    rol = asignaciones[i2];
                    i2++;
                    await Clients.Client(user.ID).SendAsync("recibeTarjeta", situacion.Nombre, rol);
                }

            }
        }
        public async Task End()
        {
            var c = ConnectedUsers.FirstOrDefault(u => u.ID == Context.ConnectionId);
            c.Room.game = false;
            await Clients.Group(c.Room.Name).SendAsync("solucion");
            c.Room.WaitingUsers.InsertRange(c.Room.WaitingUsers.Count, c.Room.ReadyUsers);
            c.Room.ReadyUsers.Clear();
            await Clients.Group(c.Room.Name).SendAsync("updateUsers", c.Room.WaitingUsers.Count, c.Room.WaitingUsers.Select(u => u.Usuario));
            await Clients.Group(c.Room.Name).SendAsync("updateReadyUsers", c.Room.WaitingUsers.Count + c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Select(u => u.Usuario), c.Room.game);
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.GetHttpContext().Request.Query["username"];
            string roomToken;
            Room room;
            if(Context.GetHttpContext().Request.Query.ContainsKey("room"))
            {
                roomToken = Context.GetHttpContext().Request.Query["room"];
                room = OnlineRooms.FirstOrDefault(r => r.Name==roomToken);
                if (room == null)
                {
                    room = new Room();
                    room.Name = roomToken;
                    room.WaitingUsers = new List<Client>();
                    room.ReadyUsers = new List<Client>();
                    OnlineRooms.Add(room);
                }
            }
            else
            {
                Random rnd = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                roomToken = new string(Enumerable.Repeat(chars, 3)
                  .Select(s => s[rnd.Next(s.Length)]).ToArray());
                room = new Room();
                room.Name = roomToken;
                room.WaitingUsers = new List<Client>();
                room.ReadyUsers = new List<Client>();
                OnlineRooms.Add(room);
            }
            

            Client c = new Client() {
                Usuario = username,
                ID = Context.ConnectionId,
                Room = room
            };
            ConnectedUsers.Add(c);
            c.Room.WaitingUsers.Add(c);
            await Groups.AddToGroupAsync(c.ID, c.Room.Name);
            await Clients.Group(c.Room.Name).SendAsync("roomToken", c.Room.Name);
            await Clients.Group(c.Room.Name).SendAsync("updateUsers", c.Room.WaitingUsers.Count, c.Room.WaitingUsers.Select(u => u.Usuario));
            await Clients.Group(c.Room.Name).SendAsync("updateReadyUsers", c.Room.WaitingUsers.Count + c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Select(u => u.Usuario), c.Room.game);

        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var c = ConnectedUsers.FirstOrDefault(u => u.ID == Context.ConnectionId);
            c.Room.ReadyUsers.Remove(c);
            c.Room.WaitingUsers.Remove(c);
            if ((c.Room.WaitingUsers.Count + c.Room.ReadyUsers.Count) == 0)
            {
                OnlineRooms.Remove(c.Room);
            }
            Game();
            await Clients.Group(c.Room.Name).SendAsync("updateUsers", c.Room.WaitingUsers.Count, c.Room.WaitingUsers.Select(u => u.Usuario));
            await Clients.Group(c.Room.Name).SendAsync("updateReadyUsers", c.Room.WaitingUsers.Count + c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Count, c.Room.ReadyUsers.Select(u => u.Usuario), c.Room.game);
            await Clients.Caller.SendAsync("close");
            ConnectedUsers.Remove(c);
        }
    }

    public class Client
    {
        public string Usuario { get; set; }
        public string ID { get; set; }
        public Room Room { get; set; }
    }
    public class Room
    {
        public string Name { get; set; }
        public List<Client> WaitingUsers { get; set; } = new List<Client>();
        public List<Client> ReadyUsers { get; set; } = new List<Client>();
        public bool game = false;
    }
}
