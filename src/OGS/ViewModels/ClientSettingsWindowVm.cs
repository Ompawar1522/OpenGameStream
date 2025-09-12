using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OGS.Core.Config;
using Avalonia.Controls;
using OGS.ViewModels.Dialogs;
using OGS.Views.Dialogs;
using OGS.Core.Config.Data;
using OGS.Core.Config.Data.Rtc;
using OGS.Core.Config.Data.Clients;

namespace OGS.ViewModels;

public sealed partial class ClientSettingsWindowVm : ObservableObject
{
    public ClientTypeSelectableOption SelectedClientType
    {
        get => field;
        set
        {
            SetProperty(ref field, value, nameof(SelectedClientType));

            if(value.ClientType == ClientType.MqttRtc && SelectedMqttServer is null)
            {
                SelectedMqttServer = MqttServerConfig.EmqxPublic;
            }
        }
    }

    [ObservableProperty] private MqttServerConfig? _selectedMqttServer;
    [ObservableProperty] private StunServerConfig _selectedStunServer = StunServerConfig.None;

    [NotifyPropertyChangedFor(nameof(ForceTurnServerEnabled))]
    [ObservableProperty]
    private TurnServerConfig _selectedTurnServer = TurnServerConfig.None;

    [ObservableProperty] private bool _forceTurnServer;

    public bool ForceTurnServerEnabled
    {
        get => SelectedTurnServer != TurnServerConfig.None;
    }

    public List<ClientTypeSelectableOption> SelectableClientTypes { get; } =
    [
        ClientTypeSelectableOption.FromClientType(ClientType.MqttRtc),
        ClientTypeSelectableOption.FromClientType(ClientType.ManualRtc)
    ];
    
    public ObservableCollection<MqttServerConfig> MqttServers { get; }
    public ObservableCollection<StunServerConfig> StunServers { get; }
    public ObservableCollection<TurnServerConfig> TurnServers { get; }
    
    private readonly IConfigService _configService;
    private Window _ownerWindow = null!;

    public ClientSettingsWindowVm(IConfigService configService)
    {
        _configService = configService;

        MqttServers = new ObservableCollection<MqttServerConfig>(_configService.Get(x => x.MqttServers));
        StunServers = new ObservableCollection<StunServerConfig>(_configService.Get(x => x.StunServers));
        TurnServers = new ObservableCollection<TurnServerConfig>(_configService.Get(x => x.TurnServers));

        var clientConfig = _configService.Get(x => x.ClientConfig);

        if(clientConfig is RtcClientConfig c)
        {
            ForceTurnServer = c.ForceTurnServer;
            SelectedStunServer = c.StunServer ?? StunServerConfig.None;
            SelectedTurnServer = c.TurnServer ?? TurnServerConfig.None;
        }

        if(clientConfig is MqttRtcClientConfig m)
        {
            SelectedMqttServer = m.ServerConfig;
            SelectedClientType = ClientTypeSelectableOption.FromClientType(ClientType.MqttRtc);
        }else if(clientConfig is ManualRtcClientConfig)
        {
            SelectedClientType = ClientTypeSelectableOption.FromClientType(ClientType.ManualRtc);
        }
        else
        {
            SelectedClientType = null!;
        }
    }

    public void OnWindowLoad(Window owner)
    {
        _ownerWindow = owner;
    }

    [RelayCommand]
    public async Task CreateMqttServer()
    {
        var window = new MqttServerEditorWindow
        {
            DataContext = new MqttServerEditorVm(_configService, null)
        };

        var result = await window.ShowDialog<MqttServerConfig?>(_ownerWindow);
        if (result is null)
            return;

        MqttServers.Add(result);
        SelectedMqttServer = result;

        _configService.Update(cfg => { cfg.MqttServers = MqttServers.ToList(); });
    }

    [RelayCommand]
    public async Task EditMqttServer()
    {
        var window = new MqttServerEditorWindow
        {
            DataContext = new MqttServerEditorVm(_configService, SelectedMqttServer)
        };

        var result = await window.ShowDialog<MqttServerConfig?>(_ownerWindow);
        if (result is null)
            return;

        var index = MqttServers.IndexOf(SelectedMqttServer!);
        if (index >= 0)
        {
            MqttServers[index] = result;
            SelectedMqttServer = result;
        }

        _configService.Update(cfg => { cfg.MqttServers = MqttServers.ToList(); });
    }

    [RelayCommand]
    public Task DeleteMqttServer()
    {
        if (SelectedMqttServer is null || SelectedMqttServer.Immutable)
            return Task.CompletedTask;

        var removed = SelectedMqttServer;
        MqttServers.Remove(removed);

        _configService.Update(cfg => { cfg.MqttServers = MqttServers.ToList(); });

        SelectedMqttServer = MqttServers.FirstOrDefault(x => x.IsDefault) ?? throw new Exception();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task CreateStunServer()
    {
        var window = new StunServerEditorWindow
        {
            DataContext = new StunServerEditorVm(null)
        };
        var result = await window.ShowDialog<StunServerConfig?>(_ownerWindow);
        if (result is null) return;
        StunServers.Add(result);
        SelectedStunServer = result;
        _configService.Update(cfg => cfg.StunServers = StunServers.ToList());
    }

    [RelayCommand]
    public async Task EditStunServer()
    {
        var window = new StunServerEditorWindow
        {
            DataContext = new StunServerEditorVm(SelectedStunServer)
        };
        var result = await window.ShowDialog<StunServerConfig?>(_ownerWindow);
        if (result is null) return;
        var idx = StunServers.IndexOf(SelectedStunServer);
        if (idx >= 0)
        {
            StunServers[idx] = result;
            SelectedStunServer = result;
        }

        _configService.Update(cfg => cfg.StunServers = StunServers.ToList());
    }

    [RelayCommand]
    public Task DeleteStunServer()
    {
        if (SelectedStunServer.Immutable)
            return Task.CompletedTask;

        var removed = SelectedStunServer;
        StunServers.Remove(removed);

        _configService.Update(cfg => cfg.StunServers = StunServers.ToList());
        SelectedStunServer = StunServers.FirstOrDefault(x => x.IsDefault) ?? throw new Exception();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task CreateTurnServer()
    {
        var window = new TurnServerEditorWindow
        {
            DataContext = new TurnServerEditorVm(null)
        };
        var result = await window.ShowDialog<TurnServerConfig?>(_ownerWindow);
        if (result is null) return;
        TurnServers.Add(result);
        SelectedTurnServer = result;
        _configService.Update(cfg => cfg.TurnServers = TurnServers.ToList());
    }

    [RelayCommand]
    public async Task EditTurnServer()
    {
        var window = new TurnServerEditorWindow
        {
            DataContext = new TurnServerEditorVm(SelectedTurnServer)
        };
        var result = await window.ShowDialog<TurnServerConfig?>(_ownerWindow);
        if (result is null) return;
        var idx = TurnServers.IndexOf(SelectedTurnServer);
        if (idx >= 0)
        {
            TurnServers[idx] = result;
            SelectedTurnServer = result;
        }

        _configService.Update(cfg => cfg.TurnServers = TurnServers.ToList());
    }

    [RelayCommand]
    public Task DeleteTurnServer()
    {
        var removed = SelectedTurnServer;
        TurnServers.Remove(removed);
        _configService.Update(cfg => cfg.TurnServers = TurnServers.ToList());

        SelectedTurnServer = TurnServerConfig.None; 
        ForceTurnServer = false;

        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task Save()
    {
        if(SelectedClientType.ClientType == ClientType.ManualRtc)
        {
            _configService.Update(c =>
            {
                c.ClientConfig = new ManualRtcClientConfig
                {
                    ForceTurnServer = ForceTurnServer,
                    StunServer = SelectedStunServer,
                    TurnServer = SelectedTurnServer
                };
            });
        }else if(SelectedClientType.ClientType == ClientType.MqttRtc)
        {
            _configService.Update(c =>
            {
                c.ClientConfig = new MqttRtcClientConfig
                {
                    ForceTurnServer = ForceTurnServer,
                    StunServer = SelectedStunServer,
                    TurnServer = SelectedTurnServer,
                    ServerConfig = SelectedMqttServer!
                };
            });
        }

        _ownerWindow.Close();
        return Task.CompletedTask;
    }

    #region Designer

    public ClientSettingsWindowVm()
    {
        _configService = new FakeConfigService();

        MqttServers = new ObservableCollection<MqttServerConfig>(_configService.Get(x => x.MqttServers));
        StunServers = new ObservableCollection<StunServerConfig>(_configService.Get(x => x.StunServers));
        TurnServers = new ObservableCollection<TurnServerConfig>(_configService.Get(x => x.TurnServers));

        var clientConfig = _configService.Get(x => x.ClientConfig);
        if (clientConfig is RtcClientConfig c)
        {
            ForceTurnServer = c.ForceTurnServer;
            SelectedStunServer = c.StunServer ?? StunServerConfig.None;
            SelectedTurnServer = c.TurnServer ?? TurnServerConfig.None;
        }

        if (clientConfig is MqttRtcClientConfig m)
        {
            SelectedMqttServer = m.ServerConfig;
            SelectedClientType = ClientTypeSelectableOption.FromClientType(ClientType.MqttRtc);
        }
        else if (clientConfig is ManualRtcClientConfig)
        {
            SelectedClientType = ClientTypeSelectableOption.FromClientType(ClientType.ManualRtc);
        }
        else
        {
            SelectedClientType = null!;
        }
    }

    #endregion
}

public class ClientTypeSelectableOption
{
    public ClientType ClientType { get; }
    public string Name { get; }

    public ClientTypeSelectableOption(ClientType type, string name)
    {
        ClientType = type;
        Name = name;
    }

    public static ClientTypeSelectableOption FromClientType(ClientType clientType)
    {
        string name = "WebRtc via MQTT (default)";

        if (clientType == ClientType.ManualRtc)
            name = "WebRtc via manual signaling";

        return new ClientTypeSelectableOption(clientType, name);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ClientTypeSelectableOption o)
            return false;

        return this.ClientType == o.ClientType;
    }

    protected bool Equals(ClientTypeSelectableOption other)
    {
        return ClientType == other.ClientType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientType);
    }

    public override string ToString()
    {
        return Name;
    }
}