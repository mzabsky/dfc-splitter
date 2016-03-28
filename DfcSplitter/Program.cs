using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace DfcSplitter
{
    [Serializable]
    public class Card
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("related")]
        public string Related { get; set; }

        [XmlElement("set")]
        public Set Set { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    public class Set
    {
        [XmlAttribute("picURL")]
        public string PicUrl { get; set; }

        public override string ToString()
        {
            return PicUrl;
        }
    }

    [Serializable]
    [XmlRoot("cockatrice_carddatabase")]
    public class Database
    {
        [XmlArray("cards")]
        [XmlArrayItem("card", typeof(Card))]
        public Card[] Cards { get; set; }
    }

    public class Program
    {
        public static string GetFileNameByCardName(string cardName)
        {
            var regex = new Regex(@"[^A-Za-z0-9-. ]");
            var replace = regex.Replace(cardName.Trim(), "");  
            return "/" + replace + ".full.jpg";
        }

        public static void Main(string[] args)
        {
            try
            {
                var cockatriceFileName = args[0];
                var basePath = Path.GetDirectoryName(cockatriceFileName);

                var fullXml = File.ReadAllText(cockatriceFileName);

                Console.WriteLine("Parsing cockatrice XML");

                var sr = new StringReader(fullXml);
                XmlSerializer serializer = new XmlSerializer(typeof(Database));
                Database database = (Database)serializer.Deserialize(sr);

                var allCards = database.Cards;

                Console.WriteLine($"Found {allCards.Length} cards");

                var imagePairs =
                    (from card in allCards
                    join relatedCard in allCards on card.Related equals relatedCard.Name
                    select new { Day = card.Name, DayArt = card.Set.PicUrl, Night = relatedCard.Name, NightArt = GetFileNameByCardName(relatedCard.Name) }).ToArray();

                sr.Close();

                Console.WriteLine($"Matched {imagePairs.Length} double faced cards");

                var dayRect = new Rectangle(0, 0, 375, 523);
                var nightRect = new Rectangle(752 - 375, 0, 375, 523);

                foreach (var imagePair in imagePairs)
                {
                    Console.WriteLine($"Processing {imagePair.Day} // {imagePair.Night}");

                    var dayFileName = basePath + imagePair.DayArt;
                    var nightFileName = basePath + imagePair.NightArt;
                    
                    Bitmap sourceImage = Image.FromFile(dayFileName) as Bitmap;

                    // Do not rewrite the images if they have already been rewritten
                    if (sourceImage.Width == 752)
                    {
                        Bitmap dayImage = new Bitmap(dayRect.Width, dayRect.Height);
                        Bitmap nightImage = new Bitmap(dayRect.Width, dayRect.Height);

                        using (Graphics g = Graphics.FromImage(dayImage))
                        {
                            g.DrawImage(sourceImage, dayRect, dayRect, GraphicsUnit.Pixel);
                        }

                        using (Graphics g = Graphics.FromImage(nightImage))
                        {
                            g.DrawImage(sourceImage, dayRect, nightRect, GraphicsUnit.Pixel);
                        }

                        sourceImage.Dispose();

                        dayImage.Save(dayFileName);
                        nightImage.Save(nightFileName);
                    }
                    
                    fullXml = fullXml.Replace(
                        $"<name>{imagePair.Night}</name>\r\n <set picURL=\"{imagePair.DayArt}\"",
                        $"<name>{imagePair.Night}</name>\r\n <set picURL=\"{imagePair.NightArt}\"");
                }

                Console.WriteLine("Writing modified XML");

                File.WriteAllText(cockatriceFileName, fullXml);

                Console.WriteLine("Done!");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Console.WriteLine(e);
            }
        }
    }
}
