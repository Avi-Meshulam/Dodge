using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Dodge
{
    static class Utils
    {
        private static Random _rand = new Random();

        public static int GetRandom(int minValue, int maxValue)
        {
            return _rand.Next(minValue, maxValue);
        }

        public static int GetRandom(int maxValue)
        {
            return GetRandom(0, maxValue);
        }

        public static int GetRandom()
        {
            return GetRandom(0, int.MaxValue);
        }

        public static bool In<T>(this T item, params T[] items)
        {
            if (items == null) {
                throw new ArgumentNullException("items");
            }
            return items.Contains(item);
        }

        public static void ResetDictionary<K, V>(IDictionary<K, V> dictionary)
        {
            var keys = dictionary.Keys.ToList();
            foreach (var key in keys)
            {
                dictionary[key] = default(V);
            }
        }

        public static K GetRandomUnsetDictionaryKey<K, V>(IDictionary<K, V> dictionary)
        {
            int index;
            V value;
            do
            {
                index = GetRandom(dictionary.Count);
                value = dictionary.ElementAt(index).Value;
            } while (!EqualityComparer<V>.Default.Equals(value, default(V)));

            return dictionary.ElementAt(index).Key;
        }

        public async static Task<IUICommand> ShowMessageDialog(string message, string title = "")
        {
            if(title == "")
            {
                title = Package.Current.DisplayName;
            }

            var dialog = new MessageDialog(message, title);
            dialog.Commands.Add(new UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new UICommand("No") { Id = 1 });

            dialog.DefaultCommandIndex = 1;
            dialog.CancelCommandIndex = 0;

            return await dialog.ShowAsync();
        }

        public async static void ShowMessageBox(string message, string title = "")
        {
            if (title == "")
            {
                title = Package.Current.DisplayName;
            }

            var dialog = new MessageDialog(message, title);

            await dialog.ShowAsync();
        }

        public async static Task<bool> IsFileExsits(string fileName)
        {
            return await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName) != null;
        }

        public static TextBlock CreateTextBlock(string name, string text, int fontSize,
            RoutedEventHandler loadedEventHandler = null)
        {
            var textBlock = new TextBlock();
            textBlock.Name = name;
            textBlock.Text = text;
            textBlock.FontSize = fontSize;
            //textBlock.TextAlignment = TextAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;

            if (loadedEventHandler != null)
            {
                textBlock.Loaded += loadedEventHandler;
            }
            
            return textBlock;
        }

        public static void CreateGridCells(Grid grid, int rowCount, int colCount)
        {
            for (int row = 0; row < rowCount; row++)
            {
                var rowDef = new RowDefinition();
                rowDef.Height = new GridLength(1, GridUnitType.Star);
                grid.RowDefinitions.Add(rowDef);
            }

            for (int col = 0; col < colCount; col++)
            {
                var colDef = new ColumnDefinition();
                colDef.Width = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions.Add(colDef);
            }

            //CreateGridBorders(grid);
        }

        public static void CreateGridBorders(Grid grid)
        {
            int rowsCount = grid.RowDefinitions.Count;
            int colsCount = grid.ColumnDefinitions.Count;

            double topThickness = 1;
            double bottomThickness = 0;
            for (int row = 0; row < rowsCount; row++)
            {
                if (row == rowsCount - 1)
                {
                    bottomThickness = 1;
                }

                double leftThickness = 1;
                double rightThickness = 0;
                for (int col = 0; col < colsCount; col++)
                {
                    if (col == colsCount - 1)
                    {
                        rightThickness = 1;
                    }

                    Border border = new Border();
                    border.BorderThickness = new Thickness(leftThickness, topThickness, rightThickness, bottomThickness);
                    border.BorderBrush = new SolidColorBrush(Colors.Blue);
                    border.HorizontalAlignment = HorizontalAlignment.Stretch;
                    border.VerticalAlignment = VerticalAlignment.Stretch;

                    Grid.SetColumn(border, col);
                    Grid.SetRow(border, row);

                    grid.Children.Add(border);
                }
            }
        }

        public static void ClearTextBlocks(Grid grid)
        {
            if(grid == null)
            {
                return;
            }

            foreach (var child in grid.Children)
            {
                if (child.GetType() == typeof(TextBlock))
                {
                    child.Visibility = Visibility.Collapsed;
                    grid.Children.Remove(child);
                    grid.UpdateLayout();
                }
            }
        }
    }
}
