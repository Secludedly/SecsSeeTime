Sec's See Time — Step 11 replacement files

Replace these files/folders in your existing project:
- SecSeeTime.csproj
- App.xaml
- App.xaml.cs
- MainWindow.xaml.cs
- Models/AlarmSettings.cs
- Services/StartupService.cs (new)
- Services/TrayService.cs (new)
- Services/NotificationService.cs (new)

Important: this version intentionally does NOT use <UseWindowsForms>true</UseWindowsForms>.
It references the Windows Forms framework explicitly so WinForms types used by the tray icon
do not become global usings and collide with WPF types such as Application, Button, Color,
ComboBox, FontFamily, MessageBox, OpenFileDialog, and Brush.

The Windows App SDK target is net10.0-windows10.0.19041.0.
