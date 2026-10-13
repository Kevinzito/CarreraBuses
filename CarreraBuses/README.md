# Equipo NN — Carrera de buses Managua–Estelí

Proyecto de la asignatura **Programación de Aplicaciones de Escritorio**
(2T1-SIS-E). Tema: hilos, ciclo de vida y sincronización.

## Integrantes y roles

 Nombre                   Usuario de GitHub          Rol 

| Kevin David Zeledon Cruz | @Kevinzito | Alumno 1 · Interfaz |
| Kevin David Zeledon Cruz | @Kevinzito | Alumno 2 · Hilos |
| Kevin David Zeledon Cruz | @Kevinzito | Alumno 3 · Sincronización |

> Proyecto desarrollado de forma individual: el mismo estudiante cubrió los
> tres roles (interfaz, hilos y sincronización).

## Cómo abrir y ejecutar el proyecto

1. Clonar el repositorio y abrir `CarreraBuses.slnx` con
   Visual Studio 2026 (carga de trabajo *Desarrollo de escritorio de .NET*).
2. Verificar que el proyecto tiene como marco de destino .NET 10
   (`net10.0-windows`) y que FrmBuses es el formulario de inicio.
3. Presionar F5.
4. En la ventana, presionar **Iniciar carrera**.

## Capturas

### Carrera en curso
![Carrera en curso](docs/capturas/carrera-en-curso.png)

### Carrera terminada con ganador
![Carrera terminada](docs/capturas/carrera-terminada.png)

## Explicación del equipo

### ¿Por qué la ventana no se congela?
Porque el trabajo largo, el viaje de cada bus, no se ejecuta en el hilo de la
interfaz. El botón Iniciar solo crea tres hilos de trabajo y los arranca; el
hilo UI queda libre y sigue atendiendo los mensajes de Windows: mover la
ventana, minimizarla, responder clics. Cada hilo de trabajo actualiza su barra
pidiéndole el cambio al hilo UI con Invoke, que es la única forma segura de
tocar un control desde otro hilo.

### ¿Qué estados observamos?
Los tres valores que aparecen en las etiquetas son "Background,
WaitSleepJoin", que es el más frecuente porque el bus está dormido entre paso
y paso; "Running", que es raro porque el Timer casi nunca lo pesca; y
"Stopped", cuando el bus llegó a Estelí. El prefijo Background aparece porque
los tres hilos tienen IsBackground = True. El detalle está en
[docs/estados-observados.md](docs/estados-observados.md).

### ¿Por qué solo hay un ganador?
Porque la escritura en la variable compartida ganador está protegida por un
bloque SyncLock. Sin ese candado, dos o tres buses pueden leer ganador vacío
al mismo tiempo y los tres escribir su nombre: eso es una condición de
carrera. Con el candado, solo un hilo a la vez puede preguntar y escribir, así
que el primer nombre que queda escrito es el único. El Invoke que muestra el
ganador va fuera del SyncLock, porque dentro del candado solo se toca el dato
compartido.

## Documentación

- [Bitácora de experimentos](docs/bitacora.md)
- [Estados observados](docs/estados-observados.md)