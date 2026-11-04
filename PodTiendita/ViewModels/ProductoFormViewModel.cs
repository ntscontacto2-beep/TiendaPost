using System.Diagnostics;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PodTiendita.Data;
using PodTiendita.Models;
using PodTiendita.Services;

namespace PodTiendita.ViewModels
{
    /// <summary>Formulario de alta y edición de un producto, con foto buscada en internet o de la computadora.</summary>
    public partial class ProductoFormViewModel : ObservableObject
    {
        private readonly ProductoService _productos = new();
        private readonly ImagenService _imagenes = new();
        private readonly Producto? _existente;

        // Foto que ya tenía el producto (nombre de archivo). Null si no tiene o si la quitaron.
        private string? _imagenActual;

        // Foto nueva elegida (de la computadora o copiada del navegador). Se guarda hasta que se guarda el producto.
        private string? _imagenLocal;
        private bool _imagenLocalEsTemporal;

        // True después de abrir el navegador: al volver a la app se busca la foto copiada.
        private bool _esperandoFoto;

        /// <summary>Se dispara al terminar; el parámetro indica si se guardó algo.</summary>
        public event Action<bool>? Cerrado;

        public ProductoFormViewModel(Producto? existente = null)
        {
            _existente = existente;

            if (existente is null)
            {
                Titulo = "Nuevo producto";
                Subtitulo = "Completa la información para agregarlo al catálogo";
                ExistenciaTexto = "0";
                StockMinimoTexto = "5";
                return;
            }

            Titulo = "Editar producto";
            Subtitulo = "Cambia los datos que necesites";
            Nombre = existente.Nombre;
            Descripcion = existente.Descripcion;
            Categoria = existente.Categoria;
            PrecioTexto = existente.Precio.ToString("0.##", CultureInfo.InvariantCulture);
            ExistenciaTexto = existente.Stock.ToString();
            StockMinimoTexto = existente.StockMinimo.ToString();
            _imagenActual = existente.ImagenRuta;
            ActualizarVistaPrevia();
        }

        public static CategoriaProducto[] Categorias { get; } = Enum.GetValues<CategoriaProducto>();

        public string Titulo { get; }
        public string Subtitulo { get; }
        public bool EsEdicion => _existente is not null;
        public string EtiquetaExistencia => EsEdicion ? "Existencia actual" : "Existencia inicial";

        // ----- Campos -----

        [ObservableProperty] private string _nombre = "";
        [ObservableProperty] private string _descripcion = "";
        [ObservableProperty] private CategoriaProducto _categoria = CategoriaProducto.Abarrotes;
        [ObservableProperty] private string _precioTexto = "";
        [ObservableProperty] private string _existenciaTexto = "";
        [ObservableProperty] private string _stockMinimoTexto = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HayError))]
        private string _error = "";

        public bool HayError => Error.Length > 0;

        // ----- Foto -----

        [ObservableProperty] private string _mensajeFoto = "";

        /// <summary>Ruta del archivo que muestra la vista previa. Null si no hay imagen.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HayImagen), nameof(SinImagen))]
        private string? _vistaPrevia;

        public bool HayImagen => VistaPrevia is not null;
        public bool SinImagen => !HayImagen;

        private void ActualizarVistaPrevia() =>
            VistaPrevia = _imagenLocal ??
                (_imagenActual is null ? null : Path.Combine(DbPaths.CarpetaImagenes, _imagenActual));

        /// <summary>Paso 1: abre el navegador con las fotos del producto para que elijan y copien una.</summary>
        [RelayCommand]
        private void BuscarFotosEnInternet()
        {
            var termino = Nombre.Trim();
            if (termino.Length < 2)
            {
                MensajeFoto = "Escribe primero el nombre del producto.";
                return;
            }

            _esperandoFoto = true;
            MensajeFoto = "En el navegador: haz clic derecho sobre la foto que te guste, elige \"Copiar imagen\" y regresa aquí.";
            Process.Start(new ProcessStartInfo(
                $"https://www.google.com/search?tbm=isch&q={Uri.EscapeDataString(termino + " producto")}")
            { UseShellExecute = true });
        }

        /// <summary>Paso 2: toma la foto copiada. También se llama sola cuando se regresa a la app.</summary>
        [RelayCommand]
        private void PegarFoto() => IntentarPegar(avisarSiNoHay: true);

        /// <summary>La ventana lo llama al volver a primer plano, después de buscar en el navegador.</summary>
        public void AlVolverALaApp()
        {
            if (_esperandoFoto) IntentarPegar(avisarSiNoHay: false);
        }

        private void IntentarPegar(bool avisarSiNoHay)
        {
            var ruta = _imagenes.TomarDelPortapapeles();
            if (ruta is null)
            {
                if (avisarSiNoHay)
                    MensajeFoto = "No hay ninguna foto copiada. En el navegador haz clic derecho sobre una foto y elige \"Copiar imagen\".";
                return;
            }

            _esperandoFoto = false;
            UsarImagenLocal(ruta, esTemporal: true);
            MensajeFoto = "¡Listo! Esta es la foto que copiaste.";
        }

        [RelayCommand]
        private void ElegirArchivo()
        {
            var ruta = Dialogos.PedirRutaImagen();
            if (ruta is null) return;

            _esperandoFoto = false;
            UsarImagenLocal(ruta, esTemporal: false);
            MensajeFoto = "";
        }

        [RelayCommand]
        private void QuitarImagen()
        {
            _esperandoFoto = false;
            LimpiarTemporal();
            _imagenLocal = null;
            _imagenActual = null;
            MensajeFoto = "";
            ActualizarVistaPrevia();
        }

        private void UsarImagenLocal(string ruta, bool esTemporal)
        {
            LimpiarTemporal();
            _imagenLocal = ruta;
            _imagenLocalEsTemporal = esTemporal;
            ActualizarVistaPrevia();
        }

        /// <summary>Borra el archivo temporal de la foto copiada (nunca los archivos que eligió el usuario).</summary>
        private void LimpiarTemporal()
        {
            if (_imagenLocalEsTemporal && _imagenLocal is not null)
            {
                try { File.Delete(_imagenLocal); } catch (IOException) { /* se limpia solo con el temporal de Windows */ }
            }
            _imagenLocalEsTemporal = false;
        }

        // ----- Guardar / cancelar -----

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
        private bool _guardando;

        [RelayCommand]
        private void Cancelar()
        {
            LimpiarTemporal();
            Cerrado?.Invoke(false);
        }

        [RelayCommand(CanExecute = nameof(PuedeGuardar))]
        private void Guardar()
        {
            Error = "";
            if (Sesion.Actual is null) return;
            if (!TryLeerNumeros(out var precio, out var existencia, out var minimo)) return;

            Guardando = true;
            string? imagenNueva = null;
            try
            {
                if (_imagenLocal is not null) imagenNueva = _imagenes.GuardarDesdeArchivo(_imagenLocal);
                var imagenFinal = imagenNueva ?? _imagenActual;

                var datos = new ProductoDatos(Nombre, Descripcion, Categoria, precio, existencia, minimo, imagenFinal);
                if (_existente is null)
                    _productos.Crear(datos, Sesion.Actual.Id);
                else
                    _productos.Actualizar(_existente.Id, datos, _existente.Stock, Sesion.Actual.Id);

                // La foto anterior ya no se usa: se libera el archivo.
                if (_existente?.ImagenRuta is { } anterior && anterior != imagenFinal)
                    _imagenes.Eliminar(anterior);

                LimpiarTemporal();
                Cerrado?.Invoke(true);
            }
            catch (Exception ex) when (ex is NegocioException or IOException)
            {
                // No quedan archivos huérfanos si no se pudo guardar el producto.
                _imagenes.Eliminar(imagenNueva);
                Error = ex is NegocioException ? ex.Message : "No se pudo guardar la foto. Intenta con otra.";
            }
            finally
            {
                Guardando = false;
            }
        }

        private bool PuedeGuardar() => !Guardando;

        private bool TryLeerNumeros(out decimal precio, out int existencia, out int minimo)
        {
            existencia = minimo = 0;

            if (!decimal.TryParse(PrecioTexto.Replace("$", "").Replace(" ", "").Replace(',', '.'),
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out precio))
            {
                Error = "El precio debe ser un número, por ejemplo 28.50";
                return false;
            }
            if (!int.TryParse(ExistenciaTexto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out existencia))
            {
                Error = "La existencia debe ser un número entero, por ejemplo 12";
                return false;
            }
            if (!int.TryParse(StockMinimoTexto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out minimo))
            {
                Error = "El stock mínimo debe ser un número entero, por ejemplo 5";
                return false;
            }
            return true;
        }
    }
}
