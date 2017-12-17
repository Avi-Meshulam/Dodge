using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Enums;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;

namespace Dodge
{
    class Obstacle : Entity
    {
        public Obstacle(int id, Board board, Position position = null, XElement xEntity = null)
            : base(id, board, EntityType.Obstacle, position)
        {
            if (xEntity != null)
            {
                LoadFromXML(xEntity);
            }
        }

        public override XElement GetXML()
        {
            XElement xElement = (
                new XElement("Entity",
                    new XAttribute("Id", Id),
                    new XAttribute("Type", Type),
                    new XAttribute("Row", Position.Row),
                    new XAttribute("Col", Position.Col)
                )
            );

            string imageURI = ((BitmapImage)Image.Source).UriSource.AbsoluteUri;
            if (imageURI != "")
            {
                xElement.Add(new XAttribute("ImageURI", imageURI));
            }

            return xElement;
        }

        private void LoadFromXML(XElement xEntity)
        {
            if (xEntity.Attribute("ImageURI") != null)
            {
                Image.Source = new BitmapImage(new Uri(xEntity.Attribute("ImageURI").Value));
            }
        }
    }
}
