using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
I've added this class so I can make my own excpetions to handle.
With this, the "Gotta catch 'em all" problem should disappear.
*/
namespace UnofficialSteamAuthenticator.Lib
{
    public class AddFileException : Exception
    {
        public AddFileException()
        {
        }

        public AddFileException(string message) : base(message)
        {
        }

        public AddFileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
