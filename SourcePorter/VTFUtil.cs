using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Sledge.Formats.Texture.Vtf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SourcePorter
{
    static public class VTFUtil
    {
        static public void ConvertVTFandAlphaToPNG(string texturepath)
        {
            // Load our VTF image into memory
            var vtffilestream = File.Open(texturepath, FileMode.Open);
            var vtf = new VtfFile(vtffilestream);

            // Iterate through all mipmaps and find the biggest one (the only one we care about)
            int largestdatasize = 0;
            VtfImage biggestVTFimage = null;
            foreach(var mipmap in vtf.Images)
            {
                if(mipmap.Data.Length > largestdatasize)
                {
                    largestdatasize = mipmap.Data.Length;
                    biggestVTFimage = mipmap;
                }
            }

            // Grab the raw 32-bit BGRA8888 data from the image
            var image = biggestVTFimage.GetBgra32Data();

            // Put the raw data into lists, one being a list of the basetexture pixels, and one being the alpha image channel
            var basepixellist = new List<Bgra32>();
            var alphapixellist = new List<Gray8>();
            for (int i = 0; i < image.Length; i += 4)
            {
                var thisbasepixel = new Bgra32(image[i + 2], image[i + 1], image[i]);
                var thisalphapixel = new Gray8(image[i + 3]);
                basepixellist.Add(thisbasepixel);
                alphapixellist.Add(thisalphapixel);
            }

            // Build the images out of our list
            var combinedbaseimage = Image.LoadPixelData<Bgra32>(basepixellist.ToArray(), biggestVTFimage.Width, biggestVTFimage.Height);
            var combinedalphaimage = Image.LoadPixelData<Gray8>(alphapixellist.ToArray(), biggestVTFimage.Width, biggestVTFimage.Height);

            // TODO: Fix the save paths based on file name

            using(var bifs = new FileStream("basetexture.png", FileMode.Create))
            {
                combinedbaseimage.SaveAsPng(bifs);
            }
            using(var aifs = new FileStream("basetexture_alpha.png", FileMode.Create))
            {
                combinedalphaimage.SaveAsPng(aifs);
            }
        }
    }
}
