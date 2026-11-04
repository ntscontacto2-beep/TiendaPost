namespace PodTiendita.Services
{
    /// <summary>Calcula un eje vertical "redondo" (0, 500, 1,000...) para las gráficas de barras.</summary>
    public static class EscalaEje
    {
        public const int Divisiones = 4;

        // Pasos "redondos" entre líneas guía (se multiplican por una potencia de 10). Más opciones = eje más ajustado.
        private static readonly double[] PasosRedondos = { 1, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10 };

        /// <summary>Máximo del eje: el menor valor redondo, dividido en 4 partes iguales, que cubre al dato más alto.</summary>
        public static decimal Maximo(decimal valorMaximo)
        {
            if (valorMaximo <= 0) return 100m;

            var paso = (double)valorMaximo / Divisiones;
            var potencia = Math.Pow(10, Math.Floor(Math.Log10(paso)));
            var fraccion = paso / potencia;
            var redondo = PasosRedondos.First(p => fraccion <= p);

            return (decimal)(redondo * potencia * Divisiones);
        }
    }
}
