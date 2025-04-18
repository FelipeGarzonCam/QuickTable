﻿using System.ComponentModel.DataAnnotations.Schema;

namespace QuickTableProyect.Dominio
{
    public class PedidosActivos
    {
        public int Id { get; set; }
        public int MeseroId { get; set; }
        public string EmpleadoNombre { get; set; }
        public int NumeroMesa { get; set; }
        public List<ItemDetalle> Detalles { get; set; }  // Relación con ItemDetalle

        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }

        public string Estado { get; set; } 
    }

    public class ItemDetalle
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Valor { get; set; }

        // Clave foránea para PedidosActivos
        public int PedidoActivoId { get; set; }
        public PedidosActivos PedidoActivo { get; set; }

        public decimal Subtotal => Cantidad * Valor;
        public int CantidadPreparada { get; set; }
        public string Comentario { get; set; }

    }
    public static class EstadosPedido
    {
        public const string EnPreparacion = "En Preparación";
        public const string Listo = "Listo";
        public const string Editado = "Editado";
        public const string Finalizado = "Finalizado";

    }
    public class HistorialPedido
    {
        public int Id { get; set; }
        public int NumeroMesa { get; set; }
        public int MeseroId { get; set; }
        public string MeseroNombre { get; set; }
        public DateTime FechaHora { get; set; } // Fecha y hora de finalización
        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }
        public decimal Propina { get; set; }
        public string MetodoPago { get; set; } // "QR", "Tarjeta", "Efectivo"
        public decimal? EfectivoRecibido { get; set; } // Solo si es efectivo
        public decimal? Cambio { get; set; }           // Solo si es efectivo

        // Relación con los detalles del pedido
        public virtual ICollection<HistorialDetalle> Detalles { get; set; }
    }
    public class HistorialDetalle
    {
        public int Id { get; set; }
        public int HistorialPedidoId { get; set; }
        public int MenuItemId { get; set; }
        public string Nombre { get; set; }
        public decimal Valor { get; set; }
        public int Cantidad { get; set; }

        public decimal Subtotal => Cantidad * Valor;
    }






}



