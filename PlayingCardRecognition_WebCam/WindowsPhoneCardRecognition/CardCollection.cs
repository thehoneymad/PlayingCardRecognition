using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsPhoneCardRecognition
{
    internal class CardCollectionExtension
    {
        public static List<Bitmap> ToImageList(this List<Card> List)
        {
            List<Bitmap> list = new List<Bitmap>();

            foreach (Card card in List)
                list.Add(card.Image);

            return list;
        }
    }
}
