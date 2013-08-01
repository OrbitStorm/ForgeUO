using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenUO.Core.Patterns;
using OpenUO.Ultima;
using OpenUO.Ultima.Windows.Forms;

namespace Server
{
    public partial class OpenUOSDK
    {
        //! You should point this to a directory containing a COPY of your client
        //! files if you are having conflict issues with AdaptUO using the same files
        //! that your client is using.
        //!+ Example: private static string _ClientData = @"C:\Server Files";
        private static string _ClientData = @"C:\Server";

        private static AnimationDataFactory _animationDataFactory;
        private static AnimationFactory _animationFactory;
        private static ArtworkFactory _artworkFactory;
        private static ASCIIFontFactory _asciiFontFactory;
        private static ClilocFactory _clilocFactory;
        private static GumpFactory _gumpFactory;
        private static SkillsFactory _skillsFactory;
        private static SoundFactory _soundFactory;
        private static TexmapFactory _texmapFactory;
        private static UnicodeFontFactory _unicodeFontFactory;

        public static AnimationDataFactory AnimationDataFactory
        {
            get
            {
                return _animationDataFactory;
            }
            set
            {
                _animationDataFactory = value;
            }
        }

        public static AnimationFactory AnimationFactory
        {
            get
            {
                return _animationFactory;
            }
            set
            {
                _animationFactory = value;
            }
        }

        public static ArtworkFactory ArtFactory
        {
            get
            {
                return _artworkFactory;
            }
            set
            {
                _artworkFactory = value;
            }
        }

        public static ASCIIFontFactory AsciiFontFactory
        {
            get
            {
                return _asciiFontFactory;
            }
            set
            {
                _asciiFontFactory = value;
            }
        }

        public static ClilocFactory ClilocFactory
        {
            get
            {
                return _clilocFactory;
            }
            set
            {
                _clilocFactory = value;
            }
        }

        public static GumpFactory GumpFactory
        {
            get
            {
                return _gumpFactory;
            }
            set
            {
                _gumpFactory = value;
            }
        }

        public static SkillsFactory SkillsFactory
        {
            get
            {
                return _skillsFactory;
            }
            set
            {
                _skillsFactory = value;
            }
        }

        public static SoundFactory SoundFactory
        {
            get
            {
                return _soundFactory;
            }
            set
            {
                _soundFactory = value;
            }
        }

        public static TexmapFactory TexmapFactory
        {
            get
            {
                return _texmapFactory;
            }
            set
            {
                _texmapFactory = value;
            }
        }

        public static UnicodeFontFactory UnicodeFontFactory
        {
            get
            {
                return _unicodeFontFactory;
            }
            set
            {
                _unicodeFontFactory = value;
            }
        }

        public OpenUOSDK(string path = "")
        {
            if (_ClientData == null && (path != "" || path != null))
            {
                if (Directory.Exists(path))
                    _ClientData = path;
            }

            IoCContainer container = new IoCContainer();
            container.RegisterModule<UltimaSDKCoreModule>();
            container.RegisterModule<UltimaSDKBitmapModule>();

            InstallLocation location = (_ClientData == null ? InstallationLocator.Locate().FirstOrDefault() : (InstallLocation)_ClientData);

            if (!Directory.Exists(location.ToString()))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("OpenUO Error: Client files not found.");
                Utility.PopColor();
            }

            _animationDataFactory = new AnimationDataFactory(location, container);
            _animationFactory = new AnimationFactory(location, container);
            _artworkFactory = new ArtworkFactory(location, container);
            _asciiFontFactory = new ASCIIFontFactory(location, container);
            _clilocFactory = new ClilocFactory(location, container);
            _gumpFactory = new GumpFactory(location, container);
            _skillsFactory = new SkillsFactory(location, container);
            _soundFactory = new SoundFactory(location, container);
            _texmapFactory = new TexmapFactory(location, container);
            _unicodeFontFactory = new UnicodeFontFactory(location, container);
        }
    }
}
