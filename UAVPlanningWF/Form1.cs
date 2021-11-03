using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
using System.Device.Location;
using System.Threading;
using System.IO;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;
using GMap.NET.WindowsForms.ToolTips;
using System.Drawing.Drawing2D;

namespace UAVPlanningWF
{
    public partial class Form1 : Form
    {

        StreamReader textFileReader;
        List<string> connections = new List<string>();
        List<uint> nodes = new List<uint>();
        List<PointLatLng> nodesList = new List<PointLatLng>();
        List<string> nodesString = new List<string>();
        PointLatLng GPS_pos, GPS1, GPS2;
        List<PointLatLng> _points = new List<PointLatLng>();
        List<PointLatLng> points = new List<PointLatLng>();
        List<PointLatLng> shortestPathPoints = new List<PointLatLng>();

        Graph<int, string> graph = new Graph<int, string>();
        GMapOverlay polyOverlay = new GMapOverlay("polygons");
        GMapOverlay polyOverlayShortestPath = new GMapOverlay("ShortestPolygons");
        GMapPolygon polygon;
        GMapOverlay markers = new GMapOverlay("markers");
        GMapOverlay markersDistOverlay = new GMapOverlay("markersDistance");
        PointLatLng shortestPath, shortestPathPrev;

        bool enableConnections = false;
        bool enableFind = false;



        public Form1()
        {
            InitializeComponent();
            timer1.Start();
            cb_maps.SelectedIndex = 0;
            loadMap(cb_maps.SelectedIndex, 40.93333839465396,29.305049843651105,10.0);

            dataGridView1.ColumnCount = 3;
            dataGridView1.Columns[0].Name = "Start Node ";
            dataGridView1.Columns[1].Name = "End Node";
            dataGridView1.Columns[2].Name = "Distance";
            //int n = dataGridView1.Columns.Count;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.EnableHeadersVisualStyles = true;
            dataGridView1.RowHeadersVisible = false;

            DataGridViewCellStyle rowStyle; // = Grid.RowHeadersDefaultCellStyle;
            rowStyle = dataGridView1.Rows[0].HeaderCell.Style;
            rowStyle.BackColor = Color.Red;
            dataGridView1.Rows[0].HeaderCell.Style = rowStyle;

            dataGridView1.BackgroundColor = System.Drawing.SystemColors.Control;
        }

        public Bitmap createTextString(string str)
        {
            int h = 20;
            int w = 35;
            Bitmap flag = new Bitmap(w, h);
            Graphics flagGraphics = Graphics.FromImage(flag);
            SolidBrush br = new SolidBrush(Color.BlueViolet);

            flagGraphics.FillRectangle(br, 0, 0, w, 2);
            flagGraphics.FillRectangle(br, 0, 0, 2, h);
            flagGraphics.FillRectangle(br, w - 2, 0, 2, h);
            flagGraphics.FillRectangle(br, 0, h - 2, w, 2);

            RectangleF rectf = new RectangleF(2, 2, w, h);

            flagGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            flagGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            flagGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            flagGraphics.DrawString(str, new Font("Calibri", 8), Brushes.Black, rectf);

            return flag;
        }

        public void loadMap(int mapCase,double lat, double lng, double zoom)
        {
            map.DragButton = MouseButtons.Left;
            switch (mapCase)
            {
                case 0:
                    map.MapProvider = GMapProviders.GoogleMap;
                    break;
                case 1:
                    map.MapProvider = GMapProviders.YandexMap;
                    break;
                case 2:
                    map.MapProvider = GMapProviders.BingMap;
                    break;
                case 3:
                    map.MapProvider = GMapProviders.YandexSatelliteMap;
                    break;
                case 4:
                    map.MapProvider = GMapProviders.GoogleSatelliteMap;
                    break;
                case 5:
                    map.MapProvider = GMapProviders.GoogleTerrainMap;
                    break;
            }
            //map.MapProvider = GMapProviders.BingMap;

            //GPS_pos.Lat = lat;//double.Parse(txtLat.Text);// 41.015137; 41.026448854869074, 28.97850803471917
            //GPS_pos.Lng = lng;// double.Parse(txtLong.Text); //28.979530;
            map.Position = new PointLatLng(lat, lng);
            map.MinZoom = 1;
            map.MaxZoom = 100;
            map.Zoom = zoom;
            map.ShowCenter = false;
        }

        public double DegreesToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public double RadiansToDegrees(double rad)
        {
            return (rad * 180.0) / Math.PI;
        }

        private GeoCoordinate MidPoint(GeoCoordinate posA, GeoCoordinate posB)
        {
            GeoCoordinate midPoint = new GeoCoordinate();

            double dLon = DegreesToRadians(posB.Longitude - posA.Longitude);
            double Bx = Math.Cos(DegreesToRadians(posB.Latitude)) * Math.Cos(dLon);
            double By = Math.Cos(DegreesToRadians(posB.Latitude)) * Math.Sin(dLon);
            double lat = 0, lon = 0;


            lat = RadiansToDegrees(Math.Atan2(
                         Math.Sin(DegreesToRadians(posA.Latitude)) + Math.Sin(DegreesToRadians(posB.Latitude)),
                         Math.Sqrt(
                             (Math.Cos(DegreesToRadians(posA.Latitude)) + Bx) *
                             (Math.Cos(DegreesToRadians(posA.Latitude)) + Bx) + By * By)));

            lon = posA.Longitude + RadiansToDegrees(Math.Atan2(By, Math.Cos(DegreesToRadians(posA.Latitude)) + Bx));
            
            if (lat > 90.0)
            {
                midPoint.Latitude = lat - 180;
            }
            else
            {
                midPoint.Latitude = lat;
            }

            if (lon > 180.0)
            {
                midPoint.Longitude = lon - 360;
            }
            else
            {
                midPoint.Longitude = lon;
            }                       
            return midPoint;
        }

        void addNodesList(string st, int num)
        {
            string[] str = st.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            GPS_pos.Lat = double.Parse(str[1]);// 41.015137; 41.026448854869074, 28.97850803471917
            GPS_pos.Lng = double.Parse(str[2]); //28.979530;

            nodesList.Add(new PointLatLng(GPS_pos.Lat, GPS_pos.Lng));

            string s = string.Concat("icons/number_", Convert.ToString(num), ".png");
            Bitmap ic = (Bitmap)Image.FromFile(s);
            //GMapMarker marker = new GMarkerGoogle(GPS_pos, GMarkerGoogleType.red_dot);

            GMapMarker marker = new GMarkerGoogle(GPS_pos, ic);
            map.Overlays.Add(markers);
            markers.Markers.Add(marker);

        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markers.Clear();
            markersDistOverlay.Clear();
            polygon.Clear();
            polyOverlay.Clear();
            polyOverlayShortestPath.Clear();

            connections.Clear(); 
            nodes.Clear();
            nodesList.Clear();
            nodesString.Clear();
            
            _points.Clear();
            points.Clear();

            cb_nodes1.Items.Clear();
            cb_nodes1.SelectedText = "";
            cb_nodes2.SelectedText = "";

            cb_nodes1.Text = "";
            cb_nodes2.Text = "";

            cb_nodes2.Items.Clear();

            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();

            rt_pathShow.Clear();

            enableConnections = false;
            enableFind = false;
            loadConnectionsToolStripMenuItem.Enabled = false;
            btn_findPath.Enabled = false;


        }

        private void loadMarkersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //dialog.InitialDirectory = @"C:\";
            dialog.Title = "Please select input text file having Node lat,long.";
            string str;
            int cntr = 1;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                graph = new Graph<int, string>();
                //txt_browse.Text = dialog.FileName;
                textFileReader = new StreamReader(dialog.FileName);
                //textFileWriter = new StreamWriter("D:/Data/Dijisktra Shortest Path Finder App/newInputFile.txt");
                while (!textFileReader.EndOfStream)
                {
                    str = textFileReader.ReadLine();
                    nodesString.Add(str);
                    addNodesList(str, cntr++);
                    addNode(str, nodes);
                }
                cb_nodes1.SelectedIndex = 0;
                cb_nodes2.SelectedIndex = 1;
                //textFileWriter.Flush();
                //textFileWriter.Close();

                textFileReader.Close();

                enableConnections = true;
                loadConnectionsToolStripMenuItem.Enabled = true;
                
                //makeConnections(connections);



            }
        }

        
        private void map_MouseMove(object sender, MouseEventArgs e)
        {
            lbl_LatLong.Text = "latitude = " + Convert.ToString(map.FromLocalToLatLng(e.X, e.Y).Lat) + "   longitude = " + Convert.ToString(map.FromLocalToLatLng(e.X, e.Y).Lng);

        }

        private void cb_maps_SelectedIndexChanged(object sender, EventArgs e)
        {

            loadMap(cb_maps.SelectedIndex, map.Position.Lat, map.Position.Lng,map.Zoom);
        }

        private void loadConnectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //dialog.InitialDirectory = @"C:\";
            dialog.Title = "Please select input text file having connections.";
            string str = "";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                //txt_browse.Text = dialog.FileName;
                textFileReader = new StreamReader(dialog.FileName);
                //textFileWriter = new StreamWriter("D:/Data/Dijisktra Shortest Path Finder App/newInputFile.txt");
                while (!textFileReader.EndOfStream)
                {
                    str = textFileReader.ReadLine();
                    string[] strArray = str.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    GPS1 = nodesList[int.Parse(strArray[0]) - 1];
                    GPS2 = nodesList[int.Parse(strArray[1]) - 1];

                    GeoCoordinate pin1 = new GeoCoordinate(GPS1.Lat, GPS1.Lng);
                    GeoCoordinate pin2 = new GeoCoordinate(GPS2.Lat, GPS2.Lng);

                    double distanceBetween = pin1.GetDistanceTo(pin2);
                    str = string.Concat(str, ",", ((int)distanceBetween).ToString());
                    connections.Add(str);


                    points.Add(GPS1);
                    points.Add(GPS2);

                    addPolyGon(polyOverlay, Color.Red, "mypolygon");
          


                    GeoCoordinate mid = MidPoint(pin1, pin2);
                    Bitmap distMarker = createTextString(Convert.ToUInt32(distanceBetween).ToString());

                    GPS_pos.Lat = mid.Latitude;
                    GPS_pos.Lng = mid.Longitude;

                    GMapMarker marker = new GMarkerGoogle(GPS_pos, distMarker);

                    map.Overlays.Add(markersDistOverlay);
                    markersDistOverlay.Markers.Add(marker);
                    points.Clear();

                    string[] row = new string[] { strArray[0], strArray[1], Convert.ToUInt32(distanceBetween).ToString() };
                    dataGridView1.Rows.Add(row);
                }

                makeConnections(connections);                                
                enableFind = true;
                btn_findPath.Enabled = true;

            }
        }
        double totalDistance = 0.0;
        private void btn_findPath_Click(object sender, EventArgs e)
        {
            uint n1 = uint.Parse(cb_nodes1.Text);
            uint n2 = uint.Parse(cb_nodes2.Text);
            shortestPathPoints.Clear();
            ShortestPathResult result = graph.Dijkstra(n1, n2); //result contains the shortest path
            var path = result.GetPath();
            string res = "";
            polyOverlayShortestPath.Clear();
            totalDistance = 0.0;
            rt_pathShow.ForeColor = Color.SeaGreen;
            if (result.IsFounded == false)
            {
                rt_pathShow.ForeColor = Color.Brown;
                res = "There is no path between Node " + n1.ToString() + " and Node " + n2.ToString();
                rt_pathShow.AppendText(res + "\n\n");
                
            }
            else
            {
                //foreach (var item in path.Select((value, i) => (value, i)))
                foreach (var (value, i) in path.Select((v, i) => (v, i)))
                {
                    if (i == 0)
                    {
                        rt_pathShow.AppendText(value.ToString());
                    }
                    int idx = Convert.ToInt32(value) - 1;
                    shortestPath = nodesList[idx];
                    if (i > 0)
                    {
                        points.Add(shortestPath);
                        GeoCoordinate pin1 = new GeoCoordinate(points[0].Lat, points[0].Lng);
                        GeoCoordinate pin2 = new GeoCoordinate(points[1].Lat, points[1].Lng);

                        double distanceBetween = pin1.GetDistanceTo(pin2);
                        totalDistance += distanceBetween;
                       // if (value != path.Last())
                        {
                            rt_pathShow.AppendText(" --(" + Convert.ToUInt32(distanceBetween).ToString() + ")--> ");
                        }
                        
                        
                        rt_pathShow.AppendText(value.ToString());
                    }
                    if (value == path.Last())
                    {
                        rt_pathShow.AppendText(". Total Distance = " + Convert.ToUInt32(totalDistance).ToString() + "\n\n");
                    }
                    addPolyGon(polyOverlayShortestPath, Color.Green, "shortestPolygon");



                    points.Clear();
                    points.Add(shortestPath);
                    shortestPathPoints.Add(shortestPath);




                }

                string strFilePath = @"D:/Data/UAV Planning/mission.csv";
                string strSeperator = ",";
                StringBuilder sbOutput = new StringBuilder();
                string str = "";
                str = "latitude" + strSeperator + "longitude" + strSeperator + "altitude(m)";
                sbOutput.AppendLine(str);
                int altitude = 5;
                int speed = 2;
                string z = ",0";
                string o = ",-1";
                foreach (var items in shortestPathPoints)
                {
                    //str = items.Lat.ToString() + strSeperator + items.Lng.ToString() + strSeperator + altitude.ToString() ;
                    str = items.Lat.ToString() + strSeperator + items.Lng.ToString() + strSeperator + altitude.ToString() + z + z + z + z + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + o + z + z + strSeperator + speed.ToString() + z + z + z + z +o +o;
                    sbOutput.AppendLine(str);

                }

                File.WriteAllText(strFilePath, sbOutput.ToString());

            }


        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lbl_time.Text = DateTime.Now.ToString();
        }

        void addNode(string st, List<uint> nL)
        {
            string[] str = st.Split(new Char[] { ' ', ',' });
            uint nodeValue1 = uint.Parse(str[0]);
            //            bool isInList = nL.IndexOf(nodeValue) != -1;
            if (nL.IndexOf(nodeValue1) == -1)
            {
                nL.Add(nodeValue1);
                cb_nodes1.Items.Add(nodeValue1);
                cb_nodes2.Items.Add(nodeValue1);
                graph.AddNode(Convert.ToInt32(nodeValue1));
            }
        }

        void makeConnections(List<string> connectList)
        {
            foreach (var item in connectList)
            {
                string[] str = item.Split(new Char[] { ' ', ',' });
                graph.Connect(uint.Parse(str[0]), uint.Parse(str[1]), int.Parse(str[2]), "some custom information in edge"); //First node has key equal 1            }


            }
        }

        void addPolyGon(GMapOverlay overlay, Color cr, string name)
        {
            polygon = new GMapPolygon(points, "name");
            polygon.Fill = new SolidBrush(Color.FromArgb(50, cr));
            polygon.Stroke = new Pen(cr, 3);
            overlay.Polygons.Add(polygon);
            map.Overlays.Add(overlay);
        }
    }
}
