using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetingoLightUI
{
    public static class Settings
    {
        public const string SCREEN_INDEX_OFF = "255@255";

        public static string SCREEN_INDEX_RED
        {
            get
            {
                return _SCREEN_INDEX_RED;
            }
            set
            {
                if (value != SCREEN_INDEX_OFF)
                    if (SCREEN_INDEX_YELLOW.Equals(value) || SCREEN_INDEX_BLUE.Equals(value))
                        value = SCREEN_INDEX_OFF;

                _SCREEN_INDEX_RED = value;
                Properties.Settings.Default["SCREEN_INDEX_0"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string SCREEN_INDEX_YELLOW
        {
            get
            {
                return _SCREEN_INDEX_YELLOW;
            }
            set
            {
                if (value != SCREEN_INDEX_OFF)
                    if (SCREEN_INDEX_RED.Equals(value) || SCREEN_INDEX_BLUE.Equals(value))
                        value = SCREEN_INDEX_OFF;

                _SCREEN_INDEX_YELLOW = value;
                Properties.Settings.Default["SCREEN_INDEX_1"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string SCREEN_INDEX_BLUE
        {
            get
            {
                return _SCREEN_INDEX_BLUE;
            }
            set
            {
                if (value != SCREEN_INDEX_OFF)
                    if (SCREEN_INDEX_YELLOW.Equals(value) || SCREEN_INDEX_RED.Equals(value))
                        value = SCREEN_INDEX_OFF;

                _SCREEN_INDEX_BLUE = value;
                Properties.Settings.Default["SCREEN_INDEX_2"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte SCREEN_BRIGHTNESS
        {
            get
            {
                return _SCREEN_BRIGHTNESS;
            }
            set
            {
                _SCREEN_BRIGHTNESS = value;
                Properties.Settings.Default["SCREEN_BRIGHTNESS"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte MODE
        {
            //number of monitors being tracked, Single/dual/triple monitor mode 
            get
            {
                return _MODE;
            }
            set
            {
                if (value > 0 && value < 4)
                {
                    _MODE = value;
                    Properties.Settings.Default["MODE"] = value;
                    Properties.Settings.Default.Save();
                }
            }
        }  
        public static bool STARTUP_WITH_WINDOWS
        {
            get
            {
                return _STARTUP_WITH_WINDOWS;
            }
            set
            {
                _STARTUP_WITH_WINDOWS = value;
                Properties.Settings.Default["STARTUP_WITH_WINDOWS"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte LED_COUNT_VER_RED
        {
            get
            {
                return _LED_COUNT_VER_RED;
            }
            set
            {
                _LED_COUNT_VER_RED = value;
                Properties.Settings.Default["LED_COUNT_VER_RED"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte LED_COUNT_VER_YELLOW
        {
            get
            {
                return _LED_COUNT_VER_YELLOW;
            }
            set
            {
                _LED_COUNT_VER_YELLOW = value;
                Properties.Settings.Default["LED_COUNT_VER_YELLOW"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte LED_COUNT_VER_BLUE
        {
            get
            {
                return _LED_COUNT_VER_BLUE;
            }
            set
            {
                _LED_COUNT_VER_BLUE = value;
                Properties.Settings.Default["LED_COUNT_VER_BLUE"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte LED_COUNT_HOR_RED
        {
            get
            {
                return _LED_COUNT_HOR_RED;
            }
            set
            {
                _LED_COUNT_HOR_RED = value;
                Properties.Settings.Default["LED_COUNT_HOR_RED"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte LED_COUNT_HOR_YELLOW
        {
            get
            {
                return _LED_COUNT_HOR_YELLOW;
            }
            set
            {
                _LED_COUNT_HOR_YELLOW = value;
                Properties.Settings.Default["LED_COUNT_HOR_YELLOW"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static byte LED_COUNT_HOR_BLUE
        {
            get
            {
                return _LED_COUNT_HOR_BLUE;
            }
            set
            {
                _LED_COUNT_HOR_BLUE = value;
                Properties.Settings.Default["LED_COUNT_HOR_BLUE"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static short LED_TARGET_TIME_ON
        {
            get
            {
                return _LED_TARGET_TIME_ON;
            }
            set
            {
                _LED_TARGET_TIME_ON = value;
                Properties.Settings.Default["LED_TARGET_TIME_ON"] = value;
                Properties.Settings.Default.Save();
            }
        }
        public static short LED_TARGET_TIME_OFF
        {
            get
            {
                return _LED_TARGET_TIME_OFF;
            }
            set
            {
                _LED_TARGET_TIME_OFF = value;
                Properties.Settings.Default["LED_TARGET_TIME_OFF"] = value;
                Properties.Settings.Default.Save();
            }
        }


        private static string _SCREEN_INDEX_RED;
        private static string _SCREEN_INDEX_YELLOW;
        private static string _SCREEN_INDEX_BLUE;
        private static short _LED_TARGET_TIME_ON;
        private static short _LED_TARGET_TIME_OFF;
        private static byte _SCREEN_BRIGHTNESS;
        private static byte _MODE;
        private static bool _STARTUP_WITH_WINDOWS;
        private static byte _LED_COUNT_VER_RED;
        private static byte _LED_COUNT_VER_YELLOW;
        private static byte _LED_COUNT_VER_BLUE;
        private static byte _LED_COUNT_HOR_RED;
        private static byte _LED_COUNT_HOR_YELLOW;
        private static byte _LED_COUNT_HOR_BLUE;

        public static void Init()
        {
            _SCREEN_INDEX_RED = (string)Properties.Settings.Default["SCREEN_INDEX_0"];
            _SCREEN_INDEX_YELLOW = (string)Properties.Settings.Default["SCREEN_INDEX_1"];
            _SCREEN_INDEX_BLUE = (string)Properties.Settings.Default["SCREEN_INDEX_2"];
            _SCREEN_BRIGHTNESS = (byte)Properties.Settings.Default["SCREEN_BRIGHTNESS"];
            _MODE = (byte)Properties.Settings.Default["MODE"];
            _STARTUP_WITH_WINDOWS = (bool)Properties.Settings.Default["STARTUP_WITH_WINDOWS"];
            _LED_COUNT_VER_RED = (byte)Properties.Settings.Default["LED_COUNT_VER_RED"];
            _LED_COUNT_VER_YELLOW = (byte)Properties.Settings.Default["LED_COUNT_VER_YELLOW"];
            _LED_COUNT_VER_BLUE = (byte)Properties.Settings.Default["LED_COUNT_VER_BLUE"];
            _LED_COUNT_HOR_RED = (byte)Properties.Settings.Default["LED_COUNT_HOR_RED"];
            _LED_COUNT_HOR_YELLOW = (byte)Properties.Settings.Default["LED_COUNT_HOR_YELLOW"];
            _LED_COUNT_HOR_BLUE = (byte)Properties.Settings.Default["LED_COUNT_HOR_BLUE"];
            _LED_TARGET_TIME_ON = (short)Properties.Settings.Default["LED_TARGET_TIME_ON"];
            _LED_TARGET_TIME_OFF = (short)Properties.Settings.Default["LED_TARGET_TIME_OFF"];
        }
    }
}
