using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VtuberBot.Bot.Models
{
    public class SendingMessage
    {
        public List<SendingContent> Contents { get; } = new List<SendingContent>();

        public long TargetGroupId { get; set; }

        public override string ToString()
        {
            var result = string.Join("", Contents.Where(v => v.Type == MessageType.Text).Select(v => v.Content));
            return result;
        }

        public static implicit operator SendingMessage(string value)
        {
            var result = new SendingMessage();
            result.Contents.Add(new SendingContent()
            {
                Type = MessageType.Text,
                Content = value
            });
            return result;
        }

        public static implicit operator SendingMessage(byte[] imageBytes)
        {
            var result = new SendingMessage();
            result.Contents.Add(new SendingContent()
            {
                Type = MessageType.Image,
                Data = imageBytes
            });
            return result;
        }

        public static implicit operator SendingMessage(Image image)
        {
            using (var memory = new MemoryStream())
            {
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                image.Save(memory, ImageFormat.Jpeg);
                return memory.GetBuffer();
            }
        }
    }

    public class SendingContent
    {
        public MessageType Type { get; set; }

        public string Content { get; set; }

        public byte[] Data { get; set; }
    }


    public enum MessageType
    {
        Text,
        Image,
    }
}
