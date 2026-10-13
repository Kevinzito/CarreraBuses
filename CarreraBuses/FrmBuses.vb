Imports System.Threading

Partial Public Class FrmBuses
    ''' <summary>
    ''' Bandera que indica que la ventana se está cerrando.
    ''' Los hilos la consultan para abortar su trabajo limpiamente.
    ''' </summary>
    Private cerrando As Boolean

    ''' <summary>
    ''' Evento Load del formulario. Se ejecuta una sola vez al abrir la ventana.
    ''' Podés dejarlo vacío o usarlo para inicializar algo si querés.
    ''' </summary>
    Private Sub FrmBuses_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Opcional: inicializar cosas aquí si las necesitás
    End Sub

    ''' <summary>
    ''' Botón Iniciar carrera: prepara la pantalla, crea los hilos y los arranca.
    ''' </summary>
    Private Sub btnIniciar_Click(sender As Object, e As EventArgs) Handles btnIniciar.Click
        ' 1. Preparar la pantalla
        pbBus1.Value = 0
        pbBus2.Value = 0
        pbBus3.Value = 0
        lblEstado1.Text = "Sin iniciar"
        lblEstado2.Text = "Sin iniciar"
        lblEstado3.Text = "Sin iniciar"
        lblGanador.Text = "Ganador: —"

        ' 2. Reiniciar la variable compartida ganador (está declarada en Monitor.vb)
        ganador = String.Empty

        ' 3. Reiniciar bandera de cierre
        cerrando = False

        ' 4. Desactivar el botón para evitar lanzar otra carrera encima
        btnIniciar.Enabled = False

        ' 5. Crear los tres hilos y arrancarlos
        '    (IniciarHilos está definido en FrmBuses.Hilos.vb)
        IniciarHilos()

        ' 6. Activar el Timer de estados
        '    (el Tick está en FrmBuses.Monitor.vb)
        tmrEstados.Start()
    End Sub

    ''' <summary>
    ''' Botón Salir: cierra el formulario.
    ''' Como CancelButton = btnSalir, la tecla Esc hace lo mismo.
    ''' </summary>
    Private Sub btnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
        Me.Close()
    End Sub

    ''' <summary>
    ''' Evento que se dispara ANTES de que el formulario se cierre.
    ''' Activa la bandera "cerrando" para que los hilos sepan que deben parar,
    ''' y detiene el Timer.
    ''' </summary>
    Private Sub FrmBuses_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        cerrando = True
        tmrEstados.Stop()
    End Sub
End Class
