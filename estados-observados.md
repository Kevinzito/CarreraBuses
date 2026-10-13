# Estados observados de los hilos

Registro de los valores de ThreadState que aparecieron en las etiquetas de
estado durante la ejecución del proyecto, y qué significa cada uno.

## Valores vistos

| Valor en la etiqueta        | Cuándo aparece                                               |
| --------------------------- | ------------------------------------------------------------ |
| "Sin iniciar"               | Antes de la primera carrera; los hilos todavía no existen.    |
| Unstarted                   | Entre que se crea el Thread y se llama a Start. Dura muy poco. |
| Background, WaitSleepJoin   | El más frecuente. El bus está dormido entre un paso y otro.   |
| Background, Running         | Raro de pescar: el bus ejecuta instrucciones entre Sleep y Sleep. |
| Running                     | También raro, mientras el bus trabaja y el Timer lo alcanza.  |
| Stopped                     | El bus llegó a Estelí y su método Viajar() terminó.           |

Valores que existen en la enumeración pero que no se observaron en este
proyecto: StopRequested, Suspended, Wait, AbortRequested. No aparecen porque
el código no llama a Interrupt, Suspend ni Abort.

## Qué significa cada uno

**Unstarted** — el objeto Thread existe pero todavía no se llamó a Start().
Se ve solo si el Timer se activa antes de arrancar los hilos, algo que en la
versión final no ocurre porque el Timer se arranca después de crearlos.

**Running** — el hilo está ejecutando instrucciones en el procesador. Como los
buses pasan dormidos la mayor parte del tiempo, este estado dura microsegundos
y es difícil de capturar con el Timer a 100 ms.

**WaitSleepJoin** — el hilo está bloqueado esperando: en Thread.Sleep(),
esperando un objeto con Wait, o esperando que otro hilo termine con Join. En
este proyecto es casi siempre el Thread.Sleep() entre paso y paso.

**Background** — no es un estado de ejecución sino una bandera: indica que la
propiedad IsBackground quedó en True. Como se puso True para los tres hilos,
esa bandera aparece combinada con los otros estados durante toda la vida del
hilo. Es la razón de que se vea "Background, WaitSleepJoin" y no solo
"WaitSleepJoin".

**Stopped** — el método que ejecutaba el hilo terminó, de forma normal o por
una excepción no controlada. El hilo no puede volver a arrancar: un Thread en
Stopped es definitivo, por eso cada carrera crea tres Thread nuevos.

## Por qué los valores vienen combinados

ThreadState está declarada como una enumeración de tipo flags: sus valores se
combinan con operaciones de bits. Por eso una sola lectura puede devolver
varios estados a la vez. Ese es el motivo por el que en el Timer no se puede
comparar con el operador "=", sino que se usa And:

    ' INCORRECTO: nunca va a ser igual a un solo valor
    If hilos(i).ThreadState = ThreadState.Stopped Then

    ' CORRECTO: pregunta si la bandera Stopped está presente
    If (hilos(i).ThreadState And ThreadState.Stopped) <> 0 Then

La comparación con And comprueba si el bit de Stopped está encendido dentro
del valor combinado, sin importar qué otros bits lo acompañen.

## Efecto de la frecuencia del Timer

| Interval | Qué se observa en las etiquetas |
| -------- | ------------------------------- |
| 1000 ms  | Casi siempre "Background, WaitSleepJoin". Nunca "Running". |
| 100 ms   | Mayormente "Background, WaitSleepJoin"; a veces "Running". |
| 20 ms    | "Running" aparece bastante más seguido. |

El Timer toma una fotografía del estado en el instante del Tick. Como el hilo
pasa dormido la mayor parte del tiempo y despierto solo microsegundos, cuanto
más corto es el Interval, más probable es pescar el estado Running.

## Ciclo de vida observado de un bus

    New Thread(...)      ->  Unstarted
    .Start()             ->  Running (y de inmediato WaitSleepJoin por el Sleep)
    Thread.Sleep()       ->  Background, WaitSleepJoin
    vuelve del Sleep     ->  Background, Running
    (se repite hasta km 150)
    Viajar() termina     ->  Stopped