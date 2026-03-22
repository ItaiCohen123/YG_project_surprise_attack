using System.Text;
using System.Diagnostics; 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace Surprise_Attack_test
{
    public class MapRenderer
    {

        public WriteableBitmap writeableBitmap;
        public TerrainMap terrainMap;
        public TerrainGraph terrainGraph;
        Image mapImage;

        public MapRenderer(Image mapImage)
        {
            terrainMap = new TerrainMap();
            this.mapImage = mapImage;
            InitializeMap(mapImage);
            terrainMap.InitiateFlatMap();
            DrawTerrain(false);
        }
        public void InitializeMap(Image MapImage)
        {
            this.writeableBitmap = new WriteableBitmap(TerrainMap.MAP_WIDTH, TerrainMap.MAP_LENGTH, 96, 96, PixelFormats.Bgra32, null);
            MapImage.Source = writeableBitmap;

        }
        public void DrawTerrain(bool showRestricted)
        {
            
            
            Algorithms.ViewShedGlobal(this.terrainMap);
            


            int width = TerrainMap.MAP_WIDTH;
            int length = TerrainMap.MAP_LENGTH;
            // Array to hold all pixles, each pixles holds 4 sells [Blue, Green, Red, Alpha]
            byte[] pixels = new byte[length * width * 4];
            byte[] BGR_Pixel = new byte[3]; // [blue, green, red]

            PositionInfo currPos;

            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * width + x) * 4;


                    currPos = this.terrainMap.terrainHeightsMap[y, x];
                    BGR_Pixel = GetColorByPos(currPos, showRestricted);
                    DrawPixel(pixels, pixelIndex, BGR_Pixel[0], BGR_Pixel[1], BGR_Pixel[2]);

                }

            }



            writeableBitmap.WritePixels(
                new Int32Rect(0, 0, width, length), // האזור לעדכון
                pixels,                             // מערך המידע
                width * 4,                          // ה-Stride (רוחב שורה בבייטים)
                0);                                 // היסט (Offset)
        }
        
        public void DrawPhermones(List<Edge> route, bool isWhite)
        {


            int width = TerrainMap.MAP_WIDTH;
            
            

            try
            {
               // Lock the bitmap so the UI doesn't try to read it while we update it
                writeableBitmap.Lock();

                foreach (Edge edge in route)
                {
                    if (edge.from == this.terrainMap.startPos || edge.from == this.terrainMap.targetPos)
                        continue;

                    int y = edge.from.yCord;
                    int x = edge.from.xCord;
                    byte[] color = GetColorByPheromone(edge.pheromone, isWhite);

                  //  Calculate the exact memory address of this pixel
                    int pixelOffset = (y * width + x) * 4;
                    IntPtr pixelAddress = writeableBitmap.BackBuffer + pixelOffset;

                    // Write the 4 bytes (B, G, R, A) directly to memory
                    System.Runtime.InteropServices.Marshal.Copy(color, 0, pixelAddress, 4);
                }

                // Tell WPF to redraw the entire map once we are done
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, TerrainMap.MAP_WIDTH, TerrainMap.MAP_LENGTH));
            }
            finally
            {
                writeableBitmap.Unlock();
            }
        }
        public void DrawPixel(byte[] pixles, int index, byte blue, byte green, byte red)
        {

            pixles[index] = blue;
            pixles[index + 1] = green;
            pixles[index + 2] = red;
            pixles[index + 3] = 255;



        }
        public void ClearMap()
        {
            int width = TerrainMap.MAP_WIDTH;
            int length = TerrainMap.MAP_LENGTH;
            byte[] pixels = new byte[length * width * 4];
            this.terrainMap.InitiateFlatMap();
            DrawTerrain(false);

        }
        public byte[] GetColorByPos(PositionInfo pos, bool showRestricted)
        {
            // BGR Order: [Blue, Green, Red]
            byte blue = 0;
            byte green = 0;
            byte red = 0;
            byte[] BGR = new byte[3];

            int h = (int)pos.height;


         

            switch (pos.typeOfTerrain)
            {
                case TerrainMap.GRASS:
                    blue = 34;
                    green = 139; // Forest Green
                    red = 34;
                    break;

                case TerrainMap.MOUNTAIN:

                    // 1. Snow Cap Logic: If the mountain is very high, make it white
                    if (h > 230)
                    {
                        blue = 255; green = 255; red = 255; // White
                    }
                    else
                    {
                        // 2. Brown Gradient Logic
                        // Brown is made by Red > Green > Blue.
                        // We use the height 'h' to control brightness.

                        // Red component is the strongest in brown
                        red = (byte)h;

                        // Green is usually about half of Red for brown
                        green = (byte)(h / 2);

                        // Blue is very low for brown
                        blue = (byte)(h / 10);

                        // This creates a gradient:
                        // Height 50  -> Dark Brown (5, 25, 50)
                        // Height 200 -> Light Brown/Orange (20, 100, 200)
                    }
                    break;
                case TerrainMap.CAMERA:
                    red = 255;
                    green = 255;
                    blue = 0;
                    break;

                default:
                    // Default to Black for errors
                    blue = 0; green = 0; red = 0;
                    break;
            }
            if (showRestricted && !pos.isSafe && pos.typeOfTerrain != TerrainMap.CAMERA)
            {
                red = 255;
                green = (byte)(h / 3);
                blue = (byte)(h / 3);
            }
            if (pos.isStartingPos)
            {
                blue = 200;
                green = (byte)(h / 3);
                red = (byte)(h / 3);

            }
            else if(pos.isTargetPos)
            {
                red = 0;
                green = 0;
                blue = 0;
            }

            BGR[0] = blue;
            BGR[1] = green;
            BGR[2] = red;

            return BGR;
        }
        public byte[] GetColorByPheromone(double pheromone, bool isWhite)
        {


            if (isWhite)
            {
                return new byte[] { 245, 245, 245, 255};

            }



            // 1. Define the expected minimum and maximum pheromone levels.
            double minPheromone = Ant.MIN_PHEROMONE_VALUE; 
            double maxPheromone = Ant.MAX_PHEROMONE_VALUE;

            // 2. Normalize the pheromone value to a scale of 0.0 to 1.0
            double range = maxPheromone - minPheromone;
            double intensity = (pheromone - minPheromone) / range;

            // 3. Clamp the intensity to ensure it never goes below 0 or above 1
            // (Otherwise, multiplying by 255 could crash the byte cast)
            intensity = Math.Max(0.0, Math.Min(1.0, intensity));

            // 4. Calculate the gradient (Yellow -> Red)
            // Low intensity  = {0, 255, 255, 255} (Yellow)
            // High intensity = {0, 0,   255, 255} (Red)

            byte blue = 200;
            byte green = (byte)(255 * (1.0 - intensity)); // Green drops to 0 as intensity goes up
            byte red = 255;                               // Red stays maxed out
            byte alpha = 255;                             // Fully opaque

            // Remember: WPF WriteableBitmap uses BGRA order!

           
            return new byte[] { blue, green, red, alpha };
        }
        public (int yCord, int xCord) ConvertMapImagePointToCords(Point mapImagePoint, Image MapImage)
        {
            if (this.writeableBitmap == null || MapImage == null || MapImage.ActualWidth == 0 || MapImage.ActualHeight == 0)
                return (-1, -1);

            double scaleX = this.writeableBitmap.PixelWidth / MapImage.ActualWidth;
            double scaleY = this.writeableBitmap.PixelHeight / MapImage.ActualHeight;

            int mapX = (int)(mapImagePoint.X * scaleX);
            int mapY = (int)(mapImagePoint.Y * scaleY);

            

            return (mapY, mapX);
        }

        public Point ConvertCordsToMapImagePoint(int mapY, int mapX, Image MapImage)
        {
            if (this.writeableBitmap == null || MapImage == null || MapImage.ActualWidth == 0 || MapImage.ActualHeight == 0)
                return new Point(0, 0);

            Point result = new Point();
            double scaleX = this.writeableBitmap.PixelWidth / MapImage.ActualWidth;
            double scaleY = this.writeableBitmap.PixelHeight / MapImage.ActualHeight;

            result.X = mapX / scaleX;
            result.Y = mapY / scaleY;

            return result;
        }

    }

    public partial class MainWindow : Window
    {
        public const int NOTHING = 0;
        public const int ADD_MOUNTAIN = 1;
        public const int ADD_CAMERA = 2;
        public const int DELETE_CAMERA = 3;
        public const int START_POS = 4;
        public const int TARGET_POS = 5;

        public const int DELAY = 750;

        MapRenderer mapRenderer;
        int action;
        bool startPosDecided = false;
        bool targetPosDecided = false;
        bool SimulationRunning = false;
        bool endSimulation = false;
       
        bool showRestricted;
        int mountainHeigth;
        int mountainRadius;

        public MainWindow()
        {
            InitializeComponent();
            MountainInputOverlay.Visibility = Visibility.Collapsed;
            this.mountainHeigth = 0;
            this.mountainRadius = 0;
            this.action = NOTHING;
            this.showRestricted = false;
           

            this.mapRenderer = new MapRenderer(MapImage);
           

            
           

        }
        private void SaveMap_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON Files (*.json)|*.json";
            saveFileDialog.DefaultExt = ".json";

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveMapData saveMapData = new SaveMapData(this.mapRenderer.terrainMap);
               
                string jsonString = JsonSerializer.Serialize(saveMapData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(saveFileDialog.FileName, jsonString);
                Console.WriteLine(jsonString);

                MessageBox.Show("Map saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void LoadMap_Click(Object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json";

            if(openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonData = File.ReadAllText(openFileDialog.FileName);
                    SaveMapData savedMap = JsonSerializer.Deserialize<SaveMapData>(jsonData,  new JsonSerializerOptions { WriteIndented = true });

                    this.mapRenderer.terrainMap.LoadMap(savedMap);
                    this.mapRenderer.DrawTerrain(this.showRestricted);




                }
                catch(Exception ex){
                    MessageBox.Show("Faild to load map: " + ex.Message);
                }
            }
            this.startPosDecided = this.mapRenderer.terrainMap.startPos != null;
            this.targetPosDecided = this.mapRenderer.terrainMap.targetPos != null;

            if(this.startPosDecided && this.targetPosDecided)
            {
                StartSimulation_Button.IsEnabled = true;
                FastStartSimulation_Button.IsEnabled = true;
            }

        }
        private void RestrictedArea_Checked(object sender, EventArgs e)
        {
            this.showRestricted = true;
            this.mapRenderer.DrawTerrain(this.showRestricted);


        }
        private void RestrictedArea_Unchecked(object sender, EventArgs e)
        { 
            this.showRestricted = false;
            this.mapRenderer.DrawTerrain(this.showRestricted);


        }
        private void DeleteCamera_Click(object sender, EventArgs e)
        {
            this.action = DELETE_CAMERA;
            DisableAllButtons();
        }
        private void AddCamera_Click(Object sender, EventArgs e)
        {
            if (this.mapRenderer.terrainMap.cameraList.Count >= TerrainMap.MAX_CAMERA_NUM)
                MessageBox.Show("Reached maximum number of cameras!!!");
            else
            {
                this.action = ADD_CAMERA;
                DisableAllButtons();
            }
        }
        private void ClearMap_Click(Object sender, RoutedEventArgs e)
        {
            this.mapRenderer.ClearMap();
            StartSimulation_Button.IsEnabled = false;
            FastStartSimulation_Button.IsEnabled = false;
            this.startPosDecided = false;
            this.targetPosDecided = false;

        }
        private void StartPos_Click(Object sender, EventArgs e)
        {
            this.action = START_POS;
            DisableAllButtons();
        }
        private void TargetPos_Click(Object sender, EventArgs e)
        {
            this.action = TARGET_POS;
            DisableAllButtons();
        }
       
        private void MapImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(MapImage);
            int mapY; 
            int mapX;
            (mapY, mapX) = this.mapRenderer.ConvertMapImagePointToCords(clickPoint, MapImage);
                  
            if(this.mapRenderer.terrainMap.InBounds(mapY, mapX))
            {
                switch (this.action)
                {
                    case ADD_CAMERA:

                        if (this.mapRenderer.terrainMap.CheckNewCamAllowed(mapY, mapX))
                        {                            
                            this.mapRenderer.DrawTerrain(this.showRestricted);
                        }
                        else
                            MessageBox.Show("New camera position is not allowed, too close to starting/target position");
                        
                        EnableAllButtons();
                        this.action = NOTHING;
                        
                        break;
                    case ADD_MOUNTAIN:
                        this.mapRenderer.terrainMap.AddCircleMountainAtPos(this.mountainHeigth, mapY, mapX, this.mountainRadius);
                        this.mapRenderer.DrawTerrain(this.showRestricted);
                        EnableAllButtons();
                        this.action = NOTHING;
                        break;
                    case DELETE_CAMERA:
                        
                        Camera camToDelete = this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX].cam;
                        if (camToDelete != null)
                        {
                            this.mapRenderer.terrainMap.DeleteCamera(camToDelete);
                            this.mapRenderer.DrawTerrain(this.showRestricted);
                        }
                        EnableAllButtons();
                        this.action = NOTHING;
                        break;
                    case START_POS:
                        if (!this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX].isSafe)
                            MessageBox.Show("Starting position must be a safe area!");
                        else if (this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX].isTargetPos)
                            MessageBox.Show("Starting position must be different from target position!");
                        else
                        {
                            if (this.mapRenderer.terrainMap.startPos != null)
                            {
                                this.mapRenderer.terrainMap.terrainHeightsMap[this.mapRenderer.terrainMap.startPos.yCord, this.mapRenderer.terrainMap.startPos.xCord].isStartingPos = false;
                            }
                                        
                            this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX].isStartingPos = true;
                            this.mapRenderer.terrainMap.startPos = this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX];
                            this.mapRenderer.DrawTerrain(this.showRestricted);
                            this.startPosDecided = true;


                            if (this.targetPosDecided)
                            {
                                StartSimulation_Button.IsEnabled = true;
                                FastStartSimulation_Button.IsEnabled = true;
                            }

                        }

                        EnableAllButtons();
                        this.action = NOTHING;
                        break;
                    case TARGET_POS:
                        if (!this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX].isSafe)
                            MessageBox.Show("Target position must be a safe area!");
                        else if(this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX].isStartingPos)
                            MessageBox.Show("Target position must be different from starting position!");
                        else
                        {
                            if (this.mapRenderer.terrainMap.targetPos != null)
                            {
                                this.mapRenderer.terrainMap.terrainHeightsMap[this.mapRenderer.terrainMap.targetPos.yCord, this.mapRenderer.terrainMap.targetPos.xCord].isTargetPos = false;
                            }

                            this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX].isTargetPos = true;
                            this.mapRenderer.terrainMap.targetPos = this.mapRenderer.terrainMap.terrainHeightsMap[mapY, mapX];
                            this.mapRenderer.DrawTerrain(this.showRestricted);
                            this.targetPosDecided = true;

                            if (this.startPosDecided)
                            {
                                StartSimulation_Button.IsEnabled = true;
                                FastStartSimulation_Button.IsEnabled = true;
                            }
                        }

                        EnableAllButtons();
                        this.action = NOTHING;
                        break;
                    default:
                        break;

                }
            }
           
            
        }
        private void AddMountain_Click(Object sender, RoutedEventArgs e)
        {
            this.action = ADD_MOUNTAIN;
            MountainInputOverlay.Visibility = Visibility.Visible;
            txtOverlayHeight.Text = "230";
            txtOverlayRadius.Text = "6";
            DisableAllButtons();
           
        }
        private async void StartSimulation_Click(Object sender, RoutedEventArgs e)
        {

            if (this.SimulationRunning)
            {
                this.endSimulation = true;
                return;
            }



            this.SimulationRunning = true;
            DisableAllButtons();
            StartSimulation_Button.Content = "End Simulation";
            FastStartSimulation_Button.IsEnabled = false;

            // builds graph from the terrain height map
            this.mapRenderer.terrainGraph = new TerrainGraph(this.mapRenderer.terrainMap);
            List<Ant> genAnts;
            Ant bestAnt;

            if (int.TryParse(GenCountBox.Text, out int genCount))
            {
                for (int i = 0; i < genCount; i++)
                {
                    this.mapRenderer.DrawTerrain(this.showRestricted);
                    GenCount_Label.Content = $"Generation Count: {i + 1}";

                    genAnts = await Task.Run(() =>
                        Algorithms.RunSplitGenerationACO(Ant.ANT_COUNT_GEN, this.mapRenderer.terrainGraph, this.mapRenderer.terrainMap.startPos, this.mapRenderer.terrainMap.targetPos)
                    );

                    bestAnt = Ant.BestAnt(genAnts);


                    this.mapRenderer.DrawPhermones(bestAnt.edgesVisited, false);

                    await Task.Delay(DELAY);


                    if(this.endSimulation)
                    {
                        this.endSimulation = false;
                        break;
                    }
                }
            }

           

           
            BestAnt_Button.IsEnabled = true;
            this.SimulationRunning = false;
            StartSimulation_Button.Content = "Start Simulation";
            StartSimulation_Button.IsEnabled = false;


        }

        private async void FastStartSimulation_Click( object sender, RoutedEventArgs e)
        {

            if (this.SimulationRunning)
            {
                this.endSimulation = true;
                return;
            }


            this.SimulationRunning = true;
            DisableAllButtons();
            StartSimulation_Button.IsEnabled = false;
            FastStartSimulation_Button.Content = "End Simulation";
            this.mapRenderer.DrawTerrain(this.showRestricted);
            // builds graph from the terrain height map
            this.mapRenderer.terrainGraph = new TerrainGraph(this.mapRenderer.terrainMap);
            List<Ant> genAnts;
            Ant bestAnt;

            if (int.TryParse(GenCountBox.Text, out int genCount))
            {
                for (int i = 0; i < genCount; i++)
                {
                    GenCount_Label.Content = $"Generation Count: {i + 1}";

                    genAnts = await Task.Run(() =>
                        Algorithms.RunSplitGenerationACO(Ant.ANT_COUNT_GEN, this.mapRenderer.terrainGraph, this.mapRenderer.terrainMap.startPos, this.mapRenderer.terrainMap.targetPos)
                    );

                    if (i % (genCount / 5) == 0)
                    {
                        this.mapRenderer.DrawTerrain(this.showRestricted);
                        bestAnt = Ant.BestAnt(Algorithms.allAnts);
                        this.mapRenderer.DrawPhermones(bestAnt.edgesVisited, false);
                    }

                    if (this.endSimulation)
                    {
                        this.endSimulation = false;
                        break;
                    }
                }
            }

           
            BestAnt_Button.IsEnabled = true;
            this.SimulationRunning = false;
            FastStartSimulation_Button.Content = "Start No Delay Simulation";
            FastStartSimulation_Button.IsEnabled = false;

        }
        private void BestAnt_Click(Object sender, RoutedEventArgs e)
        {
            Ant bestAnt = Ant.BestAnt(Algorithms.allAnts);
            this.mapRenderer.DrawTerrain(this.showRestricted);


            this.mapRenderer.DrawPhermones(bestAnt.edgesVisited, true);

            EnableAllButtons();
            StartSimulation_Button.IsEnabled = true;
            FastStartSimulation_Button.IsEnabled = true;
            BestAnt_Button.IsEnabled = false;
            GenCount_Label.Content = "Generation Count: 0";

            Algorithms.ResetParameters();
            

        }
        private void OverlayCancel_Click(Object sender, RoutedEventArgs e)
        {
            MountainInputOverlay.Visibility= Visibility.Collapsed;
            EnableAllButtons();
            this.action= NOTHING;
        }
        private void OverlayConfirm_Click(Object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtOverlayRadius.Text, out int radius) && int.TryParse(txtOverlayHeight.Text, out int height))
            {
                if (height < 256)
                {
                    MountainInputOverlay.Visibility = Visibility.Collapsed;
                    this.mountainRadius = radius;
                    this.mountainHeigth = height;
                }
                else
                {
                    MessageBox.Show("Height must be under 256");
                }
                
            }
            else
            {
                MessageBox.Show("Please enter valid whole numbers.");
            }
        }
        private void DisableAllButtons()
        {
            AddCamera_Button.IsEnabled = false;
            AddMountain_Button.IsEnabled = false;         
            ClearMap_Button.IsEnabled = false;
            chkShowRestricted.IsEnabled = false;
            DeleteCamera_Button.IsEnabled = false;
            StartPos_Button.IsEnabled = false;
            TargetPos_Button.IsEnabled = false;
            GenCountBox.IsEnabled = false;
            SaveMap_Button.IsEnabled = false;
            LoadMap_Button.IsEnabled = false;
            
        }
        private void EnableAllButtons()
        {
            AddCamera_Button.IsEnabled = true;
            AddMountain_Button.IsEnabled = true;            
            ClearMap_Button.IsEnabled = true;
            chkShowRestricted.IsEnabled = true;
            DeleteCamera_Button.IsEnabled = true;
            StartPos_Button.IsEnabled = true;
            TargetPos_Button.IsEnabled = true;
            GenCountBox.IsEnabled = true;
            SaveMap_Button.IsEnabled = true;
            LoadMap_Button.IsEnabled = true;
        }
       
    }
    
}