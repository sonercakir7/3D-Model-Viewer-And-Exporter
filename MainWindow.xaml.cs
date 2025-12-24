// 3D Model Viewer and Exporter - Soner Çakır
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using HelixToolkit.Wpf;
using Assimp;
using Assimp.Configs;

namespace ThreeDModelViewerAndExporter
{
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isDarkMode = false;
        private System.Windows.Threading.DispatcherTimer _rotationTimer;
        private Model3DGroup _currentModelGroup = new Model3DGroup();
        private ModelVisual3D _currentVisual = new ModelVisual3D();
        private Dictionary<GeometryModel3D, System.Windows.Media.Media3D.Material> _originalMaterials = new Dictionary<GeometryModel3D, System.Windows.Media.Media3D.Material>();
        private List<string> _recentFiles = new List<string>();
        private Rect3D _currentModelBounds;
        private List<MeshNode> _allMeshes = new List<MeshNode>();
        private List<TextureNode> _allTextures = new List<TextureNode>();
        private Scene? _lastLoadedScene;

        public class MeshNode { public string Name { get; set; } = ""; public GeometryModel3D? Model { get; set; } public Assimp.Mesh? AssimpMesh { get; set; } }
        public class TextureNode { public string Name { get; set; } = ""; public string Format { get; set; } = ""; public byte[]? Data { get; set; } public string? FilePath { get; set; } public ImageSource? Thumbnail { get; set; } }

        private bool _showFPS = false;
        public bool ShowFPS { get => _showFPS; set { _showFPS = value; OnPropertyChanged(nameof(ShowFPS)); } }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.AllowDrop = true;
            this.Drop += MainWindow_Drop;
            this.DragOver += MainWindow_DragOver;
            
            _rotationTimer = new System.Windows.Threading.DispatcherTimer();
            _rotationTimer.Interval = TimeSpan.FromMilliseconds(20);
            _rotationTimer.Tick += (s, e) => {
                if (viewPort != null && viewPort.Camera is ProjectionCamera cam) {
                    var pos = cam.Position; double angle = 0.02;
                    double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
                    double nx = pos.X * cosA - pos.Z * sinA;
                    double nz = pos.X * sinA + pos.Z * cosA;
                    cam.Position = new Point3D(nx, pos.Y, nz);
                    cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(-nx, -pos.Y, -nz);
                }
            };
            this.Loaded += (s, e) => { 
                LoadSettings(); 
                LoadRecentFiles(); 
                if (UnitComboBox != null) UnitComboBox.SelectedIndex = 0; 
            };
        }

        private void LoadSettings()
        {
            string lang = "en-US";
            bool fps = false;
            double sense = 1.0;
            bool loadedFromConfig = false;

            try {
                if (File.Exists("config.txt")) {
                    var lines = File.ReadAllLines("config.txt");
                    foreach (var line in lines) {
                        var p = line.Split('=');
                        if (p.Length == 2) {
                            if (p[0] == "Lang") lang = p[1];
                            else if (p[0] == "FPS") bool.TryParse(p[1], out fps);
                            else if (p[0] == "Sense") double.TryParse(p[1], out sense);
                        }
                    }
                    loadedFromConfig = true;
                }
            } catch { }

            // If not loaded from config, try to detect system language for the initial UI selection
            if (!loadedFromConfig) {
                var sysLang = System.Globalization.CultureInfo.CurrentCulture.Name;
                if (sysLang.StartsWith("tr")) lang = "tr-TR";
                else if (sysLang.StartsWith("de")) lang = "de-DE";
                else if (sysLang.StartsWith("fr")) lang = "fr-FR";
                else lang = "en-US";
            }

            // Apply Settings
            if (ShowFPSCheckBox != null) ShowFPSCheckBox.IsChecked = fps;
            if (CameraSenseSlider != null) CameraSenseSlider.Value = sense;
            
            if (LanguageComboBox != null) {
                foreach (ComboBoxItem item in LanguageComboBox.Items) {
                    if (item.Tag?.ToString() == lang) {
                        LanguageComboBox.SelectedItem = item;
                        break;
                    }
                }
                // If loop finishes and no selection (e.g. unknown system lang), default to English
                if (LanguageComboBox.SelectedItem == null) LanguageComboBox.SelectedIndex = 1; 
            }
            
            // Force language update
            ((App)Application.Current).ChangeLanguage(lang);
        }

        private void LoadRecentFiles() { try { if (File.Exists("recent.txt")) _recentFiles = File.ReadAllLines("recent.txt").Take(5).ToList(); UpdateRecentFilesMenu(); } catch { } }
        private void SaveRecentFile(string path) { _recentFiles.Remove(path); _recentFiles.Insert(0, path); _recentFiles = _recentFiles.Take(5).ToList(); File.WriteAllLines("recent.txt", _recentFiles); UpdateRecentFilesMenu(); }
        private void UpdateRecentFilesMenu() { if (RecentFilesMenu == null) return; RecentFilesMenu.Items.Clear(); foreach (var path in _recentFiles) { var item = new MenuItem { Header = Path.GetFileName(path) }; item.Click += (s, e) => Load3DModel(path); RecentFilesMenu.Items.Add(item); } }

        public void LoadModel_Click(object sender, RoutedEventArgs e) { 
            string f = "All Supported|*.obj;*.stl;*.fbx;*.gltf;*.glb;*.3ds;*.dae;*.ply|FBX|*.fbx|glTF|*.gltf;*.glb|OBJ|*.obj|All Files|*.*";
            OpenFileDialog ofd = new OpenFileDialog { Filter = f }; if (ofd.ShowDialog() == true) Load3DModel(ofd.FileName); 
        }

        private void Load3DModel(string filePath)
        {
            try {
                if (ModelNameText != null) ModelNameText.Text = (string)FindResource("Lbl_Model") + ": " + Path.GetFileName(filePath);
                using (var importer = new AssimpContext()) {
                    Scene scene = importer.ImportFile(filePath, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.JoinIdenticalVertices);
                    if (scene == null) return;
                    _currentModelGroup = new Model3DGroup(); _originalMaterials.Clear();
                    string? dir = Path.GetDirectoryName(filePath);

                    foreach (var m in scene.Meshes) {
                        MeshGeometry3D geo = new MeshGeometry3D();
                        foreach (var v in m.Vertices) geo.Positions.Add(new Point3D(v.X, v.Y, v.Z));
                        foreach (var f in m.Faces) if (f.IndexCount >= 3) for (int i = 0; i < f.IndexCount; i++) geo.TriangleIndices.Add(f.Indices[i]);
                        if (m.HasTextureCoords(0)) foreach (var uv in m.TextureCoordinateChannels[0]) geo.TextureCoordinates.Add(new System.Windows.Point(uv.X, 1 - uv.Y));
                        System.Windows.Media.Media3D.Material mat = new DiffuseMaterial(System.Windows.Media.Brushes.Gray);
                        if (m.MaterialIndex >= 0) {
                            var am = scene.Materials[m.MaterialIndex];
                            if (am.HasTextureDiffuse) {
                                string tp = am.TextureDiffuse.FilePath;
                                if (tp.StartsWith("*") && int.TryParse(tp.Substring(1), out int texIdx) && texIdx < scene.TextureCount) {
                                    var et = scene.Textures[texIdx];
                                    if (et.HasCompressedData) {
                                        try { var bmp = new System.Windows.Media.Imaging.BitmapImage(); using (var ms = new MemoryStream(et.CompressedData)) { bmp.BeginInit(); bmp.StreamSource = ms; bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bmp.EndInit(); } mat = new DiffuseMaterial(new ImageBrush(bmp)); goto MatSet; } catch { } 
                                    }
                                }
                                string? rp = FindTexturePath(dir, tp, filePath);
                                if (rp != null) { try { var bmp = new System.Windows.Media.Imaging.BitmapImage(); bmp.BeginInit(); bmp.UriSource = new Uri(rp, UriKind.Absolute); bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bmp.EndInit(); mat = new DiffuseMaterial(new ImageBrush(bmp)); } catch { } }
                            }
                        }
                    MatSet:
                        GeometryModel3D gm = new GeometryModel3D(geo, mat); gm.BackMaterial = mat;
                        _currentModelGroup.Children.Add(gm); _originalMaterials[gm] = mat;
                    }

                    _allMeshes.Clear();
                    for (int i = 0; i < scene.MeshCount; i++) _allMeshes.Add(new MeshNode { Name = scene.Meshes[i].Name ?? "Mesh_" + i, Model = _currentModelGroup.Children[i] as GeometryModel3D, AssimpMesh = scene.Meshes[i] });
                    MeshListBox.ItemsSource = null; MeshListBox.ItemsSource = _allMeshes;

                    _allTextures.Clear();
                    for (int i = 0; i < scene.TextureCount; i++) {
                        var tex = scene.Textures[i]; var node = new TextureNode { Name = "Embedded_" + i, Format = "PNG" };
                        if (tex.HasCompressedData) node.Thumbnail = CreateThumbnail(tex.CompressedData);
                        _allTextures.Add(node);
                    }
                    TextureListBox.ItemsSource = null; TextureListBox.ItemsSource = _allTextures;

                    _currentVisual = new ModelVisual3D { Content = _currentModelGroup };
                    var b = _currentVisual.Content.Bounds;
                    if (!b.IsEmpty) { var c = new Point3D(b.X + b.SizeX / 2, b.Y + b.SizeY / 2, b.Z + b.SizeZ / 2); _currentVisual.Transform = new TranslateTransform3D(-c.X, -c.Y, -c.Z); }

                    _currentModelBounds = _currentVisual.Content.Bounds;
                    UpdateDimensionsText();
                    if (PolyCountText != null) PolyCountText.Text = "Polys: " + scene.Meshes.Sum(m => m.FaceCount);
                    if (VertCountText != null) VertCountText.Text = "Verts: " + scene.Meshes.Sum(m => m.VertexCount);

                    viewPort.Children.Clear(); viewPort.Children.Add(new DefaultLights()); viewPort.Children.Add(_currentVisual);
                    viewPort.Children.Add(gridLines); viewPort.Children.Add(boundingBoxVisual); viewPort.Children.Add(selectionBoxVisual);
                    viewPort.ZoomExtents(_currentVisual.Content.Bounds, 500); 
                    SaveRecentFile(filePath); _lastLoadedScene = scene;
                    BB_Changed(this, new RoutedEventArgs());
                }
            } catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); } 
        }

        public void Unit_Changed(object sender, SelectionChangedEventArgs e) => UpdateDimensionsText();
        private void UpdateDimensionsText() {
            if (DimXText == null || _currentModelBounds.IsEmpty) return;
            double f = 1.0; string u = "unit";
            if (UnitComboBox != null && UnitComboBox.SelectedItem is ComboBoxItem item) { u = item.Content.ToString()!; f = u switch { "mm" => 1000.0, "cm" => 100.0, "m" => 1.0, _ => 1.0 }; }
            DimXText.Text = "X: " + (_currentModelBounds.SizeX * f).ToString("N2") + " " + u;
            DimYText.Text = "Y: " + (_currentModelBounds.SizeY * f).ToString("N2") + " " + u;
            DimZText.Text = "Z: " + (_currentModelBounds.SizeZ * f).ToString("N2") + " " + u;
            if (VolumeText != null) { double v = _currentModelBounds.SizeX * _currentModelBounds.SizeY * _currentModelBounds.SizeZ * Math.Pow(f, 3); VolumeText.Text = "Vol: " + v.ToString("N2"); }
        }

        public void MeshListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (MeshListBox.SelectedItem is MeshNode node && node.Model != null) {
                submeshPreviewVisual.Content = node.Model; submeshPreviewPort.ZoomExtents();
                selectionBoxVisual.Points.Clear(); var b = node.Model.Bounds;
                if (!b.IsEmpty) {
                    var p1 = new Point3D(b.X, b.Y, b.Z); var p2 = new Point3D(b.X + b.SizeX, b.Y, b.Z); var p3 = new Point3D(b.X + b.SizeX, b.Y + b.SizeY, b.Z); var p4 = new Point3D(b.X, b.Y + b.SizeY, b.Z);
                    var p5 = new Point3D(b.X, b.Y, b.Z + b.SizeZ); var p6 = new Point3D(b.X + b.SizeX, b.Y, b.Z + b.SizeZ); var p7 = new Point3D(b.X + b.SizeX, b.Y + b.SizeY, b.Z + b.SizeZ); var p8 = new Point3D(b.X, b.Y + b.SizeY, b.Z + b.SizeZ);
                    Point3D[] pts = { p1,p2, p2,p3, p3,p4, p4,p1, p5,p6, p6,p7, p7,p8, p8,p5, p1,p5, p2,p6, p3,p7, p4,p8 };
                    foreach (var p in pts) selectionBoxVisual.Points.Add(p);
                }
            }
        }

        public void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { if (_currentModelGroup == null) return; foreach (var child in _currentModelGroup.Children) { if (child is GeometryModel3D gm && gm.Material is DiffuseMaterial dm) { try { var nb = dm.Brush.Clone(); nb.Opacity = e.NewValue; gm.Material = new DiffuseMaterial(nb); gm.BackMaterial = gm.Material; } catch { } } } }
        
        public void CamView_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn && viewPort != null) {
                var look = new System.Windows.Media.Media3D.Vector3D(0, 0, -1); var up = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                switch (btn.Tag?.ToString()) { case "Front": look = new System.Windows.Media.Media3D.Vector3D(0, 0, -1); break; case "Back": look = new System.Windows.Media.Media3D.Vector3D(0, 0, 1); break; case "Top": look = new System.Windows.Media.Media3D.Vector3D(0, -1, 0); up = new System.Windows.Media.Media3D.Vector3D(0, 0, 1); break; case "Bottom": look = new System.Windows.Media.Media3D.Vector3D(0, 1, 0); up = new System.Windows.Media.Media3D.Vector3D(0, 0, -1); break; case "Left": look = new System.Windows.Media.Media3D.Vector3D(1, 0, 0); break; case "Right": look = new System.Windows.Media.Media3D.Vector3D(-1, 0, 0); break; }
                viewPort.Camera.LookDirection = look; viewPort.Camera.UpDirection = up; viewPort.ZoomExtents();
            }
        }

        public void ToggleCamera_Click(object sender, RoutedEventArgs e) { 
            if (viewPort.Camera is PerspectiveCamera p) { 
                viewPort.Camera = new OrthographicCamera { Position = p.Position, LookDirection = p.LookDirection, UpDirection = p.UpDirection, Width = 10 }; 
                CameraModeButton.Content = FindResource("Btn_Ortho"); 
            } 
            else if (viewPort.Camera is OrthographicCamera o) { 
                viewPort.Camera = new PerspectiveCamera { Position = o.Position, LookDirection = o.LookDirection, UpDirection = o.UpDirection, FieldOfView = 45 }; 
                CameraModeButton.Content = FindResource("Btn_Perspective"); 
            } 
        }

        public void BGColor_Click(object sender, RoutedEventArgs e) { var cur = (viewPort.Background as SolidColorBrush)?.Color; viewPort.Background = new SolidColorBrush(cur == Colors.Black ? Colors.White : cur == Colors.White ? Colors.DarkGray : Colors.Black); }
        public void Turntable_Click(object sender, RoutedEventArgs e) { if (TurntableToggle.IsChecked == true) _rotationTimer.Start(); else _rotationTimer.Stop(); }
        public void CenterModel_Click(object sender, RoutedEventArgs e) => viewPort?.ZoomExtents();
        public void BB_Changed(object sender, RoutedEventArgs e) { 
            if (boundingBoxVisual == null) return; boundingBoxVisual.Points.Clear();
            if (ShowBBCheckBox?.IsChecked == true && !_currentModelBounds.IsEmpty) {
                var b = _currentModelBounds;
                var p1 = new Point3D(b.X, b.Y, b.Z); var p2 = new Point3D(b.X + b.SizeX, b.Y, b.Z); var p3 = new Point3D(b.X + b.SizeX, b.Y + b.SizeY, b.Z); var p4 = new Point3D(b.X, b.Y + b.SizeY, b.Z);
                var p5 = new Point3D(b.X, b.Y, b.Z + b.SizeZ); var p6 = new Point3D(b.X + b.SizeX, b.Y, b.Z + b.SizeZ); var p7 = new Point3D(b.X + b.SizeX, b.Y + b.SizeY, b.Z + b.SizeZ); var p8 = new Point3D(b.X, b.Y + b.SizeY, b.Z + b.SizeZ);
                Point3D[] pts = { p1,p2, p2,p3, p3,p4, p4,p1, p5,p6, p6,p7, p7,p8, p8,p5, p1,p5, p2,p6, p3,p7, p4,p8 };
                foreach (var p in pts) boundingBoxVisual.Points.Add(p);
            }
        }
        public void Screenshot_Click(object sender, RoutedEventArgs e) { SaveFileDialog sfd = new SaveFileDialog { Filter = "PNG|*.png" }; if (sfd.ShowDialog() == true) viewPort.Export(sfd.FileName); } 
        
        public void ExportModel_Click(object sender, RoutedEventArgs e) {
            if (_lastLoadedScene == null) return;
            SaveFileDialog sfd = new SaveFileDialog { Filter = "FBX|*.fbx|GLB|*.glb|glTF|*.gltf|OBJ|*.obj|STL|*.stl", FileName = "Exported" };
            if (sfd.ShowDialog() == true) {
                try {
                    using (var context = new AssimpContext()) {
                        string ext = Path.GetExtension(sfd.FileName).ToLower();
                        string formatId = ext switch { ".fbx" => "fbx", ".glb" => "glb2", ".gltf" => "gltf2", ".obj" => "obj", ".stl" => "stlb", _ => "fbx" };
                        context.ExportFile(_lastLoadedScene, sfd.FileName, formatId);
                        MessageBox.Show((string)FindResource("Msg_ExportSuccess"));
                    }
                } catch (Exception ex) { MessageBox.Show((string)FindResource("Msg_ExportError") + ex.Message); }
            }
        }

        public void QuickSaveMesh_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn && btn.Tag is MeshNode node && node.AssimpMesh != null) {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "OBJ|*.obj|STL|*.stl", FileName = node.Name };
                if (sfd.ShowDialog() == true) {
                    try {
                        using (var context = new AssimpContext()) {
                            Scene s = new Scene(); s.Materials.Add(new Assimp.Material());
                            var m = new Assimp.Mesh(node.Name, node.AssimpMesh.PrimitiveType);
                            m.Vertices.AddRange(node.AssimpMesh.Vertices); m.Normals.AddRange(node.AssimpMesh.Normals); m.Faces.AddRange(node.AssimpMesh.Faces);
                            if (node.AssimpMesh.HasTextureCoords(0)) m.TextureCoordinateChannels[0].AddRange(node.AssimpMesh.TextureCoordinateChannels[0]);
                            s.Meshes.Add(m); s.RootNode = new Node("Root"); s.RootNode.MeshIndices.Add(0);
                            context.ExportFile(s, sfd.FileName, Path.GetExtension(sfd.FileName).ToLower() == ".obj" ? "obj" : "stlb");
                            MessageBox.Show((string)FindResource("Msg_PartSaved"));
                        }
                    } catch (Exception ex) { MessageBox.Show((string)FindResource("Msg_ExportError") + ex.Message); }
                }
            }
        }

        public void About_Click(object sender, RoutedEventArgs e) 
        { 
            var aboutWin = new AboutWindow();
            aboutWin.Owner = this;
            aboutWin.ShowDialog();
        }
        public void SaveSettings_Click(object sender, RoutedEventArgs e) {
            try {
                var item = LanguageComboBox.SelectedItem as ComboBoxItem;
                string t = item?.Tag?.ToString() ?? "en-US";
                string[] s = { "Lang=" + t, "FPS=" + (ShowFPSCheckBox.IsChecked == true), "Sense=" + CameraSenseSlider.Value };
                File.WriteAllLines("config.txt", s); MessageBox.Show(GetLoc("Msg_SettingsSaved"));
            } catch (Exception ex) { MessageBox.Show(GetLoc("Msg_ExportError") + ex.Message); }
        }

        private string GetLoc(string key) { try { return Application.Current.FindResource(key) as string ?? key; } catch { return key; } }

        public void Language_Changed(object sender, SelectionChangedEventArgs e) { if (this.IsLoaded && sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item) ((App)Application.Current).ChangeLanguage(item.Tag?.ToString() ?? "en-US"); }
        public void GenerateReport_Click(object sender, RoutedEventArgs e) => MessageBox.Show((string)FindResource("Msg_ReportReady"));
        public void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        public void ResetTransform_Click(object sender, RoutedEventArgs e) {
            if (viewPort != null) {
                viewPort.Camera.Position = new Point3D(10, 10, 10);
                viewPort.Camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-10, -10, -10);
                viewPort.ZoomExtents();
            }
        }
        public void ThemeToggle_Click(object sender, RoutedEventArgs e) { _isDarkMode = !_isDarkMode; ((App)Application.Current).ChangeTheme(_isDarkMode); }
        public void ShowGrid_Changed(object sender, RoutedEventArgs e) { if (gridLines != null) gridLines.Thickness = ShowGridCheckBox.IsChecked == true ? 0.01 : 0; }
        public void ShowAxis_Changed(object sender, RoutedEventArgs e) { if (viewPort != null) viewPort.ShowCoordinateSystem = ShowAxisCheckBox.IsChecked ?? true; }
        public void GridSettings_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { if (gridLines != null && GridDistanceSlider != null && GridOpacitySlider != null) { gridLines.MinorDistance = GridDistanceSlider.Value; gridLines.MajorDistance = GridDistanceSlider.Value * 10; var nb = ((SolidColorBrush)gridLines.Fill).Clone(); nb.Opacity = GridOpacitySlider.Value; gridLines.Fill = nb; } }
        public void LightIntensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { } 
        public void LightDirection_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { } 
        public void Bloom_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { } 
        public void Effect_Changed(object sender, RoutedEventArgs e) { } 
        public void AutoRotate_Changed(object sender, RoutedEventArgs e) { } 
        public void ChangeMeshColor_Click(object sender, RoutedEventArgs e) { if (MeshListBox.SelectedItem is MeshNode node && node.Model != null) { node.Model.Material = new DiffuseMaterial(Brushes.Red); node.Model.BackMaterial = node.Model.Material; } }
        public void ExportAllTextures_Click(object sender, RoutedEventArgs e) 
        { 
            if (!_allTextures.Any()) { MessageBox.Show((string)FindResource("Msg_NoTexture")); return; }
            var dialog = new SaveFileDialog { Title = "Export Folder", FileName = "Exported_Texture.png" };
            if (dialog.ShowDialog() == true) {
                string? dir = Path.GetDirectoryName(dialog.FileName);
                if (dir == null) return;
                int count = 0;
                foreach (var tex in _allTextures) {
                    try {
                        string ext = tex.Format.Contains("PNG") ? ".png" : ".jpg";
                        string path = Path.Combine(dir, tex.Name + ext);
                        if (tex.Data != null) File.WriteAllBytes(path, tex.Data);
                        else if (tex.FilePath != null) File.Copy(tex.FilePath, path, true);
                        count++;
                    } catch { }
                }
                MessageBox.Show($"{count} " + (string)FindResource("Msg_ExportSuccess"));
            }
        } 

        public void QuickSaveTexture_Click(object sender, RoutedEventArgs e) 
        { 
            if (sender is Button btn && btn.Tag is TextureNode node) {
                string ext = node.Format.Contains("PNG") ? ".png" : ".jpg";
                SaveFileDialog sfd = new SaveFileDialog { Filter = "Image File|*" + ext, FileName = node.Name };
                if (sfd.ShowDialog() == true) {
                    try {
                        if (node.Data != null) File.WriteAllBytes(sfd.FileName, node.Data);
                        else if (node.FilePath != null) File.Copy(node.FilePath, sfd.FileName, true);
                        MessageBox.Show((string)FindResource("Msg_TextureSaved"));
                    } catch (Exception ex) { MessageBox.Show((string)FindResource("Msg_SaveError") + ex.Message); }
                }
            }
        } 
        public void TextureListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { } 
        public void RenderMode_Changed(object sender, RoutedEventArgs e) { } 
        public void MainWindow_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        public void MainWindow_Drop(object sender, DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) { var files = (string[])e.Data.GetData(DataFormats.FileDrop); Load3DModel(files[0]); } }
        private string? FindTexturePath(string? d, string r, string m) { 
            if (string.IsNullOrEmpty(d)) return null; string fn = Path.GetFileName(r.Replace((char)92, '/')); 
            string[] ps = { Path.Combine(d, fn), Path.Combine(d, r), Path.Combine(d, "textures", fn), Path.Combine(d, Path.GetFileNameWithoutExtension(m) + ".fbm", fn) };
            foreach (var p in ps) try { if (File.Exists(p)) return p; } catch { } return null;
        }
        private ImageSource? CreateThumbnail(byte[] data) { try { var b = new System.Windows.Media.Imaging.BitmapImage(); using (var ms = new MemoryStream(data)) { b.BeginInit(); ms.Position = 0; b.StreamSource = ms; b.DecodePixelWidth = 150; b.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; b.EndInit(); } return b; } catch { return null; } }
    }
}