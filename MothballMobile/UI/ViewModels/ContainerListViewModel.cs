using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp;

namespace MothballMobile.UI.ViewModels;

public partial class ContainerListViewModel : ObservableObject
{
    private ObservableCollection<Container> containers;

    public ObservableCollection<Container> Containers
    {
        get => containers;
        set => SetProperty(ref containers, value);
    }

    public ContainerListViewModel()
    {
        Containers = new ObservableCollection<Container>();
    }

    [RelayCommand]
    public void AddContainer(Container container)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container), "Container cannot be null.");
        }

        Containers.Add(container);
    }
}
