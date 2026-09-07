using SecSeeTime.Models;
using SecSeeTime.Services;
using SecSeeTime.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SecSeeTime.Views
{
    public partial class WorldClockWindow : Window
    {
        private readonly AlarmSettings _settings;
        private readonly StorageService _storage;
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private List<ZoneChoice> _all = new();
        private List<WorldClockTile> _tiles = new();
        private bool _loading;

        public WorldClockWindow(AlarmSettings settings, StorageService storage)
        {
            InitializeComponent(); _settings=settings; _storage=storage;
            NormalizeSavedZones(); BuildZoneCatalog(); RefreshZoneList(); RefreshClockList();
            _timer.Tick += (_,__) => UpdateTimes(); _timer.Start(); UpdateTimes();
            ThemeService.Current.ThemeChanged += ThemeChanged;
        }

        private void NormalizeSavedZones()
        {
            _settings.WorldClocks ??= new List<WorldClockEntry>();
            foreach (var e in _settings.WorldClocks)
            {
                if (string.IsNullOrWhiteSpace(e.DisplayName)) e.DisplayName = FriendlyName(e.TimeZoneId);
            }
        }

        private void BuildZoneCatalog()
        {
            _all = TimeZoneInfo.GetSystemTimeZones().Select(z => new ZoneChoice(z.Id, FriendlyName(z.Id), z.DisplayName)).OrderBy(z => z.Name).ToList();
        }

        private void RefreshZoneList()
        {
            string q=SearchBox.Text.Trim();
            var used=new HashSet<string>(_settings.WorldClocks.Select(x=>x.TimeZoneId),StringComparer.OrdinalIgnoreCase);
            ZoneList.ItemsSource=_all.Where(z=>string.IsNullOrWhiteSpace(q)||z.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||z.Id.Contains(q,StringComparison.OrdinalIgnoreCase)||z.Display.Contains(q,StringComparison.OrdinalIgnoreCase)).Take(200).ToList();
            if(ZoneList.Items.Count>0) ZoneList.SelectedIndex=0;
        }

        private void RefreshClockList()
        {
            _tiles = _settings.WorldClocks.Select(e => new WorldClockTile(e)).ToList();
            ClockList.ItemsSource = _tiles;

            CountText.Text =
                $"{_settings.WorldClocks.Count} " +
                (_settings.WorldClocks.Count == 1 ? "CITY" : "CITIES");

            UpdateTimes();
        }

        private void UpdateTimes()
        {
            DateTime utc = DateTime.UtcNow;
            bool use24 = TimeFormatService.Use24HourNow;

            foreach (WorldClockTile tile in _tiles)
                tile.Update(utc, use24);
        }

        private void Add_Click(object s,RoutedEventArgs e){if(ZoneList.SelectedItem is not ZoneChoice z)return;if(_settings.WorldClocks.Any(x=>string.Equals(x.TimeZoneId,z.Id,StringComparison.OrdinalIgnoreCase))){MessageBox.Show("That city is already in your World Clock.","World Clock",MessageBoxButton.OK,MessageBoxImage.Information);return;} _settings.WorldClocks.Add(new WorldClockEntry{TimeZoneId=z.Id,DisplayName=z.Name,IsFavorite=false});SaveAndRefresh();}
        private void Remove_Click(object s,RoutedEventArgs e){if((s as Button)?.Tag is WorldClockTile t){_settings.WorldClocks.Remove(t.Entry);SaveAndRefresh();}}
        private void Favorite_Click(object s,RoutedEventArgs e){if((s as Button)?.Tag is WorldClockTile t){t.Entry.IsFavorite=!t.Entry.IsFavorite;_settings.WorldClocks=_settings.WorldClocks.OrderByDescending(x=>x.IsFavorite).ToList();SaveAndRefresh();}}
        private void Up_Click(object s,RoutedEventArgs e)=>Move(s,-1);
        private void Down_Click(object s,RoutedEventArgs e)=>Move(s,1);
        private void Move(object s,int delta){if((s as Button)?.Tag is not WorldClockTile t)return;int i=_settings.WorldClocks.FindIndex(x=>x.Id==t.Entry.Id),j=i+delta;if(i<0||j<0||j>=_settings.WorldClocks.Count)return;(_settings.WorldClocks[i],_settings.WorldClocks[j])=(_settings.WorldClocks[j],_settings.WorldClocks[i]);SaveAndRefresh();}
        private void SaveAndRefresh(){_storage.SaveSettings(_settings);RefreshZoneList();RefreshClockList();}
        private void SearchBox_TextChanged(object s,TextChangedEventArgs e){if(!_loading)RefreshZoneList();}
        private void ThemeChanged(object? s,AlarmTheme e){UpdateTimes();}
        private void Header_MouseLeftButtonDown(object s,MouseButtonEventArgs e){if(e.LeftButton==MouseButtonState.Pressed)try{DragMove();}catch{}}
        private void CloseButton_Click(object s,RoutedEventArgs e){_timer.Stop();ThemeService.Current.ThemeChanged-=ThemeChanged;Close();}

        private static string FriendlyName(string id)
        {
            if(string.IsNullOrWhiteSpace(id))return "Unknown";
            string x=id.Replace('_',' ').Replace("Standard Time","").Replace("Daylight Time","").Replace("Romance Standard Time","Madrid / Paris").Trim();
            var map=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){["Eastern Standard Time"]="New York / Eastern",["Central Standard Time"]="Chicago / Central",["Mountain Standard Time"]="Denver / Mountain",["Pacific Standard Time"]="Los Angeles / Pacific",["Alaskan Standard Time"]="Alaska",["Hawaiian Standard Time"]="Honolulu / Hawaii",["GMT Standard Time"]="London",["W. Europe Standard Time"]="Berlin / Central Europe",["Tokyo Standard Time"]="Tokyo",["China Standard Time"]="Shanghai / Beijing",["India Standard Time"]="Mumbai / India",["AUS Eastern Standard Time"]="Sydney / Melbourne",["UTC"]="UTC"};
            return map.TryGetValue(id,out var n)?n:x;
        }
        private sealed record ZoneChoice(string Id,string Name,string Display){public override string ToString()=>Name+"  •  "+Id;}
    }
}
