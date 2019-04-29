using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EspiaPerdido.Models
{

    public class Situacion
    {
        public Situacion(string nombre, string[] roles)
        {
            Nombre = nombre;
            Roles = roles;
        }
        public string Nombre { get; set; }
        public string[] Roles { get; set; }
        public Tarjeta SacarTarjeta()
        {
            Random rnd = new Random();
            var rol = rnd.Next(0, 3);
            var tarjeta = new Tarjeta(Nombre, Roles[rol]);
            return tarjeta;
        }
    }
    public class Tarjeta
    {
        public Tarjeta(string situacion, string rol)
        {
            Situacion = situacion;
            Rol = rol;
        }
        public string Situacion { get; set; }
        public string Rol { get; set; }
    }
}
