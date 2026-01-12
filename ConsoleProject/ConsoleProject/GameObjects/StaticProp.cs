using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleProject.GameObjects
{
    public class StaticProp : GameObject
    {
        public string EmojiToken { get; set; }     
        public string Tag { get; }                 

        public StaticProp(string emojiToken, Vector pos, string tag = "Base")
        {
            EmojiToken = emojiToken;
            Tag = tag;

            Position = pos;
            Symbol = ' '; 
        }
    }
}
