Imports System.Threading

Partial Public Class FrmBuses
    ''' <summary>
    ''' Arreglo que contiene los tres objetos Thread (Bus 1, Bus 2, Bus 3).
    ''' Se recrea cada vez que se inicia una carrera nueva.
    ''' </summary>
    Private hilos(2) As Thread

    ''' <summary>
    ''' Crea los tres hilos, les asigna nombre, los marca como segundo plano
    ''' y los arranca. Lo llama btnIniciar_Click.
    ''' </summary>
    Public Sub IniciarHilos()
        ' Un hilo en estado Stopped NO se puede reiniciar.
        ' Siempre creamos objetos nuevos para cada carrera.
        For i As Integer = 0 To 2
            Dim nombre As String = $"Bus {i + 1}"
            hilos(i) = New Thread(AddressOf Viajar)
            hilos(i).Name = nombre              ' Asignar nombre UNA sola vez
            hilos(i).IsBackground = True         ' Segundo plano: al cerrar la ventana, mueren
            hilos(i).Start()                     ' ¡Arranca!
        Next
    End Sub

    ''' <summary>
    ''' Devuelve la barra de progreso que le corresponde a un bus, según su nombre.
    ''' Así los tres hilos usan el mismo método Viajar() y cada uno sabe
    ''' cuál es su barra.
    ''' </summary>
    Private Function ObtenerBarra(nombreBus As String) As ProgressBar
        Select Case nombreBus
            Case "Bus 1" : Return pbBus1
            Case "Bus 2" : Return pbBus2
            Case "Bus 3" : Return pbBus3
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' MÉTODO PRINCIPAL DEL PROYECTO.
    ''' Se ejecuta en el hilo de cada bus (NUNCA en el hilo UI).
    ''' Simula el viaje de 0 a 150 km en pasos de 5 km con pausas aleatorias,
    ''' actualiza su barra mediante Invoke y al llegar intenta registrarse
    ''' como ganador.
    ''' </summary>
    Private Sub Viajar()
        ' --- Paso 0: averiguar quién soy ---
        Dim nombre As String = Thread.CurrentThread.Name
        Dim barra As ProgressBar = ObtenerBarra(nombre)

        ' --- Paso 1: crear mi propio generador de números aleatorios ---
        ' La clase Random NO es segura entre hilos. Si varios hilos usan
        ' el MISMO objeto Random, su estado interno se corrompe y empieza
        ' a devolver ceros o valores repetidos. Cada hilo tiene el suyo.
        Dim rnd As New Random()

        ' --- Paso 2: avanzar de 5 en 5 km hasta 150 ---
        For km As Integer = 5 To 150 Step 5
            ' Si el usuario cerró la ventana, salgo inmediatamente
            If cerrando OrElse Me.IsDisposed Then
                Return
            End If

            ' Guardo el km actual en una variable LOCAL de esta vuelta.
            ' Esto es crítico: si la lambda capturara directamente "km",
            ' y algún día cambiás Invoke por BeginInvoke, todas las lambdas
            ' verían el valor final de km (150). Con kmActual, cada vuelta
            ' captura su propio valor.
            Dim kmActual As Integer = km

            ' --- Paso 3: pedir al hilo UI que mueva mi barra ---
            Try
                If Not Me.IsDisposed Then
                    Me.Invoke(New MethodInvoker(Sub()
                                                    barra.Value = kmActual
                                                End Sub))
                End If
            Catch ex As ObjectDisposedException
                ' El formulario se destruyó mientras esperaba el Invoke.
                ' Salgo limpiamente.
                Return
            End Try

            ' --- Paso 4: dormir un tiempo aleatorio (50 a 200 ms) ---
            ' Next(min, max): min INCLUSIVE, max EXCLUSIVE.
            ' Para 50 a 200 inclusive → max debe ser 201.
            Dim pausa As Integer = rnd.Next(50, 201)
            Thread.Sleep(pausa)
        Next

        ' --- Paso 5: llegué a Estelí (km 150). Intentar registrarme como ganador ---
        '
        ' La variable "soyGanador" es LOCAL a este hilo. Cada bus tiene la suya.
        ' Nadie más la ve. Solo se pone en True si este bus logró escribir en "ganador".
        Dim soyGanador As Boolean = False

        ' SECCIÓN CRÍTICA: solo un hilo a la vez puede ejecutar este bloque.
        ' El objeto "candado" (declarado en Monitor.vb) es la llave del baño.
        SyncLock candado
            If String.IsNullOrEmpty(ganador) Then
                ganador = nombre
                soyGanador = True
            End If
        End SyncLock

        ' --- Paso 6: si fui el ganador, mostrarlo en pantalla ---
        ' El Invoke va FUERA del SyncLock. Regla: dentro del candado solo
        ' se lee/escribe el dato compartido. Nunca algo lento ni que dependa
        ' de otro hilo (como Invoke), porque podrías causar un interbloqueo.
        If soyGanador Then
            Try
                If Not Me.IsDisposed Then
                    Me.Invoke(New MethodInvoker(Sub()
                                                    lblGanador.Text = $"Ganador: {nombre}"
                                                End Sub))
                End If
            Catch ex As ObjectDisposedException
                ' Ventana cerrada; no hay dónde mostrar el ganador
            End Try
        End If

        ' Al salir de Viajar(), el hilo pasa automáticamente a estado Stopped.
        ' No hace falta hacer nada más.
    End Sub
End Class
