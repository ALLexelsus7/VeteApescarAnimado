using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;

//Escena de pesca de CLUBPENGUIN
//Proyecto 2do Parcial AlexRJ 24110097
namespace VeteApescarAnimado
{
    public partial class Form1 : Form
    {

        // Objeto del lienzo donde se proyecta la escena final
        private Panel panelLienzo;

        // Objeto para la matriz de pixeles (buffer de dibujo)
        private Bitmap mapaBits;

        //Para la musica (? es que puede ser null)
        private SoundPlayer? reproductorAudio;

        //Para el timer de veces que se reejecuta la imagen para animacion
        private System.Windows.Forms.Timer timerAnimacion;
        // Posicion inicial de algunos objetos movibles y estaticos
        private int tinyPlaneX = -650; // fuera de la pantalla por la izquierda
        private int tinyPlaneY = 330;
        private int anzueloX = 0; //Fijo en 0 para centrarlo
        private int anzueloY = 0; // Controlado por el ratón      
        private int pezAmarilloX = -600; 
        private int pezAmarilloY = -150; 
        private int pezGrisX = 600;
        private int pezGrisY = -10;
        private int velocidad = 5;
        private int barrelX = -700;
        private int barrelY = -100;
        private int bootX = 700;
        private int bootY = -250;
        private int pinguX = 150;
        private int pinguY = 180;

        private Region regionRecorteHilo;

        public Form1()
        {
            InitializeComponent();

            // Configuración de la ventana
            this.Text = "Vete a Pescar";
            this.FormBorderStyle = FormBorderStyle.FixedSingle; //sin arrastre de bordes
            this.MaximizeBox = false;
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(255, 0, 129, 204);
            // Crear el lienzo y configurarlo dentro del form
            panelLienzo = new Panel();
            panelLienzo.Size = new Size(1200, 700);
            panelLienzo.BackColor = Color.White;
            //Ventana que ajusta su área INTERNA para alojar el panel con un margen uniforme (ejemplo: 20px)
            this.ClientSize = new Size(1200 + (20 * 2), 700 + (20 * 2));
            //Centramos el panel exactamente dentro del ClientSize del formulario
            int xCentro = (this.ClientSize.Width - panelLienzo.Width) / 2;
            int yCentro = (this.ClientSize.Height - panelLienzo.Height) / 2;
            panelLienzo.Location = new Point(xCentro, yCentro);
            // Agregarlo al formulario
            this.Controls.Add(panelLienzo);
            //Instanciamos el Bitmap una sola vez
            mapaBits = new Bitmap(panelLienzo.Width, panelLienzo.Height);
            //Genera el paisaje estatico completo antes de mostrar nada
            GenerarPaisaje();

            // Evento de dibujo para mostrar el paisaje y los objetos movibles
            panelLienzo.Paint += DibujarLienzo;

            //Evento de mover mouse
            panelLienzo.MouseMove += PanelLienzo_MouseMove;

            //Habilita Doble Buffer en el panel
            typeof(Panel).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, panelLienzo, new object[] { true });

            // Config del Timer para animacion continua
            timerAnimacion = new System.Windows.Forms.Timer();
            timerAnimacion.Interval = 30; // Se ejecuta cada 30 ms (aprox. 30 FPS)
            timerAnimacion.Tick += TimerAnimacion_Tick;
            timerAnimacion.Start();

            //Musiquirri
            try
            {
                string rutaAudio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ParisStGermain.wav");
                if (File.Exists(rutaAudio))
                {
                    reproductorAudio = new SoundPlayer(rutaAudio);
                    reproductorAudio.PlayLooping();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se pudo reproducir la música: " + ex.Message);
            }

            // Pre-calcular la máscara de recorte del hielo para evitar lag
            Point inicioHielo = ConvertirCoordenada(-20, 160);
            Point finHielo = ConvertirCoordenada(20, 135);
            int altoHielo = finHielo.Y - inicioHielo.Y;
            Rectangle zonaProhibida = new Rectangle(inicioHielo.X, inicioHielo.Y, 40, altoHielo);
            Rectangle pantallaCompleta = new Rectangle(0, 0, panelLienzo.Width, panelLienzo.Height);
            regionRecorteHilo = new Region(pantallaCompleta);
            regionRecorteHilo.Exclude(zonaProhibida);

        }

        //Metodo para el evento del mouse del fishing rod
        private void PanelLienzo_MouseMove(object sender, MouseEventArgs e)
        {
            // Actualiza las coordenadas con la pos. actual del cursor
            int posicionY = e.Y;

            //Limites en Y
            int limSup = 125;
            int limInf = 650;
            if (posicionY < limSup)
            {
                posicionY = limSup;
            }
            else if (posicionY > limInf)
            {
                posicionY = limInf;
            }

            //Convierte coord de pantalla a cartesiana -> guarda la coord en la variable global
            int origenY = panelLienzo.Height / 2;
            anzueloY = origenY - posicionY;
        }

        //Metodo para mover los objetos del mar
        private void TimerAnimacion_Tick(object sender, EventArgs e)
        {
            // Mover izq o derecha
            pezAmarilloX += velocidad;
            pezGrisX -= velocidad + 2;
            barrelX += velocidad - 2;
            bootX -= velocidad - 3;
            tinyPlaneX += velocidad - 4;

            // Si sale del límite, reiniciarlo
            if (tinyPlaneX > 650) tinyPlaneX = -650;

            if (pezAmarilloX > 600) pezAmarilloX = -600;         

            if (pezGrisX < -600) pezGrisX = 600;

            if (barrelX > 700) barrelX = -700;

            if (bootX < -700) bootX = 700;

            // Vuelve a ejecutar DibujarLienzo
            panelLienzo.Invalidate();
        }

        // Funcion para calcular coordenadas originadas al centro 0,0
        private Point ConvertirCoordenada(int x, int y)
        {
            int origenX = panelLienzo.Width / 2;
            int origenY = panelLienzo.Height / 2;

            int pantallaX = origenX + x;
            int pantallaY = origenY - y;

            return new Point(pantallaX, pantallaY);
        }

        // Funcion para dibujar un pixel 'seguro' en el bitmap, comprobanto limite de pantalla
        void DibujarPixel(Bitmap btmp, int x, int y, Color color)
        {
            // Verificamos que el pixel esté dentro del lienzo
            if (x >= 0 && x < btmp.Width &&
                y >= 0 && y < btmp.Height)
            {
                btmp.SetPixel(x, y, color);
            }
        }

        //Funcion para dibujar un circulo con coordenadas centradas
        //(cx = centro para 'x'; cy = centro para 'y'), y su color
        void DibujarCirculo(Bitmap btmp, int cx, int cy, int radio, Color color)
        {
            // Recorremos el círculo utilizando ángulos en radianes
            for (double angulo = 0; angulo <= 2 * Math.PI; angulo += 0.001)
            {
                // Ecuaciones paramétricas del círculo en el espacio cartesiano
                int x = (int)(cx + radio * Math.Cos(angulo));
                int y = (int)(cy + radio * Math.Sin(angulo));

                // Convierte el punto a coordenadas del lienzo (pantalla)
                Point pt = ConvertirCoordenada(x, y);

                // Dibujamos el pixel
                DibujarPixel(btmp, pt.X, pt.Y, color);
            }
        }

        // Funcion para Dibujar una linea
        private void DibujarLinea(Graphics g, int x1, int y1, int x2, int y2, Color color)
        {
            Point inicio = ConvertirCoordenada(x1, y1);
            Point fin = ConvertirCoordenada(x2, y2);

            using (Pen lapiz = new Pen(color, 2))
            {
                g.DrawLine(lapiz, inicio, fin);
            }
        }

        // 🌊Funcion para coloreado por cascada🌊  (flood fill iterativo con pila)
        private void RellenarCascada(Bitmap imagen, int x, int y, Color colorOriginal, Color colorNuevo)
        {
            // Pila que almacena los píxeles pendientes por revisar
            Stack<Point> pixeles = new Stack<Point>();
            // Agregamos el píxel donde comenzará el relleno
            pixeles.Push(new Point(x, y));

            // Continuamos mientras existan píxeles pendientes
            while (pixeles.Count > 0)
            {
                // Obtenemos el siguiente píxel
                Point actual = pixeles.Pop();

                int actualX = actual.X;
                int actualY = actual.Y;

                // Evitamos revisar píxeles fuera de la imagen
                if (actualX < 0 || actualX >= imagen.Width ||
                    actualY < 0 || actualY >= imagen.Height)
                {
                    continue;
                }

                // Consultamos el color del píxel
                Color colorActual =
                    imagen.GetPixel(actualX, actualY);

                // Si no tiene el color original, la cascada se detiene aquí
                if (colorActual.ToArgb() != colorOriginal.ToArgb())
                {
                    continue;
                }

                // Cambiamos el color del píxel
                imagen.SetPixel(
                    actualX,
                    actualY,
                    colorNuevo
                );

                // Agregamos los 4 píxeles vecinos para revisarlos
                pixeles.Push(
                    new Point(actualX + 1, actualY)
                );

                pixeles.Push(
                    new Point(actualX - 1, actualY)
                );

                pixeles.Push(
                    new Point(actualX, actualY + 1)
                );

                pixeles.Push(
                    new Point(actualX, actualY - 1)
                );
            }
        }

        // Función para dibujar una línea ondulante (olas o agua) de lado a lado
        private void DibujarLineaOndulante(Graphics g, int xInicio, int xFin, int yBase, int amplitud, int longitudOnda, Color color, float grosor)
        {
            List<Point> puntosOndulantes = new List<Point>();

            // Avanzamos de izquierda a derecha calculando la altura de cada onda
            for (int x = xInicio; x <= xFin; x += 10) // paso de 10px para que la curva sea fluida
            {
                // Ángulo en radianes para la función seno
                double angulo = (x - xInicio) * (2 * Math.PI / longitudOnda);
                int y = (int)(yBase + amplitud * Math.Sin(angulo));

                // Convertimos a coordenadas de pantalla usando tu función
                puntosOndulantes.Add(ConvertirCoordenada(x, y));
            }

            // Dibujamos la curva suave continua
            using (Pen lapizOnda = new Pen(color, grosor))
            {
                g.DrawCurve(lapizOnda, puntosOndulantes.ToArray());
            }
        }

        //∭•••••••••••••••••••••••••••••••••••••••••••••••••∭
        // Funcion principal para dibujar la escena ESTATICA
        //∭•••••••••••••••••••••••••••••••••••••••••••••••••∭
        private void GenerarPaisaje()
        {
            using (Graphics g = Graphics.FromImage(mapaBits))
            {

                //Punto master y obtencion del color original que será reemplazado 
                //para rellenar con cascada
                Point puntoMaster = ConvertirCoordenada(10, 10);              
                Color colorOriginalLienzo = mapaBits.GetPixel(
                     puntoMaster.X,
                     puntoMaster.Y
                );

                //MAR
                // Area del mar
                Point esquinaSuperiorIzquierda = ConvertirCoordenada(-600, 135);
                Point esquinaInferiorDerecha = ConvertirCoordenada(600, -350);
                // ancho y alto real en píxeles de pantalla
                int anchoMar = esquinaInferiorDerecha.X - esquinaSuperiorIzquierda.X;
                int altoMar = esquinaInferiorDerecha.Y - esquinaSuperiorIzquierda.Y;
                Rectangle rectanguloMar = new Rectangle(
                    esquinaSuperiorIzquierda.X,
                    esquinaSuperiorIzquierda.Y,
                    anchoMar,
                    altoMar
                );
                // Colores
                Color colorMarOscuro = Color.FromArgb(255, 94, 143, 219);
                Color colorMarClaro = Color.FromArgb(255, 183, 211, 233);
                // Brocha con degradado vertical y rellenar
                using (LinearGradientBrush brochaDegradada = new LinearGradientBrush(
                    rectanguloMar,
                    colorMarClaro, // Color de arriba
                    colorMarOscuro,  // Color de abajo
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brochaDegradada, rectanguloMar);
                }               

                //MONTANIAS (de izq a der)
                //1
                DibujarLinea(g, -600, 260, -500, 290 ,Color.Gainsboro);
                DibujarLinea(g, -500, 290, -400, 260, Color.Gainsboro);
                    //picos
                    DibujarLinea(g, -520, 285, -510, 283, Color.Gainsboro);
                    DibujarLinea(g, -510, 283, -508, 280, Color.Gainsboro);
                    DibujarLinea(g, -508, 280, -500, 283, Color.Gainsboro);
                    DibujarLinea(g, -500, 283, -490, 280, Color.Gainsboro);
                    DibujarLinea(g, -490, 280, -480, 285, Color.Gainsboro);
                //2
                DibujarLinea(g, -300, 257, -200, 290, Color.Gainsboro);
                DibujarLinea(g, -200, 290, -100, 263, Color.Gainsboro);
                    //picos
                    DibujarLinea(g, -220, 285, -210, 283, Color.Gainsboro);
                    DibujarLinea(g, -210, 283, -208, 280, Color.Gainsboro);
                    DibujarLinea(g, -208, 280, -200, 283, Color.Gainsboro);
                    DibujarLinea(g, -200, 283, -190, 280, Color.Gainsboro);
                    DibujarLinea(g, -190, 280, -180, 285, Color.Gainsboro);
                //3
                DibujarLinea(g, 100, 258, 200, 290, Color.Gainsboro);
                DibujarLinea(g, 200, 290, 300, 262, Color.Gainsboro);
                    //picos
                    DibujarLinea(g, 220, 285, 210, 283, Color.Gainsboro);
                    DibujarLinea(g, 210, 283, 208, 280, Color.Gainsboro);
                    DibujarLinea(g, 208, 280, 200, 283, Color.Gainsboro);
                    DibujarLinea(g, 200, 283, 190, 280, Color.Gainsboro);
                    DibujarLinea(g, 190, 280, 180, 285, Color.Gainsboro);
                //4
                DibujarLinea(g, 400, 260, 500, 290, Color.Gainsboro);
                DibujarLinea(g, 500, 290, 600, 260, Color.Gainsboro);
                    //picos
                    DibujarLinea(g, 520, 285, 510, 283, Color.Gainsboro);
                    DibujarLinea(g, 510, 283, 508, 280, Color.Gainsboro);
                    DibujarLinea(g, 508, 280, 500, 283, Color.Gainsboro);
                    DibujarLinea(g, 500, 283, 490, 280, Color.Gainsboro);
                    DibujarLinea(g, 490, 280, 480, 285, Color.Gainsboro);
                //HORIZONTE (base de montanias)
                //DibujarLinea(g, -600, 260, 600, 260, Color.Gainsboro);
                DibujarLineaOndulante(g, -600, 600, 260, amplitud: 3, longitudOnda: 400,
                    Color.FromArgb(255, 130, 190, 240), 2.5f);
                //Puntos para relleno
                Point[] puntosInicioMontanias = new Point[]
                {
                    new Point(-500, 270),
                    new Point(-200, 270),
                    new Point(200, 270),
                    new Point(500, 270)
                };
                Point[] puntosInicioPicos = new Point[]
                {
                    new Point(-500, 289),
                    new Point(-200, 289),
                    new Point(200, 289),
                    new Point(500, 289)
                };
                foreach (Point pCartesiano in puntosInicioMontanias)
                {
                    Point pPantalla = ConvertirCoordenada(pCartesiano.X, pCartesiano.Y);
                    RellenarCascada(mapaBits, pPantalla.X, pPantalla.Y,
                                colorOriginalLienzo, Color.FromArgb(255, 170, 171, 181)
                    );
                }
                foreach (Point pCartesiano in puntosInicioPicos)
                {
                    Point pPantalla = ConvertirCoordenada(pCartesiano.X, pCartesiano.Y);
                    RellenarCascada(mapaBits, pPantalla.X, pPantalla.Y,
                                colorOriginalLienzo, Color.FromArgb(239,255,254)
                    );
                }
                //CIELO  
                Point puntoIniciocielo = ConvertirCoordenada(0, 349);
                RellenarCascada(mapaBits, puntoIniciocielo.X, puntoIniciocielo.Y,
                    colorOriginalLienzo, Color.FromArgb(2, 101, 177));

                //ARBOLITOS
                //izq
                //1
                    DibujarLinea(g, -350,250, -350, 270, Color.Brown);
                    DibujarLinea(g, -360, 255, -340, 255, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -358, 258, -342, 258, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -356, 261, -344, 261, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -354, 264, -346, 264, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -352, 267, -348, 267, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -351, 270, -349, 270, Color.FromArgb(127, 148, 134));
                //2
                    DibujarLinea(g, -400, 240, -400, 260, Color.Brown);
                    DibujarLinea(g, -410, 245, -390, 245, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -408, 248, -392, 248, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -406, 251, -394, 251, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -404, 254, -396, 254, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -402, 257, -398, 257, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -401, 260, -399, 260, Color.FromArgb(127, 148, 134));
                //3
                    DibujarLinea(g, -300, 240, -300, 260, Color.Brown);
                    DibujarLinea(g, -310, 245, -290, 245, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -308, 248, -292, 248, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -306, 251, -294, 251, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -304, 254, -296, 254, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -302, 257, -298, 257, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, -301, 260, -299, 260, Color.FromArgb(127, 148, 134));
                //der
                //1
                    DibujarLinea(g, 350, 250, 350, 270, Color.Brown);
                    DibujarLinea(g, 360, 255, 340, 255, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 358, 258, 342, 258, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 356, 261, 344, 261, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 354, 264, 346, 264, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 352, 267, 348, 267, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 351, 270, 349, 270, Color.FromArgb(127, 148, 134));
                //2
                    DibujarLinea(g, 400, 240, 400, 260, Color.Brown);
                    DibujarLinea(g, 410, 245, 390, 245, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 408, 248, 392, 248, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 406, 251, 394, 251, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 404, 254, 396, 254, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 402, 257, 398, 257, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 401, 260, 399, 260, Color.FromArgb(127, 148, 134));
                //3
                    DibujarLinea(g, 300, 240, 300, 260, Color.Brown);
                    DibujarLinea(g, 310, 245, 290, 245, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 308, 248, 292, 248, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 306, 251, 294, 251, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 304, 254, 296, 254, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 302, 257, 298, 257, Color.FromArgb(127, 148, 134));
                    DibujarLinea(g, 301, 260, 299, 260, Color.FromArgb(127, 148, 134));


                //HOYO de pesca               
                Point[] puntosHoyo = {
                    ConvertirCoordenada(-50, 170),
                    ConvertirCoordenada(0, 180),
                    ConvertirCoordenada(50, 170),
                    ConvertirCoordenada(0, 160),
                    ConvertirCoordenada(-50, 170),
                    ConvertirCoordenada(0, 180),
                    ConvertirCoordenada(50, 170),
                    ConvertirCoordenada(0, 180),
                    ConvertirCoordenada(-50, 170),
                    ConvertirCoordenada(0, 180),
                    ConvertirCoordenada(50, 170),
                    ConvertirCoordenada(0, 180),
                    ConvertirCoordenada(-50, 170)
                };
                //BROCHAs para relleno y LAPIZ para bordes              
                using (SolidBrush brochaCeleste = new SolidBrush(Color.FromArgb(255, 183, 211, 233)))
                using (Pen lapizNegro2 = new Pen(Color.Black, 2))
                {
                    // Primeras capas de relleno                                
                    g.FillClosedCurve(brochaCeleste, puntosHoyo);

                    // Segundas capas de contorno                 
                    g.DrawCurve(lapizNegro2, puntosHoyo);
                }             

                //LATA
                Point[] puntosLata = {
                    ConvertirCoordenada(190, 170),
                    ConvertirCoordenada(190, 190),
                    ConvertirCoordenada(205, 190),
                    ConvertirCoordenada(205, 170),
                    //ConvertirCoordenada(190, 170) ESTE NO HACE FALTA YA QUE SE CONECTA DIRECTO AL PUNTO INICIAL
                };
                Point ptAbertura = ConvertirCoordenada(188, 192);
                using (SolidBrush brochaGris = new SolidBrush(Color.DarkGray))
                using (SolidBrush brochaOscura = new SolidBrush(Color.FromArgb(255, 30, 28, 27)))
                using (Pen lapizNegro1 = new Pen(Color.Black, 1))
                {
                    // Relleno y contorno principal
                    g.FillClosedCurve(brochaGris, puntosLata);
                    g.DrawClosedCurve(lapizNegro1, puntosLata);

                    //Abertura superior                  
                    g.FillEllipse(brochaOscura, ptAbertura.X, ptAbertura.Y, 19, 7);
                    g.FillEllipse(brochaOscura, ptAbertura.X + 15, ptAbertura.Y - 15, 7, 19);
                    g.FillEllipse(brochaGris, ptAbertura.X + 16, ptAbertura.Y - 14, 5, 17);
                }

                //PINGUINO               
                // Arreglos de puntos para formar figuras            
                Point[] puntosCuerpoP = {//150   180
                    ConvertirCoordenada(pinguX, pinguY),
                    ConvertirCoordenada(pinguX-20,pinguY+5),
                    ConvertirCoordenada(pinguX-35, pinguY+30),
                    ConvertirCoordenada(pinguX-20, pinguY+80),
                    ConvertirCoordenada(pinguX-15, pinguY+95),
                    ConvertirCoordenada(pinguX, pinguY+105),
                    ConvertirCoordenada(pinguX+15, pinguY+95),
                    ConvertirCoordenada(pinguX+20, pinguY+80),
                    ConvertirCoordenada(pinguX+35, pinguY+30),
                    ConvertirCoordenada(pinguX+20,pinguY+5),
                };
                Point[] puntosPicoP = {
                    ConvertirCoordenada(130, 250),
                    ConvertirCoordenada(120, 245),
                    ConvertirCoordenada(118, 240),
                    ConvertirCoordenada(125, 238),
                    ConvertirCoordenada(135, 238),
                    ConvertirCoordenada(140, 240),
                    ConvertirCoordenada(145, 245),
                };
                Point[] puntosPanzaP = {
                    ConvertirCoordenada(pinguX-10, pinguY),
                    ConvertirCoordenada(pinguX-20,pinguY+5),
                    ConvertirCoordenada(pinguX-35, pinguY+30),
                    ConvertirCoordenada(pinguX-20, pinguY+45),
                    ConvertirCoordenada(pinguX-10, pinguY+30),
                    ConvertirCoordenada(pinguX+5,pinguY+5),
                };
                Point[] puntosBrazoP = {
                    ConvertirCoordenada(pinguX+10, pinguY+50),
                    ConvertirCoordenada(pinguX+5, pinguY+30),
                    ConvertirCoordenada(pinguX-15, pinguY+10),
                    ConvertirCoordenada(pinguX+15, pinguY+20),
                    ConvertirCoordenada(pinguX+25, pinguY+45),
                };
                Point[] puntosPieDP = {
                    ConvertirCoordenada(pinguX+15, pinguY),
                    ConvertirCoordenada(pinguX, pinguY-8),
                    ConvertirCoordenada(pinguX-15, pinguY-2),
                };
                Point[] puntosPieIP = {
                    ConvertirCoordenada(pinguX-5, pinguY+2),
                    ConvertirCoordenada(pinguX-20, pinguY-6),
                    ConvertirCoordenada(pinguX-35, pinguY),
                };
                Point ptOjoP = ConvertirCoordenada(125, 265);
                Point ptBocaP = ConvertirCoordenada(125, 249);
                //BROCHAs para relleno y LAPIZ para bordes de algunos objetos.
                using (SolidBrush brochaNegra = new SolidBrush(Color.FromArgb(255, 45, 45, 41)))
                using (SolidBrush brochaBlanca = new SolidBrush(Color.WhiteSmoke))
                //using (SolidBrush brochaVerde = new SolidBrush(Color.Green))
                using (SolidBrush brochaNaranja = new SolidBrush(Color.FromArgb(255, 224, 139, 66)))
                using (Pen lapizNegro1 = new Pen(Color.Black, 1))
                using (Pen lapizNegro2 = new Pen(Color.Black, 2))
                {
                    //Pie izq, cuerpo y panza
                    g.FillClosedCurve(brochaNaranja, puntosPieIP);
                    g.DrawClosedCurve(lapizNegro2, puntosPieIP);
                    g.FillClosedCurve(brochaNegra, puntosCuerpoP);
                    g.FillClosedCurve(brochaBlanca, puntosPanzaP);                   
                    g.DrawClosedCurve(lapizNegro2, puntosCuerpoP);
                    g.DrawClosedCurve(lapizNegro1, puntosPanzaP);
                    //Boca
                        g.FillClosedCurve(brochaNaranja, puntosPicoP);
                        g.DrawClosedCurve(lapizNegro1, puntosPicoP);
                        g.DrawArc(lapizNegro1, ptBocaP.X , ptBocaP.Y, 15, 10, 0, 90);
                        g.DrawArc(lapizNegro1, ptBocaP.X+12, ptBocaP.Y+5, 8, 5, 200, 90);
                    //Brazo, pie der
                    g.FillPolygon(brochaNegra, puntosBrazoP);
                    g.DrawLines(lapizNegro2, puntosBrazoP);
                    g.FillClosedCurve(brochaNaranja, puntosPieDP);
                    g.DrawClosedCurve(lapizNegro2, puntosPieDP);
                    //Ojos
                    g.FillEllipse(brochaBlanca, ptOjoP.X, ptOjoP.Y, 11, 15);
                    //g.FillEllipse(brochaVerde, ptOjoP.X + 1, ptOjoP.Y + 3, 7, 7);
                    g.FillEllipse(brochaBlanca, ptOjoP.X + 11, ptOjoP.Y, 11, 15);
                    //g.FillEllipse(brochaVerde, ptOjoP.X + 12, ptOjoP.Y + 3, 7, 7);
                }

                //FISHING ROD
                DibujarLinea(g, 150, 185, 0, 270, Color.Brown);
                DibujarLinea(g, 150, 184, 0, 269, Color.Brown);
                Point inicioPalo = ConvertirCoordenada(0, 270);
                using (SolidBrush brochaCafe = new SolidBrush(Color.SaddleBrown))
                using (SolidBrush brochaCafe2 = new SolidBrush(Color.BurlyWood))
                using (Pen lapizGris1 = new Pen(Color.Gray, 1))
                {
                    //(lapiz, coord en x, coord en y, limite ancho, limite alto, angulo inicial, grados abarcados)
                    g.DrawArc(lapizGris1, inicioPalo.X, inicioPalo.Y, 20, 10, 45, 180);
                    g.DrawArc(lapizGris1, inicioPalo.X + 16, inicioPalo.Y + 10, 20, 9, 45, 180);
                    g.DrawArc(lapizGris1, inicioPalo.X + 32, inicioPalo.Y + 20, 20, 8, 45, 180);
                    g.DrawArc(lapizGris1, inicioPalo.X + 48, inicioPalo.Y + 30, 20, 7, 45, 180);
                    g.DrawArc(lapizGris1, inicioPalo.X + 64, inicioPalo.Y + 40, 20, 6, 45, 180);
                    g.FillEllipse(brochaCafe, inicioPalo.X + 128, inicioPalo.Y + 70, 15, 15);
                    g.FillEllipse(brochaCafe2, inicioPalo.X + 130, inicioPalo.Y + 72, 11, 11);
                    g.FillEllipse(brochaCafe, inicioPalo.X + 136, inicioPalo.Y + 75, 4, 4);
                }

                //CAJA de pesca
                DibujarLinea(g, -150, 175, -210, 185, Color.Black); // abajo izq
                DibujarLinea(g, -150, 175, -70, 185, Color.Black); // abajo der
                DibujarLinea(g, -150, 175, -150, 230, Color.Black); // medio
                DibujarLinea(g, -150, 230, -210, 240, Color.Black); // arriba izq delante
                DibujarLinea(g, -150, 225, -210, 235, Color.Black); // arriba izq delante borde
                DibujarLinea(g, -150, 230, -70, 240, Color.Black); // arriba der delante
                DibujarLinea(g, -150, 225, -70, 235, Color.Black); // arriba der delante
                DibujarLinea(g, -210, 240, -120, 250, Color.Black); // arriba izq atras
                DibujarLinea(g, -70, 240, -120, 250, Color.Black); // arriba der atras
                DibujarLinea(g, -210, 185, -210, 240, Color.Black); // izq
                DibujarLinea(g, -70, 185, -70, 240, Color.Black); // der
                //Agarradera
                DibujarLinea(g, -190, 210, -170, 207, Color.Black); //abajo
                DibujarLinea(g, -190, 210, -190, 215, Color.Black); //izq
                DibujarLinea(g, -170, 207, -170, 212, Color.Black); //der
                Point[] puntosInicioCaja = new Point[]
                {
                    new Point(-155, 180),
                    new Point(-145, 180)
                };
                foreach (Point pCartesiano in puntosInicioCaja)
                {
                    // Convierte la coordenada cartesiana a coordenada de pantalla (Bitmap)
                    Point pPantalla = ConvertirCoordenada(pCartesiano.X, pCartesiano.Y);
                    RellenarCascada(mapaBits, pPantalla.X, pPantalla.Y,
                                colorOriginalLienzo, Color.FromArgb(255, 227, 160)
                    );
                }
                Point puntoInicioCajaI = ConvertirCoordenada(-150, 235);
                RellenarCascada(mapaBits, puntoInicioCajaI.X, puntoInicioCajaI.Y,
                            colorOriginalLienzo, Color.FromArgb(255, 77, 69, 49));

                //HIELO
                DibujarLinea(g, -600, 150, 600, 150, Color.Black);
                DibujarLinea(g, -600, 135, 600, 135, Color.Black);
                Point puntoInicioHielo = ConvertirCoordenada(599, 155);
                Point puntoInicioBordeHielo = ConvertirCoordenada(599, 140);
                RellenarCascada(mapaBits, puntoInicioHielo.X, puntoInicioHielo.Y,
                                colorOriginalLienzo, Color.FromArgb(239, 255, 254));
                RellenarCascada(mapaBits, puntoInicioBordeHielo.X, puntoInicioBordeHielo.Y,
                                colorOriginalLienzo, Color.FromArgb(239, 255, 254));
            }
           
        }

        //∭••••••••••••••••••••••••••••••••••••••••••∭
        // Funciones para eventos de dibujo dinamicos 
        //∭••••••••••••••••••••••••••••••••••••••••••∭

        //Avioncito mini en el cielo
        private void PaintTinyPlane(Graphics g, int x, int y)
        {
            Point ptAvion = ConvertirCoordenada(x, y);
            using (Pen lapizNegro1 = new Pen(Color.White, 1))
            {
                g.DrawLine(lapizNegro1, ptAvion.X, ptAvion.Y, ptAvion.X - 1, ptAvion.Y - 2); //cola
                g.DrawLine(lapizNegro1, ptAvion.X, ptAvion.Y, ptAvion.X+10, ptAvion.Y); //cuerpo
                g.DrawLine(lapizNegro1, ptAvion.X+4, ptAvion.Y-2, ptAvion.X + 7, ptAvion.Y+2); //alas
            }              
        }

        //Pez amarillo
        private void PaintYellowFish(Graphics g, int x, int y)
        {
            // Arreglos de puntos para formar figuras
            Point[] puntosCola = {
                ConvertirCoordenada(x, y+5),
                ConvertirCoordenada(x - 20, y + 15),
                ConvertirCoordenada(x - 13, y),
                ConvertirCoordenada(x - 20, y - 15),
                ConvertirCoordenada(x, y-5)
            };
            Point[] puntosCuerpo = {
                ConvertirCoordenada(x, y + 5),             // cola
                ConvertirCoordenada(x + 30, y + 10),     // Lomo 
                ConvertirCoordenada(x + 25, y + 20),
                ConvertirCoordenada(x + 65, y + 10),     // Lomo 
                ConvertirCoordenada(x + 80, y),          // trompa
                ConvertirCoordenada(x + 65, y - 15),     // Vientre / aleta
                ConvertirCoordenada(x + 30, y - 15),     // Vientre / aleta
                ConvertirCoordenada(x + 30, y - 10),    // Vientre / aleta
                ConvertirCoordenada(x, y - 5)              // Regresa a cola
            };

            //Brocha para relleno y lapiz para bordes.
            using (SolidBrush brochaAmarilla = new SolidBrush(Color.FromArgb(255, 203, 55)))
            using (Pen lapizNegro = new Pen(Color.Black, 2))
            {
                // Primera capa de relleno
                g.FillPolygon(brochaAmarilla, puntosCola);
                g.FillClosedCurve(brochaAmarilla, puntosCuerpo);

                // Segunda capa de contorno
                g.DrawPolygon(lapizNegro, puntosCola);
                g.DrawClosedCurve(lapizNegro, puntosCuerpo);
            }

            //Branqueas, Detalles cola y boca
            Point ptBranqueas = ConvertirCoordenada(x + 50, y + 5);
            Point ptDetallesCola = ConvertirCoordenada(x - 13, y);
            Point ptBoca = ConvertirCoordenada(x + 80, y);
            using (Pen lapizNegro2 = new Pen(Color.Black, 1))
            {
                //Branqueas (lapiz, coord en x, coord en y, limite ancho, limite alto, angulo inicial, grados abarcados)
                g.DrawArc(lapizNegro2, ptBranqueas.X, ptBranqueas.Y, 5, 10, 90, 180);
                g.DrawArc(lapizNegro2, ptBranqueas.X - 5, ptBranqueas.Y + 1, 3, 8, 90, 180);

                //detalles cola
                g.DrawLine(lapizNegro2, ptDetallesCola.X, ptDetallesCola.Y - 5, ptDetallesCola.X + 5, ptDetallesCola.Y - 3);
                g.DrawLine(lapizNegro2, ptDetallesCola.X, ptDetallesCola.Y + 5, ptDetallesCola.X + 5, ptDetallesCola.Y + 3);

                //boca
                g.DrawLine(lapizNegro2, ptBoca.X, ptBoca.Y, ptBoca.X - 10, ptBoca.Y + 2);
            }

            // Ojo (con coordenadas relativas a la trompa)
            Point ptOjo = ConvertirCoordenada(x + 60, y + 10);
            using (SolidBrush brochaBlanca = new SolidBrush(Color.White))
            using (SolidBrush brochaNegra = new SolidBrush(Color.Black))
            {
                g.FillEllipse(brochaBlanca, ptOjo.X, ptOjo.Y, 11, 11); //globo ocular
                g.FillEllipse(brochaNegra, ptOjo.X + 4, ptOjo.Y + 3, 5, 5); //pupila
            }
        }

        //Pez gris
        private void PaintGrayFish(Graphics g, int x, int y)
        {
            Point[] puntosCola = {
                ConvertirCoordenada(x, y + 5),
                ConvertirCoordenada(x + 20, y + 15),
                ConvertirCoordenada(x + 13, y),
                ConvertirCoordenada(x + 20, y - 15),
                ConvertirCoordenada(x, y - 5)
            };

            Point[] puntosCuerpo = {
                ConvertirCoordenada(x, y + 5),
                ConvertirCoordenada(x - 15, y + 15),
                ConvertirCoordenada(x - 10, y + 20),
                ConvertirCoordenada(x - 80, y),
                ConvertirCoordenada(x - 10, y - 20),
                ConvertirCoordenada(x - 15, y - 15),
                ConvertirCoordenada(x, y - 5)
            };

            using (SolidBrush brochaGris = new SolidBrush(Color.FromArgb(255, 150, 150, 150)))
            using (Pen lapizNegro = new Pen(Color.Black, 2))
            {
                g.FillPolygon(brochaGris, puntosCola);
                g.FillClosedCurve(brochaGris, puntosCuerpo);

                g.DrawPolygon(lapizNegro, puntosCola);
                g.DrawClosedCurve(lapizNegro, puntosCuerpo);
            }

            Point ptBranqueas = ConvertirCoordenada(x - 50, y + 5);
            Point ptDetallesCola = ConvertirCoordenada(x + 13, y);
            Point ptBoca = ConvertirCoordenada(x - 80, y);

            using (Pen lapizNegro2 = new Pen(Color.Black, 1))
            {
                g.DrawArc(lapizNegro2, ptBranqueas.X, ptBranqueas.Y, 5, 10, 270, 180);
                g.DrawArc(lapizNegro2, ptBranqueas.X + 5, ptBranqueas.Y + 1, 3, 8, 270, 180);

                g.DrawLine(lapizNegro2, ptDetallesCola.X, ptDetallesCola.Y - 5, ptDetallesCola.X - 5, ptDetallesCola.Y - 3);
                g.DrawLine(lapizNegro2, ptDetallesCola.X, ptDetallesCola.Y + 5, ptDetallesCola.X - 5, ptDetallesCola.Y + 3);

                g.DrawLine(lapizNegro2, ptBoca.X, ptBoca.Y, ptBoca.X + 10, ptBoca.Y + 2); // Antes -10
            }

            Point ptOjo = ConvertirCoordenada(x - 65, y + 10);
            using (SolidBrush brochaBlanca = new SolidBrush(Color.White))
            using (SolidBrush brochaNegra = new SolidBrush(Color.Black))
            {
                g.FillEllipse(brochaBlanca, ptOjo.X, ptOjo.Y, 7, 7);
                g.FillEllipse(brochaNegra, ptOjo.X, ptOjo.Y + 2, 5, 5);
            }
        }

        //Barril
        private void PaintBarrel(Graphics g, int x, int y)
        {
            Point[] puntosBarril = {
                ConvertirCoordenada(x - 20, y + 25), // Arriba Izquierda
                ConvertirCoordenada(x + 20, y + 25), // Arriba Derecha
                ConvertirCoordenada(x + 25, y),      // Medio Derecha (el abombado)
                ConvertirCoordenada(x + 20, y - 25), // Abajo Derecha
                ConvertirCoordenada(x - 20, y - 25), // Abajo Izquierda
                ConvertirCoordenada(x - 25, y)       // Medio Izquierda (el abombado)
            };
            using (SolidBrush brochaMadera = new SolidBrush(Color.FromArgb(255, 160, 82, 45)))
            using (Pen lapizNegro = new Pen(Color.Black, 2))
            {
                // Rellenar y contornear el cuerpo del barril
                g.FillClosedCurve(brochaMadera, puntosBarril);
                g.DrawClosedCurve(lapizNegro, puntosBarril);
            }

            //Tablones de madera
            Point ptTablaCenSup = ConvertirCoordenada(x, y + 28);
            Point ptTablaCenInf = ConvertirCoordenada(x, y - 28);
            Point ptTablaIzqSup = ConvertirCoordenada(x - 10, y + 26);
            Point ptTablaIzqInf = ConvertirCoordenada(x - 10, y - 26);
            Point ptTablaIzqSup2 = ConvertirCoordenada(x - 20, y + 25);
            Point ptTablaIzqInf2 = ConvertirCoordenada(x - 20, y - 25);
            Point ptTablaDerSup = ConvertirCoordenada(x + 10, y + 26);
            Point ptTablaDerInf = ConvertirCoordenada(x + 10, y - 26);
            Point ptTablaDerSup2 = ConvertirCoordenada(x + 20, y + 25);
            Point ptTablaDerInf2 = ConvertirCoordenada(x + 20, y - 25);

            using (Pen lapizTablas = new Pen(Color.FromArgb(255, 100, 50, 20), 2))
            {
                g.DrawLine(lapizTablas, ptTablaCenSup, ptTablaCenInf);
                g.DrawLine(lapizTablas, ptTablaIzqSup, ptTablaIzqInf);
                g.DrawLine(lapizTablas, ptTablaIzqSup2, ptTablaIzqInf2);
                g.DrawLine(lapizTablas, ptTablaDerSup, ptTablaDerInf);
                g.DrawLine(lapizTablas, ptTablaDerSup2, ptTablaDerInf2);
            }

            // Metal
            Point ptAroSupIzq = ConvertirCoordenada(x - 24, y + 15);
            Point ptAroSupDer = ConvertirCoordenada(x + 24, y + 15);
            Point ptAroInfIzq = ConvertirCoordenada(x - 24, y - 15);
            Point ptAroInfDer = ConvertirCoordenada(x + 24, y - 15);
            using (Pen lapizMetal = new Pen(Color.Gold, 3))
            {
                g.DrawLine(lapizMetal, ptAroSupIzq, ptAroSupDer);
                g.DrawLine(lapizMetal, ptAroInfIzq, ptAroInfDer);
            }
        }

        //Si la Bota te queda
        private void PaintBoot(Graphics g, int x, int y)
        {
            Point[] puntosBota = {
                ConvertirCoordenada(x + 15, y + 25),  // 1. Borde superior izquierdo (abertura)
                ConvertirCoordenada(x - 15, y + 25),  // 2. Borde superior derecho
                ConvertirCoordenada(x - 10, y - 5),   // 3. Tobillo trasero
                ConvertirCoordenada(x - 15, y - 25),  // 4. Talón (abajo derecha)
                ConvertirCoordenada(x + 30, y - 25),  // 5. Punta de la bota (abajo izquierda)
                ConvertirCoordenada(x + 35, y - 10),  // 6. Empeine (arriba de la punta)
                ConvertirCoordenada(x + 10, y)        // 7. Tobillo delantero
            };

            // Colores para cuero viejo y suela
            using (SolidBrush brochaCafe = new SolidBrush(Color.FromArgb(255, 139, 69, 19)))
            using (Pen lapizNegro = new Pen(Color.Black, 2))
            {
                // Relleno y contorno principal
                g.FillClosedCurve(brochaCafe, puntosBota);
                g.DrawClosedCurve(lapizNegro, puntosBota);
            }

            // Detalles: La abertura superior (un óvalo negro para dar profundidad)
            Point ptAbertura = ConvertirCoordenada(x - 15, y + 28);
            using (SolidBrush brochaOscura = new SolidBrush(Color.FromArgb(255, 60, 30, 10)))
            {
                // Dibujamos una elipse aplastada en la parte de arriba
                g.FillEllipse(brochaOscura, ptAbertura.X, ptAbertura.Y, 30, 10);
            }

            // Detalles: Línea de la suela
            Point ptSuelaInicio = ConvertirCoordenada(x + 35, y - 23);
            Point ptSuelaFin = ConvertirCoordenada(x - 15, y - 23);
            using (Pen lapizSuela = new Pen(Color.Black, 2))
            {
                g.DrawLine(lapizSuela, ptSuelaInicio.X, ptSuelaInicio.Y, ptSuelaFin.X, ptSuelaFin.Y);
            }
        }
       
        //Gusano y Pupilas del pingu
        private void PaintWorm(Graphics g, int x, int y)
        {
            // Define a qué altura de 'y' el gusano sale del agua. 
            // Ajusta este número según tus coordenadas cartesianas.
            int limiteSuperficie = 170;
            bool enElAgua = y < limiteSuperficie;

            Point[] puntosGusano;
            Point ptOjoP = ConvertirCoordenada(125, 265);

            if (enElAgua)
            {
                // Estado 1:
                // Gusano Sumergido (flotanto)
                puntosGusano = new Point[] {
                ConvertirCoordenada(x - 12, y - 20),  // Cola izquierda
                ConvertirCoordenada(x - 5, y - 13),   // Doblez en el anzuelo
                ConvertirCoordenada(x + 3, y - 11),   // Centro
                ConvertirCoordenada(x + 14, y - 18)   // Cola derecha
                };
                //pupilas pa bajo               
                using (SolidBrush brochaVerde = new SolidBrush(Color.Green))
                {                 
                    g.FillEllipse(brochaVerde, ptOjoP.X + 1, ptOjoP.Y + 8, 7, 7);                 
                    g.FillEllipse(brochaVerde, ptOjoP.X + 12, ptOjoP.Y + 8, 7, 7);
                }                       
            }
            else
            {
                // Estado 2:
                // Gusano Superficie (Caido)
                puntosGusano = new Point[] {
                ConvertirCoordenada(x - 6, y - 26),  // Cola izquierda recta hacia abajo
                ConvertirCoordenada(x - 4, y - 12),   // Curva cerrada izquierda
                ConvertirCoordenada(x + 3, y - 11),   // Curva cerrada derecha
                ConvertirCoordenada(x + 5, y - 28)   // Cola derecha recta hacia abajo
                };
                //pupilas pa bajo               
                using (SolidBrush brochaVerde = new SolidBrush(Color.Green))
                {                 
                    g.FillEllipse(brochaVerde, ptOjoP.X + 1, ptOjoP.Y + 5, 7, 7);                 
                    g.FillEllipse(brochaVerde, ptOjoP.X + 12, ptOjoP.Y + 5, 7, 7);
                }         
            }

            // Contorno
            using (Pen bordeNegro = new Pen(Color.Gray, 5))
            {
                bordeNegro.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                bordeNegro.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                // Usamos Tension = 0.6f para que la curva sea un poco más suave
                g.DrawCurve(bordeNegro, puntosGusano, 0.6f);
            }

            // Cuerpo
            using (Pen rellenoRosa = new Pen(Color.LightPink, 4))
            {
                rellenoRosa.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                rellenoRosa.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                g.DrawCurve(rellenoRosa, puntosGusano, 0.6f);
            }
        }

        //FishingRod (El hilo y anzuelo)
        private void PaintFRod(Graphics g, int x, int y)
        {
            // Punto inicial del hilo
            //                                 x = 0, y = 250 
            Point inicioHilo = ConvertirCoordenada(x, 270);

            // Punto final del hilo (donde está el anzuelo, controlado por 'y')
            Point finHilo = ConvertirCoordenada(x, y);

            using (Pen lapizHilo = new Pen(Color.Black, 1.0f))
            using (Pen lapizAnzuelo = new Pen(Color.Black, 2.0f))
            {
                // Hilo usando las coordenadas de pantalla convertidas
                g.DrawLine(lapizHilo, inicioHilo.X, inicioHilo.Y, finHilo.X, finHilo.Y);

                //Dibuja al gusano
                PaintWorm(g, x, y);
                // Anzuelo en el punto final
                g.DrawArc(lapizAnzuelo, finHilo.X - 7, finHilo.Y, 15, 15, 0, 270);

            }
        }

        //∭••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••∭
        // Funcion para dibujar el lienzo FINAL (primero lo estatico y luego lo dinamico)
        //∭••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••∭
        private void DibujarLienzo(object sender, PaintEventArgs e)
        {

            Graphics g = e.Graphics;

            //Dibuja lo estatico
            if (mapaBits != null)
            {
                e.Graphics.DrawImage(mapaBits, 0, 0);
            }

            //Dibuja lo dinamico
            PaintTinyPlane(g, tinyPlaneX, tinyPlaneY);
            PaintYellowFish(g, pezAmarilloX, pezAmarilloY);
            PaintGrayFish(g, pezGrisX, pezGrisY);
            PaintBarrel(g, barrelX, barrelY);
            PaintBoot(g, bootX, bootY);
            // Aplica la máscara pre-calculada en Form1 (con cero lag)
            System.Drawing.Drawing2D.GraphicsState estadoOriginal = g.Save();
            g.Clip = regionRecorteHilo;
            PaintFRod(g, anzueloX, anzueloY);
            g.Restore(estadoOriginal);

        }

        // Se ejecuta solito cuando el se cierra la ventana
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            //Buena practica quitar el audio
            reproductorAudio?.Stop();
        }
    }
}
