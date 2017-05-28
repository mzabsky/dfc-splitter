using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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
        public static string GetFileNameByCardNameWithoutSpecialChars(string cardName)
        {
            var regex = new Regex(@"[^A-Za-z0-9-. ]");
            var replace = regex.Replace(cardName.Trim(), "");  
            return "/" + replace + ".jpg";
        }

        public static string GetFileNameByCardName(string cardName)
        {

            return "/" + cardName.Replace('’', '\'') + ".jpg";
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

                var imagePairs = allCards
                .Where(card => !allCards.Any(dayCard => dayCard.Related == card.Name))
                .Select(card => new
                {
                    DayCard = card,
                    NightCard = allCards.SingleOrDefault(nightCard => nightCard.Name == card.Related)
                })
                .Select(pair => new
                {
                    Day = pair.DayCard.Name,
                    DayArt = GetFileNameByCardNameWithoutSpecialChars(pair.DayCard.Name),
                    Night = pair.NightCard?.Name,
                    NightArt = pair.NightCard != null ? GetFileNameByCardName(pair.NightCard.Name) : null
                }).ToList();
                
                /*var imagePairs =
                    (from card in allCards
                    join relatedCard in allCards on card.Related equals relatedCard.Name
                    select new { Day = card.Name, DayArt = card.Set.PicUrl, Night = relatedCard.Name, NightArt = GetFileNameByCardName(relatedCard.Name) }).ToArray();
*/
                sr.Close();

                Console.WriteLine($"Found {imagePairs.Count} cards");

                var dayRect = new Rectangle(0, 0, 375, 523);
                var nightRect = new Rectangle(752 - 375, 0, 375, 523);

                foreach (var imagePair in imagePairs)
                {
                    Console.WriteLine($"Processing {imagePair.Day} // {imagePair.Night}");
                    
                    var newDayArt = GetFileNameByCardName(imagePair.Day);

                    var dayFileName = basePath + imagePair.DayArt;
                    var newDayFileName = basePath + newDayArt;
                    var nightFileName = basePath + imagePair.NightArt;
                    
                    Bitmap sourceImage = Image.FromFile(dayFileName) as Bitmap;

                    // Do not rewrite the images if they have already been rewritten
                    if (imagePair.Night != null && sourceImage.Width == 752)
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

                        File.Delete(dayFileName);
                        dayImage.Save(newDayFileName);
                        nightImage.Save(nightFileName);
                    }
                    else if(dayFileName != newDayFileName)
                    {
                        sourceImage.Dispose();
                        File.Move(dayFileName, newDayFileName);
                    }

                    /*fullXml = fullXml.Replace(
                        $"<name>{imagePair.Day}</name>\r\n <set picURL=\"{imagePair.DayArt}\"",
                        $"<name>{imagePair.Day}</name>\r\n <set");

                    fullXml = fullXml.Replace(
                        $"<name>{imagePair.Night}</name>\r\n <set picURL=\"{imagePair.DayArt}\"",
                        $"<name>{imagePair.Night}</name>\r\n <set");*/
                }

                fullXml = Regex.Replace(fullXml, $" <set[^>]*>", "<set>");

                fullXml = fullXml.Replace('’', '\'');

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
