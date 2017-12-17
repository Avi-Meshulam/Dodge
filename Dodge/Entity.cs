using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Enums;
using System.Xml.Linq;

namespace Dodge
{
    abstract class Entity
    {
        public virtual event EventHandler ImageOpened;
        public virtual event EventHandler PositionChanged;

        public readonly int Id;
        public EntityType Type { get; protected set; }
        public Image Image { get; set; } = new Image();
        public Position Position { get; protected set; }
        public int RowSpan { get; set; } = 1;
        public int ColSpan { get; set; } = 1;
        public Board Board { get; protected set; }
        public bool IsPositioned { get; set; }

        //Entity must be identified by Id and must be in a context of a board
        public Entity(int id, Board board, EntityType entityType, Position position = null) 
        {
            Id = id;
            Board = board;
            Type = entityType;
            Position = position ?? new Position();
            Image.ImageOpened += Image_ImageOpened;
        }

        public abstract XElement GetXML();

        private void Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            ImageOpened?.Invoke(this, EventArgs.Empty);
        }
    }
}
