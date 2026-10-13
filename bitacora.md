#Bitacora de experimentos

Registro de los seis experimentos de la sección 6 de la guía. Cada uno se hizo
sobre una copia de prueba del proyecto (rama `pruebas`), se observó el resultado
y luego se deshicieron los cambios para dejar el entregable como estaba.

---

## Experimento 1 · Congelar la ventana a propósito

**Qué cambiamos**
En btnIniciar_Click comentamos la llamada a IniciarHilos() y en su lugar
llamamos a Viajar() directamente, es decir, en el hilo de la interfaz. Como
Viajar() averigua qué bus es leyendo Thread.CurrentThread.Name, y el hilo UI
no tiene nombre, antes de la llamada le asignamos "Bus 1".

**Qué observamos**
La ventana dejó de responder mientras el ciclo estaba en curso: no se podía
mover, ni minimizar, ni cerrar. Windows dibujó el texto "No responde" en la
barra de título. Las barras no avanzaron de a poco: el valor apareció de golpe
cuando el ciclo terminó. Con el Sleep aumentado a 600 ms el efecto fue mucho
más evidente: la ventana quedó tiesa aproximadamente 18 segundos, hasta que la
última vuelta del ciclo terminó y el hilo UI volvió a atender sus mensajes.

**Por qué pasó**
Un formulario de Windows Forms atiende sus mensajes (dibujar, mover, responder
clics, repintar) en su hilo UI, a través de un bucle de mensajes. Mientras ese
hilo está ejecutando el ciclo del viaje, no puede atender ningún mensaje: todo
queda en cola. Por eso la ventana se congela. Lo mismo ocurre con el dibujo:
aunque el código cambiaba la propiedad Value de la barra en cada vuelta, el
formulario no se redibujaba hasta que el hilo UI quedaba libre, y por eso el
usuario ve el salto final y no el avance gradual.

**Conclusión**
Este experimento es la razón de ser del proyecto: el trabajo largo nunca debe
ejecutarse en el hilo de la interfaz. Debe ir en hilos de trabajo y la interfaz
se actualiza mediante Invoke.

---

## Experimento 2 · Saltarse Invoke

**Qué cambiamos**
Dentro de Viajar() reemplazamos el bloque que hacía

    Me.Invoke(New MethodInvoker(Sub()
        barra.Value = kmActual
    End Sub))

por una escritura directa al control, sin Invoke:

    barra.Value = kmActual

**Qué observamos**
Al ejecutar con depuración (F5) la aplicación se detuvo en esa línea y el
depurador mostró:

    System.InvalidOperationException: 'Operación no válida a través de
    subprocesos: se tuvo acceso al control pbBus1 desde un subproceso distinto
    del subproceso en el que se creó.'

En la ventana Pila de llamadas se veía que la ejecución estaba dentro de
Viajar, en un hilo de trabajo, no en el hilo principal. La aplicación quedó
detenida hasta que se presionó Detener.

**Por qué pasó**
Los controles de Windows Forms pertenecen al hilo que los creó, que es el hilo
UI, y no son seguros para subprocesos. .NET lleva un registro del hilo dueño de
cada control y lo comprueba en tiempo de ejecución cuando la depuración está
activa; si otro hilo intenta tocarlo, lanza InvalidOperationException. Invoke
resuelve el problema porque no toca el control directamente: empaqueta el cambio
y lo pone en la cola de mensajes del hilo UI, que es el dueño del control, para
que lo ejecute él.

**Nota**
Ejecutando sin depuración (Ctrl+F5) esa comprobación no siempre se lanza, pero
el comportamiento queda indefinido y varía entre versiones de .NET. Es un error
latente, no una alternativa válida: el código correcto usa Invoke siempre.

---

## Experimento 3 · Hilos de primer plano

**Qué cambiamos**
Pusimos IsBackground = False en los tres hilos y presionamos Salir a mitad de
la carrera.

**Qué observamos**
La ventana desapareció de la pantalla, pero el proceso no terminó: Visual Studio
siguió en modo depuración con el botón Detener activo, y en el Administrador de
tareas CarreraBuses.exe continuaba en la lista de procesos. El programa terminó
de verdad solo cuando los tres buses llegaron a los 150 km y sus métodos
Viajar() terminaron.

**Por qué pasó**
Un proceso de .NET no termina mientras queden hilos de primer plano vivos,
aunque no quede ninguna ventana abierta: los hilos de primer plano mantienen la
aplicación en ejecución. Los hilos de segundo plano (IsBackground = True), en
cambio, son terminados por el runtime en cuanto todos los hilos de primer plano
acaban. El hilo UI es de primer plano; al cerrar la última ventana ese hilo
termina y con él caen todos los de segundo plano.

**Conclusión**
Por eso el proyecto exige IsBackground = True: cerrar la ventana debe terminar
el proceso completo, sin dejarlo colgado en el Administrador de tareas.

---

## Experimento 4 · Provocar la condición de carrera

**Qué cambiamos**
Quitamos el bloque SyncLock. Para forzar el empate hicimos que los tres buses
durmieran siempre el mismo tiempo, y agregamos una pausa pequeña entre la
pregunta "¿está vacío?" y la escritura. Además llevamos un contador de cuántos
buses se anotaron como ganadores y lo mostramos en la etiqueta.

**Qué observamos**
Con el SyncLock quitado el contador marcó 2 y hasta 3 ganadores en la misma
carrera. Los tres buses llegaban casi al mismo tiempo, los tres veían la
variable ganador vacía y los tres escribían su nombre encima del anterior. La
etiqueta terminaba mostrando el que escribió último, que no era
necesariamente el primero en llegar. Al volver a poner el SyncLock y repetir,
el contador marcó 1 en todas las corridas.

**Por qué pasó**
La secuencia "leer ganador -> comprobar que está vacío -> escribir ganador" es
una sección crítica, pero no es una operación atómica: el planificador del
sistema operativo puede interrumpir un hilo entre la lectura y la escritura
para darle turno a otro. Si dos hilos se intercalan ahí, ambos ven vacío y
ambos escriben. Eso es una condición de carrera. SyncLock garantiza exclusión
mutua: solo un hilo puede estar dentro del bloque a la vez, así que la pregunta
y la escritura ocurren como una sola operación indivisible.

**Conclusión**
Sin SyncLock no hay un ganador: hay varios, y decide el azar del planificador.
La regla que se desprende de esto es que dentro del candado va solamente el
acceso al dato compartido, nada más: por eso el Invoke que muestra el ganador
va fuera del SyncLock.

---

## Experimento 5 · La frecuencia del Timer

**Qué cambiamos**
Pusimos el Interval de tmrEstados en 1000 ms, después en 100 ms y después en
20 ms, y observamos las etiquetas de estado durante una carrera completa.

**Qué observamos**

| Interval | Qué se ve en las etiquetas |
| -------- | -------------------------- |
| 1000 ms  | Casi siempre "Background, WaitSleepJoin". Nunca "Running". |
| 100 ms   | Mayormente "Background, WaitSleepJoin"; de vez en cuando "Running". |
| 20 ms    | "Running" aparece con bastante más frecuencia, y se alcanza a ver la transición hacia "Stopped". |

En todos los casos, al terminar la carrera las tres etiquetas terminan en
"Stopped".

**Por qué casi nunca se ve Running**
El Timer toma una fotografía del ThreadState en el instante exacto del Tick. Un
hilo de trabajo está dormido (WaitSleepJoin) casi todo el tiempo: duerme entre
50 y 200 ms en cada paso. El tiempo que pasa despierto ejecutando instrucciones
(Running) es de microsegundos. Con un Interval de 1000 ms la probabilidad de
que la foto caiga justo en esos microsegundos es casi nula. Con 20 ms el
muestreo es cincuenta veces más fino y alcanza a pescar ese estado.

**Por qué aparece "Background" adelante**
Background no es un estado de ejecución sino una bandera: indica que la
propiedad IsBackground quedó en True. Como la pusimos True para los tres hilos,
esa bandera acompaña a los demás estados durante toda la vida del hilo. Por eso
se ve "Background, WaitSleepJoin" y no solo "WaitSleepJoin".

**Por qué los valores vienen combinados**
ThreadState es una enumeración de tipo flags: sus valores se combinan con
operaciones de bits, así que una sola lectura puede devolver varios estados a
la vez. Ese es el motivo por el que en el Timer no se puede comparar con el
operador "=", sino con And:

    ' INCORRECTO: nunca va a ser igual a un solo valor
    If hilos(i).ThreadState = ThreadState.Stopped Then

    ' CORRECTO: pregunta si la bandera Stopped está presente
    If (hilos(i).ThreadState And ThreadState.Stopped) <> 0 Then

---

## Experimento 6 · Invoke contra BeginInvoke

**Qué cambiamos**
Reemplazamos Invoke por BeginInvoke sin copiar el kilómetro a una variable
local de la vuelta, de modo que la lambda capturaba directamente la variable
del ciclo.

**Qué observamos**
Las barras dejaron de mostrar el avance correcto: se quedaban casi en 0 durante
casi toda la carrera y saltaban al valor final al terminar, o mostraban valores
repetidos y desordenados. También apareció una InvalidOperationException al
cerrar la ventana, porque BeginInvoke no espera a que el formulario termine de
destruirse.

**Por qué pasó**
Hay dos causas distintas que se suman.

Primero, BeginInvoke es asíncrono: encola el trabajo en el hilo UI y retorna de
inmediato, sin esperar a que se ejecute. El hilo del bus sigue con su ciclo, y
cuando el hilo UI por fin llega a ejecutar la lambda ya pasaron muchas vueltas.

Segundo, y más importante: una lambda captura la variable, no su valor. La
expresión Sub() ... End Sub guarda una referencia a la variable km del ciclo. Si
el bucle sigue avanzando antes de que la lambda se ejecute, todas las lambdas
pendientes leen el valor actual de km, que para entonces vale 150. Por eso todas
las barras saltan al valor final.

Al declarar Dim kmActual As Integer = km dentro del ciclo, cada vuelta crea una
variable nueva, distinta de la anterior, y la lambda captura esa. Cada lambda
queda con su propio valor y las barras avanzan correctamente.

**Conclusión**
Con Invoke el problema no se nota, porque Invoke bloquea al hilo del bus hasta
que el hilo UI ejecuta la lambda: cuando la lambda corre, km todavía vale lo
mismo. Pero es una bomba de tiempo: el día que alguien cambie Invoke por
BeginInvoke, el error aparece. Copiar a una variable local es la forma correcta
de escribirlo desde el principio, y es lo que hace la versión final del
proyecto.

---

## Conclusión general

Los seis experimentos confirman las tres reglas que sostienen el proyecto:

1. El trabajo largo nunca se ejecuta en el hilo UI, porque lo congela.
2. Un control solo lo toca su hilo dueño; desde otro hilo se usa Invoke.
3. Un dato compartido solo se modifica dentro de un SyncLock, y dentro del
   candado no se hace nada más que el acceso a ese dato.