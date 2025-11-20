using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using Pulsar.Server.Statistics;

#nullable enable

namespace Pulsar.Server.Controls.Wpf
{
    public sealed class HeatMapViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<StatCardViewModel> _statCards = new()
        {
            new StatCardViewModel("Total Clients"),
            new StatCardViewModel("Geolocated"),
            new StatCardViewModel("Unknown Location"),
            new StatCardViewModel("Unique Countries")
        };

        private readonly ObservableCollection<CountryHeatItem> _topCountries = new();

        private HeatLandSeries[] _series = Array.Empty<HeatLandSeries>();
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        private string _lastUpdated = string.Empty;
        private bool _isDarkMode;
        private ClientGeoSnapshot? _lastSnapshot;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReadOnlyObservableCollection<StatCardViewModel> StatCards { get; }

        public HeatLandSeries[] Series
        {
            get => _series;
            private set => SetField(ref _series, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetField(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsContentVisible));
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                if (SetField(ref _hasError, value))
                {
                    OnPropertyChanged(nameof(IsContentVisible));
                }
            }
        }

        public bool IsContentVisible => !IsLoading && !HasError;

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetField(ref _errorMessage, value);
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            private set => SetField(ref _lastUpdated, value);
        }

        public ReadOnlyObservableCollection<CountryHeatItem> TopCountries { get; }

        public HeatMapViewModel()
        {
            StatCards = new ReadOnlyObservableCollection<StatCardViewModel>(_statCards);
            TopCountries = new ReadOnlyObservableCollection<CountryHeatItem>(_topCountries);
        }

        public void SetLoading()
        {
            ErrorMessage = null;
            HasError = false;
            IsLoading = true;
        }

        public void SetError(string message)
        {
            _lastSnapshot = null;
            ErrorMessage = message;
            HasError = true;
            IsLoading = false;
            LastUpdated = string.Empty;
            ClearData();
        }

        public void UpdateSnapshot(ClientGeoSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            ErrorMessage = snapshot.ErrorMessage;
            HasError = snapshot.HasError;
            IsLoading = false;

            if (snapshot.HasError)
            {
                LastUpdated = string.Empty;
                ClearData();
                return;
            }

            LastUpdated = $"Updated {snapshot.GeneratedAtUtc.ToLocalTime():g}";
            UpdateCards(snapshot);
            UpdateTopCountries(snapshot);
            BuildSeries();
        }

        public void UpdateTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            BuildSeries();
        }

        private void UpdateCards(ClientGeoSnapshot snapshot)
        {
            _statCards[0].Update(snapshot.TotalClients.ToString("N0"), "Records processed");
            _statCards[1].Update(snapshot.MappedClients.ToString("N0"), "Known country");
            _statCards[2].Update(snapshot.UnknownClients.ToString("N0"), "No location data");
            _statCards[3].Update(snapshot.UniqueCountryCount.ToString("N0"), "Countries represented");
        }

        private void UpdateTopCountries(ClientGeoSnapshot snapshot)
        {
            _topCountries.Clear();
            var rank = 1;
            foreach (var country in snapshot.Countries.Take(15))
            {
                _topCountries.Add(new CountryHeatItem(rank++, country.Name, country.CountryCode3.ToUpperInvariant(), country.Count, country.Share));
            }
        }
        public void RefreshSeries()
        {
            Series = Series.ToArray(); // simple nudge to force binding update
        }

        private void BuildSeries()
        {
            if (_lastSnapshot == null || _lastSnapshot.HasError)
            {
                Series = Array.Empty<HeatLandSeries>();
                return;
            }

            var lands = _lastSnapshot.Countries
                .Where(c => !string.IsNullOrWhiteSpace(c.CountryCode3))
                .Select(c => new HeatLand
                {
                    Name = c.CountryCode3.ToLowerInvariant(),
                    Value = c.Count
                })
                .ToArray();

            if (lands.Length == 0)
            {
                Series = Array.Empty<HeatLandSeries>();
                return;
            }

            Series = new[]
            {
                new HeatLandSeries
                {
                    Lands = lands
                }
            };
        }

        private void ClearData()
        {
            Series = Array.Empty<HeatLandSeries>();
            _topCountries.Clear();
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class CountryHeatItem
    {
        public CountryHeatItem(int rank, string country, string code, int count, double share)
        {
            Rank = rank;
            Country = string.IsNullOrWhiteSpace(country) ? "Unknown" : country;
            Code = string.IsNullOrWhiteSpace(code) ? "" : code;
            Count = count;
            Share = share;
        }

        public int Rank { get; }

        public string Country { get; }

        public string Code { get; }

        public int Count { get; }

        public double Share { get; }

        public string SharePercent => Share > 0 ? Share.ToString("P1") : "0%";
    }
}
