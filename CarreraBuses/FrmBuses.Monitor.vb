Imports System.Threading

Partial Public Class FrmBuses
    ''' <summary>
    ''' Variable compartida donde se guarda el nombre del primer bus que llega.
    ''' La protege el objeto "candado" mediante SyncLock.
    ''' </summary>
    Private ganador As String

    ''' <summary>
    ''' Objeto candado PRIVADO, de SOLO LECTURA, usado EXCLUSIVAMENTE
    ''' para la instrucción SyncLock. Nunca se usa Me, un String ni la
    ''' propia variable ganador como candado.
    ''' </summary>
    Private ReadOnly candado As New Object()

    ''' <summary>
    ''' Evento Tick del Timer. Se ejecuta en el HILO UI cada 100 ms.
    ''' Por estar en el hilo UI, puede tocar los controles directamente,
    ''' sin Invoke.
    '''
    ''' Lee el ThreadState de cada bus, lo muestra en su etiqueta,
    ''' y detecta cuándo los tres terminaron para detener el Timer.
    ''' </summary>
    Private Sub tmrEstados_Tick(sender As Object, e As EventArgs) Handles tmrEstados.Tick
        ' Arreglo de etiquetas para recorrerlas fácilmente
        Dim etiquetas() As Label = {lblEstado1, lblEstado2, lblEstado3}

        Dim todosDetenidos As Boolean = True

        For i As Integer = 0 To 2
            If hilos(i) IsNot Nothing Then
                ' Mostrar el estado actual del hilo
                etiquetas(i).Text = hilos(i).ThreadState.ToString()

                ' Verificar si este hilo ya terminó.
                ' ThreadState es un flags enum: puede combinar valores
                ' (ej: "Background, WaitSleepJoin").
                ' Por eso NO comparamos con "=" sino que usamos And.
                If (hilos(i).ThreadState And ThreadState.Stopped) = 0 Then
                    todosDetenidos = False
                End If
            Else
                ' El hilo todavía no fue creado (no debería pasar si el
                ' Timer se activa después de crearlos, pero por seguridad)
                etiquetas(i).Text = "Sin iniciar"
                todosDetenidos = False
            End If
        Next

        ' Si los tres buses llegaron, detener el Timer y re-habilitar el botón
        If todosDetenidos Then
            ' Forzar una última lectura para que las etiquetas muestren
            ' el estado final correctamente
            For i As Integer = 0 To 2
                If hilos(i) IsNot Nothing Then
                    etiquetas(i).Text = hilos(i).ThreadState.ToString()
                End If
            Next

            tmrEstados.Stop()
            btnIniciar.Enabled = True
        End If
    End Sub
End Class
