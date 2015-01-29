using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WindowsPhoneCardRecognition
{
    public class CardRecognizer
    {
        private const int CardWidth = 200;  //Card width for scaling
        private const int CardHeight = 300; //Card height for scaling

        //Template Images
        private Bitmap j, k, q; //Face Card Character Templates
        private Bitmap clubs, diamonds, spades, hearts; //Suit Templates

        private bool resourceLoaded = false;

        private FiltersSequence commonSeq; //Commonly filter sequence to be used 


        /// <summary>
        /// Constructor
        /// </summary>
        public CardRecognizer()
        {
            //Initialize common filter sequence , this sequence generally will be applied
            commonSeq = new FiltersSequence();
            commonSeq.Add(Grayscale.CommonAlgorithms.BT709);
            commonSeq.Add(new BradleyLocalThresholding());
            commonSeq.Add(new DifferenceEdgeDetector());

            
            //Load Templates From Resources , 
            //Templates will be used for template matching

            LoadResources();
            
            
        }

        private async void LoadResources()
        {
            j = (Bitmap)(await LoadImage("ms-appx:///Templates/J.bmp"));
            k = (Bitmap)(await LoadImage("ms-appx:///Templates/K.bmp"));
            q = (Bitmap)(await LoadImage("ms-appx:///Templates/Q.bmp"));
            clubs = (Bitmap)(await LoadImage("ms-appx:///Templates/Clubs.bmp"));
            diamonds = (Bitmap)(await LoadImage("ms-appx:///Templates/Diamonds.bmp"));
            spades = (Bitmap)(await LoadImage("ms-appx:///Templates/Spades.bmp"));
            hearts = (Bitmap)(await LoadImage("ms-appx:///Templates/Hearts.bmp"));

            resourceLoaded = true;
        }

        private static async Task<WriteableBitmap> LoadImage(string filePath)
        {
            WriteableBitmap writeableBitmap=null;

            StorageFile storageFile =
                 await StorageFile.GetFileFromApplicationUriAsync(new Uri(filePath));
            using (IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);

                writeableBitmap =
                    new WriteableBitmap(bitmapImage.PixelWidth, bitmapImage.PixelHeight);
                fileStream.Seek(0);
                await writeableBitmap.SetSourceAsync(fileStream);

                
            }

            return writeableBitmap;
        }

        /// <summary>
        /// Scans  and returns suit of face cards.
        /// Uses template matching for recognizing suit of card
        /// </summary>
        /// <param name="bmp">Top right part of card Image</param>
        /// <param name="color">Color of card</param>
        /// <returns>Scanned Suit</returns>
        private Suit ScanFaceSuit(Bitmap bmp, char color)
        {
            //Initialize templateMatching class with 0.8 similarity threshold
            ExhaustiveTemplateMatching templateMatching = new ExhaustiveTemplateMatching(0.8f);
            Suit suit = Suit.NOT_RECOGNIZED;

            if (color == 'R') //If card is red then it can be hearts or diamonds
            {
                ExhaustiveTemplateMatching TemplateMatching = new ExhaustiveTemplateMatching(0);

                TemplateMatch[] matching = TemplateMatching.ProcessImage(bmp, hearts);

                if (matching[0].Similarity >= 0.85f)
                {
                    suit = Suit.Hearts;
                    return suit;
                }

                matching = TemplateMatching.ProcessImage(bmp, diamonds);

                if (matching[0].Similarity >= 0.75f)
                {
                    suit = Suit.Diamonds;
                    return suit;
                }


                //if (templateMatching.ProcessImage(bmp, hearts).Length > 0)  
                //  suit = Suit.Hearts; //Check If template matches for hearts
                //else if (templateMatching.ProcessImage(bmp, diamonds).Length > 0) 
                // suit = Suit.Diamonds; //Check If template matches for diamonds
            }
            else //If its black
            {
                ExhaustiveTemplateMatching TemplateMatching = new ExhaustiveTemplateMatching(0);

                TemplateMatch[] matching = TemplateMatching.ProcessImage(bmp, spades);

                if (matching[0].Similarity >= 0.82f)
                {
                    suit = Suit.Spades;
                    return suit;
                }

                matching = TemplateMatching.ProcessImage(bmp, clubs);

                if (matching[0].Similarity >= 0.75f)
                {
                    suit = Suit.Clubs;
                    return suit;
                }


                //if (templateMatching.ProcessImage(bmp, clubs).Length > 0)
                //suit = Suit.Clubs;//Check If template matches for clubs


            }
            return suit;
        }
        /// <summary>
        /// Scans  and returns suit of card. NOTE : Scans suit of cards that are not face cards
        /// For recognizing suit, analyzes color and size of suit blob 
        /// </summary>
        /// <param name="suitBmp">Suit image to be scanned</param>
        /// <param name="color">Color of card. 'R' means Red, 'B' means black</param>
        /// <returns>Scanned Suit</returns>
        private Suit ScanSuit(Bitmap suitBmp, char color)
        {
            Bitmap temp = commonSeq.Apply(suitBmp);
            ExtractBiggestBlob extractor = new ExtractBiggestBlob(); //Extract biggest blob on card
            temp = extractor.Apply(temp);  //Biggest blob is suit blob so extract it
            Suit suit = Suit.NOT_RECOGNIZED;

            //Determine type of suit according to its color and width
            if (color == 'B')
                suit = temp.Width <= 45 ? Suit.Spades : Suit.Clubs;
            if (color == 'R')
                suit = temp.Height >= 53 ? Suit.Diamonds : Suit.Hearts;


            return suit;
        }
        /// <summary>
        /// Scans rank of face cards
        /// </summary>
        /// <param name="cardImage"></param>
        /// <returns></returns>
        private Rank ScanFaceRank(Bitmap bmp)
        {
            //Initiliaze template matching class with 0.75 threshold

            Rank rank = Rank.Ace;

            ExhaustiveTemplateMatching tempMatch = new ExhaustiveTemplateMatching(0.0f);


            TemplateMatch[] matchings = tempMatch.ProcessImage(bmp, j);
            if (matchings[0].Similarity > 0.85)
            {
                rank = Rank.Queen;
                return rank;
            }


            matchings = tempMatch.ProcessImage(bmp, j);
            if (matchings[0].Similarity > 0.83)
            {
                rank = Rank.Jack;
                return rank;
            }

            matchings = tempMatch.ProcessImage(bmp, k);
            if (matchings[0].Similarity > 0.80)
            {

                rank = Rank.King;
                return rank;
            }

            matchings = tempMatch.ProcessImage(bmp, q);




            return rank;



            /*
                        int jlength, klength;
                        jlength = templateMatchin.ProcessImage(bmp, j).Length;
            
                        if (templateMatchin.ProcessImage(bmp, j).Length > 0) //If Jack template matches
                            rank = Rank.Jack;

                        if (templateMatchin.ProcessImage(bmp, k).Length > 0)//If King template matches
                            rank = Rank.King;
                        if (templateMatchin.ProcessImage(bmp, q).Length > 0)//If Queen template matches
                            rank = Rank.Queen;*/


        }
        /// <summary>
        /// Scans rank of card and returns result.
        /// For recognizing rank, counts suits on image 
        /// </summary>
        /// <param name="cardImage">Card image to be scanned</param>
        /// <returns>Rank of card</returns>
        private Rank ScanRank(Bitmap cardImage)
        {
            Rank rank = Rank.NOT_RECOGNIZED;

            int total = 0;
            Bitmap temp = commonSeq.Apply(cardImage); //Apply filters on image

            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            //Filter blobs whose width or height less than 30 pixels
            blobCounter.MinHeight = blobCounter.MinWidth = 30;
            blobCounter.ProcessImage(temp);

            total = blobCounter.GetObjectsInformation().Length; //Get total number of objects

            rank = (Rank)total; //Convert it to Rank

            return rank;
        }
        /// <summary>
        /// Determines whether card is a face card(Jack,Queen,King) or not.
        /// If card is a face card , then it will have  a big blob whose width will be
        /// larger than half width of card
        /// If card isn't a face card, then width of all blobs will be less than half width of card
        /// </summary>
        /// <param name="bmp">Card of image to be analyzed</param>
        /// <returns>True if its a face card, false if not</returns>
        private bool IsFaceCard(Bitmap bmp)
        {
            Bitmap temp = this.commonSeq.Apply(bmp);
            ExtractBiggestBlob extractor = new ExtractBiggestBlob();
            temp = extractor.Apply(temp); //Extract biggest blob

            if (temp.Width > bmp.Width / 2)  //If width is larger than half width of card
                return true; //Its a face card

            return false;  //It is not a face card
        }
        /// <summary>
        /// Scans dominant color on image and returns it.
        /// Crops rank part on image and analyzes suit part on image
        /// </summary>
        /// <param name="bmp">Bitmap to be scanned</param>
        /// <returns>Returns 'B' for black , 'R' for red</returns>
        private char ScanColor(Bitmap bmp)
        {
            //System.Diagnostics.Debug.Flush();
            //System.Diagnostics.Debug.Print("I'm here");
            char color = 'B';
            //Crop rank part


            Crop crop = new Crop(new Rectangle(0, bmp.Height / 2, bmp.Width, bmp.Height / 2));
            bmp = crop.Apply(bmp);
            Bitmap temp = commonSeq.Apply(bmp); //Apply filters

            //Find suit blob on image
            BlobCounter counter = new BlobCounter();
            counter.ProcessImage(temp);
            Blob[] blobs = counter.GetObjectsInformation();

            if (blobs.Length > 0) //If blobs found
            {
                Blob max = blobs[0];
                //Find blob whose size is biggest 
                foreach (Blob blob in blobs)
                {
                    if (blob.Rectangle.Height > max.Rectangle.Height)
                        max = blob;
                    else if (blob.Rectangle.Height == max.Rectangle.Height)
                        max = blob.Rectangle.Width > max.Rectangle.Width ? blob : max;
                }
                QuadrilateralTransformation trans = new QuadrilateralTransformation();
                trans.SourceQuadrilateral = PointsCloud.FindQuadrilateralCorners(counter.GetBlobsEdgePoints(max));
                bmp = trans.Apply(bmp); //Extract suit
            }
            //Lock Bits for processing

            //int bitsPerPixel = ((int)bmp.PixelFormat & 0xff00) >> 8;
            //int bytesPerPixel = (bitsPerPixel + 7) / 8;
            //int stride = 4 * ((bmp.Width * bytesPerPixel + 3) / 4);


           // BitmapData imageData=new BitmapData(bmp.Width, bmp.Height, bmp. )
          // BitmapData imageData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),ImageLockMode.ReadOnly, bmp.PixelFormat);
            int totalRed = 0;
            int totalBlack = 0;

            WriteableBitmap wbmp = (WriteableBitmap)bmp;

            wbmp.ForEach((x, y, pcolor) =>
            {
                int r=Convert.ToInt32(pcolor.R);
                int g=Convert.ToInt32(pcolor.G);
                int b=Convert.ToInt32(pcolor.B);

                if (r > g + b)
                    totalRed++;
                else if (r <= g + b && (r < 50 && g < 50))
                    totalBlack++;

                return pcolor;

            });

            

            if (totalRed > totalBlack) //If red is dominant
                color = 'R'; //Set color as Red

            return color;
        }
        /// <summary>
        /// Returns point who has minimum x and y
        /// </summary>
        /// <param name="points">Points to be searched</param>
        /// <returns>Returns point who has minimum x and y</returns>
        public static AForge.Point GetStringPoint(AForge.Point[] points)
        {
            AForge.Point[] tempArr = new AForge.Point[points.Length];
            Array.Copy(points, tempArr, points.Length);
            Array.Sort(tempArr, new PointComparer());

            return tempArr[0].X < tempArr[1].X ? tempArr[0] : tempArr[1];
        }
        /// <summary>
        /// Detects and recognizes cards from source image
        /// </summary>
        /// <param name="source">Source image to be scanned</param>
        /// <returns>Recognized Cards</returns>
        public List<Card> Recognize(Bitmap source)
        {
            List<Card> collection = new List<Card>();
            
            Bitmap temp = source.Clone(source.PixelFormat) as Bitmap; //Clone image to keep original image

            FiltersSequence seq = new FiltersSequence();
            seq.Add(Grayscale.CommonAlgorithms.BT709);  //First add  grayScaling filter
            seq.Add(new OtsuThreshold()); //Then add binarization(thresholding) filter
            temp = seq.Apply(source); // Apply filters on source image

            //Extract blobs from image whose size width and height larger than 150
            BlobCounter extractor = new BlobCounter();
            extractor.FilterBlobs = true;
            extractor.MinWidth = extractor.MinHeight = 150;
            extractor.MaxWidth = extractor.MaxHeight = 350;
            extractor.ProcessImage(temp);

            //Will be used transform(extract) cards on source image 
            QuadrilateralTransformation quadTransformer = new QuadrilateralTransformation();

            //Will be used resize(scaling) cards 
            ResizeBilinear resizer = new ResizeBilinear(CardWidth, CardHeight);

            foreach (Blob blob in extractor.GetObjectsInformation())
            {
                //Get Edge points of card
                List<IntPoint> edgePoints = extractor.GetBlobsEdgePoints(blob);
                //Calculate/Find corners of card on source image from edge points
                List<IntPoint> corners = PointsCloud.FindQuadrilateralCorners(edgePoints);

                quadTransformer.SourceQuadrilateral = corners; //Set corners for transforming card 
                quadTransformer.AutomaticSizeCalculaton = true;

                Bitmap cardImg = quadTransformer.Apply(source); //Extract(transform) card image

                if (cardImg.Width > cardImg.Height) //If card is positioned horizontally
                {
                    WriteableBitmap wbmp=(WriteableBitmap)cardImg;
                    wbmp = wbmp.Rotate(90);
                    cardImg = (Bitmap)wbmp; //Rotate
                }
                cardImg = resizer.Apply(cardImg); //Normalize card size

                Card card = new Card(cardImg, corners.ToArray()); //Create Card Object
                char color = ScanColor(card.GetTopLeftPart()); //Scan color
                bool faceCard = IsFaceCard(cardImg); //Determine type of card(face or not)

                if (!faceCard)
                {
                    card.Suit = ScanSuit(cardImg, color); //Scan Suit of non-face card
                    card.Rank = ScanRank(cardImg); //Scan Rank of non-face card
                }
                else
                {
                    Bitmap topLeft = card.GetTopLeftPart();

                    seq = null;
                    seq = new FiltersSequence();

                    seq.Add(Grayscale.CommonAlgorithms.BT709);
                    seq.Add(new BradleyLocalThresholding());
                    topLeft = seq.Apply(topLeft);
                    BlobsFiltering bFilter = new BlobsFiltering(5, 5, 150, 150);
                    bFilter.ApplyInPlace(topLeft); //Filter blobs that can not be a suit

                    //topLeft.Save("topleft.bmp", ImageFormat.Bmp);

                    card.Suit = ScanFaceSuit(topLeft, color); //Scan suit of face card
                    card.Rank = ScanFaceRank(topLeft); //Scan rank of face card
                }
                collection.Add(card); //Add card to collection
            }
            return collection;
        }
    }
}
