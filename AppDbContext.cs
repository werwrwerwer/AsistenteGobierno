using System;
using Azure;
using Azure.Data.Tables;

namespace AsistenteGobierno
{
   
    public class DatosCita
    {
        public string? nombre { get; set; }
        public string? identificador { get; set; }
        public string? tramite { get; set; }
        public string? fecha { get; set; }
    }

    public class CitaEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Nombre { get; set; }
        public string Identificador { get; set; }
        public string Tramite { get; set; }
        public string Fecha { get; set; }
    }
}