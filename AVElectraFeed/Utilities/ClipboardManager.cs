using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace AVCheckPrinting.Utilities
{
    public class ClipboardManager
    {
        public void PutItemInClipboard(object pItem)
        {
            // register my custom data format with Windows 
            // or get it if it's already registered 
            DataFormats.Format format = DataFormats.GetFormat(pItem.GetType().FullName);

            // now copy to clipboard 
            IDataObject dataObj = new DataObject();
            dataObj.SetData(format.Name, false, pItem);
            Clipboard.SetDataObject(dataObj, true);

            Clipboard.SetData(pItem.GetType().ToString(), pItem);
        }

        public object RetreiveObjectFromClipboard(string pDataType)
        {
            if (Clipboard.GetDataObject().GetDataPresent(pDataType))
            {
                return Clipboard.GetDataObject().GetData(pDataType);
            }
            else
            {
                return null;
            }
        }

        public bool ContainsObjectInClipboard(string pDataType)
        {
            return Clipboard.GetDataObject().GetDataPresent(pDataType);
        }

        public string[] GetFormats()
        {
            return (string[])Clipboard.GetDataObject().GetFormats();
        }

        public void ClearClipboard()
        {
            Clipboard.Clear();
        }

        public bool IsSerializable(object obj)
        {
            MemoryStream mem = new MemoryStream();
            BinaryFormatter bin = new BinaryFormatter();
            bool result = false;
            string exceptInfo;
            try
            {
                bin.Serialize(mem, obj);
                result = true;
            }
            catch (Exception ex)
            {
                exceptInfo = "The object cannot be serialized." + " Reason: " + ex.ToString();
            }

            return result;
        }

        public byte[] RetreiveBitmapFromClipboard()
        {
            Bitmap image;
            if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Bitmap))
            {
                image = (Bitmap)Clipboard.GetDataObject().GetData(DataFormats.Bitmap);
                var stream = new MemoryStream();
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                var bytes = stream.ToArray();

                return bytes;
            }

            return null;
        }
    }
}