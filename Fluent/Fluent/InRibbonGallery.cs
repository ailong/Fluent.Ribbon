﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fluent
{
    [ContentProperty("Items")]
    public class InRibbonGallery:RibbonItemsControl,IScalableRibbonControl
    {
        #region Fields

        private ObservableCollection<GalleryGroupFilter> filters;

        private ObservableCollection<GalleryGroupIcon> groupIcons;

        private ICollectionView view;

        private RibbonListBox listBox;

        private ContextMenu contextMenu;

        private Gallery gallery = new Gallery();
        private MenuPanel menuBar = new MenuPanel();
        private ToggleButton expandButton;
        private ToggleButton dropDownButton;

        // Collection of toolbar items
        private ObservableCollection<UIElement> menuItems;

        private double currentSize;

        private Panel layoutRoot;

        private double cachedWidthDelta;

        // Freezed image (created during snapping)
        Image snappedImage;
        // Visuals which were removed diring snapping
        Visual[] snappedVisuals;
        // Is visual currently snapped
        private bool isSnapped;

        private bool isInitializing;

        private DropDownButton quickAccessButton;

        #endregion

        #region Properties

        #region ScrollBarsVisibility

        /// <summary> 
        /// HorizonalScollbarVisibility is a Windows.Controls.ScrollBarVisibility that
        /// determines if a horizontal scrollbar is shown. 
        /// </summary> 
        [Bindable(true), Category("Appearance")]
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for HorizontalScrollBarVisibility.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(InRibbonGallery), new UIPropertyMetadata(ScrollBarVisibility.Disabled));

        /// <summary> 
        /// VerticalScrollBarVisibility is a System.Windows.Controls.ScrollBarVisibility that 
        /// determines if a vertical scrollbar is shown.
        /// </summary> 
        [Bindable(true), Category("Appearance")]
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for VerticalScrollBarVisibility.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(InRibbonGallery), new UIPropertyMetadata(ScrollBarVisibility.Visible));

        #endregion

        #region GroupBy

        public string GroupBy
        {
            get { return (string)GetValue(GroupByProperty); }
            set { SetValue(GroupByProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GroupBy.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GroupByProperty =
            DependencyProperty.Register("GroupBy", typeof(string), typeof(InRibbonGallery), new UIPropertyMetadata(null));        

        #endregion

        #region Orientation

        /// <summary>
        /// Gets or sets orientation of gallery
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(InRibbonGallery), new UIPropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d as InRibbonGallery).gallery != null)
            {
                //(d as InRibbonGallery).gallery.Orientation = (Orientation) e.NewValue;
                if ((Orientation)e.NewValue == Orientation.Horizontal)
                {
                    ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof (WrapPanel)));
                    template.Seal();
                    (d as InRibbonGallery).ItemsPanel = template;
                }
                else
                {
                    ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
                    template.Seal();
                    (d as InRibbonGallery).ItemsPanel = template;
                }
            }
        }

        #endregion

        #region Filters

        /// <summary>
        /// Gets collection of filters
        /// </summary>
        /// <summary>
        /// Gets collection of filters
        /// </summary>
        public ObservableCollection<GalleryGroupFilter> Filters
        {
            get
            {
                if (this.filters == null)
                {
                    this.filters = new ObservableCollection<GalleryGroupFilter>();
                    this.filters.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnFilterCollectionChanged);
                }
                return this.filters;
            }
        }


        // Handle toolbar iitems changes
        private void OnFilterCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Filters.Count > 0) HasFilter = true;
            else HasFilter = false;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (object obj2 in e.NewItems)
                    {
                        gallery.Filters.Add(obj2 as GalleryGroupFilter);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (object obj3 in e.OldItems)
                    {
                        gallery.Filters.Remove(obj3 as GalleryGroupFilter);
                        
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (object obj4 in e.OldItems)
                    {
                        gallery.Filters.Remove(obj4 as GalleryGroupFilter);
                    }
                    foreach (object obj5 in e.NewItems)
                    {
                        gallery.Filters.Add(obj5 as GalleryGroupFilter);
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets or sets selected filter
        /// </summary>               
        public GalleryGroupFilter SelectedFilter
        {
            get { return (GalleryGroupFilter)GetValue(SelectedFilterProperty); }
            set { SetValue(SelectedFilterProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for SelectedFilter.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register("SelectedFilter", typeof(GalleryGroupFilter), typeof(InRibbonGallery), new UIPropertyMetadata(null, OnFilterChanged));

        // Handles filter property changed
        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as InRibbonGallery).UpdateFilter();
            if (e.NewValue != null) (d as InRibbonGallery).SelectedFilterTitle = (e.NewValue as GalleryGroupFilter).Title;
            else (d as InRibbonGallery).SelectedFilterTitle = "";
        }

        /// <summary>
        /// Gets selected filter title
        /// </summary>
        public string SelectedFilterTitle
        {
            get { return (string)GetValue(SelectedFilterTitleProperty); }
            private set { SetValue(SelectedFilterTitleProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for SelectedFilterTitle.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty SelectedFilterTitleProperty =
            DependencyProperty.Register("SelectedFilterTitle", typeof(string), typeof(InRibbonGallery), new UIPropertyMetadata(null));

        /// <summary>
        /// Gets whether gallery has selected filter
        /// </summary>       
        public bool HasFilter
        {
            get { return (bool)GetValue(HasFilterProperty); }
            private set { SetValue(HasFilterProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for HasFilter.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty HasFilterProperty =
            DependencyProperty.Register("HasFilter", typeof(bool), typeof(InRibbonGallery), new UIPropertyMetadata(false));


        // Set filter
        private void UpdateFilter()
        {
            if ((listBox != null) && (listBox.ItemsSource != null))
            {
                if (view != null) view.Filter -= OnFiltering;
                view = CollectionViewSource.GetDefaultView(listBox.ItemsSource);
                view.Filter += OnFiltering;
            }
        }

        private bool OnFiltering(object obj)
        {
            if (SelectedFilter == null) return true;
            string[] filterItems = SelectedFilter.Groups.Split(",".ToCharArray());
            return filterItems.Contains(GetItemGroupName(obj));
        }

        #endregion

        #region Selectable

        public bool Selectable
        {
            get { return (bool)GetValue(SelectableProperty); }
            set { SetValue(SelectableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Selectable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectableProperty =
            DependencyProperty.Register("Selectable", typeof(bool), typeof(InRibbonGallery), new UIPropertyMetadata(true));

        #endregion

        #region SelectedIndex

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(InRibbonGallery), new UIPropertyMetadata(-1, OnSelectedIndexChanged, CoerceSelectedIndex));

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if((d as InRibbonGallery).listBox!=null)
            {
                (d as InRibbonGallery).listBox.SelectedIndex = (int) e.NewValue;
            }
        }

        private static object CoerceSelectedIndex(DependencyObject d, object basevalue)
        {
            if (!(d as InRibbonGallery).Selectable)
            {
                (d as InRibbonGallery).listBox.SelectedIndex = -1;
                return -1;
            }
            else return basevalue;
        }

        #endregion

        #region SelectedItem

        [Bindable(true)]
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(InRibbonGallery), new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChange, CoerceSelectedItem));

        private static void OnSelectedItemChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d as InRibbonGallery).listBox != null)
            {
                (d as InRibbonGallery).listBox.SelectedItem = e.NewValue;
            }
        }

        private static object CoerceSelectedItem(DependencyObject d, object basevalue)
        {
            if (!(d as InRibbonGallery).Selectable)
            {
                if (basevalue != null)
                {
                    ((d as InRibbonGallery).listBox.ContainerFromElement(basevalue as DependencyObject) as ListBoxItem).IsSelected = false;
                    (d as InRibbonGallery).listBox.SelectedItem = null;
                }
                return null;
            }
            else return basevalue;
        }

        #endregion

        #region GroupIcons

        /// <summary>
        /// Gets collection of group icons
        /// </summary>
        public ObservableCollection<GalleryGroupIcon> GroupIcons
        {
            get
            {
                if (this.groupIcons == null)
                {
                    this.groupIcons = new ObservableCollection<GalleryGroupIcon>();
                    this.groupIcons.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnGroupIconCollectionChanged);
                }
                return this.groupIcons;
            }
        }


        // Handle toolbar iitems changes
        private void OnGroupIconCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (object obj2 in e.NewItems)
                    {
                        gallery.GroupIcons.Add(obj2 as GalleryGroupIcon);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (object obj3 in e.OldItems)
                    {
                        gallery.GroupIcons.Remove(obj3 as GalleryGroupIcon);

                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (object obj4 in e.OldItems)
                    {
                        gallery.GroupIcons.Remove(obj4 as GalleryGroupIcon);
                    }
                    foreach (object obj5 in e.NewItems)
                    {
                        gallery.GroupIcons.Add(obj5 as GalleryGroupIcon);
                    }
                    break;
            }
        }

        #endregion

        #region IsOpen



        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(InRibbonGallery), new UIPropertyMetadata(false, OnIsOpenChanged, CoerceIsOpen));

        // Coerce IsOpen
        private static object CoerceIsOpen(DependencyObject d, object basevalue)
        {
            if ((d as InRibbonGallery).isInitializing) return true;
            return basevalue;
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                if ((d as InRibbonGallery).contextMenu == null) (d as InRibbonGallery).CreateMenu();
                else (d as InRibbonGallery).contextMenu.IsOpen = true;
                if((d as InRibbonGallery).IsCollapsed)(d as InRibbonGallery).dropDownButton.IsChecked= true;
            }
            else
            {
                if ((d as InRibbonGallery).contextMenu != null) (d as InRibbonGallery).contextMenu.IsOpen = false;
                //(d as InRibbonGallery).dropDownButton.IsHitTestVisible = true;
                if ((d as InRibbonGallery).IsCollapsed) (d as InRibbonGallery).dropDownButton.IsChecked = false;
            }
        }

        #endregion

        #region ResizeMode

        /// <summary>
        /// Gets or sets context menu resize mode
        /// </summary>
        public ContextMenuResizeMode ResizeMode
        {
            get { return (ContextMenuResizeMode)GetValue(ResizeModeProperty); }
            set { SetValue(ResizeModeProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for ResizeMode.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty ResizeModeProperty =
            DependencyProperty.Register("ResizeMode", typeof(ContextMenuResizeMode), typeof(InRibbonGallery), new UIPropertyMetadata(ContextMenuResizeMode.None));

        #endregion

        #region MenuItems

        /// <summary>
        /// Gets collection of menu items
        /// </summary>
        public new ObservableCollection<UIElement> MenuItems
        {
            get
            {
                if (this.menuItems == null)
                {
                    this.menuItems = new ObservableCollection<UIElement>();
                    this.menuItems.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnMenuItemsCollectionChanged);
                }
                return this.menuItems;
            }
        }

        /// <summary>
        /// handles colection of menu items changes
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">The event data</param>
        private void OnMenuItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (object obj2 in e.NewItems)
                    {
                        if (menuBar != null) menuBar.Children.Add(obj2 as UIElement);
                        else AddLogicalChild(obj2);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (object obj3 in e.OldItems)
                    {
                        if (menuBar != null) menuBar.Children.Remove(obj3 as UIElement);
                        else RemoveLogicalChild(obj3);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (object obj4 in e.OldItems)
                    {
                        if (menuBar != null) menuBar.Children.Remove(obj4 as UIElement);
                        else RemoveLogicalChild(obj4);
                    }
                    foreach (object obj5 in e.NewItems)
                    {
                        if (menuBar != null) menuBar.Children.Add(obj5 as UIElement);
                        else AddLogicalChild(obj5);
                    }
                    break;
            }

        }

        #endregion

        #region CanCollapseToButton

        /// <summary>
        /// Gets or sets whether InRibbonGallery
        /// </summary>
        public bool CanCollapseToButton
        {
            get { return (bool)GetValue(CanCollapseToButtonProperty); }
            set { SetValue(CanCollapseToButtonProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for CanCollapseToButton.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CanCollapseToButtonProperty =
            DependencyProperty.Register("CanCollapseToButton", typeof(bool), typeof(InRibbonGallery), new UIPropertyMetadata(true));

        #endregion

        #region IsCollapsed

        /// <summary>
        /// Gets whether InRibbonGallery is collapsed to button
        /// </summary>
        public bool IsCollapsed
        {
            get { return (bool)GetValue(IsCollapsedProperty); }
            set { SetValue(IsCollapsedProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for IsCollapsed.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register("IsCollapsed", typeof(bool), typeof(InRibbonGallery), new UIPropertyMetadata(false));

        #endregion

        #region LargeIcon

        /// <summary>
        /// Button large icon
        /// </summary>
        public ImageSource LargeIcon
        {
            get { return (ImageSource)GetValue(LargeIconProperty); }
            set { SetValue(LargeIconProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for SmallIcon.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty LargeIconProperty =
            DependencyProperty.Register("LargeIcon", typeof(ImageSource), typeof(InRibbonGallery), new UIPropertyMetadata(null));

        #endregion

        #region Snapping

        /// <summary>
        /// Snaps / Unsnaps the Visual 
        /// (remove visuals and substitute with freezed image)
        /// </summary>
        private bool IsSnapped
        {
            get
            {
                return isSnapped;
            }
            set
            {
                if (value == isSnapped) return;

                if (value)
                {
                    // Render the freezed image
                    snappedImage = new Image();
                    RenderOptions.SetBitmapScalingMode(snappedImage, BitmapScalingMode.NearestNeighbor);
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((Visual)VisualTreeHelper.GetChild(this, 0));
                    snappedImage.Source = renderTargetBitmap;
                    // Detach current visual children
                    snappedVisuals = new Visual[VisualTreeHelper.GetChildrenCount(this)];
                    for (int childIndex = 0; childIndex < snappedVisuals.Length; childIndex++)
                    {
                        snappedVisuals[childIndex] = (Visual)VisualTreeHelper.GetChild(this, childIndex);
                        RemoveVisualChild(snappedVisuals[childIndex]);
                    }

                    // Attach freezed image
                    AddVisualChild(snappedImage);
/*
                    PngBitmapEncoder enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                    string path = Path.GetTempFileName() + ".png";
                    using (FileStream f = new FileStream(path, FileMode.Create))
                    {
                        enc.Save(f);
                        
                    }
                    Process.Start(path);*/
                }
                else
                {
                    RemoveVisualChild(snappedImage);
                     for (int childIndex = 0; childIndex < snappedVisuals.Length; childIndex++)
                     {
                         AddVisualChild(snappedVisuals[childIndex]);
                     }

                    // Clean up
                    snappedImage = null;
                    snappedVisuals = null;
                }
                isSnapped = value;
                InvalidateVisual();
                //UpdateLayout();
            }
        }

        /// <summary>
        /// Gets visual children count
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                if (isSnapped) return 1;
                return base.VisualChildrenCount;
            }
        }

        /// <summary>
        /// Returns a child at the specified index from a collection of child elements
        /// </summary>
        /// <param name="index">The zero-based index of the requested child element in the collection</param>
        /// <returns>The requested child element</returns>
        protected override Visual GetVisualChild(int index)
        {
            if (isSnapped) return snappedImage;
            return base.GetVisualChild(index);
        }

        #endregion

        #region Min/Max Sizes

        /// <summary>
        /// Gets or sets max size of gallery in pixels
        /// </summary>
        public double MaxSize
        {
            get { return (double)GetValue(MaxSizeProperty); }
            set { SetValue(MaxSizeProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for MaxSize.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty MaxSizeProperty =
                DependencyProperty.Register("MaxSize", typeof(double), typeof(InRibbonGallery), new UIPropertyMetadata(100.0));

        /// <summary>
        /// Gets or sets min size of gallery in pixels
        /// </summary>
        public double MinSize
        {
            get { return (double)GetValue(MinSizeProperty); }
            set { SetValue(MinSizeProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for MaxSize.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty MinSizeProperty =
                DependencyProperty.Register("MinSize", typeof(double), typeof(InRibbonGallery), new UIPropertyMetadata(1.0));

        #endregion

        #region LogicalChildren

        protected override System.Collections.IEnumerator LogicalChildren
        {
            get
            {
                ArrayList list = new ArrayList();
                if (listBox != null) list.AddRange(listBox.Items);
                list.AddRange(MenuItems);
                return list.GetEnumerator();
            }
        }

        #endregion

        #endregion

        #region Events

        public event EventHandler MenuOpened;
        public event EventHandler MenuClosed;

        #endregion

        #region Constructors

        /// <summary>
        /// Static constructor
        /// </summary>
        static InRibbonGallery()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InRibbonGallery), new FrameworkPropertyMetadata(typeof(InRibbonGallery)));
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public InRibbonGallery()
        {
            Binding binding = new Binding("DisplayMemberPath");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.DisplayMemberPathProperty, binding);
            binding = new Binding("ItemBindingGroup");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.ItemBindingGroupProperty, binding);
            binding = new Binding("ItemContainerStyle");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.ItemContainerStyleProperty, binding);
            binding = new Binding("ItemContainerStyleSelector");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.ItemContainerStyleSelectorProperty, binding);
            binding = new Binding("ItemsPanel");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.ItemsPanelProperty, binding);
            binding = new Binding("ItemStringFormat");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.ItemStringFormatProperty, binding);
            binding = new Binding("ItemTemplate");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.ItemTemplateProperty, binding);
            binding = new Binding("ItemTemplateSelector");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.ItemTemplateSelectorProperty, binding);
            binding = new Binding("IsTextSearchEnabled");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.IsTextSearchEnabledProperty, binding);

            binding = new Binding("VerticalScrollBarVisibility");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.VerticalScrollBarVisibilityProperty, binding);
            binding = new Binding("HorizontalScrollBarVisibility");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.HorizontalScrollBarVisibilityProperty, binding);

            binding = new Binding("GroupBy");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.GroupByProperty, binding);

            /*binding = new Binding("Orientation");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.OrientationProperty, binding);*/
            gallery.Orientation = Orientation;

            binding = new Binding("SelectedFilter");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = this;
            gallery.SetBinding(Gallery.SelectedFilterProperty, binding);

            binding = new Binding("SelectedIndex");
            binding.Source = this;
            binding.Mode = BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            gallery.SetBinding(SelectedIndexProperty, binding);

            binding = new Binding("SelectedItem");
            binding.Source = this;
            binding.Mode = BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            gallery.SetBinding(SelectedItemProperty, binding);   

            AddHandler(RibbonControl.ClickEvent, new RoutedEventHandler(OnClick));
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            IsOpen = true;
            e.Handled = true;
        }

        #endregion  
      
        #region Overrides

        public override void OnApplyTemplate()
        {
            if (listBox != null)
            {
                listBox.SelectionChanged -= OnListBoxSelectionChanged;
                listBox.ItemsSource = null;                
            }
            listBox = GetTemplateChild("PART_ListBox") as RibbonListBox;
            if (listBox != null)
            {
                if (ItemsSource != null) listBox.ItemsSource = ItemsSource;
                else listBox.ItemsSource = Items;
                
                listBox.SelectedItem = SelectedItem;
                if (SelectedIndex!=-1) listBox.SelectedIndex = SelectedIndex;
                
                /*Binding binding = new Binding("SelectedIndex");
                binding.Source = listBox;
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                this.SetBinding(SelectedIndexProperty, binding);

                binding = new Binding("SelectedItem");
                binding.Source = this;
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                listBox.SetBinding(SelectedItemProperty, binding);

                gallery.SelectedIndex = SelectedIndex;*/

                listBox.SelectionChanged += OnListBoxSelectionChanged;                           
            }
            if (expandButton != null) expandButton.Click -= OnExpandClick;
            expandButton = GetTemplateChild("PART_ExpandButton") as ToggleButton;
            if (expandButton != null) expandButton.Click += OnExpandClick;

            if (dropDownButton != null) dropDownButton.Click -= OnDropDownClick;
            dropDownButton = GetTemplateChild("PART_DropDownButton") as ToggleButton;
            if (dropDownButton != null) dropDownButton.Click += OnDropDownClick;

            layoutRoot = GetTemplateChild("PART_LayoutRoot") as Panel;

            // Clear cache then style changed
            cachedWidthDelta = 0;
        }

        private void OnDropDownClick(object sender, RoutedEventArgs e)
        {            
            dropDownButton.IsChecked = true;
            IsOpen = true;
            e.Handled = true;            
        }

        private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndex = listBox.SelectedIndex;
            SelectedItem = listBox.SelectedItem;
        }

        private void OnExpandClick(object sender, RoutedEventArgs e)
        {                        
            IsOpen = true;            
            e.Handled = true;
        }
        
        protected override Size MeasureOverride(Size constraint)
        {
            if (isSnapped) return new Size(snappedImage.Width, snappedImage.Height);
            if (IsCollapsed) return base.MeasureOverride(constraint); 
            if (listBox == null) return base.MeasureOverride(constraint);
            if (Items.Count == 0) return base.MeasureOverride(constraint);
            GalleryItem item = (listBox.ItemContainerGenerator.ContainerFromItem(Items[0]) as GalleryItem);
            bool useHack = false;
            if (item == null)
            {
                useHack = true;
                RemoveLogicalChild(Items[0]);
                item = new GalleryItem();
                item.Content = Items[0];                
            }
            item.Measure(constraint);
            double itemWidth = item.DesiredSize.Width;
            if(useHack)
            {
                item.Content = null;
                AddLogicalChild(Items[0]);
            }
            if(cachedWidthDelta==0)
            {
                base.MeasureOverride(constraint);
                cachedWidthDelta = layoutRoot.DesiredSize.Width - listBox.InnerPanelWidth;
            }
            if (currentSize == 0) currentSize = MaxSize*itemWidth;
            base.MeasureOverride(new Size(Math.Max(Math.Min(MaxSize * itemWidth, currentSize), MinSize * itemWidth) + cachedWidthDelta, constraint.Height));
            return layoutRoot.DesiredSize;
        }

        protected override void OnSizePropertyChanged(RibbonControlSize previous, RibbonControlSize current)
        {
            if (CanCollapseToButton)
            {
                double itemWidth = (listBox.ItemContainerGenerator.ContainerFromItem(Items[0]) as GalleryItem).DesiredSize.Width;
                if ((current == RibbonControlSize.Large) && ((currentSize > MinSize * itemWidth))) IsCollapsed = false;
                else IsCollapsed = true;
            }
            else IsCollapsed = false;
            base.OnSizePropertyChanged(previous, current);
        }

        protected override void OnItemsCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsCollectionChanged(e);
            if (listBox != null)
            {
                if (ItemsSource == null) listBox.ItemsSource = Items;
                else listBox.ItemsSource = ItemsSource;
            }
        }

        protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnItemsSourceChanged(e);
            if (listBox != null)
            {
                if (ItemsSource == null) listBox.ItemsSource = Items;
                else listBox.ItemsSource = ItemsSource;
            }
        }

        #endregion

        #region Private Methods

        internal string GetItemGroupName(object obj)
        {
            return obj.GetType().GetProperty(GroupBy, BindingFlags.Public | BindingFlags.Instance).GetValue(obj, null).ToString();
        }

        private void CreateMenu()
        {
            isInitializing = true;
            contextMenu = new ContextMenu();
            contextMenu.Owner = this;
            AddLogicalChild(contextMenu.RibbonPopup);
            contextMenu.IsOpen = true;

            if(!IsCollapsed)IsSnapped = true;            
            object selectedItem = listBox.SelectedItem;
            int selectedIndex = listBox.SelectedIndex;
            listBox.ItemsSource = null;
            gallery.MinWidth = ActualWidth;
            gallery.MinHeight = ActualHeight;
            if (ItemsSource == null) gallery.ItemsSource = Items;
            else gallery.ItemsSource = ItemsSource;
            gallery.SelectedItem = selectedItem;
            gallery.SelectedIndex = selectedIndex;
            SelectedItem = selectedItem;
            SelectedIndex = selectedIndex;
            expandButton.IsChecked = true;
            contextMenu.RibbonPopup.Opened += OnMenuOpened;
            contextMenu.RibbonPopup.Closed += OnMenuClosed;            

            Binding binding = new Binding("ResizeMode");
            binding.Mode = BindingMode.OneWay;
            binding.Source = this;
            contextMenu.SetBinding(Fluent.ContextMenu.ResizeModeProperty, binding);

            contextMenu.PlacementTarget = this;
            if(IsCollapsed)contextMenu.Placement = PlacementMode.Bottom;
            else contextMenu.Placement = PlacementMode.Relative;   

            contextMenu.Items.Add(gallery);
            contextMenu.Items.Add(menuBar);
            
            isInitializing = false;
            Mouse.Capture(null);
            IsOpen = true;
            contextMenu.IsOpen = true;
        }

        private void OnMenuClosed(object sender, EventArgs e)
        {
            object selectedItem = gallery.SelectedItem;
            gallery.ItemsSource = null;
            if (ItemsSource == null) listBox.ItemsSource = Items;
            else listBox.ItemsSource = ItemsSource;
            listBox.SelectedItem = selectedItem;
            SelectedItem = selectedItem;
            SelectedIndex = listBox.SelectedIndex;
            if (MenuClosed != null) MenuClosed(this, e);
            if (!IsCollapsed) IsSnapped = false;
            expandButton.IsChecked = false;
            expandButton.InvalidateVisual();
            IsOpen = false;
        }

        private void OnMenuOpened(object sender, EventArgs e)
        {
            if (!IsCollapsed) IsSnapped = true;
            if (IsCollapsed) contextMenu.Placement = PlacementMode.Bottom;
            else contextMenu.Placement = PlacementMode.Relative;
            object selectedItem = listBox.SelectedItem;
            listBox.ItemsSource = null;
            gallery.MinWidth = ActualWidth;
            gallery.MinHeight = ActualHeight;
            if (ItemsSource == null) gallery.ItemsSource = Items;
            else gallery.ItemsSource = ItemsSource;
            gallery.SelectedItem = selectedItem;
            SelectedItem = selectedItem;
            SelectedIndex = gallery.SelectedIndex;
            if (MenuOpened != null) MenuOpened(this, e);
            //InvalidateVisual();
            UpdateLayout();
            expandButton.IsChecked = true;
        }

        #endregion

        #region Quick Access Item Creating

        /// <summary>
        /// Gets control which represents shortcut item.
        /// This item MUST be syncronized with the original 
        /// and send command to original one control.
        /// </summary>
        /// <returns>Control which represents shortcut item</returns>
        public override UIElement CreateQuickAccessItem()
        {
            DropDownButton button = new DropDownButton();
            BindQuickAccessItem(button);
            return button;
        }

        /// <summary>
        /// This method must be overriden to bind properties to use in quick access creating
        /// </summary>
        /// <param name="element">Toolbar item</param>
        protected override void BindQuickAccessItem(FrameworkElement element)
        {
            DropDownButton button = element as DropDownButton;            
            base.BindQuickAccessItem(element);
            button.PreviewMouseLeftButtonDown += OnQuickAccessClick;
        }

        private void OnQuickAccessClick(object sender, MouseButtonEventArgs e)
        {
            DropDownButton button = sender as DropDownButton;

            if (!IsCollapsed) IsSnapped = true;
            object selectedItem = listBox.SelectedItem;
            listBox.ItemsSource = null;
            gallery.MinWidth = ActualWidth;
            gallery.MinHeight = ActualHeight;
            if (ItemsSource == null) gallery.ItemsSource = Items;
            else gallery.ItemsSource = ItemsSource;
            gallery.SelectedItem = selectedItem;
            SelectedItem = selectedItem;
            SelectedIndex = gallery.SelectedIndex;

            if (contextMenu != null)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    UIElement item = contextMenu.Items[0];
                    contextMenu.Items.Remove(item);
                    button.Items.Add(item);
                    i--;
                }
            }
            else
            {                
                RemoveLogicalChild(gallery);
                RemoveLogicalChild(menuBar);
                button.Items.Add(gallery);
                button.Items.Add(menuBar);
            }
            button.MenuClosed += OnQuickAccessMenuClosed;
            quickAccessButton = button;
        }

        private void OnQuickAccessMenuClosed(object sender, EventArgs e)
        {
            quickAccessButton.MenuClosed -= OnQuickAccessMenuClosed;
            if (contextMenu != null)
            {
                for (int i = 0; i < quickAccessButton.Items.Count; i++)
                {
                    UIElement item = quickAccessButton.Items[0];
                    quickAccessButton.Items.Remove(item);
                    contextMenu.Items.Add(item);
                    i--;
                }
            }
            else
            {
                quickAccessButton.Items.Remove(gallery);
                quickAccessButton.Items.Remove(menuBar);
                AddLogicalChild(gallery);
                AddLogicalChild(menuBar);
            }

            object selectedItem = gallery.SelectedItem;
            gallery.ItemsSource = null;
            if (ItemsSource == null) listBox.ItemsSource = Items;
            else listBox.ItemsSource = ItemsSource;
            listBox.SelectedItem = selectedItem;
            SelectedItem = selectedItem;
            SelectedIndex = listBox.SelectedIndex;
            if (!IsCollapsed) IsSnapped = false;
        }

        #endregion

        #region Implementation of IScalableRibbonControl

        /// <summary>
        /// Enlarge control size
        /// </summary>
        public void Enlarge()
        {
            if(listBox==null) return;
            if (Items.Count == 0) return;
            double itemWidth = (listBox.ItemContainerGenerator.ContainerFromItem(listBox.Items[0]) as GalleryItem).DesiredSize.Width;
            double newSize = DesiredSize.Width + itemWidth;
            if ((CanCollapseToButton) && (newSize > MinSize * itemWidth)&&(Size==RibbonControlSize.Large)) IsCollapsed = false;
            newSize = Math.Max(Math.Min(MaxSize * itemWidth, newSize), MinSize * itemWidth);
            currentSize = newSize;
            InvalidateMeasure();
        }

        /// <summary>
        /// Reduce control size
        /// </summary>
        public void Reduce()
        {
            if (listBox == null) return;
            if (Items.Count == 0) return;
            double itemWidth = (listBox.ItemContainerGenerator.ContainerFromItem(listBox.Items[0]) as GalleryItem).DesiredSize.Width;
            double newSize = DesiredSize.Width - itemWidth;
            if ((CanCollapseToButton) && (newSize < MinSize * itemWidth)) IsCollapsed = true;
            newSize = Math.Max(Math.Min(MaxSize * itemWidth, newSize), MinSize * itemWidth);
            currentSize = newSize;
            InvalidateMeasure();
        }

        #endregion
    }
}
