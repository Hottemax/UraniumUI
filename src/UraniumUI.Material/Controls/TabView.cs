﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows.Input;
using UraniumUI.Material.Resources;
using UraniumUI.Triggers;

namespace UraniumUI.Material.Controls;

[ContentProperty(nameof(Items))]
public partial class TabView : Grid
{
    public IList<TabItem> Items { get; protected set; } = new ObservableCollection<TabItem>();
    public static DataTemplate DefaultTabHeaderItemTemplate => new DataTemplate(() =>
    {
        var grid = new Grid();
        grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
        grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
        grid.Opacity = .5;

        var tabButton = new Button
        {
            StyleClass = new[] { "TextButton" },
        };
        tabButton.CornerRadius = 0;
        tabButton.SetAppThemeColor(Button.TextColorProperty, ColorResource.GetColor("OnBackground"), ColorResource.GetColor("OnBackgroundDark"));
        tabButton.SetBinding(Button.TextProperty, new Binding(nameof(TabItem.Title)));
        tabButton.SetBinding(Button.CommandProperty, new Binding(nameof(TabItem.Command)));

        grid.Add(tabButton, 0, 0);
        grid.Triggers.Add(new DataTrigger(typeof(Grid))
        {
            Binding = new Binding(nameof(TabItem.IsSelected), BindingMode.TwoWay),
            Value = true,
            EnterActions =
            {
                new GenericTriggerAction<Grid>((sender) =>
                {
                    sender.BackgroundColor = ColorResource.GetColor("SurfaceTint0");

                    var box = (sender.Children.FirstOrDefault(x => x is BoxView) as BoxView);

                    box.FadeTo(1, easing: Easing.SpringIn);
                    sender.FadeTo(1);

                    var button = sender.Children.FirstOrDefault(x=>x is Button) as Button;
                    button?.SetAppThemeColor(Button.TextColorProperty, ColorResource.GetColor("Primary"), ColorResource.GetColor("PrimaryDark"));
                })
            },
            ExitActions =
            {
                new GenericTriggerAction<Grid>((sender) =>
                {
                    var box = (sender.Children.FirstOrDefault(x => x is BoxView) as BoxView);

                    sender.BackgroundColor = Colors.Transparent;

                    box.FadeTo(0, easing: Easing.SpringIn);
                    sender.FadeTo(.5);

                    var button = sender.Children.FirstOrDefault(x=>x is Button) as Button;
                    button?.SetAppThemeColor(Button.TextColorProperty, ColorResource.GetColor("OnBackground"), ColorResource.GetColor("OnBackgroundDark"));
                })
            }
        });

        var selectionIndicator = new BoxView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            HeightRequest = 5,
            CornerRadius = 1,
            Opacity = 0,
        };

        selectionIndicator.SetAppThemeColor(BoxView.ColorProperty,
            ColorResource.GetColor("SurfaceTint2"),
            ColorResource.GetColor("PrimaryDark"));

        grid.Add(selectionIndicator, row: 0);

        return grid;
    });

    protected readonly StackLayout _headerContainer = new StackLayout
    {
        HorizontalOptions = LayoutOptions.Fill
    };

    protected readonly ContentView _contentContainer = new ContentView
    {
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill
    };

    public TabView()
    {
        InitializeLayout();
        if (Items is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged -= Items_CollectionChanged;
            observable.CollectionChanged += Items_CollectionChanged;
        }

        Render();
    }

    protected virtual void InitializeLayout()
    {
        this.Clear();
        this.ColumnDefinitions.Clear();
        this.RowDefinitions.Clear();

        switch (TabPlacement)
        {
            case TabViewTabPlacement.Top:
                {
                    this.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    this.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                    _headerContainer.Orientation = StackOrientation.Horizontal;

                    this.Add(_headerContainer, row: 0);
                    this.Add(_contentContainer, row: 1);
                }
                break;
            case TabViewTabPlacement.Bottom:
                {
                    this.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                    this.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    _headerContainer.Orientation = StackOrientation.Horizontal;

                    this.Add(_headerContainer, row: 1);
                    this.Add(_contentContainer, row: 0);
                }
                break;
            case TabViewTabPlacement.Start:
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                    this.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                    _headerContainer.Orientation = StackOrientation.Vertical;

                    this.Add(_headerContainer, column: 0);
                    this.Add(_contentContainer, column: 1);
                }
                break;
            case TabViewTabPlacement.End:
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                    this.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                    _headerContainer.Orientation = StackOrientation.Vertical;

                    this.Add(_headerContainer, column: 1);
                    this.Add(_contentContainer, column: 0);
                }
                break;
        }
    }

    private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    foreach (var item in e.NewItems)
                    {
                        if (item is TabItem tabItem)
                        {
                            AddHeaderFor(tabItem);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    foreach (var item in e.OldItems)
                    {
                        if (item is TabItem tabItem)
                        {
                            RemoveHeaderFor(tabItem);
                        }
                    }
                }
                break;
            default:
                // TODO: Optimize
                Render();
                break;
        }
    }

    protected virtual void Render()
    {
        if (Items?.Count > 0)
        {
            _headerContainer.Children.Clear();
            RenderHeaders();

            if (CurrentItem is null)
            {
                ResetSelectedItem();
            }
        }
    }

    protected virtual void RenderHeaders()
    {
        foreach (var item in Items)
        {
            AddHeaderFor(item);
        }
    }

    protected void ResetSelectedItem()
    {
        CurrentItem = Items.FirstOrDefault();
    }

    protected virtual void AddHeaderFor(TabItem tabItem)
    {
        tabItem.TabView = this;
        var view = TabHeaderItemTemplate?.CreateContent() as View;
        view.BindingContext = tabItem;
        view.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => CurrentItem = tabItem) });

        if (!_headerContainer.Children.Any())
        {
            CurrentItem = tabItem;
        }

        _headerContainer.Add(view);
    }

    protected virtual void RemoveHeaderFor(TabItem tabItem)
    {
        var existing = _headerContainer.Children.FirstOrDefault(x => x is View view && view.BindingContext == tabItem);

        if (CurrentItem == tabItem)
        {
            ResetSelectedItem();
        }

        _headerContainer.Children.Remove(existing);
    }

    protected virtual async void OnCurrentItemChanged(TabItem newCurrentTabItem)
    {
        var content = newCurrentTabItem.Content ??= (View)newCurrentTabItem.ContentTemplate?.CreateContent();


        foreach (var item in Items)
        {
            item.NotifyIsSelectedChanged();
        }

        if (_contentContainer.Content != null)
        {
            await _contentContainer.Content?.FadeTo(0, 125);
        }

        content.Opacity = 0;
        _contentContainer.Content = content;

        await content.FadeTo(1, 125);
    }
    protected virtual void OnTabPlacementChanged()
    {
        InitializeLayout();
    }
}

public class TabItem : UraniumBindableObject
{
    public string Title { get; set; }
    public object Data { get; set; }
    public DataTemplate ContentTemplate { get; set; }
    public View Content { get; set; }
    public TabView TabView { get; internal set; }
    public bool IsSelected => TabView.CurrentItem == this;
    public ICommand Command { get; private set; }

    public TabItem()
    {
        Command = new Command(ChangedCommand);
    }

    private void ChangedCommand(object obj)
    {
        TabView.CurrentItem = this;
    }

    protected internal void NotifyIsSelectedChanged()
    {
        OnPropertyChanged(nameof(IsSelected));
    }
}
